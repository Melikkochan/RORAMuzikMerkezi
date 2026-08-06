using System;
using System.Collections.Generic;
using System.Globalization;

namespace RORAMuzikMerkezi
{
    // Rapor alınacak dönem: kapalı bir ay aralığı. Başlangıç ve bitiş ayı da
    // dahildir.
    //
    // Yıllık rapor ayrı bir tür değil, Ocak–Aralık aralığının kendisi. Böylece
    // rapor üreten taraf tek bir kavramla çalışıyor; "yıl mı, aralık mı"
    // ayrımı yalnızca dönemi oluşturan yerde kalıyor.
    public class Donem
    {
        public int BasYil { get; private set; }
        public int BasAy { get; private set; }
        public int BitYil { get; private set; }
        public int BitAy { get; private set; }

        // Yıllık rapor başlıkta "2026 Yılı" diye görünsün diye işaretleniyor.
        // Aralık olarak da doğru çalışır; bu yalnızca sunum farkı.
        public bool TamYil { get; private set; }

        private static CultureInfo TrKultur { get { return new CultureInfo("tr-TR"); } }

        private Donem(int basYil, int basAy, int bitYil, int bitAy, bool tamYil)
        {
            BasYil = basYil;
            BasAy = basAy;
            BitYil = bitYil;
            BitAy = bitAy;
            TamYil = tamYil;
        }

        public static Donem Yil(int yil)
        {
            return new Donem(yil, 1, yil, 12, true);
        }

        // Başlangıç bitişten sonraysa istisna atmak yerine ikisi yer değiştirir.
        // Kullanıcı iki tarihi ters girdiğinde niyeti belli; onu hata ekranıyla
        // karşılamak yerine anladığımız işi yapıyoruz.
        public static Donem Aralik(int basYil, int basAy, int bitYil, int bitAy)
        {
            if (basAy < 1 || basAy > 12) throw new ArgumentOutOfRangeException("basAy");
            if (bitAy < 1 || bitAy > 12) throw new ArgumentOutOfRangeException("bitAy");

            if (basYil * 12 + basAy > bitYil * 12 + bitAy)
            {
                int y = basYil, a = basAy;
                basYil = bitYil; basAy = bitAy;
                bitYil = y; bitAy = a;
            }

            bool tamYil = basYil == bitYil && basAy == 1 && bitAy == 12;
            return new Donem(basYil, basAy, bitYil, bitAy, tamYil);
        }

        public int AySayisi
        {
            get { return (BitYil * 12 + BitAy) - (BasYil * 12 + BasAy) + 1; }
        }

        // Dönemdeki her ayın ilk günü, baştan sona. Rapor üreten taraf bu
        // listeyi dolaşıp aylık verileri toplar.
        public IEnumerable<DateTime> Aylar()
        {
            var ay = new DateTime(BasYil, BasAy, 1);
            var son = new DateTime(BitYil, BitAy, 1);
            while (ay <= son)
            {
                yield return ay;
                ay = ay.AddMonths(1);
            }
        }

        // "2026 Yılı" ya da "Eylül 2025 – Haziran 2026"
        public string Baslik
        {
            get
            {
                if (TamYil) return BasYil + " Yılı";
                return AyYil(BasYil, BasAy) + " – " + AyYil(BitYil, BitAy);
            }
        }

        // Dosya adında kullanılır; boşluk ve Türkçe karakter içermez.
        public string DosyaAdiParcasi
        {
            get
            {
                if (TamYil) return BasYil.ToString(CultureInfo.InvariantCulture);
                return string.Format(CultureInfo.InvariantCulture, "{0:D4}-{1:D2}_{2:D4}-{3:D2}",
                                     BasYil, BasAy, BitYil, BitAy);
            }
        }

        public static string AyYil(int yil, int ay)
        {
            return new DateTime(yil, ay, 1).ToString("MMMM yyyy", TrKultur);
        }

        public static string AyAdi(int ay)
        {
            return new DateTime(2000, ay, 1).ToString("MMMM", TrKultur);
        }

        // Rapor ekranlarında listelenecek yıllar: kayıtlardaki en eski yıldan
        // gelecek yıla kadar. Sabit bir aralık yerine veriye bakılıyor; eski
        // kayıtları olan bir merkezde o yıllar da seçilebilmeli, yeni kurulan
        // bir merkezde ise liste gereksiz yere uzamamalı.
        //
        // Hem aylık hem dönemsel rapor ekranı bunu kullanır; iki ekranda iki
        // farklı yıl listesi çıkmasın diye tek yerde duruyor.
        public static int[] SecilebilirYillar()
        {
            int buYil = DateTime.Now.Year;
            int enEski = buYil;

            foreach (var ogrenci in VeriYoneticisi.Veriler.Ogrenciler)
            {
                foreach (var ders in ogrenci.DersKayitlari)
                    if (ders.Yil < enEski) enEski = ders.Yil;
                foreach (var odeme in ogrenci.OdemeKayitlari)
                    if (odeme.Yil < enEski) enEski = odeme.Yil;
            }

            var yillar = new List<int>();
            for (int y = enEski; y <= buYil + 1; y++) yillar.Add(y);
            return yillar.ToArray();
        }
    }
}
