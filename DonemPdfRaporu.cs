using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace RORAMuzikMerkezi
{
    // Dönemsel raporu PDF olarak üretir: ay ay tahsilat ve çalgı bazında
    // dağılım.
    //
    // Aylık rapordan farkı, tek tek öğrencileri değil toplamları göstermesi.
    // Yıl sonu değerlendirmesinde aranan şey "kim ne zaman ödedi" değil,
    // "hangi ay ne kadar geldi, hangi çalgı ne kadar getirdi".
    //
    // Sayfa çerçevesi, logo, yazdırma kurulumu ve sayfalama PdfRapor'da.
    public class DonemPdfRaporu : PdfRapor
    {
        private enum OgeTuru { BolumBasligi, TabloBasligi, Satir, ToplamSatiri, Bosluk, Ozet }

        private class Oge
        {
            public OgeTuru Tur;
            public string[] Hucreler;
            public int[] Sutunlar;
            public StringAlignment[] Hizalar;
            public string Metin;
            public string IkinciMetin;
            public bool Zebra;
        }

        // Sütun genişlikleri, 1/100 inç. İki tablonun da toplamı
        // KullanilabilirGenislik (707) olmalı; aksi hâlde tablo kenar boşluğuna taşar.
        private static readonly int[] AySutunlari = { 250, 160, 190, 107 };
        private static readonly string[] AyBasliklari = { "Ay", "Ödeme Yapan", "Tahsilat", "Pay" };
        private static readonly StringAlignment[] AyHizalari =
        {
            StringAlignment.Near, StringAlignment.Center, StringAlignment.Far, StringAlignment.Far
        };

        private static readonly int[] CalgiSutunlari = { 230, 100, 100, 170, 107 };
        private static readonly string[] CalgiBasliklari = { "Çalgı", "Öğrenci", "Ödeme", "Tahsilat", "Pay" };
        private static readonly StringAlignment[] CalgiHizalari =
        {
            StringAlignment.Near, StringAlignment.Center, StringAlignment.Center, StringAlignment.Far, StringAlignment.Far
        };

        private readonly Donem donem;
        private readonly List<Oge> ogeler = new List<Oge>();
        private int siradaki;

        public DonemPdfRaporu(Donem donem)
        {
            this.donem = donem;
            OgeleriHazirla();
        }

        protected override string BelgeAdi { get { return $"RORA Dönem Raporu {donem.Baslik}"; } }
        protected override string BaslikAltSatiri { get { return $"{donem.Baslik.ToUpper(TrKultur)} DÖNEM RAPORU"; } }
        protected override string DevamMetni { get { return $"{donem.Baslik} dönem raporu (devam)"; } }
        protected override int ToplamGenislik { get { return KullanilabilirGenislik; } }

        protected override void BasaSar() { siradaki = 0; }
        protected override bool KalanOgeVar { get { return siradaki < ogeler.Count; } }
        protected override int SiradakiGerekenYer { get { return GerekenYer(ogeler[siradaki].Tur); } }

        private void OgeleriHazirla()
        {
            decimal donemToplam = VeriYoneticisi.DonemToplamGelir(donem);

            // ---- Ay ay tahsilat ----
            ogeler.Add(new Oge { Tur = OgeTuru.BolumBasligi, Metin = "AY AY TAHSİLAT" });
            ogeler.Add(new Oge { Tur = OgeTuru.TabloBasligi, Hucreler = AyBasliklari, Sutunlar = AySutunlari, Hizalar = AyHizalari });

            int sira = 0;
            DateTime enIyiAy = new DateTime(donem.BasYil, donem.BasAy, 1);
            decimal enIyiTutar = -1m;

            foreach (var nokta in donem.Aylar())
            {
                decimal tutar = VeriYoneticisi.AylikToplamGelir(nokta.Year, nokta.Month);
                int odeyen = VeriYoneticisi.AylikOdeyenSayisi(nokta.Year, nokta.Month);

                if (tutar > enIyiTutar) { enIyiTutar = tutar; enIyiAy = nokta; }

                ogeler.Add(new Oge
                {
                    Tur = OgeTuru.Satir,
                    Zebra = (sira++ % 2 == 1),
                    Sutunlar = AySutunlari,
                    Hizalar = AyHizalari,
                    Hucreler = new[]
                    {
                        Donem.AyYil(nokta.Year, nokta.Month),
                        odeyen.ToString(),
                        VeriYoneticisi.TutarYazi(tutar),
                        VeriYoneticisi.PayYazi(tutar, donemToplam)
                    }
                });
            }

            ogeler.Add(new Oge
            {
                Tur = OgeTuru.ToplamSatiri,
                Sutunlar = AySutunlari,
                Hizalar = AyHizalari,
                Hucreler = new[] { "TOPLAM", string.Empty, VeriYoneticisi.TutarYazi(donemToplam), string.Empty }
            });
            ogeler.Add(new Oge { Tur = OgeTuru.Bosluk });

            // ---- Çalgı bazında ----
            ogeler.Add(new Oge { Tur = OgeTuru.BolumBasligi, Metin = "ÇALGI BAZINDA TAHSİLAT" });
            ogeler.Add(new Oge { Tur = OgeTuru.TabloBasligi, Hucreler = CalgiBasliklari, Sutunlar = CalgiSutunlari, Hizalar = CalgiHizalari });

            var calgilar = VeriYoneticisi.Calgilar();
            if (calgilar.Count == 0)
            {
                ogeler.Add(new Oge
                {
                    Tur = OgeTuru.Satir,
                    Sutunlar = CalgiSutunlari,
                    Hizalar = CalgiHizalari,
                    Hucreler = new[] { "(kayıtlı öğrenci yok)", string.Empty, string.Empty, string.Empty, string.Empty }
                });
            }
            else
            {
                sira = 0;
                foreach (var calgi in calgilar)
                {
                    int ogrenciSayisi = VeriYoneticisi.Veriler.Ogrenciler.Count(o => o.Calgı == calgi);
                    int odemeAdedi = VeriYoneticisi.CalgiDonemOdemeAdedi(calgi, donem);
                    decimal tutar = VeriYoneticisi.CalgiDonemGeliri(calgi, donem);

                    ogeler.Add(new Oge
                    {
                        Tur = OgeTuru.Satir,
                        Zebra = (sira++ % 2 == 1),
                        Sutunlar = CalgiSutunlari,
                        Hizalar = CalgiHizalari,
                        Hucreler = new[]
                        {
                            calgi,
                            ogrenciSayisi.ToString(),
                            odemeAdedi.ToString(),
                            VeriYoneticisi.TutarYazi(tutar),
                            VeriYoneticisi.PayYazi(tutar, donemToplam)
                        }
                    });
                }
            }

            ogeler.Add(new Oge
            {
                Tur = OgeTuru.ToplamSatiri,
                Sutunlar = CalgiSutunlari,
                Hizalar = CalgiHizalari,
                Hucreler = new[]
                {
                    "TOPLAM",
                    VeriYoneticisi.Veriler.Ogrenciler.Count.ToString(),
                    string.Empty,
                    VeriYoneticisi.TutarYazi(donemToplam),
                    string.Empty
                }
            });

            // ---- Özet kutusu ----
            decimal ortalama = donem.AySayisi > 0 ? donemToplam / donem.AySayisi : 0m;
            string ikinci = $"Aylık ortalama: {VeriYoneticisi.TutarYazi(ortalama)}   ·   Kayıtlı öğrenci: {VeriYoneticisi.Veriler.Ogrenciler.Count}";
            if (enIyiTutar > 0m)
                ikinci += $"   ·   En yüksek ay: {Donem.AyYil(enIyiAy.Year, enIyiAy.Month)} ({VeriYoneticisi.TutarYazi(enIyiTutar)})";

            ogeler.Add(new Oge
            {
                Tur = OgeTuru.Ozet,
                Metin = $"{donem.Baslik.ToUpper(TrKultur)}  ·  {donem.AySayisi} ay  ·  Toplam tahsilat: {VeriYoneticisi.TutarYazi(donemToplam)}",
                IkinciMetin = ikinci
            });
        }

        protected override int SiradakiOgeyiCiz(Graphics g, Kaynaklar k, int sol, int y)
        {
            var oge = ogeler[siradaki];

            switch (oge.Tur)
            {
                case OgeTuru.BolumBasligi:
                    y += 6;
                    g.FillRectangle(k.Bolum, sol, y, ToplamGenislik, 24);
                    g.FillRectangle(k.Altin, sol, y, 5, 24);
                    g.DrawString(oge.Metin, k.BolumBasligi, k.Lacivert, sol + 13, y + 3);
                    y += 28;
                    break;

                case OgeTuru.TabloBasligi:
                    g.FillRectangle(k.Lacivert, sol, y, ToplamGenislik, SatirYuksekligi);
                    CizSatir(g, oge, k.TabloBasligi, k.Beyaz, sol, y, k.Cizgi, false);
                    y += SatirYuksekligi;
                    break;

                case OgeTuru.Satir:
                    if (oge.Zebra) g.FillRectangle(k.Zebra, sol, y, ToplamGenislik, SatirYuksekligi);
                    CizSatir(g, oge, k.Satir, k.Siyah, sol, y, k.Cizgi, true);
                    y += SatirYuksekligi;
                    break;

                case OgeTuru.ToplamSatiri:
                    // Toplam satırı tablonun bir parçası ama okurken gözün
                    // takılması gereken yer; üstüne lacivert bir çizgi çekilip
                    // kalın yazılıyor.
                    g.DrawLine(k.LacivertKalem, sol, y, sol + ToplamGenislik, y);
                    CizSatir(g, oge, k.SatirKalin, k.Lacivert, sol, y, k.Cizgi, false);
                    y += SatirYuksekligi;
                    break;

                case OgeTuru.Bosluk:
                    y += 16;
                    break;

                case OgeTuru.Ozet:
                    y += 10;
                    var kutu = new Rectangle(sol, y, ToplamGenislik - 1, 52);
                    g.FillRectangle(k.Bolum, kutu);
                    g.FillRectangle(k.Altin, sol, y, ToplamGenislik, 3);
                    g.DrawRectangle(k.LacivertKalem, kutu);
                    g.DrawString(oge.Metin, k.Genel, k.Lacivert, sol + 10, y + 9);
                    g.DrawString(oge.IkinciMetin, k.Ozet, k.Gri, sol + 10, y + 30);
                    y += 58;
                    break;
            }

            siradaki++;
            return y;
        }

        // Bölüm başlığı için tablo başlığı ve bir satır da hesaba katılır:
        // yoksa başlık sayfanın en altında öksüz kalıp tablosu sonraki sayfaya
        // düşerdi.
        private static int GerekenYer(OgeTuru tur)
        {
            switch (tur)
            {
                case OgeTuru.BolumBasligi: return 28 + SatirYuksekligi * 2;
                case OgeTuru.Ozet: return 68;
                default: return SatirYuksekligi;
            }
        }

        private static void CizSatir(Graphics g, Oge oge, Font yazi, Brush firca,
                                     int sol, int y, Pen cizgi, bool altCizgi)
        {
            int x = sol;
            for (int i = 0; i < oge.Sutunlar.Length && i < oge.Hucreler.Length; i++)
            {
                var alan = new RectangleF(x + 3, y + 4, oge.Sutunlar[i] - 6, SatirYuksekligi - 6);
                var bicim = new StringFormat
                {
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap,
                    Alignment = oge.Hizalar[i]
                };

                g.DrawString(oge.Hucreler[i] ?? string.Empty, yazi, firca, alan, bicim);
                bicim.Dispose();
                x += oge.Sutunlar[i];
            }

            if (altCizgi)
                g.DrawLine(cizgi, sol, y + SatirYuksekligi, x, y + SatirYuksekligi);
        }
    }
}
