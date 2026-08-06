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

        private static readonly string YedekKlasoru = Path.Combine(
            Path.GetDirectoryName(VeriDosyasi), "yedekler");

        // Kullanıcıya dosya konumunu gösterebilmek için
        public static string VeriDosyaYolu { get { return VeriDosyasi; } }
        public static string YedekKlasorYolu { get { return YedekKlasoru; } }

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

        // Kayıt dosyasının doğrudan üstüne yazılmaz. StreamWriter dosyayı açar
        // açmaz içeriğini sıfırlar; serileştirme bitene kadar geçen sürede
        // dosya yarım durumdadır ve o aralıkta elektrik kesilirse, program
        // çökerse veya bilgisayar kapanırsa tek kopya bozulur. Kaydet her ders
        // işaretlemesinde ve her ödeme girişinde çağrıldığı için bu aralık gün
        // içinde defalarca oluşuyor.
        //
        // Bunun yerine önce aynı klasördeki geçici bir dosyaya yazılır, yazma
        // tamamlanıp içerik diske indirildikten sonra dosya tek adımda asıl
        // dosyanın yerine geçirilir. Yazma yarıda kalırsa asıl dosya eski
        // hâliyle sağlam kalır; en kötü ihtimalle son işlem kaydedilmemiş olur.
        public static void Kaydet()
        {
            string geciciYol = VeriDosyasi + ".yeni";

            try
            {
                string klasor = Path.GetDirectoryName(VeriDosyasi);
                if (!Directory.Exists(klasor))
                    Directory.CreateDirectory(klasor);

                var serializer = new XmlSerializer(typeof(Veriler));
                using (var akis = new FileStream(geciciYol, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(akis))
                {
                    serializer.Serialize(writer, Veriler);
                    writer.Flush();

                    // İçerik işletim sisteminin önbelleğinde beklerken dosya
                    // yerine konursa ani kapanmada geçici dosya da boş kalabilir.
                    akis.Flush(true);
                }

                YerineKoy(geciciYol, VeriDosyasi);
            }
            catch (Exception ex)
            {
                SessizceSil(geciciYol);
                MessageBox.Show($"Kayıt hatası: {ex.Message}", "Hata");
            }
        }

        // Geçici dosyayı asıl dosyanın yerine geçirir. File.Replace değiştirmeyi
        // tek adımda yapar: yarıda kalırsa hedef ya eski ya yeni içerikle kalır,
        // ikisinin arası bir hâl oluşmaz. Bir önceki sürüm de ".onceki" adıyla
        // saklanır; otomatik yedek günde bir alındığı için bu, gün içindeki son
        // sağlam hâle dönebilmeyi sağlar.
        //
        // Geçici dosya asıl dosyayla aynı klasörde, dolayısıyla aynı sürücüde
        // duruyor; farklı sürücüde olsaydı değiştirme atomik olmazdı.
        private static void YerineKoy(string geciciYol, string hedefYol)
        {
            if (File.Exists(hedefYol))
                File.Replace(geciciYol, hedefYol, hedefYol + ".onceki");
            else
                File.Move(geciciYol, hedefYol);
        }

        // Temizlik başarısız olursa sessiz geçilir: asıl iş zaten yapılamamış,
        // kullanıcıya ikinci bir hata göstermenin faydası yok.
        private static void SessizceSil(string yol)
        {
            try { if (File.Exists(yol)) File.Delete(yol); }
            catch { }
        }

        public static Ogrenci OgrenciEkle(string ad, string soyad, string telefon, string calgi, decimal? aylikUcret = null)
        {
            Veriler.SonOgrenciId++;
            var ogrenci = new Ogrenci
            {
                Id = Veriler.SonOgrenciId,
                Ad = ad,
                Soyad = soyad,
                Telefon = telefon,
                Calgı = calgi,
                AylikUcret = aylikUcret,
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
        public static bool OgrenciGuncelle(int ogrenciId, string ad, string soyad, string telefon, string calgi, decimal? aylikUcret = null)
        {
            var ogrenci = Veriler.Ogrenciler.FirstOrDefault(o => o.Id == ogrenciId);
            if (ogrenci == null) return false;

            ogrenci.Ad = ad;
            ogrenci.Soyad = soyad;
            ogrenci.Telefon = telefon;
            ogrenci.Calgı = calgi;
            ogrenci.AylikUcret = aylikUcret;
            Kaydet();
            return true;
        }
        // Aynı ad, soyad ve çalgı ile kayıtlı başka bir öğrenci var mı?
        // hariçTutulanId, düzenleme sırasında öğrencinin kendisini dışarıda
        // bırakmak için kullanılır.
        public static Ogrenci MukerrerBul(string ad, string soyad, string calgi, int haricTutulanId = 0)
        {
            return Veriler.Ogrenciler.FirstOrDefault(o =>
                o.Id != haricTutulanId &&
                string.Equals(o.Ad, ad, StringComparison.CurrentCultureIgnoreCase) &&
                string.Equals(o.Soyad, soyad, StringComparison.CurrentCultureIgnoreCase) &&
                string.Equals(o.Calgı, calgi, StringComparison.CurrentCultureIgnoreCase));
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

        public static void OdemeYap(int ogrenciId, int yil, int ay, decimal tutar = 0m)
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
            odeme.Tutar = tutar;
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
                odeme.Tutar = 0m;
                Kaydet();
            }
        }
        // Belirtilen ay için tahsil edilen tutar. Ödeme yoksa veya alan hiç
        // doldurulmamışsa 0 döner.
        public static decimal OdemeTutari(int ogrenciId, int yil, int ay)
        {
            var ogrenci = Veriler.Ogrenciler.FirstOrDefault(o => o.Id == ogrenciId);
            if (ogrenci == null) return 0m;
            var odeme = ogrenci.OdemeKayitlari.FirstOrDefault(o => o.Yil == yil && o.Ay == ay);
            return (odeme != null && odeme.OdemeYapildi) ? odeme.Tutar : 0m;
        }

        // Bir ayda tüm öğrencilerden tahsil edilen toplam
        public static decimal AylikToplamGelir(int yil, int ay)
        {
            return Veriler.Ogrenciler.Sum(o => OdemeTutari(o.Id, yil, ay));
        }

        public static bool OdemeYapildiMi(int ogrenciId, int yil, int ay)
        {
            var ogrenci = Veriler.Ogrenciler.FirstOrDefault(o => o.Id == ogrenciId);
            if (ogrenci == null) return false;
            var odeme = ogrenci.OdemeKayitlari.FirstOrDefault(o => o.Yil == yil && o.Ay == ay);
            return odeme?.OdemeYapildi ?? false;
        }

        // ---- Tek öğrencinin geçmişi ----

        // O aya ait bir ödeme kaydı açılmış mı? Kayıt yoksa ay hiç işlenmemiş
        // demektir; kayıt var ama OdemeYapildi false ise ödeme bekliyor
        // demektir. İkisi farklı durumlar, geçmiş ekranı bunları ayırıyor.
        public static bool OdemeKaydiVarMi(int ogrenciId, int yil, int ay)
        {
            var ogrenci = Veriler.Ogrenciler.FirstOrDefault(o => o.Id == ogrenciId);
            if (ogrenci == null) return false;
            return ogrenci.OdemeKayitlari.Any(o => o.Yil == yil && o.Ay == ay);
        }

        // Bir öğrenciden bugüne kadar tahsil edilen toplam
        public static decimal OgrenciToplamTahsilat(int ogrenciId)
        {
            var ogrenci = Veriler.Ogrenciler.FirstOrDefault(o => o.Id == ogrenciId);
            if (ogrenci == null) return 0m;
            return ogrenci.OdemeKayitlari.Where(o => o.OdemeYapildi).Sum(o => o.Tutar);
        }

        // Bir öğrencinin bugüne kadar aldığı toplam ders
        public static int OgrenciToplamDers(int ogrenciId)
        {
            var ogrenci = Veriler.Ogrenciler.FirstOrDefault(o => o.Id == ogrenciId);
            if (ogrenci == null) return 0;
            return ogrenci.DersKayitlari.Count(d => d.DersAlindi);
        }

        // Bellekteki son durumu diske yazar, sonra seçilen konuma kopyalar.
        // Hata olursa istisna yukarı taşınır; çağıran taraf kullanıcıyı bilgilendirir.
        public static void YedekAl(string hedefYol)
        {
            Kaydet();
            File.Copy(VeriDosyasi, hedefYol, true);
        }

        // Seçilen dosyayı önce doğrular; okunamıyorsa mevcut verilere hiç dokunmadan
        // istisna fırlatır. Doğrulama geçerse, mevcut veri kenara alınıp yenisi yüklenir.
        public static void GeriYukle(string kaynakYol)
        {
            Veriler dogrulanan;
            var serializer = new XmlSerializer(typeof(Veriler));
            using (var reader = new StreamReader(kaynakYol))
            {
                dogrulanan = (Veriler)serializer.Deserialize(reader);
            }

            if (File.Exists(VeriDosyasi))
            {
                string oncesi = Path.Combine(
                    Path.GetDirectoryName(VeriDosyasi),
                    $"veriler.geriyukleme-oncesi.{DateTime.Now:yyyyMMdd_HHmmss}.xml");
                File.Copy(VeriDosyasi, oncesi, true);
            }

            Veriler = dogrulanan;
            Kaydet();
        }

        // Uygulama açılışında günde bir kez çağrılır. Son saklanacakGun kadar
        // otomatik yedek tutulur, eskiler silinir.
        //
        // Bu metot hatayı bilerek yutar: yedekleme en iyi çaba ile yapılan,
        // veri kaybettirmeyen bir işlemdir ve başarısız olması uygulamanın
        // açılmasını engellememelidir. Elle yedeklemede (YedekAl) hatalar
        // kullanıcıya bildirilir.
        public static void OtomatikYedekAl(int saklanacakGun = 7)
        {
            try
            {
                if (!File.Exists(VeriDosyasi)) return;
                if (!Directory.Exists(YedekKlasoru)) Directory.CreateDirectory(YedekKlasoru);

                string hedef = Path.Combine(YedekKlasoru, $"otomatik_{DateTime.Now:yyyyMMdd}.xml");
                if (!File.Exists(hedef)) File.Copy(VeriDosyasi, hedef);

                var eskiler = new DirectoryInfo(YedekKlasoru)
                    .GetFiles("otomatik_*.xml")
                    .OrderByDescending(f => f.Name)
                    .Skip(saklanacakGun);
                foreach (var f in eskiler) f.Delete();
            }
            catch { }
        }

        // ---- Ücret tanımları ----

        // Çalgının varsayılan aylık ücreti; tanımlı değilse null.
        public static decimal? CalgiUcretiGetir(string calgi)
        {
            var kayit = Veriler.CalgiUcretleri.FirstOrDefault(c =>
                string.Equals(c.Calgi, calgi, StringComparison.CurrentCultureIgnoreCase));
            return kayit == null ? (decimal?)null : kayit.Ucret;
        }

        // null veya negatif verilirse tanım kaldırılır.
        public static void CalgiUcretiAyarla(string calgi, decimal? ucret)
        {
            var kayit = Veriler.CalgiUcretleri.FirstOrDefault(c =>
                string.Equals(c.Calgi, calgi, StringComparison.CurrentCultureIgnoreCase));

            if (ucret == null || ucret < 0m)
            {
                if (kayit != null) Veriler.CalgiUcretleri.Remove(kayit);
            }
            else if (kayit == null)
            {
                Veriler.CalgiUcretleri.Add(new CalgiUcreti { Calgi = calgi, Ucret = ucret.Value });
            }
            else
            {
                kayit.Ucret = ucret.Value;
            }
            Kaydet();
        }

        // Ödeme alınırken kutuya gelecek varsayılan tutar.
        // Öncelik: öğrenciye özel ücret > çalgının ücreti > 0 (tanımsız).
        public static decimal VarsayilanUcret(int ogrenciId)
        {
            var ogrenci = Veriler.Ogrenciler.FirstOrDefault(o => o.Id == ogrenciId);
            if (ogrenci == null) return 0m;
            if (ogrenci.AylikUcret.HasValue) return ogrenci.AylikUcret.Value;

            var calgiUcreti = CalgiUcretiGetir(ogrenci.Calgı);
            return calgiUcreti ?? 0m;
        }

        // Tutarları rapor ve arayüzde aynı biçimde göstermek için tek nokta
        public static string TutarYazi(decimal tutar)
        {
            return tutar.ToString("N2", new System.Globalization.CultureInfo("tr-TR")) + " ₺";
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
                sb.AppendLine($"{"Ad Soyad",-25} {"Tel",-13} {"H1",-4} {"H2",-4} {"H3",-4} {"H4",-4} {"Toplam",-8} {"Ödeme",-10} {"Tutar",10}");
                sb.AppendLine(new string('-', 84));

                foreach (var ogr in ogrenciler)
                {
                    bool h1 = HaftaDersAldiMi(ogr.Id, yil, ay, 1);
                    bool h2 = HaftaDersAldiMi(ogr.Id, yil, ay, 2);
                    bool h3 = HaftaDersAldiMi(ogr.Id, yil, ay, 3);
                    bool h4 = HaftaDersAldiMi(ogr.Id, yil, ay, 4);
                    int toplam = (h1 ? 1 : 0) + (h2 ? 1 : 0) + (h3 ? 1 : 0) + (h4 ? 1 : 0);
                    bool odeme = OdemeYapildiMi(ogr.Id, yil, ay);

                    decimal tutar = OdemeTutari(ogr.Id, yil, ay);
                    sb.AppendLine($"{ogr.TamAd,-25} {ogr.Telefon,-13} {(h1?"✓":"✗"),-4} {(h2?"✓":"✗"),-4} {(h3?"✓":"✗"),-4} {(h4?"✓":"✗"),-4} {toplam,-8} {(odeme ? "Ödendi" : "Ödenmedi"),-10} {TutarYazi(tutar),10}");
                }

                int toplamOgrenci = ogrenciler.Count;
                int odeyenler = ogrenciler.Count(o => OdemeYapildiMi(o.Id, yil, ay));
                sb.AppendLine();
                decimal grupTahsilat = ogrenciler.Sum(o => OdemeTutari(o.Id, yil, ay));
                sb.AppendLine($"  Toplam: {toplamOgrenci} öğrenci | Ödeme Yapan: {odeyenler} | Yapmayan: {toplamOgrenci - odeyenler} | Tahsilat: {TutarYazi(grupTahsilat)}");
                sb.AppendLine();
            }

            sb.AppendLine("========================================");
            int genelToplam = Veriler.Ogrenciler.Count;
            int genelOdeyen = Veriler.Ogrenciler.Count(o => OdemeYapildiMi(o.Id, yil, ay));
            sb.AppendLine($"GENEL TOPLAM: {genelToplam} öğrenci");
            sb.AppendLine($"ÖDEME YAPAN : {genelOdeyen} öğrenci");
            sb.AppendLine($"ÖDEME YAPMAYAN: {genelToplam - genelOdeyen} öğrenci");
            sb.AppendLine($"TOPLAM TAHSİLAT: {TutarYazi(AylikToplamGelir(yil, ay))}");
            sb.AppendLine("========================================");

            return sb.ToString();
        }

        // ---- Dönemsel rapor ----
        //
        // Aylık rapor tek bir ayın ayrıntısını verir: hangi öğrenci hangi hafta
        // ders aldı, ödedi mi. Dönemsel rapor bunun yerine toplamlara bakar:
        // ay ay tahsilat ve çalgı bazında dağılım. İkisi farklı soruları
        // cevapladığı için aylık rapor olduğu gibi duruyor.

        // Bir ayda ödeme yapan öğrenci sayısı
        public static int AylikOdeyenSayisi(int yil, int ay)
        {
            return Veriler.Ogrenciler.Count(o => OdemeYapildiMi(o.Id, yil, ay));
        }

        // Bir çalgıdan bir ayda tahsil edilen toplam
        public static decimal CalgiAylikGelir(string calgi, int yil, int ay)
        {
            return Veriler.Ogrenciler
                .Where(o => o.Calgı == calgi)
                .Sum(o => OdemeTutari(o.Id, yil, ay));
        }

        // Bir çalgıdan dönem boyunca tahsil edilen toplam
        public static decimal CalgiDonemGeliri(string calgi, Donem donem)
        {
            decimal toplam = 0m;
            foreach (var nokta in donem.Aylar())
                toplam += CalgiAylikGelir(calgi, nokta.Year, nokta.Month);
            return toplam;
        }

        // Bir çalgıda dönem boyunca kaç ödeme alındığı (öğrenci × ay adedi).
        // Tahsilatın kaç işlemden oluştuğunu gösterir; tek bir yüksek ödeme ile
        // çok sayıda küçük ödeme aynı toplamı verse de aynı şey değildir.
        public static int CalgiDonemOdemeAdedi(string calgi, Donem donem)
        {
            int adet = 0;
            foreach (var ogrenci in Veriler.Ogrenciler.Where(o => o.Calgı == calgi))
                foreach (var nokta in donem.Aylar())
                    if (OdemeYapildiMi(ogrenci.Id, nokta.Year, nokta.Month)) adet++;
            return adet;
        }

        public static decimal DonemToplamGelir(Donem donem)
        {
            decimal toplam = 0m;
            foreach (var nokta in donem.Aylar())
                toplam += AylikToplamGelir(nokta.Year, nokta.Month);
            return toplam;
        }

        public static List<string> Calgilar()
        {
            return Veriler.Ogrenciler
                .Select(o => o.Calgı)
                .Distinct()
                .OrderBy(c => c, StringComparer.CurrentCulture)
                .ToList();
        }

        // Bir tutarın dönem toplamı içindeki payı. Dönem toplamı sıfırken
        // bölme yapılmaz; hiç tahsilat yokken "%0,0" yazmak da yanıltıcı
        // olmadığı için o değer dönülür.
        public static string PayYazi(decimal tutar, decimal toplam)
        {
            if (toplam <= 0m) return "—";
            double oran = (double)(tutar / toplam) * 100.0;
            return oran.ToString("N1", new System.Globalization.CultureInfo("tr-TR")) + "%";
        }

        public static string DonemRaporuOlustur(Donem donem)
        {
            var sb = new System.Text.StringBuilder();
            decimal donemToplam = DonemToplamGelir(donem);

            sb.AppendLine("================================================================");
            sb.AppendLine("   RORA SANAT MERKEZİ");
            sb.AppendLine($"   {donem.Baslik.ToUpper(new System.Globalization.CultureInfo("tr-TR"))} DÖNEM RAPORU");
            sb.AppendLine($"   Rapor Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}");
            sb.AppendLine("================================================================");
            sb.AppendLine();

            // ---- Ay ay tahsilat ----
            sb.AppendLine("--- AY AY TAHSİLAT ---");
            sb.AppendLine($"{"Ay",-18} {"Ödeme Yapan",12} {"Tahsilat",16} {"Pay",8}");
            sb.AppendLine(new string('-', 58));

            DateTime enIyiAy = new DateTime(donem.BasYil, donem.BasAy, 1);
            decimal enIyiTutar = -1m;

            foreach (var nokta in donem.Aylar())
            {
                decimal tutar = AylikToplamGelir(nokta.Year, nokta.Month);
                int odeyen = AylikOdeyenSayisi(nokta.Year, nokta.Month);

                if (tutar > enIyiTutar) { enIyiTutar = tutar; enIyiAy = nokta; }

                sb.AppendLine($"{Donem.AyYil(nokta.Year, nokta.Month),-18} {odeyen,12} {TutarYazi(tutar),16} {PayYazi(tutar, donemToplam),8}");
            }

            sb.AppendLine(new string('-', 58));
            sb.AppendLine($"{"TOPLAM",-18} {"",12} {TutarYazi(donemToplam),16} {"",8}");
            sb.AppendLine();

            // ---- Çalgı bazında ----
            sb.AppendLine("--- ÇALGI BAZINDA TAHSİLAT ---");
            sb.AppendLine($"{"Çalgı",-20} {"Öğrenci",8} {"Ödeme",8} {"Tahsilat",16} {"Pay",8}");
            sb.AppendLine(new string('-', 64));

            var calgilar = Calgilar();
            if (calgilar.Count == 0)
            {
                sb.AppendLine("(kayıtlı öğrenci yok)");
            }
            else
            {
                foreach (var calgi in calgilar)
                {
                    int ogrenciSayisi = Veriler.Ogrenciler.Count(o => o.Calgı == calgi);
                    int odemeAdedi = CalgiDonemOdemeAdedi(calgi, donem);
                    decimal tutar = CalgiDonemGeliri(calgi, donem);

                    sb.AppendLine($"{calgi,-20} {ogrenciSayisi,8} {odemeAdedi,8} {TutarYazi(tutar),16} {PayYazi(tutar, donemToplam),8}");
                }
            }

            sb.AppendLine(new string('-', 64));
            sb.AppendLine($"{"TOPLAM",-20} {Veriler.Ogrenciler.Count,8} {"",8} {TutarYazi(donemToplam),16} {"",8}");
            sb.AppendLine();

            // ---- Özet ----
            sb.AppendLine("================================================================");
            sb.AppendLine($"DÖNEM          : {donem.Baslik}  ({donem.AySayisi} ay)");
            sb.AppendLine($"TOPLAM TAHSİLAT: {TutarYazi(donemToplam)}");
            sb.AppendLine($"AYLIK ORTALAMA : {TutarYazi(donem.AySayisi > 0 ? donemToplam / donem.AySayisi : 0m)}");
            if (enIyiTutar > 0m)
                sb.AppendLine($"EN YÜKSEK AY   : {Donem.AyYil(enIyiAy.Year, enIyiAy.Month)}  ({TutarYazi(enIyiTutar)})");
            sb.AppendLine($"KAYITLI ÖĞRENCİ: {Veriler.Ogrenciler.Count}");
            sb.AppendLine("================================================================");

            return sb.ToString();
        }
    }
}
