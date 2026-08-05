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
            this.Text = "🎵 RORA Sanat Merkezi - Öğrenci Takip Sistemi";
            this.Size = new Size(1000, 680);
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(800, 550);
            this.BackColor = Color.FromArgb(245, 245, 250);

            // İkon simgesi için form ikonu (bulunamazsa varsayılan ikonla açılır)
            var ikon = Varliklar.Ikon("favicon.ico");
            if (ikon != null) this.Icon = ikon;

            // Menu
            menuStrip = new MenuStrip { BackColor = Color.FromArgb(50, 65, 130), ForeColor = Color.White };
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
            menuRapor.DropDownItems.AddRange(new ToolStripItem[] { menuOzetRapor, menuOncekiAy });

            var menuHakkinda = new ToolStripMenuItem("ℹ️ Hakkında") { ForeColor = Color.White, Font = new Font("Segoe UI", 10) };
            menuHakkinda.Click += (s, e) => MessageBox.Show(
                "RORA Sanat Merkezi\nÖğrenci Takip Sistemi v1.0\n\nTüm hakları saklıdır.\nMelik KOÇHAN tarafından geliştirildi",
                "Hakkında", MessageBoxButtons.OK, MessageBoxIcon.Information);

            var menuVeri = new ToolStripMenuItem("💾 Veri") { ForeColor = Color.White, Font = new Font("Segoe UI", 10) };
            var menuYedekAl = new ToolStripMenuItem("📤 Yedek Al...", null, (s, e) => YedekAl());
            var menuGeriYukle = new ToolStripMenuItem("📥 Yedekten Geri Yükle...", null, (s, e) => YedektenGeriYukle());
            var menuKlasorAc = new ToolStripMenuItem("📂 Veri Klasörünü Aç", null, (s, e) => VeriKlasorunuAc());
            menuVeri.DropDownItems.AddRange(new ToolStripItem[] { menuYedekAl, menuGeriYukle, new ToolStripSeparator(), menuKlasorAc });

            menuStrip.Items.AddRange(new ToolStripItem[] { menuOgrenci, menuRapor, menuVeri, menuHakkinda });

            // Ana buton çubuğu
            var toolPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.FromArgb(70, 90, 160),
                Padding = new Padding(10, 8, 10, 8)
            };

            var btnYeniOgrenci = new Button
            {
                Text = "➕ Yeni Öğrenci",
                Location = new Point(10, 9),
                Size = new Size(145, 34),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(40, 180, 90),
                ForeColor = Color.White,
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
                BackColor = Color.FromArgb(40, 130, 210),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRapor.FlatAppearance.BorderSize = 0;
            btnRapor.Click += (s, e) => OzetRaporHazirla(DateTime.Now.Year, DateTime.Now.Month);

            var btnYenile = new Button
            {
                Text = "🔄 Yenile",
                Location = new Point(320, 9),
                Size = new Size(110, 34),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(130, 100, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnYenile.FlatAppearance.BorderSize = 0;
            btnYenile.Click += (s, e) => TumSekmeletiYenile();

            var lblLogo = new Label
            {
                Text = "🎵 RORA SANAT MERKEZİ",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Right,
                Width = 280,
                Padding = new Padding(0, 0, 10, 0)
            };

            toolPanel.Controls.AddRange(new Control[] { btnYeniOgrenci, btnRapor, btnYenile, lblLogo });

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
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

            var lblBaslik = new Label
            {
                Text = "RORA Sanat Merkezi - Genel Durum",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 60, 140),
                Location = new Point(20, 20),
                Size = new Size(500, 40),
                AutoSize = false
            };

            var lblTarih = new Label
            {
                Text = $"📅 {DateTime.Now:dd MMMM yyyy, dddd}",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(100, 100, 140),
                Location = new Point(20, 65),
                Size = new Size(400, 28)
            };

            // Özet bilgi kutuları
            int toplamOgrenci = VeriYoneticisi.Veriler.Ogrenciler.Count;
            int odeyenler = VeriYoneticisi.Veriler.Ogrenciler.Count(o =>
                VeriYoneticisi.OdemeYapildiMi(o.Id, DateTime.Now.Year, DateTime.Now.Month));
            int buHaftaDers = VeriYoneticisi.Veriler.Ogrenciler.Count(o =>
                VeriYoneticisi.BuHaftaDersAldiMi(o.Id));

            var kutular = new[]
            {
                ("👥", "Toplam Öğrenci", toplamOgrenci.ToString(), Color.FromArgb(60, 100, 200)),
                ("💰", "Bu Ay Ödeme Yapan", $"{odeyenler}/{toplamOgrenci}", Color.FromArgb(40, 160, 80)),
                ("📚", "Bu Hafta Ders Alan", $"{buHaftaDers}/{toplamOgrenci}", Color.FromArgb(180, 100, 40)),
            };

            int xPos = 20;
            foreach (var (ikon, baslik, deger, renk) in kutular)
            {
                var kutu = new Panel
                {
                    Location = new Point(xPos, 105),
                    Size = new Size(180, 110),
                    BackColor = renk,
                    BorderStyle = BorderStyle.None
                };

                kutu.Controls.Add(new Label
                {
                    Text = ikon,
                    Font = new Font("Segoe UI", 22),
                    ForeColor = Color.White,
                    Location = new Point(10, 8),
                    Size = new Size(50, 40),
                    TextAlign = ContentAlignment.MiddleCenter
                });

                kutu.Controls.Add(new Label
                {
                    Text = deger,
                    Font = new Font("Segoe UI", 22, FontStyle.Bold),
                    ForeColor = Color.White,
                    Location = new Point(65, 8),
                    Size = new Size(105, 40),
                    TextAlign = ContentAlignment.MiddleCenter
                });

                kutu.Controls.Add(new Label
                {
                    Text = baslik,
                    Font = new Font("Segoe UI", 9),
                    ForeColor = Color.FromArgb(220, 220, 255),
                    Location = new Point(5, 58),
                    Size = new Size(170, 25),
                    TextAlign = ContentAlignment.MiddleCenter
                });

                panel.Controls.Add(kutu);
                xPos += 200;
            }

            // Kısa öğrenci listesi
            var lblKisaListe = new Label
            {
                Text = "Bu ay ödeme yapmayan öğrenciler:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 50, 50),
                Location = new Point(20, 235),
                Size = new Size(400, 25)
            };

            var lstOdemeyenler = new ListBox
            {
                Location = new Point(20, 265),
                Size = new Size(550, 200),
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(255, 250, 250)
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
                // Dosyaya yazılamazsa önizleme göster
                var previewForm = new Form
                {
                    Text = $"Özet Rapor - {ay}/{yil}",
                    Size = new Size(650, 550),
                    StartPosition = FormStartPosition.CenterParent
                };
                var txt = new TextBox
                {
                    Multiline = true, ReadOnly = true,
                    Dock = DockStyle.Fill,
                    Font = new Font("Courier New", 10),
                    ScrollBars = ScrollBars.Both,
                    Text = rapor
                };
                previewForm.Controls.Add(txt);
                previewForm.ShowDialog(this);
            }
        }

        private void GuncelleStatusBar()
        {
            lblDurum.Text = $"RORA Sanat Merkezi  |  {DateTime.Now:dd MMMM yyyy}  |  Toplam Öğrenci: {VeriYoneticisi.Veriler.Ogrenciler.Count}";
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
