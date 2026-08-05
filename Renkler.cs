using System.Drawing;

namespace RORAMuzikMerkezi
{
    // Arayüzün tek renk kaynağı. Önceden her ekran kendi tonlarını
    // tanımlıyordu; yan yana gelince aynı işi yapan iki şey farklı, farklı
    // işi yapan iki şey aynı görünebiliyordu.
    //
    // Palet PDF raporundan alındı: lacivert açılış ekranının zemininden,
    // altın ise logodaki madalyon tonundan. Böylece ekran ile basılı çıktı
    // aynı dili konuşuyor.
    public static class Renkler
    {
        // ---- Ana renk ----
        public static readonly Color Lacivert = Color.FromArgb(35, 45, 100);
        public static readonly Color LacivertOrta = Color.FromArgb(58, 72, 140);
        public static readonly Color LacivertSolgun = Color.FromArgb(232, 236, 247);

        // Vurgu. Yalnızca dikkat çekmesi gereken tek şeyde kullanılır;
        // her yerde kullanılırsa vurgu olmaktan çıkar.
        public static readonly Color Altin = Color.FromArgb(190, 152, 106);

        // ---- Yüzeyler ----
        public static readonly Color Zemin = Color.FromArgb(246, 247, 251);
        public static readonly Color Yuzey = Color.White;
        public static readonly Color YuzeyVurgu = Color.FromArgb(250, 251, 253);
        public static readonly Color Cizgi = Color.FromArgb(223, 227, 238);

        // ---- Yazı ----
        public static readonly Color Metin = Color.FromArgb(33, 37, 52);
        public static readonly Color MetinSolgun = Color.FromArgb(108, 114, 135);
        public static readonly Color MetinTers = Color.White;

        // ---- Anlam taşıyan renkler ----
        public static readonly Color Olumlu = Color.FromArgb(30, 130, 75);
        public static readonly Color Olumsuz = Color.FromArgb(180, 60, 60);
        public static readonly Color OlumluSolgun = Color.FromArgb(233, 245, 238);
        public static readonly Color OlumsuzSolgun = Color.FromArgb(250, 238, 238);

        // ---- Tablo ----
        public static readonly Color SatirSecili = Color.FromArgb(222, 229, 246);
        public static readonly Color SatirUzerinde = Color.FromArgb(239, 243, 251);
    }

    // Tipografi ölçeği. Başlık, alt başlık ve gövde arasında belirgin bir
    // fark olsun diye tek yerden veriliyor; her ekranın kendi punto seçmesi
    // hiyerarşiyi siliyordu.
    public static class Yazilar
    {
        private const string Aile = "Segoe UI";

        public static Font Baslik { get { return new Font(Aile, 16, FontStyle.Bold); } }
        public static Font AltBaslik { get { return new Font(Aile, 12, FontStyle.Bold); } }
        public static Font Govde { get { return new Font(Aile, 10); } }
        public static Font GovdeKalin { get { return new Font(Aile, 10, FontStyle.Bold); } }
        public static Font Kucuk { get { return new Font(Aile, 8.5f); } }
        public static Font Sayi { get { return new Font(Aile, 24, FontStyle.Bold); } }
    }
}
