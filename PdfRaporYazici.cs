using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;

namespace RORAMuzikMerkezi
{
    // Aylık özet raporu PDF olarak üretir.
    //
    // Dış kütüphane kullanılmaz. Windows'un yerleşik "Microsoft Print to PDF"
    // yazıcısına System.Drawing.Printing ile çizim yapılır. Böylece proje
    // bağımlılıksız kalır ve dağıtım tek klasör kopyalamaktan ibaret olmaya
    // devam eder. Bunun bilinen sınırı, bu yazıcının kaldırıldığı makinelerde
    // PDF üretilememesidir; o durumda çağıran tarafa açık bir hata bildirilir.
    public class PdfRaporYazici
    {
        public const string YaziciAdi = "Microsoft Print to PDF";

        private enum OgeTuru { CalgiBasligi, TabloBasligi, OgrenciSatiri, GrupToplami, Bosluk, GenelToplam }

        private class Oge
        {
            public OgeTuru Tur;
            public string[] Hucreler;
            public string Metin;
        }

        // Sütun genişlikleri, 1/100 inç. Toplam 707 = A4 genişliği eksi kenar boşlukları.
        private static readonly int[] SutunGenislikleri = { 170, 100, 45, 45, 45, 45, 60, 90, 107 };
        private static readonly string[] SutunBasliklari = { "Ad Soyad", "Telefon", "H1", "H2", "H3", "H4", "Toplam", "Ödeme", "Tutar" };

        private const int SatirYuksekligi = 22;
        private const int KenarBosluk = 60;

        private readonly int yil;
        private readonly int ay;
        private readonly string ayAdi;
        private readonly List<Oge> ogeler = new List<Oge>();
        private int siradaki;
        private int sayfaNo;

        private static CultureInfo TrKultur { get { return new CultureInfo("tr-TR"); } }

        public PdfRaporYazici(int yil, int ay)
        {
            this.yil = yil;
            this.ay = ay;
            this.ayAdi = new DateTime(yil, ay, 1).ToString("MMMM yyyy", TrKultur);
            OgeleriHazirla();
        }

        public static bool YaziciKullanilabilirMi()
        {
            foreach (string ad in PrinterSettings.InstalledPrinters)
            {
                if (string.Equals(ad, YaziciAdi, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        // Raporun tüm satırlarını önceden düzleştirir. Sayfalama, çizim
        // sırasında bu listeden sığdığı kadarını almakla yapılır.
        private void OgeleriHazirla()
        {
            var calgilar = VeriYoneticisi.Veriler.Ogrenciler
                .Select(o => o.Calgı)
                .Distinct()
                .OrderBy(c => c, StringComparer.CurrentCulture)
                .ToList();

            foreach (var calgi in calgilar)
            {
                var ogrenciler = VeriYoneticisi.CalginaGoreOgrenciler(calgi);
                if (ogrenciler.Count == 0) continue;

                ogeler.Add(new Oge { Tur = OgeTuru.CalgiBasligi, Metin = calgi.ToUpper(TrKultur) });
                ogeler.Add(new Oge { Tur = OgeTuru.TabloBasligi });

                foreach (var ogr in ogrenciler)
                {
                    bool h1 = VeriYoneticisi.HaftaDersAldiMi(ogr.Id, yil, ay, 1);
                    bool h2 = VeriYoneticisi.HaftaDersAldiMi(ogr.Id, yil, ay, 2);
                    bool h3 = VeriYoneticisi.HaftaDersAldiMi(ogr.Id, yil, ay, 3);
                    bool h4 = VeriYoneticisi.HaftaDersAldiMi(ogr.Id, yil, ay, 4);
                    int toplam = (h1 ? 1 : 0) + (h2 ? 1 : 0) + (h3 ? 1 : 0) + (h4 ? 1 : 0);
                    bool odendi = VeriYoneticisi.OdemeYapildiMi(ogr.Id, yil, ay);
                    decimal tutar = VeriYoneticisi.OdemeTutari(ogr.Id, yil, ay);

                    ogeler.Add(new Oge
                    {
                        Tur = OgeTuru.OgrenciSatiri,
                        Hucreler = new[]
                        {
                            ogr.TamAd,
                            ogr.Telefon ?? string.Empty,
                            h1 ? "✓" : "–",
                            h2 ? "✓" : "–",
                            h3 ? "✓" : "–",
                            h4 ? "✓" : "–",
                            toplam.ToString(),
                            odendi ? "Ödendi" : "Ödenmedi",
                            odendi ? VeriYoneticisi.TutarYazi(tutar) : string.Empty
                        }
                    });
                }

                int odeyen = ogrenciler.Count(o => VeriYoneticisi.OdemeYapildiMi(o.Id, yil, ay));
                decimal grupTahsilat = ogrenciler.Sum(o => VeriYoneticisi.OdemeTutari(o.Id, yil, ay));
                ogeler.Add(new Oge
                {
                    Tur = OgeTuru.GrupToplami,
                    Metin = $"{ogrenciler.Count} öğrenci  ·  Ödeme yapan: {odeyen}  ·  Yapmayan: {ogrenciler.Count - odeyen}  ·  Tahsilat: {VeriYoneticisi.TutarYazi(grupTahsilat)}"
                });
                ogeler.Add(new Oge { Tur = OgeTuru.Bosluk });
            }

            int genelToplam = VeriYoneticisi.Veriler.Ogrenciler.Count;
            int genelOdeyen = VeriYoneticisi.Veriler.Ogrenciler.Count(o => VeriYoneticisi.OdemeYapildiMi(o.Id, yil, ay));
            ogeler.Add(new Oge
            {
                Tur = OgeTuru.GenelToplam,
                Metin = $"GENEL TOPLAM: {genelToplam} öğrenci   ·   Ödeme yapan: {genelOdeyen}   ·   Yapmayan: {genelToplam - genelOdeyen}   ·   Toplam tahsilat: {VeriYoneticisi.TutarYazi(VeriYoneticisi.AylikToplamGelir(yil, ay))}"
            });
        }

        // Hata durumunda istisna yukarı taşınır; çağıran taraf kullanıcıyı bilgilendirir.
        public void Yazdir(string hedefYol)
        {
            if (!YaziciKullanilabilirMi())
                throw new InvalidOperationException($"\"{YaziciAdi}\" yazıcısı bu bilgisayarda bulunamadı. PDF üretimi bu yazıcıya bağlıdır.");

            if (File.Exists(hedefYol)) File.Delete(hedefYol);

            siradaki = 0;
            sayfaNo = 0;

            using (var belge = new PrintDocument())
            {
                belge.DocumentName = $"RORA Özet Rapor {ayAdi}";
                belge.PrinterSettings.PrinterName = YaziciAdi;
                belge.PrinterSettings.PrintToFile = true;
                belge.PrinterSettings.PrintFileName = hedefYol;
                belge.DefaultPageSettings.Margins = new Margins(KenarBosluk, KenarBosluk, KenarBosluk, KenarBosluk);

                // Sütun genişlikleri A4'e göre hesaplandı. Yazıcının varsayılanı
                // Letter ise son sütun taşacağı için kağıdı açıkça seçiyoruz.
                foreach (PaperSize boyut in belge.PrinterSettings.PaperSizes)
                {
                    if (boyut.Kind == PaperKind.A4) { belge.DefaultPageSettings.PaperSize = boyut; break; }
                }

                belge.PrintPage += SayfaCiz;
                belge.Print();
            }

            if (!TamamlanmayiBekle(hedefYol, TimeSpan.FromSeconds(60)))
                throw new IOException("PDF dosyası oluşturulamadı. Yazıcı çıktıyı yazamamış olabilir.");
        }

        // Yazdırma kuyruğu dosyayı anında oluşturur ama içeriğini arka planda
        // yazar. Bu yüzden yalnızca dosyanın varlığına bakmak yetmez: boyutu
        // oluşana ve kuyruk dosyayı bırakana kadar beklenir. Aksi hâlde çağıran
        // taraf henüz boş olan bir dosyayı açmaya çalışır.
        private static bool TamamlanmayiBekle(string yol, TimeSpan sure)
        {
            DateTime bitis = DateTime.UtcNow + sure;
            while (DateTime.UtcNow < bitis)
            {
                try
                {
                    var bilgi = new FileInfo(yol);
                    if (bilgi.Exists && bilgi.Length > 0)
                    {
                        // Tek başına açabiliyorsak kuyruk işini bitirmiş demektir.
                        using (File.Open(yol, FileMode.Open, FileAccess.Read, FileShare.None)) { }
                        return true;
                    }
                }
                catch (IOException) { }
                System.Threading.Thread.Sleep(150);
            }
            return false;
        }

        private void SayfaCiz(object gonderen, PrintPageEventArgs e)
        {
            sayfaNo++;
            var g = e.Graphics;
            int sol = e.MarginBounds.Left;
            int y = e.MarginBounds.Top;
            int altSinir = e.MarginBounds.Bottom - 40;

            using (var fBaslik = new Font("Segoe UI", 16, FontStyle.Bold))
            using (var fAltBaslik = new Font("Segoe UI", 10))
            using (var fCalgi = new Font("Segoe UI", 11, FontStyle.Bold))
            using (var fTabloBaslik = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (var fSatir = new Font("Segoe UI", 9))
            using (var fOzet = new Font("Segoe UI", 9, FontStyle.Italic))
            using (var fGenel = new Font("Segoe UI", 10, FontStyle.Bold))
            using (var fDipnot = new Font("Segoe UI", 8))
            using (var koyuMavi = new SolidBrush(Color.FromArgb(46, 74, 143)))
            using (var siyah = new SolidBrush(Color.FromArgb(30, 30, 30)))
            using (var gri = new SolidBrush(Color.FromArgb(110, 110, 110)))
            using (var baslikArka = new SolidBrush(Color.FromArgb(232, 236, 247)))
            using (var cizgi = new Pen(Color.FromArgb(184, 194, 220)))
            {
                if (sayfaNo == 1)
                {
                    g.DrawString("RORA SANAT MERKEZİ", fBaslik, koyuMavi, sol, y);
                    y += 32;
                    g.DrawString($"{ayAdi.ToUpper(TrKultur)} AYI ÖZET RAPORU", fAltBaslik, siyah, sol, y);
                    y += 18;
                    g.DrawString($"Rapor tarihi: {DateTime.Now.ToString("dd.MM.yyyy HH:mm", TrKultur)}", fAltBaslik, gri, sol, y);
                    y += 26;
                    g.DrawLine(cizgi, sol, y, e.MarginBounds.Right, y);
                    y += 14;
                }
                else
                {
                    g.DrawString($"RORA SANAT MERKEZİ — {ayAdi} özet raporu (devam)", fAltBaslik, gri, sol, y);
                    y += 20;
                    g.DrawLine(cizgi, sol, y, e.MarginBounds.Right, y);
                    y += 12;
                }

                while (siradaki < ogeler.Count)
                {
                    var oge = ogeler[siradaki];
                    int gerekenYer = oge.Tur == OgeTuru.CalgiBasligi ? SatirYuksekligi * 2 : SatirYuksekligi;
                    if (y + gerekenYer > altSinir) break;

                    switch (oge.Tur)
                    {
                        case OgeTuru.CalgiBasligi:
                            y += 4;
                            g.DrawString(oge.Metin, fCalgi, koyuMavi, sol, y);
                            y += 22;
                            break;

                        case OgeTuru.TabloBasligi:
                            g.FillRectangle(baslikArka, sol, y, e.MarginBounds.Width, SatirYuksekligi);
                            CizSatir(g, SutunBasliklari, fTabloBaslik, koyuMavi, sol, y, cizgi, true);
                            y += SatirYuksekligi;
                            break;

                        case OgeTuru.OgrenciSatiri:
                            CizSatir(g, oge.Hucreler, fSatir, siyah, sol, y, cizgi, false);
                            y += SatirYuksekligi;
                            break;

                        case OgeTuru.GrupToplami:
                            y += 2;
                            g.DrawString(oge.Metin, fOzet, gri, sol + 4, y);
                            y += 20;
                            break;

                        case OgeTuru.Bosluk:
                            y += 14;
                            break;

                        case OgeTuru.GenelToplam:
                            y += 6;
                            g.DrawLine(cizgi, sol, y, e.MarginBounds.Right, y);
                            y += 8;
                            g.DrawString(oge.Metin, fGenel, koyuMavi, sol, y);
                            y += 22;
                            break;
                    }

                    siradaki++;
                }

                string dipnot = $"Sayfa {sayfaNo}";
                var olcu = g.MeasureString(dipnot, fDipnot);
                g.DrawString(dipnot, fDipnot, gri,
                    e.MarginBounds.Left + (e.MarginBounds.Width - olcu.Width) / 2,
                    e.MarginBounds.Bottom + 8);
            }

            e.HasMorePages = siradaki < ogeler.Count;
        }

        private static void CizSatir(Graphics g, string[] hucreler, Font yazi, Brush firca,
                                     int sol, int y, Pen cizgi, bool baslikMi)
        {
            int x = sol;
            for (int i = 0; i < SutunGenislikleri.Length && i < hucreler.Length; i++)
            {
                var alan = new RectangleF(x + 3, y + 4, SutunGenislikleri[i] - 6, SatirYuksekligi - 6);
                var bicim = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };

                // Ad ve telefon sola, sayısal alanlar ortaya, tutar sağa
                if (i == 0 || i == 1) bicim.Alignment = StringAlignment.Near;
                else if (i == 8) bicim.Alignment = StringAlignment.Far;
                else bicim.Alignment = StringAlignment.Center;

                g.DrawString(hucreler[i] ?? string.Empty, yazi, firca, alan, bicim);
                bicim.Dispose();
                x += SutunGenislikleri[i];
            }

            if (!baslikMi)
                g.DrawLine(cizgi, sol, y + SatirYuksekligi, x, y + SatirYuksekligi);
        }
    }
}
