using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;

namespace RORAMuzikMerkezi
{
    // PDF raporlarının ortak iskeleti.
    //
    // Dış kütüphane kullanılmaz. Windows'un yerleşik "Microsoft Print to PDF"
    // yazıcısına System.Drawing.Printing ile çizim yapılır. Böylece proje
    // bağımlılıksız kalır ve dağıtım tek klasör kopyalamaktan ibaret olmaya
    // devam eder. Bunun bilinen sınırı, bu yazıcının kaldırıldığı makinelerde
    // PDF üretilememesidir; o durumda çağıran tarafa açık bir hata bildirilir.
    //
    // Bu sınıf yazdırma kurulumunu, logoyu, sayfa başlığını, dipnotu ve
    // sayfalamayı yürütür. Sayfaya ne çizileceğini alt sınıflar söyler. İkinci
    // bir rapor türü (dönemsel rapor) eklenirken bu ayrım yapıldı: aylık
    // raporun düzeni kendine ait, ama sayfa çerçevesi ikisinde de aynı olmalı,
    // biri değişince diğeri geride kalmamalı.
    public abstract class PdfRapor
    {
        public const string YaziciAdi = "Microsoft Print to PDF";

        protected const int SatirYuksekligi = 22;
        protected const int KenarBosluk = 60;

        // A4 genişliği eksi kenar boşlukları, 1/100 inç. Alt sınıfların sütun
        // genişlikleri toplamı bu değeri aşmamalı.
        protected const int KullanilabilirGenislik = 707;

        // Uygulamanın kendi paletiyle uyumlu: lacivert SplashForm'un zemininden,
        // altın ise logodaki madalyon tonundan alındı.
        protected static readonly Color Lacivert = Color.FromArgb(35, 45, 100);
        protected static readonly Color Altin = Color.FromArgb(190, 152, 106);
        protected static readonly Color Metin = Color.FromArgb(30, 30, 30);
        protected static readonly Color SolukMetin = Color.FromArgb(110, 110, 110);
        protected static readonly Color ZebraZemin = Color.FromArgb(245, 247, 252);
        protected static readonly Color BolumZemin = Color.FromArgb(232, 236, 247);
        protected static readonly Color CizgiRengi = Color.FromArgb(205, 212, 230);
        protected static readonly Color OdendiRengi = Color.FromArgb(30, 130, 75);
        protected static readonly Color OdenmediRengi = Color.FromArgb(180, 60, 60);

        protected static CultureInfo TrKultur { get { return new CultureInfo("tr-TR"); } }

        private Image logo;   // kırpılmış, yuvarlatılmış, basıma hazır hâli
        private int sayfaNo;

        // ---- Alt sınıfın dolduracakları ----

        // Yazdırma kuyruğunda görünen belge adı
        protected abstract string BelgeAdi { get; }

        // İlk sayfada başlığın altındaki satır, ör. "MART 2026 AYI ÖZET RAPORU"
        protected abstract string BaslikAltSatiri { get; }

        // Sonraki sayfaların üst bilgisi, ör. "Mart 2026 özet raporu (devam)"
        protected abstract string DevamMetni { get; }

        // Çizim alanının genişliği; başlık şeridi ve dipnot çizgisi buna uyar.
        protected abstract int ToplamGenislik { get; }

        // Sayfalama, alt sınıfın öğe listesini bu üçlü üzerinden yürütür.
        protected abstract void BasaSar();
        protected abstract bool KalanOgeVar { get; }
        protected abstract int SiradakiGerekenYer { get; }

        // Sıradaki öğeyi çizer ve çizimden sonraki y değerini döndürür.
        protected abstract int SiradakiOgeyiCiz(Graphics g, Kaynaklar k, int sol, int y);

        // ---- Ortak yürütme ----

        public static bool YaziciKullanilabilirMi()
        {
            foreach (string ad in PrinterSettings.InstalledPrinters)
            {
                if (string.Equals(ad, YaziciAdi, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        // Hata durumunda istisna yukarı taşınır; çağıran taraf kullanıcıyı bilgilendirir.
        public void Yazdir(string hedefYol)
        {
            if (!YaziciKullanilabilirMi())
                throw new InvalidOperationException($"\"{YaziciAdi}\" yazıcısı bu bilgisayarda bulunamadı. PDF üretimi bu yazıcıya bağlıdır.");

            if (File.Exists(hedefYol)) File.Delete(hedefYol);

            BasaSar();
            sayfaNo = 0;

            LogoyuHazirla();

            try
            {
                using (var belge = new PrintDocument())
                {
                    belge.DocumentName = BelgeAdi;
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
                if (logo != null) { logo.Dispose(); logo = null; }
            }

            if (!TamamlanmayiBekle(hedefYol, TimeSpan.FromSeconds(60)))
                throw new IOException("PDF dosyası oluşturulamadı. Yazıcı çıktıyı yazamamış olabilir.");
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

            using (var k = new Kaynaklar())
            {
                y = sayfaNo == 1
                    ? IlkSayfaBasligiCiz(g, sol, y, k)
                    : DevamBasligiCiz(g, sol, y, k);

                bool sayfayaBirSeyCizildi = false;

                while (KalanOgeVar)
                {
                    // Sayfaya hiçbir şey sığmadıysa sıradaki öğe yine de çizilir.
                    // Aksi hâlde tek bir öğe sayfadan uzun olduğunda sayfalama
                    // ilerlemez ve yazdırma sonsuza kadar boş sayfa üretir.
                    if (y + SiradakiGerekenYer > altSinir && sayfayaBirSeyCizildi) break;

                    y = SiradakiOgeyiCiz(g, k, sol, y);
                    sayfayaBirSeyCizildi = true;
                }

                DipnotCiz(g, e, sol, k);
            }

            e.HasMorePages = KalanOgeVar;
        }

        private int IlkSayfaBasligiCiz(Graphics g, int sol, int y, Kaynaklar k)
        {
            const int LogoYuksekligi = 78;
            int metinSol = sol;

            if (logo != null)
            {
                int logoGenislik = LogoGenisligi(LogoYuksekligi);
                LogoCiz(g, sol, y, logoGenislik, LogoYuksekligi);
                metinSol = sol + logoGenislik + 22;
            }

            g.DrawString("RORA SANAT MERKEZİ", k.Baslik, k.Lacivert, metinSol, y + 4);
            g.DrawString(BaslikAltSatiri, k.AltBaslik, k.Lacivert, metinSol, y + 34);
            g.DrawString($"Rapor tarihi: {DateTime.Now.ToString("dd.MM.yyyy HH:mm", TrKultur)}",
                         k.AltBaslik, k.Gri, metinSol, y + 52);

            y += LogoYuksekligi + 12;
            g.FillRectangle(k.Altin, sol, y, ToplamGenislik, 3);
            g.FillRectangle(k.Lacivert, sol, y + 5, ToplamGenislik, 1);
            return y + 20;
        }

        private int DevamBasligiCiz(Graphics g, int sol, int y, Kaynaklar k)
        {
            const int LogoYuksekligi = 26;
            int metinSol = sol;

            if (logo != null)
            {
                int logoGenislik = LogoGenisligi(LogoYuksekligi);
                LogoCiz(g, sol, y - 4, logoGenislik, LogoYuksekligi);
                metinSol = sol + logoGenislik + 10;
            }

            g.DrawString($"RORA SANAT MERKEZİ — {DevamMetni}", k.UstBilgi, k.Gri, metinSol, y + 3);
            y += LogoYuksekligi + 2;
            g.DrawLine(k.Cizgi, sol, y, sol + ToplamGenislik, y);
            return y + 12;
        }

        private void DipnotCiz(Graphics g, PrintPageEventArgs e, int sol, Kaynaklar k)
        {
            int dipY = e.MarginBounds.Bottom + 6;
            g.DrawLine(k.Cizgi, sol, dipY, sol + ToplamGenislik, dipY);

            g.DrawString("RORA Sanat Merkezi", k.Dipnot, k.Gri, sol, dipY + 6);

            string dipnot = $"Sayfa {sayfaNo}";
            var olcu = g.MeasureString(dipnot, k.Dipnot);
            g.DrawString(dipnot, k.Dipnot, k.Gri, sol + (ToplamGenislik - olcu.Width) / 2, dipY + 6);
        }

        // ---- Logo ----

        // Logo sayfaya çizilmeden önce bellekte hazırlanır: boş çerçevesi
        // kırpılır, madalyonun dışı beyaza boyanır. Sayfaya çizilen şey böylece
        // sıradan bir dörtgen resim olur.
        //
        // Bu dolambaç şundan: kırpma bölgesi (SetClip) ve kaynak dörtgen alan
        // isteyen DrawImage, yazdırma sürücüsünde bitmap üzerindekinden farklı
        // davranıyor. Aynı kod bitmap ve metafile üzerinde doğru çizerken PDF
        // çıktısında logoyu büyütülmüş ve kaydırılmış basıyordu. Sayfaya sade
        // bir resim göndermek bu farkı tümüyle ortadan kaldırıyor.
        //
        // Madalyonun dışı saydam değil beyaz bırakılıyor; sayfa zemini zaten
        // beyaz ve yazdırma sürücülerinin saydamlığı güvenilir biçimde
        // basmadığı biliniyor.
        private void LogoyuHazirla()
        {
            // Rapor rora.jpeg kullanır, splash ekranındaki rora1.png'yi değil:
            // o dosyada madalyon dört yanından hafifçe kırpılmış, büyük
            // basıldığında kenarları düz görünüyor. rora.jpeg'de madalyon tam
            // ve çözünürlüğü iki katı. İkisi de yoksa rapor logosuz üretilir.
            using (var kaynak = Varliklar.Resim("rora.jpeg") ?? Varliklar.Resim("rora1.png"))
            {
                if (kaynak == null) return;

                Rectangle icerik = DoluAlan(kaynak);
                var hazir = new Bitmap(icerik.Width, icerik.Height);

                using (var gl = Graphics.FromImage(hazir))
                {
                    gl.Clear(Color.White);
                    gl.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    gl.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    using (var yol = new System.Drawing.Drawing2D.GraphicsPath())
                    {
                        // Madalyon tam daire değil; çevresindeki koyu gölge ince
                        // bir halka olarak kalmasın diye biraz içeri çekiliyor.
                        var daire = new RectangleF(0, 0, icerik.Width, icerik.Height);
                        daire.Inflate(-icerik.Width * 0.015f, -icerik.Height * 0.015f);
                        yol.AddEllipse(daire);
                        gl.SetClip(yol);

                        gl.DrawImage(kaynak, new Rectangle(0, 0, icerik.Width, icerik.Height),
                                     icerik.X, icerik.Y, icerik.Width, icerik.Height,
                                     GraphicsUnit.Pixel);
                    }
                }

                logo = hazir;
            }
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

        private int LogoGenisligi(int yukseklik)
        {
            return (int)Math.Round(yukseklik * ((double)logo.Width / logo.Height));
        }

        // Logo LogoyuHazirla'da kırpılıp yuvarlatılmış hâlde bekliyor; sayfaya
        // yalnızca ölçeklenerek çiziliyor.
        private void LogoCiz(Graphics g, int x, int y, int genislik, int yukseklik)
        {
            g.DrawImage(logo, new Rectangle(x, y, genislik, yukseklik));
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

        // Sayfa çizimi boyunca kullanılan yazı tipleri ve fırçalar. Her sayfada
        // bir kez oluşturulup sayfa bitiminde bırakılır; alt sınıflar da aynı
        // kümeyi kullanır, böylece iki rapor türü arasında punto ve ton farkı
        // oluşmuyor.
        protected sealed class Kaynaklar : IDisposable
        {
            public readonly Font Baslik = new Font("Segoe UI", 16, FontStyle.Bold);
            public readonly Font AltBaslik = new Font("Segoe UI", 10);
            public readonly Font UstBilgi = new Font("Segoe UI", 8.5f);
            public readonly Font BolumBasligi = new Font("Segoe UI", 11, FontStyle.Bold);
            public readonly Font TabloBasligi = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            public readonly Font Satir = new Font("Segoe UI", 9);
            public readonly Font SatirKalin = new Font("Segoe UI", 9, FontStyle.Bold);
            public readonly Font Ozet = new Font("Segoe UI", 9, FontStyle.Italic);
            public readonly Font Genel = new Font("Segoe UI", 10, FontStyle.Bold);
            public readonly Font Dipnot = new Font("Segoe UI", 8);

            public readonly Brush Lacivert = new SolidBrush(PdfRapor.Lacivert);
            public readonly Brush Altin = new SolidBrush(PdfRapor.Altin);
            public readonly Brush Siyah = new SolidBrush(PdfRapor.Metin);
            public readonly Brush Gri = new SolidBrush(PdfRapor.SolukMetin);
            public readonly Brush Beyaz = new SolidBrush(Color.White);
            public readonly Brush Odendi = new SolidBrush(PdfRapor.OdendiRengi);
            public readonly Brush Odenmedi = new SolidBrush(PdfRapor.OdenmediRengi);
            public readonly Brush Zebra = new SolidBrush(PdfRapor.ZebraZemin);
            public readonly Brush Bolum = new SolidBrush(PdfRapor.BolumZemin);
            public readonly Pen Cizgi = new Pen(PdfRapor.CizgiRengi);
            public readonly Pen LacivertKalem = new Pen(PdfRapor.Lacivert);

            public void Dispose()
            {
                Baslik.Dispose(); AltBaslik.Dispose(); UstBilgi.Dispose(); BolumBasligi.Dispose();
                TabloBasligi.Dispose(); Satir.Dispose(); SatirKalin.Dispose(); Ozet.Dispose();
                Genel.Dispose(); Dipnot.Dispose();

                Lacivert.Dispose(); Altin.Dispose(); Siyah.Dispose(); Gri.Dispose(); Beyaz.Dispose();
                Odendi.Dispose(); Odenmedi.Dispose(); Zebra.Dispose(); Bolum.Dispose();
                Cizgi.Dispose(); LacivertKalem.Dispose();
            }
        }
    }
}
