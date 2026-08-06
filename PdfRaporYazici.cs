using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace RORAMuzikMerkezi
{
    // Aylık özet raporunu PDF olarak üretir: her çalgı için öğrenci listesi,
    // haftalık ders işaretleri ve ödeme durumu.
    //
    // Sayfa çerçevesi, logo, yazdırma kurulumu ve sayfalama PdfRapor'da;
    // burada yalnızca bu raporun düzeni var.
    public class PdfRaporYazici : PdfRapor
    {
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

        private readonly int yil;
        private readonly int ay;
        private readonly string ayAdi;
        private readonly List<Oge> ogeler = new List<Oge>();
        private int siradaki;

        public PdfRaporYazici(int yil, int ay)
        {
            this.yil = yil;
            this.ay = ay;
            this.ayAdi = new DateTime(yil, ay, 1).ToString("MMMM yyyy", TrKultur);
            OgeleriHazirla();
        }

        protected override string BelgeAdi { get { return $"RORA Özet Rapor {ayAdi}"; } }
        protected override string BaslikAltSatiri { get { return $"{ayAdi.ToUpper(TrKultur)} AYI ÖZET RAPORU"; } }
        protected override string DevamMetni { get { return $"{ayAdi} özet raporu (devam)"; } }
        protected override int ToplamGenislik { get { return SutunGenislikleri.Sum(); } }

        protected override void BasaSar() { siradaki = 0; }
        protected override bool KalanOgeVar { get { return siradaki < ogeler.Count; } }
        protected override int SiradakiGerekenYer { get { return GerekenYer(ogeler[siradaki].Tur); } }

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

        protected override int SiradakiOgeyiCiz(Graphics g, Kaynaklar k, int sol, int y)
        {
            var oge = ogeler[siradaki];

            switch (oge.Tur)
            {
                case OgeTuru.CalgiBasligi:
                    y += 6;
                    // Bölüm bandı: soluk zemin üzerinde altın bir işaret
                    g.FillRectangle(k.Bolum, sol, y, ToplamGenislik, 24);
                    g.FillRectangle(k.Altin, sol, y, 5, 24);
                    g.DrawString(oge.Metin, k.BolumBasligi, k.Lacivert, sol + 13, y + 3);
                    y += 28;
                    break;

                case OgeTuru.TabloBasligi:
                    g.FillRectangle(k.Lacivert, sol, y, ToplamGenislik, SatirYuksekligi);
                    CizSatir(g, SutunBasliklari, k.TabloBasligi, k.Beyaz, null, sol, y, k.Cizgi, true);
                    y += SatirYuksekligi;
                    break;

                case OgeTuru.OgrenciSatiri:
                    if (oge.Zebra) g.FillRectangle(k.Zebra, sol, y, ToplamGenislik, SatirYuksekligi);
                    CizSatir(g, oge.Hucreler, k.Satir, k.Siyah,
                             oge.Odendi ? k.Odendi : k.Odenmedi, sol, y, k.Cizgi, false);
                    y += SatirYuksekligi;
                    break;

                case OgeTuru.GrupToplami:
                    y += 3;
                    g.DrawString(oge.Metin, k.Ozet, k.Gri, sol + 6, y);
                    y += 20;
                    break;

                case OgeTuru.Bosluk:
                    y += 14;
                    break;

                case OgeTuru.GenelToplam:
                    y += 10;
                    var kutu = new Rectangle(sol, y, ToplamGenislik - 1, 36);
                    g.FillRectangle(k.Bolum, kutu);
                    g.FillRectangle(k.Altin, sol, y, ToplamGenislik, 3);
                    g.DrawRectangle(k.LacivertKalem, kutu);
                    g.DrawString(oge.Metin, k.Genel, k.Lacivert, sol + 10, y + 11);
                    y += 42;
                    break;
            }

            siradaki++;
            return y;
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
                var bicim = new System.Drawing.StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };

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
