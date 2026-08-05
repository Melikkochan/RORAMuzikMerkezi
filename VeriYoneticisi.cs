using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Linq;
using System.Windows.Forms;

namespace RORAMuzikMerkezi
{
    public static class VeriYoneticisi
    {
        private static readonly string VeriDosyasi = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RORAMuzikMerkezi", "veriler.xml");

        public static Veriler Veriler { get; private set; } = new Veriler();

        public static void Yukle()
        {
            try
            {
                string klasor = Path.GetDirectoryName(VeriDosyasi);
                if (!Directory.Exists(klasor))
                    Directory.CreateDirectory(klasor);

                if (File.Exists(VeriDosyasi))
                {
                    var serializer = new XmlSerializer(typeof(Veriler));
                    using (var reader = new StreamReader(VeriDosyasi))
                    {
                        Veriler = (Veriler)serializer.Deserialize(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                Veriler = new Veriler();
                BozukDosyayiKurtar(ex);
            }
        }

        // Kayıt dosyası okunamadığında çağrılır. Bozuk dosyayı silmek yerine
        // tarihli bir adla kenara alır, kullanıcıyı bilgilendirir ve boş bir
        // listeyle devam edip etmeyeceğini sorar. Böylece bozuk dosyanın
        // üzerine yazılıp verinin kalıcı olarak kaybolması engellenir.
        private static void BozukDosyayiKurtar(Exception hata)
        {
            string yedekYolu = null;
            try
            {
                if (File.Exists(VeriDosyasi))
                {
                    yedekYolu = Path.Combine(
                        Path.GetDirectoryName(VeriDosyasi),
                        $"veriler.bozuk.{DateTime.Now:yyyyMMdd_HHmmss}.xml");
                    File.Move(VeriDosyasi, yedekYolu);
                }
            }
            catch { yedekYolu = null; }

            var mesaj = new System.Text.StringBuilder();
            mesaj.AppendLine("Kayıt dosyası okunamadı, mevcut veriler yüklenemedi.");
            mesaj.AppendLine();
            mesaj.AppendLine($"Dosya : {VeriDosyasi}");
            mesaj.AppendLine($"Hata  : {hata.Message}");
            mesaj.AppendLine();

            if (yedekYolu != null)
            {
                mesaj.AppendLine("Bozuk dosya silinmedi, şu adla kenara alındı:");
                mesaj.AppendLine(yedekYolu);
            }
            else
            {
                mesaj.AppendLine("DİKKAT: Bozuk dosya yedeklenemedi.");
                mesaj.AppendLine("Devam ederseniz dosyanın üzerine yazılabilir.");
            }

            mesaj.AppendLine();
            mesaj.AppendLine("Boş bir öğrenci listesiyle devam etmek istiyor musunuz?");
            mesaj.AppendLine("Hayır'ı seçerseniz uygulama kapanır ve dosyayı elle kurtarabilirsiniz.");

            var sonuc = MessageBox.Show(mesaj.ToString(), "Veri Yükleme Hatası",
                MessageBoxButtons.YesNo, MessageBoxIcon.Error);

            if (sonuc != DialogResult.Yes)
                Environment.Exit(1);
        }

        public static void Kaydet()
        {
            try
            {
                string klasor = Path.GetDirectoryName(VeriDosyasi);
                if (!Directory.Exists(klasor))
                    Directory.CreateDirectory(klasor);

                var serializer = new XmlSerializer(typeof(Veriler));
                using (var writer = new StreamWriter(VeriDosyasi))
                {
                    serializer.Serialize(writer, Veriler);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kayıt hatası: {ex.Message}", "Hata");
            }
        }

        public static Ogrenci OgrenciEkle(string ad, string soyad, string telefon, string calgi)
        {
            Veriler.SonOgrenciId++;
            var ogrenci = new Ogrenci
            {
                Id = Veriler.SonOgrenciId,
                Ad = ad,
                Soyad = soyad,
                Telefon = telefon,
                Calgı = calgi,
                KayitTarihi = DateTime.Now
            };
            Veriler.Ogrenciler.Add(ogrenci);
            OdemeKaydiOlustur(ogrenci);
            Kaydet();
            return ogrenci;
        }

        public static void OgrenciSil(int ogrenciId)
        {
            var ogrenci = Veriler.Ogrenciler.FirstOrDefault(o => o.Id == ogrenciId);
            if (ogrenci != null)
            {
                Veriler.Ogrenciler.Remove(ogrenci);
                Kaydet();
            }
        }

        // Kayıtlı bir öğrencinin bilgilerini günceller. Ders ve ödeme kayıtları
        // öğrenci nesnesinin içinde tutulduğu için bu işlemden etkilenmez.
        public static bool OgrenciGuncelle(int ogrenciId, string ad, string soyad, string telefon, string calgi)
        {
            var ogrenci = Veriler.Ogrenciler.FirstOrDefault(o => o.Id == ogrenciId);
            if (ogrenci == null) return false;

            ogrenci.Ad = ad;
            ogrenci.Soyad = soyad;
            ogrenci.Telefon = telefon;
            ogrenci.Calgı = calgi;
            Kaydet();
            return true;
        }
        public static List<Ogrenci> CalginaGoreOgrenciler(string calgi)
        {
            return Veriler.Ogrenciler.Where(o => o.Calgı == calgi).ToList();
        }

        // Belirli bir ay ve hafta numarasına (1-4) göre ders kaydı
        public static void HaftaDersKaydet(int ogrenciId, int yil, int ay, int haftaNo, bool alindi)
        {
            var ogrenci = Veriler.Ogrenciler.FirstOrDefault(o => o.Id == ogrenciId);
            if (ogrenci == null) return;

            var mevcut = ogrenci.DersKayitlari.FirstOrDefault(d =>
                d.Yil == yil && d.Ay == ay && d.HaftaNo == haftaNo);

            if (mevcut != null)
            {
                mevcut.DersAlindi = alindi;
            }
            else
            {
                Veriler.SonDersId++;
                ogrenci.DersKayitlari.Add(new DersKaydi
                {
                    Id = Veriler.SonDersId,
                    OgrenciId = ogrenciId,
                    Yil = yil,
                    Ay = ay,
                    HaftaNo = haftaNo,
                    DersAlindi = alindi,
                    DersTarihi = DateTime.Now
                });
            }
            Kaydet();
        }

        // Belirli ay ve hafta için ders aldı mı?
        public static bool HaftaDersAldiMi(int ogrenciId, int yil, int ay, int haftaNo)
        {
            var ogrenci = Veriler.Ogrenciler.FirstOrDefault(o => o.Id == ogrenciId);
            if (ogrenci == null) return false;

            var kayit = ogrenci.DersKayitlari.FirstOrDefault(d =>
                d.Yil == yil && d.Ay == ay && d.HaftaNo == haftaNo);
            return kayit?.DersAlindi ?? false;
        }

        // Geriye uyumluluk: bu hafta ders aldı mı (mevcut haftayı bul)
        public static bool BuHaftaDersAldiMi(int ogrenciId)
        {
            int buHaftaNo = BuAyinHaftasiNo();
            return HaftaDersAldiMi(ogrenciId, DateTime.Now.Year, DateTime.Now.Month, buHaftaNo);
        }

        // Şu anki tarihin o aydaki hafta numarasını döndür (1-4)
        public static int BuAyinHaftasiNo()
        {
            int gun = DateTime.Now.Day;
            if (gun <= 7) return 1;
            if (gun <= 14) return 2;
            if (gun <= 21) return 3;
            return 4;
        }

        public static int AylikDersSayisi(int ogrenciId, int yil, int ay)
        {
            var ogrenci = Veriler.Ogrenciler.FirstOrDefault(o => o.Id == ogrenciId);
            if (ogrenci == null) return 0;
            return ogrenci.DersKayitlari.Count(d =>
                d.DersAlindi && d.Yil == yil && d.Ay == ay);
        }

        private static void OdemeKaydiOlustur(Ogrenci ogrenci)
        {
            var simdi = DateTime.Now;
            if (!ogrenci.OdemeKayitlari.Any(o => o.Yil == simdi.Year && o.Ay == simdi.Month))
            {
                Veriler.SonOdemeId++;
                ogrenci.OdemeKayitlari.Add(new OdemeKaydi
                {
                    Id = Veriler.SonOdemeId,
                    OgrenciId = ogrenci.Id,
                    Yil = simdi.Year,
                    Ay = simdi.Month,
                    OdemeYapildi = false
                });
            }
        }

        public static void AylikOdemeKayitlariniOlustur()
        {
            var simdi = DateTime.Now;
            foreach (var ogrenci in Veriler.Ogrenciler)
            {
                if (!ogrenci.OdemeKayitlari.Any(o => o.Yil == simdi.Year && o.Ay == simdi.Month))
                {
                    Veriler.SonOdemeId++;
                    ogrenci.OdemeKayitlari.Add(new OdemeKaydi
                    {
                        Id = Veriler.SonOdemeId,
                        OgrenciId = ogrenci.Id,
                        Yil = simdi.Year,
                        Ay = simdi.Month,
                        OdemeYapildi = false
                    });
                }
            }
            Kaydet();
        }

        public static void OdemeYap(int ogrenciId, int yil, int ay)
        {
            var ogrenci = Veriler.Ogrenciler.FirstOrDefault(o => o.Id == ogrenciId);
            if (ogrenci == null) return;

            var odeme = ogrenci.OdemeKayitlari.FirstOrDefault(o => o.Yil == yil && o.Ay == ay);
            if (odeme == null)
            {
                Veriler.SonOdemeId++;
                odeme = new OdemeKaydi
                {
                    Id = Veriler.SonOdemeId,
                    OgrenciId = ogrenciId,
                    Yil = yil,
                    Ay = ay
                };
                ogrenci.OdemeKayitlari.Add(odeme);
            }
            odeme.OdemeYapildi = true;
            odeme.OdemeTarihi = DateTime.Now;
            Kaydet();
        }
        public static void OdemeGeriAl(int ogrenciId, int yil, int ay)
        {
            var ogrenci = Veriler.Ogrenciler.FirstOrDefault(o => o.Id == ogrenciId);
            if (ogrenci == null) return;

            var odeme = ogrenci.OdemeKayitlari.FirstOrDefault(o => o.Yil == yil && o.Ay == ay);
            if (odeme != null)
            {
                odeme.OdemeYapildi = false;
                odeme.OdemeTarihi = null;
                Kaydet();
            }
        }
        public static bool OdemeYapildiMi(int ogrenciId, int yil, int ay)
        {
            var ogrenci = Veriler.Ogrenciler.FirstOrDefault(o => o.Id == ogrenciId);
            if (ogrenci == null) return false;
            var odeme = ogrenci.OdemeKayitlari.FirstOrDefault(o => o.Yil == yil && o.Ay == ay);
            return odeme?.OdemeYapildi ?? false;
        }

        public static string OzetRaporOlustur(int yil, int ay)
        {
            string ayAdi = new DateTime(yil, ay, 1).ToString("MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("========================================");
            sb.AppendLine($"   RORA SANAT MERKEZİ");
            sb.AppendLine($"   {ayAdi.ToUpper()} AYI ÖZET RAPORU");
            sb.AppendLine($"   Rapor Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}");
            sb.AppendLine("========================================");
            sb.AppendLine();

            var calgilar = Veriler.Ogrenciler.Select(o => o.Calgı).Distinct().OrderBy(c => c).ToList();

            foreach (var calgi in calgilar)
            {
                var ogrenciler = Veriler.Ogrenciler.Where(o => o.Calgı == calgi).ToList();
                sb.AppendLine($"--- {calgi.ToUpper()} ---");
                sb.AppendLine($"{"Ad Soyad",-25} {"Tel",-13} {"H1",-4} {"H2",-4} {"H3",-4} {"H4",-4} {"Toplam",-8} {"Ödeme"}");
                sb.AppendLine(new string('-', 72));

                foreach (var ogr in ogrenciler)
                {
                    bool h1 = HaftaDersAldiMi(ogr.Id, yil, ay, 1);
                    bool h2 = HaftaDersAldiMi(ogr.Id, yil, ay, 2);
                    bool h3 = HaftaDersAldiMi(ogr.Id, yil, ay, 3);
                    bool h4 = HaftaDersAldiMi(ogr.Id, yil, ay, 4);
                    int toplam = (h1 ? 1 : 0) + (h2 ? 1 : 0) + (h3 ? 1 : 0) + (h4 ? 1 : 0);
                    bool odeme = OdemeYapildiMi(ogr.Id, yil, ay);

                    sb.AppendLine($"{ogr.TamAd,-25} {ogr.Telefon,-13} {(h1?"✓":"✗"),-4} {(h2?"✓":"✗"),-4} {(h3?"✓":"✗"),-4} {(h4?"✓":"✗"),-4} {toplam,-8} {(odeme ? "Ödendi" : "Ödenmedi")}");
                }

                int toplamOgrenci = ogrenciler.Count;
                int odeyenler = ogrenciler.Count(o => OdemeYapildiMi(o.Id, yil, ay));
                sb.AppendLine();
                sb.AppendLine($"  Toplam: {toplamOgrenci} öğrenci | Ödeme Yapan: {odeyenler} | Yapmayan: {toplamOgrenci - odeyenler}");
                sb.AppendLine();
            }

            sb.AppendLine("========================================");
            int genelToplam = Veriler.Ogrenciler.Count;
            int genelOdeyen = Veriler.Ogrenciler.Count(o => OdemeYapildiMi(o.Id, yil, ay));
            sb.AppendLine($"GENEL TOPLAM: {genelToplam} öğrenci");
            sb.AppendLine($"ÖDEME YAPAN : {genelOdeyen} öğrenci");
            sb.AppendLine($"ÖDEME YAPMAYAN: {genelToplam - genelOdeyen} öğrenci");
            sb.AppendLine("========================================");

            return sb.ToString();
        }
    }
}
