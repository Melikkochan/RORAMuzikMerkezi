using System;
using System.Collections.Generic;

namespace RORAMuzikMerkezi
{
    [Serializable]
    public class Ogrenci
    {
        public int Id { get; set; }
        public string Ad { get; set; }
        public string Soyad { get; set; }
        public string Telefon { get; set; }
        public string Calgı { get; set; }
        public DateTime KayitTarihi { get; set; }
        public List<DersKaydi> DersKayitlari { get; set; } = new List<DersKaydi>();
        public List<OdemeKaydi> OdemeKayitlari { get; set; } = new List<OdemeKaydi>();

        public string TamAd => $"{Ad} {Soyad}";
    }

    [Serializable]
    public class DersKaydi
    {
        public int Id { get; set; }
        public int OgrenciId { get; set; }
        public int Yil { get; set; }
        public int Ay { get; set; }
        public int HaftaNo { get; set; }   // 1, 2, 3 veya 4
        public bool DersAlindi { get; set; }
        public DateTime DersTarihi { get; set; }
    }

    [Serializable]
    public class OdemeKaydi
    {
        public int Id { get; set; }
        public int OgrenciId { get; set; }
        public int Yil { get; set; }
        public int Ay { get; set; }
        public bool OdemeYapildi { get; set; }

        // Tahsil edilen tutar. Bu alan sonradan eklendi; eski veri
        // dosyalarında bulunmadığı için XML okunurken 0 kabul edilir.
        public decimal Tutar { get; set; }
        public DateTime? OdemeTarihi { get; set; }
        public string AyYilStr => $"{Yil}/{Ay:D2}";
    }

    [Serializable]
    public class Veriler
    {
        public List<Ogrenci> Ogrenciler { get; set; } = new List<Ogrenci>();
        public int SonOgrenciId { get; set; } = 0;
        public int SonDersId { get; set; } = 0;
        public int SonOdemeId { get; set; } = 0;
    }
}
