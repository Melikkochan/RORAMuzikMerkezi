using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace RORAMuzikMerkezi
{
    public class MainForm : Form
    {
        private TabControl tabControl;
        private MenuStrip menuStrip;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblDurum;
        private Dictionary<string, CalgiSekmesi> calgiSekmeler = new Dictionary<string, CalgiSekmesi>();
        private TabPage tabGenel;
        private Panel toolPanel;
        private TextBox txtGenelAra;
        private Label lblGenelAra;
        private Label lblLogo;
        private ListBox lstAramaSonuclari;

        // Arama kutusu bu düğmenin sağından başlıyor. Sabit bir piksel değeri
        // yerine düğmenin gerçek sınırı kullanılıyor: düğme ölçekle birlikte
        // büyüyünce arama kutusu da kendiliğinden kayıyor.
        private Button btnYenile;
        private readonly List<Ogrenci> aramaSonuclari = new List<Ogrenci>();

        public MainForm()
        {
            VeriYoneticisi.Yukle();
            VeriYoneticisi.OtomatikYedekAl();
            VeriYoneticisi.AylikOdemeKayitlariniOlustur();
            InitializeComponent();
            SekmeleriBuild();
            OdemeHatirlatici();
        }

        private void InitializeComponent()
        {
            // Yerleşim 96 DPI'a göre yazıldı; ölçekli ekranlarda
            // konum ve boyutları WinForms bu ayarla kendisi büyütür.
            this.AutoScaleDimensions = new System.Drawing.SizeF(Olcek.TasarimDpi, Olcek.TasarimDpi);
            this.AutoScaleMode = AutoScaleMode.Dpi;

            this.Text = "🎵 RORA Sanat Merkezi - Öğrenci Takip Sistemi";
            this.Size = new Size(1000, 680);
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(800, 550);
            this.BackColor = Renkler.Zemin;

            // İkon simgesi için form ikonu (bulunamazsa varsayılan ikonla açılır)
            var ikon = Varliklar.Ikon("favicon.ico");
            if (ikon != null) this.Icon = ikon;

            // Menu
            menuStrip = new MenuStrip { BackColor = Renkler.Lacivert, ForeColor = Renkler.MetinTers };
            menuStrip.Renderer = new ToolStripProfessionalRenderer(new RoraMenuColors());

            var menuOgrenci = new ToolStripMenuItem("👤 Öğrenci") { ForeColor = Color.White, Font = new Font("Segoe UI", 10) };
            var menuYeniOgrenci = new ToolStripMenuItem("➕ Yeni Öğrenci Ekle", null, (s, e) => YeniOgrenciEkle());
            var menuAyarlar = new ToolStripMenuItem("🔄 Sekmeleri Yenile", null, (s, e) => TumSekmeletiYenile());
            menuOgrenci.DropDownItems.AddRange(new ToolStripItem[] { menuYeniOgrenci, new ToolStripSeparator(), menuAyarlar });

            var menuRapor = new ToolStripMenuItem("📊 Rapor") { ForeColor = Color.White, Font = new Font("Segoe UI", 10) };
            var menuOzetRapor = new ToolStripMenuItem("📄 Bu Ay Özet Raporu", null, (s, e) => OzetRaporHazirla(DateTime.Now.Year, DateTime.Now.Month));
            var menuOncekiAy = new ToolStripMenuItem("📅 Önceki Ay Raporu", null, (s, e) => {
                var gecen = DateTime.Now.AddMonths(-1);
                OzetRaporHazirla(gecen.Year, gecen.Month);
            });
            var menuPdfBuAy = new ToolStripMenuItem("📕 Bu Ay Raporu (PDF)", null, (s, e) => PdfRaporHazirla(DateTime.Now.Year, DateTime.Now.Month));
            var menuPdfOncekiAy = new ToolStripMenuItem("📕 Önceki Ay Raporu (PDF)", null, (s, e) => {
                var gecen = DateTime.Now.AddMonths(-1);
                PdfRaporHazirla(gecen.Year, gecen.Month);
            });
            var menuBaskaAy = new ToolStripMenuItem("🗓️ Başka Ay…", null, (s, e) => BaskaAyRaporu());
            menuRapor.DropDownItems.AddRange(new ToolStripItem[] {
                menuOzetRapor, menuOncekiAy, new ToolStripSeparator(), menuPdfBuAy, menuPdfOncekiAy,
                new ToolStripSeparator(), menuBaskaAy });

            var menuHakkinda = new ToolStripMenuItem("ℹ️ Hakkında") { ForeColor = Color.White, Font = new Font("Segoe UI", 10) };
            menuHakkinda.Click += (s, e) => MessageBox.Show(
                "RORA Sanat Merkezi\nÖğrenci Takip Sistemi v1.0\n\nTüm hakları saklıdır.\nMelik KOÇHAN tarafından geliştirildi",
                "Hakkında", MessageBoxButtons.OK, MessageBoxIcon.Information);

            var menuVeri = new ToolStripMenuItem("💾 Veri") { ForeColor = Color.White, Font = new Font("Segoe UI", 10) };
            var menuYedekAl = new ToolStripMenuItem("📤 Yedek Al...", null, (s, e) => YedekAl());
            var menuGeriYukle = new ToolStripMenuItem("📥 Yedekten Geri Yükle...", null, (s, e) => YedektenGeriYukle());
            var menuKlasorAc = new ToolStripMenuItem("📂 Veri Klasörünü Aç", null, (s, e) => VeriKlasorunuAc());
            menuVeri.DropDownItems.AddRange(new ToolStripItem[] { menuYedekAl, menuGeriYukle, new ToolStripSeparator(), menuKlasorAc });

            var menuUcret = new ToolStripMenuItem("💵 Ücretler") { ForeColor = Color.White, Font = new Font("Segoe UI", 10) };
            var menuCalgiUcretleri = new ToolStripMenuItem("🎵 Çalgı Ücret Tanımları...", null, (s, e) => CalgiUcretleriniAc());
            menuUcret.DropDownItems.Add(menuCalgiUcretleri);

            menuStrip.Items.AddRange(new ToolStripItem[] { menuOgrenci, menuRapor, menuVeri, menuUcret, menuHakkinda });

            // Ana buton çubuğu
            toolPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Renkler.Lacivert,
                Padding = new Padding(10, 8, 10, 8)
            };

            var btnYeniOgrenci = new Button
            {
                Text = "➕ Yeni Öğrenci",
                Location = new Point(10, 9),
                Size = new Size(145, 34),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Renkler.Altin,
                ForeColor = Renkler.Lacivert,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnYeniOgrenci.FlatAppearance.BorderSize = 0;
            btnYeniOgrenci.Click += (s, e) => YeniOgrenciEkle();

            var btnRapor = new Button
            {
                Text = "📊 Özet Rapor",
                Location = new Point(165, 9),
                Size = new Size(145, 34),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Renkler.LacivertOrta,
                ForeColor = Renkler.MetinTers,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRapor.FlatAppearance.BorderSize = 0;
            btnRapor.Click += (s, e) => OzetRaporHazirla(DateTime.Now.Year, DateTime.Now.Month);

            btnYenile = new Button
            {
                Text = "🔄 Yenile",
                Location = new Point(320, 9),
                Size = new Size(110, 34),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Renkler.LacivertOrta,
                ForeColor = Renkler.MetinTers,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnYenile.FlatAppearance.BorderSize = 0;
            btnYenile.Click += (s, e) => TumSekmeletiYenile();

            lblLogo = new Label
            {
                Text = "🎵 RORA SANAT MERKEZİ",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Right,
                Width = 280,
                Padding = new Padding(0, 0, 10, 0)
            };

            lblGenelAra = new Label
            {
                Text = "🔍",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12),
                Location = new Point(445, 14),
                Size = new Size(28, 26),
                TextAlign = ContentAlignment.MiddleCenter
            };

            txtGenelAra = new TextBox
            {
                Location = new Point(475, 13),
                Size = new Size(250, 26),
                Font = new Font("Segoe UI", 10)
            };
            txtGenelAra.TextChanged += (s, e) => GenelAramaYap();
            txtGenelAra.KeyDown += TxtGenelAra_KeyDown;
            txtGenelAra.Leave += (s, e) => { if (!lstAramaSonuclari.Focused) lstAramaSonuclari.Visible = false; };

            // Sonuç listesi forma eklenir; sekmelerin üstünde açılır bir liste gibi davranır
            lstAramaSonuclari = new ListBox
            {
                Visible = false,
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle,
                Width = 420,
                Height = 160,
                BackColor = Renkler.Yuzey
            };
            lstAramaSonuclari.MouseClick += (s, e) => SecilenSonucaGit();
            lstAramaSonuclari.KeyDown += LstAramaSonuclari_KeyDown;
            lstAramaSonuclari.Leave += (s, e) => lstAramaSonuclari.Visible = false;

            toolPanel.Controls.AddRange(new Control[] { btnYeniOgrenci, btnRapor, btnYenile, lblGenelAra, txtGenelAra, lblLogo });
            toolPanel.Resize += (s, e) => AramaKutusunuKonumlandir();
            AramaKutusunuKonumlandir();

            // TabControl
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Padding = new Point(12, 5)
            };

            // Status bar
            statusStrip = new StatusStrip { BackColor = Color.FromArgb(230, 230, 240) };
            lblDurum = new ToolStripStatusLabel($"RORA Sanat Merkezi  |  {DateTime.Now:dd MMMM yyyy}  |  Toplam Öğrenci: {VeriYoneticisi.Veriler.Ogrenciler.Count}");

            var lblGelistirici = new ToolStripStatusLabel("Melik KOÇHAN tarafından geliştirildi")
            {
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 100, 140)
            };
            statusStrip.Items.Add(lblGelistirici);
            lblDurum.Font = new Font("Segoe UI", 9);
            statusStrip.Items.Add(lblDurum);

            this.MainMenuStrip = menuStrip;
            this.Controls.Add(lstAramaSonuclari);
            this.Controls.Add(tabControl);
            this.Controls.Add(toolPanel);
            this.Controls.Add(menuStrip);
            this.Controls.Add(statusStrip);
        }

        private void SekmeleriBuild()
        {
            tabControl.TabPages.Clear();
            calgiSekmeler.Clear();

            // Genel özet sekmesi
            tabGenel = new TabPage("📋 Genel Özet");
            tabGenel.BackColor = Color.FromArgb(248, 248, 252);
            OzetPanelOlustur(tabGenel);
            tabControl.TabPages.Add(tabGenel);

            // Her çalgı için sekme
            var mevcutCalgilar = VeriYoneticisi.Veriler.Ogrenciler
                .Select(o => o.Calgı).Distinct().OrderBy(c => c).ToList();

            // Boş çalgılara da sekme ekle (OgrenciKayitForm'dan gelen liste)
            var tumCalgilar = OgrenciKayitForm.Calgilar.Union(mevcutCalgilar).OrderBy(c => c).ToList();

            foreach (var calgi in tumCalgilar)
            {
                var sayı = VeriYoneticisi.CalginaGoreOgrenciler(calgi).Count;
                string tabBaslik = $"🎵 {calgi}";
                if (sayı > 0) tabBaslik += $" ({sayı})";

                var tab = new TabPage(tabBaslik)
                {
                    BackColor = Color.White
                };

                var sekme = new CalgiSekmesi(calgi);
                // Çalgı değişikliğinde sekmelerin yeniden kurulması gerekir.
                // BeginInvoke ile ertelenir; aksi hâlde sekme, kendi olayı
                // işlenirken yok edilmiş olurdu.
                sekme.OgrenciGuncellendi += (s, e) => this.BeginInvoke(new Action(TumSekmeletiYenile));
                tab.Controls.Add(sekme);
                calgiSekmeler[calgi] = sekme;
                tabControl.TabPages.Add(tab);
            }

            GuncelleStatusBar();
        }

        private void OzetPanelOlustur(TabPage tab)
        {
            // AutoScroll, ölçekli ekranlarda içerik pencereye sığmadığında
            // kaydırma çubuğu çıkarır. Olmazsa alttaki liste sessizce kırpılır:
            // %150 ölçekte, küçük ekranlı bir dizüstüde tam olarak bu oluyor.
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), AutoScroll = true };

            var lblBaslik = new Label
            {
                Text = "RORA Sanat Merkezi - Genel Durum",
                Font = Yazilar.Baslik,
                ForeColor = Renkler.Lacivert,
                Location = new Point(20, 20),
                Size = new Size(500, 40),
                AutoSize = false
            };

            var lblTarih = new Label
            {
                Text = $"📅 {DateTime.Now:dd MMMM yyyy, dddd}",
                Font = Yazilar.Govde,
                ForeColor = Renkler.MetinSolgun,
                Location = new Point(20, 65),
                Size = new Size(400, 28)
            };

            // Özet bilgi kutuları
            int toplamOgrenci = VeriYoneticisi.Veriler.Ogrenciler.Count;
            int odeyenler = VeriYoneticisi.Veriler.Ogrenciler.Count(o =>
                VeriYoneticisi.OdemeYapildiMi(o.Id, DateTime.Now.Year, DateTime.Now.Month));
            int buHaftaDers = VeriYoneticisi.Veriler.Ogrenciler.Count(o =>
                VeriYoneticisi.BuHaftaDersAldiMi(o.Id));

            // Üç kutu eskiden aynı görsel ağırlıktaydı; hepsi birden bağırınca
            // hangisinin önemli olduğu anlaşılmıyordu. Toplam öğrenci sayısı
            // asıl bağlam olduğu için dolu, diğer ikisi ise beyaz zeminde
            // renkli bir şeritle veriliyor.
            var kutular = new[]
            {
                ("👥", "Toplam Öğrenci", toplamOgrenci.ToString(), Renkler.Lacivert, true),
                ("💰", "Bu Ay Ödeme Yapan", $"{odeyenler}/{toplamOgrenci}", Renkler.Olumlu, false),
                ("📚", "Bu Hafta Ders Alan", $"{buHaftaDers}/{toplamOgrenci}", Renkler.Altin, false),
            };

            int xPos = 20;
            foreach (var (ikon, baslik, deger, renk, dolu) in kutular)
            {
                var kutu = new Panel
                {
                    Location = new Point(xPos, 105),
                    Size = new Size(190, 120),
                    BackColor = dolu ? renk : Renkler.Yuzey,
                    BorderStyle = BorderStyle.None
                };

                if (!dolu)
                {
                    // Soldaki renkli şerit: kutuyu boyamadan konuyu belli eder
                    kutu.Controls.Add(new Panel
                    {
                        BackColor = renk,
                        Location = new Point(0, 0),
                        Size = new Size(4, 120)
                    });
                    kutu.Paint += (s, e) => ControlPaint.DrawBorder(
                        e.Graphics, ((Control)s).ClientRectangle, Renkler.Cizgi, ButtonBorderStyle.Solid);
                }

                kutu.Controls.Add(new Label
                {
                    Text = ikon,
                    Font = new Font("Segoe UI", 18),
                    ForeColor = dolu ? Renkler.MetinTers : renk,
                    Location = new Point(14, 8),
                    Size = new Size(40, 36),
                    TextAlign = ContentAlignment.MiddleLeft
                });

                kutu.Controls.Add(new Label
                {
                    Text = deger,
                    Font = Yazilar.Sayi,
                    ForeColor = dolu ? Renkler.MetinTers : Renkler.Metin,
                    // Yükseklikler yazının ölçülen boyuna göre veriliyor.
                    // Daha dar verilirse sayının altı sessizce kesiliyor;
                    // yerleşim testi bunu ölçerek denetliyor.
                    Location = new Point(12, 44),
                    Size = new Size(166, 48),
                    TextAlign = ContentAlignment.MiddleLeft
                });

                kutu.Controls.Add(new Label
                {
                    Text = baslik,
                    Font = Yazilar.Kucuk,
                    ForeColor = dolu ? Color.FromArgb(205, 212, 235) : Renkler.MetinSolgun,
                    Location = new Point(14, 94),
                    Size = new Size(170, 20),
                    TextAlign = ContentAlignment.MiddleLeft
                });

                panel.Controls.Add(kutu);
                xPos += 210;
            }

            // Kısa öğrenci listesi
            var lblKisaListe = new Label
            {
                Text = "Bu ay ödeme yapmayan öğrenciler:",
                Font = Yazilar.GovdeKalin,
                ForeColor = Renkler.Metin,
                Location = new Point(20, 235),
                Size = new Size(400, 25)
            };

            var lstOdemeyenler = new ListBox
            {
                Location = new Point(20, 265),
                Size = new Size(600, 200),
                Font = Yazilar.Govde,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Renkler.Yuzey,
                ForeColor = Renkler.Metin
            };

            var odemeyenler = VeriYoneticisi.Veriler.Ogrenciler
                .Where(o => !VeriYoneticisi.OdemeYapildiMi(o.Id, DateTime.Now.Year, DateTime.Now.Month))
                .ToList();

            foreach (var ogr in odemeyenler)
                lstOdemeyenler.Items.Add($"❌  {ogr.TamAd} — {ogr.Calgı} — {ogr.Telefon}");

            if (odemeyenler.Count == 0)
                lstOdemeyenler.Items.Add("✅ Tüm öğrenciler bu ay ödeme yapmış!");

            panel.Controls.AddRange(new Control[] { lblBaslik, lblTarih, lblKisaListe, lstOdemeyenler });
            tab.Controls.Add(panel);
        }

        private void YeniOgrenciEkle()
        {
            using (var form = new OgrenciKayitForm())
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    SekmeleriBuild(); // Yeni öğrenci varsa sekmeleri yenile
                }
            }
        }

        private void TumSekmeletiYenile()
        {
            int secilenIndex = tabControl.SelectedIndex;
            SekmeleriBuild();
            if (secilenIndex < tabControl.TabCount)
                tabControl.SelectedIndex = secilenIndex;
        }

        private void OzetRaporHazirla(int yil, int ay)
        {
            string rapor = VeriYoneticisi.OzetRaporOlustur(yil, ay);

            string ayAdi = new DateTime(yil, ay, 1).ToString("MMMM_yyyy", new System.Globalization.CultureInfo("tr-TR"));
            string masaustu = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string dosyaAdi = Path.Combine(masaustu, $"RORA_Rapor_{ayAdi}.txt");

            try
            {
                File.WriteAllText(dosyaAdi, rapor, System.Text.Encoding.UTF8);

                if (MessageBox.Show($"Rapor masaüstüne kaydedildi:\n{dosyaAdi}\n\nRaporu şimdi açmak ister misiniz?",
                    "Rapor Hazır", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start("notepad.exe", dosyaAdi);
                }
            }
            catch (Exception ex)
            {
                // Dosyaya yazılamazsa sebebini bildir ve önizleme göster
                string uyari =
                    $"Rapor masaüstüne kaydedilemedi: {ex.Message}" + Environment.NewLine +
                    $"Denenen konum: {dosyaAdi}" + Environment.NewLine +
                    new string('-', 72) + Environment.NewLine + Environment.NewLine;

                var previewForm = new Form
                {
                    Text = $"Özet Rapor - {ay}/{yil} (dosyaya yazılamadı)",
                    Size = new Size(650, 550),
                    StartPosition = FormStartPosition.CenterParent
                };
                var txt = new TextBox
                {
                    Multiline = true, ReadOnly = true,
                    Dock = DockStyle.Fill,
                    Font = new Font("Courier New", 10),
                    ScrollBars = ScrollBars.Both,
                    Text = uyari + rapor
                };
                previewForm.Controls.Add(txt);
                previewForm.ShowDialog(this);
            }
        }

        private void GuncelleStatusBar()
        {
            lblDurum.Text = $"RORA Sanat Merkezi  |  {DateTime.Now:dd MMMM yyyy}  |  Toplam Öğrenci: {VeriYoneticisi.Veriler.Ogrenciler.Count}";
        }

        // Üst çubuktaki arama kutusunu pencere genişliğine göre yerleştirir.
        //
        // Pencere daraldığında sağa yapışık logo etiketi ile arama kutusu
        // çakışıyordu (800 piksel genişlikte 231 piksellik örtüşme). Çakışma
        // durumunda logo gizlenir: logo dekoratif, arama ise işlevsel.
        private void AramaKutusunuKonumlandir()
        {
            if (txtGenelAra == null || lblGenelAra == null || lblLogo == null) return;

            // Konumlar sabit piksel yerine komşu denetimlerin gerçek sınırından
            // türetiliyor. Bu denetimleri WinForms ölçekle birlikte büyüttüğü
            // için hesap her ölçekte kendiliğinden doğru çıkıyor; yalnızca
            // aradaki boşluklar tasarım pikseli olarak ölçekleniyor.
            int bosluk = Olcek.Piksel(this, 20);
            int solSinir = (btnYenile != null ? btnYenile.Right : Olcek.Piksel(this, 415)) + bosluk;
            int logoAlani = (lblLogo.Width > 0 ? lblLogo.Width : Olcek.Piksel(this, 270)) + bosluk;
            int enDarKutu = Olcek.Piksel(this, 120);
            int enGenisKutu = Olcek.Piksel(this, 250);

            int gerekenGenislik = solSinir + lblGenelAra.Width + bosluk + enDarKutu;
            bool logoSigiyor = (toolPanel.ClientSize.Width - logoAlani) >= gerekenGenislik;
            lblLogo.Visible = logoSigiyor;

            int sagSinir = logoSigiyor
                ? toolPanel.ClientSize.Width - logoAlani
                : toolPanel.ClientSize.Width - Olcek.Piksel(this, 10);

            lblGenelAra.Location = new Point(solSinir, Olcek.Piksel(this, 14));
            txtGenelAra.Location = new Point(lblGenelAra.Right + Olcek.Piksel(this, 4), Olcek.Piksel(this, 13));
            txtGenelAra.Width = Math.Max(enDarKutu, Math.Min(enGenisKutu, sagSinir - txtGenelAra.Left));
        }

        // "Bu Ay" ve "Önceki Ay" en sık istenenler, onlar menüde tek tıkla
        // duruyor. Geçmiş bir ay gerektiğinde ay, yıl ve biçim burada seçiliyor.
        private void BaskaAyRaporu()
        {
            using (var form = new RaporAySecimFormu())
            {
                if (form.ShowDialog(this) != DialogResult.OK) return;

                if (form.PdfIstendi)
                    PdfRaporHazirla(form.SecilenYil, form.SecilenAy);
                else
                    OzetRaporHazirla(form.SecilenYil, form.SecilenAy);
            }
        }

        // Aynı raporu PDF olarak üretir. Metin raporu değişmeden çalışmaya
        // devam eder; bu ek bir çıktı biçimidir.
        private void PdfRaporHazirla(int yil, int ay)
        {
            if (!PdfRaporYazici.YaziciKullanilabilirMi())
            {
                MessageBox.Show(
                    $"PDF üretimi için gereken \"{PdfRaporYazici.YaziciAdi}\" yazıcısı bulunamadı." + Environment.NewLine + Environment.NewLine +
                    "Bu yazıcı Windows 10 ve üzeri sürümlerde standart olarak gelir." + Environment.NewLine +
                    "Kaldırılmışsa Denetim Masası > Aygıtlar ve Yazıcılar üzerinden yeniden eklenebilir." + Environment.NewLine + Environment.NewLine +
                    "Metin raporu (.txt) etkilenmez, kullanılabilir durumda.",
                    "PDF Üretilemiyor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string ayAdi = new DateTime(yil, ay, 1).ToString("MMMM_yyyy", new System.Globalization.CultureInfo("tr-TR"));

            using (var sfd = new SaveFileDialog())
            {
                sfd.Title = "Raporu PDF olarak kaydet";
                sfd.Filter = "PDF dosyası (*.pdf)|*.pdf";
                sfd.FileName = $"RORA_Rapor_{ayAdi}.pdf";
                sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (sfd.ShowDialog(this) != DialogResult.OK) return;

                Cursor eskiImlec = this.Cursor;
                this.Cursor = Cursors.WaitCursor;
                try
                {
                    new PdfRaporYazici(yil, ay).Yazdir(sfd.FileName);
                    this.Cursor = eskiImlec;

                    if (MessageBox.Show(
                        $"PDF kaydedildi:{Environment.NewLine}{sfd.FileName}{Environment.NewLine}{Environment.NewLine}Şimdi açmak ister misiniz?",
                        "Rapor Hazır", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        System.Diagnostics.Process.Start(sfd.FileName);
                    }
                }
                catch (Exception ex)
                {
                    this.Cursor = eskiImlec;
                    MessageBox.Show(
                        $"PDF oluşturulamadı.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                        "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Tüm çalgılardaki öğrenciler arasında arar. Sekme içi aramayla aynı
        // kuralları kullanır (Metin sınıfı): ad, soyad ve telefonda, Türkçe
        // karakterlere ve büyük/küçük harfe duyarsız.
        private void GenelAramaYap()
        {
            string arama = txtGenelAra.Text.Trim();
            lstAramaSonuclari.Items.Clear();
            aramaSonuclari.Clear();

            // Tek harfte tüm listeyi açmanın faydası yok
            if (arama.Length < 2)
            {
                lstAramaSonuclari.Visible = false;
                return;
            }

            var bulunanlar = VeriYoneticisi.Veriler.Ogrenciler
                .Where(o => Metin.OgrenciEslesiyorMu(o, arama))
                .OrderBy(o => o.Calgı)
                .ThenBy(o => o.TamAd)
                .Take(15)
                .ToList();

            foreach (var o in bulunanlar)
            {
                aramaSonuclari.Add(o);
                string telefon = string.IsNullOrWhiteSpace(o.Telefon) ? "" : $"  —  {o.Telefon}";
                lstAramaSonuclari.Items.Add($"{o.TamAd}  —  🎵 {o.Calgı}{telefon}");
            }

            if (bulunanlar.Count == 0)
                lstAramaSonuclari.Items.Add("(eşleşen öğrenci yok)");

            lstAramaSonuclari.Location = new Point(txtGenelAra.Left, toolPanel.Bottom);
            lstAramaSonuclari.Height = Math.Min(220, lstAramaSonuclari.Items.Count * 22 + 6);
            lstAramaSonuclari.Visible = true;
            lstAramaSonuclari.BringToFront();
        }

        private void TxtGenelAra_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                txtGenelAra.Text = string.Empty;
                lstAramaSonuclari.Visible = false;
            }
            else if (e.KeyCode == Keys.Down && lstAramaSonuclari.Visible && aramaSonuclari.Count > 0)
            {
                lstAramaSonuclari.SelectedIndex = 0;
                lstAramaSonuclari.Focus();
            }
            else if (e.KeyCode == Keys.Enter && aramaSonuclari.Count > 0)
            {
                lstAramaSonuclari.SelectedIndex = 0;
                SecilenSonucaGit();
            }
        }

        private void LstAramaSonuclari_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) SecilenSonucaGit();
            else if (e.KeyCode == Keys.Escape) { lstAramaSonuclari.Visible = false; txtGenelAra.Focus(); }
        }

        private void SecilenSonucaGit()
        {
            int sira = lstAramaSonuclari.SelectedIndex;
            if (sira < 0 || sira >= aramaSonuclari.Count) return;

            var ogrenci = aramaSonuclari[sira];
            lstAramaSonuclari.Visible = false;
            txtGenelAra.Text = string.Empty;
            OgrenciyeGit(ogrenci);
        }

        // Öğrencinin çalgı sekmesini açar ve satırını seçili hâle getirir.
        private void OgrenciyeGit(Ogrenci ogrenci)
        {
            CalgiSekmesi sekme;
            if (!calgiSekmeler.TryGetValue(ogrenci.Calgı, out sekme)) return;

            foreach (TabPage sayfa in tabControl.TabPages)
            {
                if (!sayfa.Controls.Contains(sekme)) continue;
                tabControl.SelectedTab = sayfa;
                break;
            }

            sekme.OgrenciSec(ogrenci.Id);
        }

        private void CalgiUcretleriniAc()
        {
            using (var form = new UcretTanimFormu())
            {
                form.ShowDialog(this);
            }
        }

        private void YedekAl()
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Title = "Yedek dosyasını kaydet";
                sfd.Filter = "XML dosyası (*.xml)|*.xml";
                sfd.FileName = $"RORA_Yedek_{DateTime.Now:yyyyMMdd_HHmm}.xml";
                sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (sfd.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    VeriYoneticisi.YedekAl(sfd.FileName);
                    MessageBox.Show($"Yedek alındı:{Environment.NewLine}{sfd.FileName}",
                        "Yedekleme", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Yedek alınamadı.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                        "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void YedektenGeriYukle()
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Geri yüklenecek yedeği seçin";
                ofd.Filter = "XML dosyası (*.xml)|*.xml";
                ofd.InitialDirectory = VeriYoneticisi.YedekKlasorYolu;
                if (ofd.ShowDialog(this) != DialogResult.OK) return;

                if (MessageBox.Show(
                    "Mevcut veriler seçilen yedekle değiştirilecek." + Environment.NewLine + Environment.NewLine +
                    "Değiştirmeden önce şu anki verinin bir kopyası otomatik olarak saklanacak." + Environment.NewLine + Environment.NewLine +
                    "Devam edilsin mi?",
                    "Geri Yükleme Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

                try
                {
                    VeriYoneticisi.GeriYukle(ofd.FileName);
                    SekmeleriBuild();
                    MessageBox.Show($"Geri yükleme tamamlandı.{Environment.NewLine}Toplam {VeriYoneticisi.Veriler.Ogrenciler.Count} öğrenci yüklendi.",
                        "Geri Yükleme", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Seçilen dosya geri yüklenemedi. Mevcut verilere dokunulmadı." + Environment.NewLine + Environment.NewLine + ex.Message,
                        "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void VeriKlasorunuAc()
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe",
                    Path.GetDirectoryName(VeriYoneticisi.VeriDosyaYolu));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Klasör açılamadı.{Environment.NewLine}{ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OdemeHatirlatici()
        {
            var simdi = DateTime.Now;
            // 15'i geçmişse ve ödeme yapmayan varsa uyar
            if (simdi.Day >= 15)
            {
                int odemeyenSayisi = VeriYoneticisi.Veriler.Ogrenciler.Count(o =>
                    !VeriYoneticisi.OdemeYapildiMi(o.Id, simdi.Year, simdi.Month));
                if (odemeyenSayisi > 0)
                {
                    MessageBox.Show(
                        $"⚠️ Hatırlatma: Bu ay ödeme yapmayan {odemeyenSayisi} öğrenci var!\n\n" +
                        "Özet rapor almak için '📊 Özet Rapor' butonunu kullanabilirsiniz.",
                        "Ödeme Hatırlatıcı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
    }

    // Özel menü renklendirmesi
    public class RoraMenuColors : ProfessionalColorTable
    {
        public override Color MenuItemSelected => Color.FromArgb(90, 110, 190);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(90, 110, 190);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(90, 110, 190);
        public override Color MenuStripGradientBegin => Color.FromArgb(50, 65, 130);
        public override Color MenuStripGradientEnd => Color.FromArgb(50, 65, 130);
        public override Color ToolStripDropDownBackground => Color.FromArgb(240, 242, 255);
        public override Color ImageMarginGradientBegin => Color.FromArgb(220, 225, 255);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(220, 225, 255);
        public override Color ImageMarginGradientEnd => Color.FromArgb(220, 225, 255);
    }
}
