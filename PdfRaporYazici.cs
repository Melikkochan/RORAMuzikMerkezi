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
            public bool Zebra;     // satır zemininin renklendirileceğini belirtir
            public bool Odendi;    // ödeme sütununun rengini belirler
        }

        // Sütun genişlikleri, 1/100 inç. Toplam 707 = A4 genişliği eksi kenar boşlukları.
        private static readonly int[] SutunGenislikleri = { 170, 100, 45, 45, 45, 45, 60, 90, 107 };
        private static readonly string[] SutunBasliklari = { "Ad Soyad", "Telefon", "H1", "H2", "H3", "H4", "Toplam", "Ödeme", "Tutar" };

        private const int SatirYuksekligi = 22;
        private const int KenarBosluk = 60;

        // Uygulamanın kendi paletiyle uyumlu: lacivert SplashForm'un zemininden,
        // altın ise logodaki madalyon tonundan alındı.
        private static readonly Color Lacivert = Color.FromArgb(35, 45, 100);
        private static readonly Color Altin = Color.FromArgb(190, 152, 106);
        private static readonly Color Metin = Color.FromArgb(30, 30, 30);
        private static readonly Color SolukMetin = Color.FromArgb(110, 110, 110);
        private static readonly Color ZebraZemin = Color.FromArgb(245, 247, 252);
        private static readonly Color BolumZemin = Color.FromArgb(232, 236, 247);
        private static readonly Color CizgiRengi = Color.FromArgb(205, 212, 230);
        private static readonly Color OdendiRengi = Color.FromArgb(30, 130, 75);
        private static readonly Color OdenmediRengi = Color.FromArgb(180, 60, 60);

        private static int ToplamGenislik { get { return SutunGenislikleri.Sum(); } }

        private readonly int yil;
        private readonly int ay;
        private readonly string ayAdi;
        private readonly List<Oge> ogeler = new List<Oge>();
        private int siradaki;
        private int sayfaNo;
        private Image logo;
        private Rectangle logoAlani;   // logonun boş kenarları kırpılmış hâli

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

                int sira = 0;
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
                        Zebra = (sira++ % 2 == 1),
                        Odendi = odendi,
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

            // Logo bulunamazsa rapor logosuz üretilir; Varliklar.Resim zaten
            // dosya yoksa null döner, çizim tarafı bunu karşılıyor.
            logo = Varliklar.Resim("rora1.png");
            logoAlani = logo != null ? DoluAlan(logo) : Rectangle.Empty;

            try
            {
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
            }
            finally
            {
                // Image.FromFile dosyayı kilitli tutar; bırakılmazsa logo
                // dosyası ikinci rapora kadar kilitli kalır.
                if (logo != null) { logo.Dispose(); logo = null; }
            }

            if (!TamamlanmayiBekle(hedefYol, TimeSpan.FromSeconds(60)))
                throw new IOException("PDF dosyası oluşturulamadı. Yazıcı çıktıyı yazamamış olabilir.");
        }

        // Logo dosyasının çevresindeki boş çerçeveyi bulur. rora1.png geniş bir
        // beyaz zemin üzerine oturtulmuş; olduğu gibi çizilirse madalyon küçük
        // kalıp başlıkta boşluk oluşturuyor. Köşe pikseli zemin kabul edilip
        // ondan belirgin şekilde ayrılan ilk/son satır ve sütun aranır. Tarama
        // ikişer piksel adımlıdır: kenar payı zaten sonradan bir miktar
        // genişletildiği için tam hassasiyet gerekmiyor.
        private static Rectangle DoluAlan(Image resim)
        {
            var bmp = resim as Bitmap;
            if (bmp == null) return new Rectangle(0, 0, resim.Width, resim.Height);

            try
            {
                Color zemin = bmp.GetPixel(0, 0);
                int solX = bmp.Width, sagX = -1, ustY = bmp.Height, altY = -1;

                for (int py = 0; py < bmp.Height; py += 2)
                {
                    for (int px = 0; px < bmp.Width; px += 2)
                    {
                        if (ZeminMi(bmp.GetPixel(px, py), zemin)) continue;
                        if (px < solX) solX = px;
                        if (px > sagX) sagX = px;
                        if (py < ustY) ustY = py;
                        if (py > altY) altY = py;
                    }
                }

                if (sagX < 0 || altY < 0) return new Rectangle(0, 0, bmp.Width, bmp.Height);

                // Tarama adımı ve yumuşak kenarlar için birkaç piksel pay bırak
                const int Pay = 3;
                solX = Math.Max(0, solX - Pay);
                ustY = Math.Max(0, ustY - Pay);
                sagX = Math.Min(bmp.Width - 1, sagX + Pay);
                altY = Math.Min(bmp.Height - 1, altY + Pay);

                return new Rectangle(solX, ustY, sagX - solX + 1, altY - ustY + 1);
            }
            catch
            {
                return new Rectangle(0, 0, resim.Width, resim.Height);
            }
        }

        private static bool ZeminMi(Color renk, Color zemin)
        {
            if (renk.A < 24) return true;   // saydam alan da zemin sayılır
            const int Tolerans = 18;
            return Math.Abs(renk.R - zemin.R) <= Tolerans
                && Math.Abs(renk.G - zemin.G) <= Tolerans
                && Math.Abs(renk.B - zemin.B) <= Tolerans;
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

            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            using (var fBaslik = new Font("Segoe UI", 16, FontStyle.Bold))
            using (var fAltBaslik = new Font("Segoe UI", 10))
            using (var fUstBilgi = new Font("Segoe UI", 8.5f))
            using (var fCalgi = new Font("Segoe UI", 11, FontStyle.Bold))
            using (var fTabloBaslik = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (var fSatir = new Font("Segoe UI", 9))
            using (var fOzet = new Font("Segoe UI", 9, FontStyle.Italic))
            using (var fGenel = new Font("Segoe UI", 10, FontStyle.Bold))
            using (var fDipnot = new Font("Segoe UI", 8))
            using (var lacivert = new SolidBrush(Lacivert))
            using (var altin = new SolidBrush(Altin))
            using (var siyah = new SolidBrush(Metin))
            using (var gri = new SolidBrush(SolukMetin))
            using (var beyaz = new SolidBrush(Color.White))
            using (var odendiFirca = new SolidBrush(OdendiRengi))
            using (var odenmediFirca = new SolidBrush(OdenmediRengi))
            using (var zebraFirca = new SolidBrush(ZebraZemin))
            using (var bolumFirca = new SolidBrush(BolumZemin))
            using (var cizgi = new Pen(CizgiRengi))
            {
                y = sayfaNo == 1
                    ? IlkSayfaBasligiCiz(g, sol, y, fBaslik, fAltBaslik, lacivert, gri, altin)
                    : DevamBasligiCiz(g, sol, y, fUstBilgi, gri, cizgi);

                while (siradaki < ogeler.Count)
                {
                    var oge = ogeler[siradaki];
                    if (y + GerekenYer(oge.Tur) > altSinir) break;

                    switch (oge.Tur)
                    {
                        case OgeTuru.CalgiBasligi:
                            y += 6;
                            // Bölüm bandı: soluk zemin üzerinde altın bir işaret
                            g.FillRectangle(bolumFirca, sol, y, ToplamGenislik, 24);
                            g.FillRectangle(altin, sol, y, 5, 24);
                            g.DrawString(oge.Metin, fCalgi, lacivert, sol + 13, y + 3);
                            y += 28;
                            break;

                        case OgeTuru.TabloBasligi:
                            g.FillRectangle(lacivert, sol, y, ToplamGenislik, SatirYuksekligi);
                            CizSatir(g, SutunBasliklari, fTabloBaslik, beyaz, null, sol, y, cizgi, true);
                            y += SatirYuksekligi;
                            break;

                        case OgeTuru.OgrenciSatiri:
                            if (oge.Zebra) g.FillRectangle(zebraFirca, sol, y, ToplamGenislik, SatirYuksekligi);
                            CizSatir(g, oge.Hucreler, fSatir, siyah,
                                     oge.Odendi ? odendiFirca : odenmediFirca, sol, y, cizgi, false);
                            y += SatirYuksekligi;
                            break;

                        case OgeTuru.GrupToplami:
                            y += 3;
                            g.DrawString(oge.Metin, fOzet, gri, sol + 6, y);
                            y += 20;
                            break;

                        case OgeTuru.Bosluk:
                            y += 14;
                            break;

                        case OgeTuru.GenelToplam:
                            y += 10;
                            var kutu = new Rectangle(sol, y, ToplamGenislik - 1, 36);
                            g.FillRectangle(bolumFirca, kutu);
                            g.FillRectangle(altin, sol, y, ToplamGenislik, 3);
                            using (var kutuKalem = new Pen(Lacivert))
                                g.DrawRectangle(kutuKalem, kutu);
                            g.DrawString(oge.Metin, fGenel, lacivert, sol + 10, y + 11);
                            y += 42;
                            break;
                    }

                    siradaki++;
                }

                DipnotCiz(g, e, sol, fDipnot, gri, cizgi);
            }

            e.HasMorePages = siradaki < ogeler.Count;
        }

        // Bir öğenin sayfada kaplayacağı en az yer. Çalgı başlığı için tablo
        // başlığı ve bir satır da hesaba katılır: yoksa başlık sayfanın en
        // altında öksüz kalıp içeriği sonraki sayfaya düşerdi.
        private static int GerekenYer(OgeTuru tur)
        {
            switch (tur)
            {
                case OgeTuru.CalgiBasligi: return 28 + SatirYuksekligi * 2;
                case OgeTuru.GenelToplam: return 52;
                default: return SatirYuksekligi;
            }
        }

        private int IlkSayfaBasligiCiz(Graphics g, int sol, int y, Font fBaslik, Font fAltBaslik,
                                       Brush lacivert, Brush gri, Brush altin)
        {
            const int LogoYuksekligi = 78;
            int metinSol = sol;

            if (logo != null)
            {
                int logoGenislik = LogoGenisligi(LogoYuksekligi);
                LogoCiz(g, sol, y, logoGenislik, LogoYuksekligi);
                metinSol = sol + logoGenislik + 22;
            }

            g.DrawString("RORA SANAT MERKEZİ", fBaslik, lacivert, metinSol, y + 4);
            g.DrawString($"{ayAdi.ToUpper(TrKultur)} AYI ÖZET RAPORU", fAltBaslik, lacivert, metinSol, y + 34);
            g.DrawString($"Rapor tarihi: {DateTime.Now.ToString("dd.MM.yyyy HH:mm", TrKultur)}",
                         fAltBaslik, gri, metinSol, y + 52);

            y += LogoYuksekligi + 12;
            g.FillRectangle(altin, sol, y, ToplamGenislik, 3);
            g.FillRectangle(lacivert, sol, y + 5, ToplamGenislik, 1);
            return y + 20;
        }

        private int LogoGenisligi(int yukseklik)
        {
            return (int)Math.Round(yukseklik * ((double)logoAlani.Width / logoAlani.Height));
        }

        private void LogoCiz(Graphics g, int x, int y, int genislik, int yukseklik)
        {
            g.DrawImage(logo, new Rectangle(x, y, genislik, yukseklik),
                        logoAlani.X, logoAlani.Y, logoAlani.Width, logoAlani.Height,
                        GraphicsUnit.Pixel);
        }

        private int DevamBasligiCiz(Graphics g, int sol, int y, Font fUstBilgi, Brush gri, Pen cizgi)
        {
            const int LogoYuksekligi = 26;
            int metinSol = sol;

            if (logo != null)
            {
                int logoGenislik = LogoGenisligi(LogoYuksekligi);
                LogoCiz(g, sol, y - 4, logoGenislik, LogoYuksekligi);
                metinSol = sol + logoGenislik + 10;
            }

            g.DrawString($"RORA SANAT MERKEZİ — {ayAdi} özet raporu (devam)", fUstBilgi, gri, metinSol, y + 3);
            y += LogoYuksekligi + 2;
            g.DrawLine(cizgi, sol, y, sol + ToplamGenislik, y);
            return y + 12;
        }

        private void DipnotCiz(Graphics g, PrintPageEventArgs e, int sol, Font fDipnot, Brush gri, Pen cizgi)
        {
            int dipY = e.MarginBounds.Bottom + 6;
            g.DrawLine(cizgi, sol, dipY, sol + ToplamGenislik, dipY);

            g.DrawString("RORA Sanat Merkezi", fDipnot, gri, sol, dipY + 6);

            string dipnot = $"Sayfa {sayfaNo}";
            var olcu = g.MeasureString(dipnot, fDipnot);
            g.DrawString(dipnot, fDipnot, gri, sol + (ToplamGenislik - olcu.Width) / 2, dipY + 6);
        }

        // odemeFirca verilirse 7. sütun (Ödeme) o renkle yazılır; ödeme durumu
        // rapora bakan kişinin ilk aradığı bilgi olduğu için diğerlerinden ayrışır.
        private static void CizSatir(Graphics g, string[] hucreler, Font yazi, Brush firca,
                                     Brush odemeFirca, int sol, int y, Pen cizgi, bool baslikMi)
        {
            const int OdemeSutunu = 7;
            int x = sol;
            for (int i = 0; i < SutunGenislikleri.Length && i < hucreler.Length; i++)
            {
                var alan = new RectangleF(x + 3, y + 4, SutunGenislikleri[i] - 6, SatirYuksekligi - 6);
                var bicim = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };

                // Ad ve telefon sola, sayısal alanlar ortaya, tutar sağa
                if (i == 0 || i == 1) bicim.Alignment = StringAlignment.Near;
                else if (i == 8) bicim.Alignment = StringAlignment.Far;
                else bicim.Alignment = StringAlignment.Center;

                Brush hucreFirca = (i == OdemeSutunu && odemeFirca != null) ? odemeFirca : firca;
                g.DrawString(hucreler[i] ?? string.Empty, yazi, hucreFirca, alan, bicim);
                bicim.Dispose();
                x += SutunGenislikleri[i];
            }

            if (!baslikMi)
                g.DrawLine(cizgi, sol, y + SatirYuksekligi, x, y + SatirYuksekligi);
        }
    }
}
