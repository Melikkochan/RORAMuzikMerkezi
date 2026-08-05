using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace RORAMuzikMerkezi
{
    public class CalgiSekmesi : UserControl
    {
        private string calgiAdi;
        private DataGridView dgvOgrenciler;
        private Button btnOdemeYapildi, btnOgrenciSil, btnDuzenle;

        // Öğrencinin çalgısı değişince ana pencerenin sekmeleri yenilemesi gerekir
        public event EventHandler OgrenciGuncellendi;
        private Label lblBilgi;
        private ComboBox cmbAy, cmbYil;
        private TextBox txtAra;
        private Panel panelUst, panelAlt;

        // Checkbox sütun adları
        private static readonly string[] HaftaSutunlari = { "colH1", "colH2", "colH3", "colH4" };

        public CalgiSekmesi(string calgi)
        {
            this.calgiAdi = calgi;
            InitializeComponent();
            ListeyiYenile();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(248, 248, 252);

            // Üst panel
            panelUst = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(60, 80, 150)
            };

            lblBilgi = new Label
            {
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(10, 15),
                Size = new Size(300, 30),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblAyFiltre = new Label
            {
                Text = "Ay:",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Location = new Point(320, 18),
                Size = new Size(30, 25)
            };

            cmbAy = new ComboBox
            {
                Location = new Point(355, 17),
                Size = new Size(100, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            for (int i = 1; i <= 12; i++)
                cmbAy.Items.Add(new DateTime(2000, i, 1).ToString("MMMM", new System.Globalization.CultureInfo("tr-TR")));
            cmbAy.SelectedIndex = DateTime.Now.Month - 1;
            cmbAy.SelectedIndexChanged += (s, e) => ListeyiYenile();

            cmbYil = new ComboBox
            {
                Location = new Point(465, 17),
                Size = new Size(75, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            for (int y = DateTime.Now.Year - 1; y <= DateTime.Now.Year + 1; y++)
                cmbYil.Items.Add(y);
            cmbYil.SelectedItem = DateTime.Now.Year;
            cmbYil.SelectedIndexChanged += (s, e) => ListeyiYenile();

            var lblAra = new Label
            {
                Text = "Ara:",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Location = new Point(560, 18),
                Size = new Size(36, 25)
            };

            txtAra = new TextBox
            {
                Location = new Point(600, 17),
                Size = new Size(220, 28),
                Font = new Font("Segoe UI", 9)
            };
            // Yazdıkça filtrele: ad soyad veya telefon içinde arar
            txtAra.TextChanged += (s, e) => ListeyiYenile();

            panelUst.Controls.AddRange(new Control[] { lblBilgi, lblAyFiltre, cmbAy, cmbYil, lblAra, txtAra });

            // DataGridView
            dgvOgrenciler = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BorderStyle = BorderStyle.None,
                BackgroundColor = Color.White,
                GridColor = Color.FromArgb(220, 220, 235),
                ColumnHeadersHeight = 40,
                RowTemplate = { Height = 36 },
                Font = new Font("Segoe UI", 10),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            };

            dgvOgrenciler.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(240, 240, 250),
                ForeColor = Color.FromArgb(60, 60, 120),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                Padding = new Padding(5)
            };

            dgvOgrenciler.DefaultCellStyle = new DataGridViewCellStyle
            {
                SelectionBackColor = Color.FromArgb(200, 210, 255),
                SelectionForeColor = Color.Black,
                Padding = new Padding(5, 0, 5, 0)
            };

            dgvOgrenciler.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 248, 255),
                SelectionBackColor = Color.FromArgb(200, 210, 255),
                SelectionForeColor = Color.Black
            };

            // Sütunlar
            var colId = new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "ID", Visible = false };
            var colAd = new DataGridViewTextBoxColumn { Name = "colAd", HeaderText = "Ad Soyad", FillWeight = 25, ReadOnly = true };
            var colTelefon = new DataGridViewTextBoxColumn { Name = "colTelefon", HeaderText = "Telefon", FillWeight = 18, ReadOnly = true };

            // 4 hafta checkbox sütunu
            var colH1 = new DataGridViewCheckBoxColumn { Name = "colH1", HeaderText = "1. Hafta", FillWeight = 12, TrueValue = true, FalseValue = false };
            var colH2 = new DataGridViewCheckBoxColumn { Name = "colH2", HeaderText = "2. Hafta", FillWeight = 12, TrueValue = true, FalseValue = false };
            var colH3 = new DataGridViewCheckBoxColumn { Name = "colH3", HeaderText = "3. Hafta", FillWeight = 12, TrueValue = true, FalseValue = false };
            var colH4 = new DataGridViewCheckBoxColumn { Name = "colH4", HeaderText = "4. Hafta", FillWeight = 12, TrueValue = true, FalseValue = false };

            var colOdeme = new DataGridViewTextBoxColumn { Name = "colOdeme", HeaderText = "Ödeme", FillWeight = 16, ReadOnly = true };

            dgvOgrenciler.Columns.AddRange(new DataGridViewColumn[] {
                colId, colAd, colTelefon, colH1, colH2, colH3, colH4, colOdeme
            });

            foreach (DataGridViewColumn col in dgvOgrenciler.Columns)
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvOgrenciler.Columns["colAd"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            // Checkbox tıklanınca kaydet
            dgvOgrenciler.CellValueChanged += DgvOgrenciler_CellValueChanged;
            // Ad veya telefon hücresine çift tıklayınca düzenleme formu açılır
            dgvOgrenciler.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                string sutun = dgvOgrenciler.Columns[e.ColumnIndex].Name;
                if (sutun == "colAd" || sutun == "colTelefon") OgrenciDuzenle();
            };
            dgvOgrenciler.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgvOgrenciler.IsCurrentCellDirty)
                    dgvOgrenciler.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            // Alt panel
            panelAlt = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 58,
                BackColor = Color.FromArgb(240, 240, 248)
            };

            btnOdemeYapildi = new Button
            {
                Text = "💰 Bu Ay Ödeme Yapıldı",
                Location = new Point(10, 10),
                Size = new Size(210, 38),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(40, 120, 200),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnOdemeYapildi.FlatAppearance.BorderSize = 0;
            btnOdemeYapildi.Click += BtnOdemeYapildi_Click;

            btnDuzenle = new Button
            {
                Text = "✏️ Düzenle",
                Location = new Point(415, 10),
                Size = new Size(145, 38),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(90, 110, 190),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDuzenle.FlatAppearance.BorderSize = 0;
            btnDuzenle.Click += (s, e) => OgrenciDuzenle();

            btnOgrenciSil = new Button
            {
                Text = "🗑️ Öğrenci Sil",
                Location = new Point(570, 10),
                Size = new Size(160, 38),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(200, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnOgrenciSil.FlatAppearance.BorderSize = 0;
            btnOgrenciSil.Click += BtnOgrenciSil_Click;

            var lblAciklama = new Label
            {
                Text = "💡 Hafta kutucuklarına tıklayarak ders kaydı yapabilirsiniz",
                Location = new Point(745, 18),
                Size = new Size(450, 22),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(100, 100, 140)
            };
            var btnOdemeIptal = new Button
            {
                Text = "❌ Ödeme Yapılmadı",
                Location = new Point(230, 10),
                Size = new Size(175, 38),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(180, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnOdemeIptal.FlatAppearance.BorderSize = 0;
            btnOdemeIptal.Click += BtnOdemeIptal_Click;
            panelAlt.Controls.AddRange(new Control[] { btnOdemeYapildi, btnOdemeIptal, btnDuzenle, btnOgrenciSil, lblAciklama });

            this.Controls.AddRange(new Control[] { dgvOgrenciler, panelUst, panelAlt });
        }

        public void ListeyiYenile()
        {
            // Geçici olarak event'i kapat (yüklerken tetiklenmesin)
            dgvOgrenciler.CellValueChanged -= DgvOgrenciler_CellValueChanged;
            dgvOgrenciler.Rows.Clear();

            int secilenAy = cmbAy.SelectedIndex + 1;
            int secilenYil = cmbYil.SelectedItem != null ? (int)cmbYil.SelectedItem : DateTime.Now.Year;

            var tumOgrenciler = VeriYoneticisi.CalginaGoreOgrenciler(calgiAdi);
            string arama = txtAra == null ? "" : txtAra.Text.Trim();
            var ogrenciler = OgrencileriFiltrele(tumOgrenciler, arama);

            if (arama.Length == 0)
                lblBilgi.Text = $"🎵 {calgiAdi}  |  {tumOgrenciler.Count} Öğrenci";
            else
                lblBilgi.Text = $"🎵 {calgiAdi}  |  {ogrenciler.Count} / {tumOgrenciler.Count} Öğrenci";

            foreach (var ogr in ogrenciler)
            {
                bool odeme = VeriYoneticisi.OdemeYapildiMi(ogr.Id, secilenYil, secilenAy);

                // Her hafta için ders alındı mı?
                bool h1 = VeriYoneticisi.HaftaDersAldiMi(ogr.Id, secilenYil, secilenAy, 1);
                bool h2 = VeriYoneticisi.HaftaDersAldiMi(ogr.Id, secilenYil, secilenAy, 2);
                bool h3 = VeriYoneticisi.HaftaDersAldiMi(ogr.Id, secilenYil, secilenAy, 3);
                bool h4 = VeriYoneticisi.HaftaDersAldiMi(ogr.Id, secilenYil, secilenAy, 4);

                decimal tutar = VeriYoneticisi.OdemeTutari(ogr.Id, secilenYil, secilenAy);
                string odemeYazi = odeme
                    ? (tutar > 0m ? $"✅ {VeriYoneticisi.TutarYazi(tutar)}" : "✅ Ödendi")
                    : "❌ Ödenmedi";

                int rowIdx = dgvOgrenciler.Rows.Add(
                    ogr.Id,
                    ogr.TamAd,
                    ogr.Telefon,
                    h1, h2, h3, h4,
                    odemeYazi
                );

                var row = dgvOgrenciler.Rows[rowIdx];
                row.Cells["colOdeme"].Style.ForeColor = odeme ? Color.FromArgb(30, 130, 60) : Color.FromArgb(190, 50, 50);
                row.Cells["colOdeme"].Style.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            }

            // Event'i tekrar aç
            dgvOgrenciler.CellValueChanged += DgvOgrenciler_CellValueChanged;
            // Ad veya telefon hücresine çift tıklayınca düzenleme formu açılır
            dgvOgrenciler.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                string sutun = dgvOgrenciler.Columns[e.ColumnIndex].Name;
                if (sutun == "colAd" || sutun == "colTelefon") OgrenciDuzenle();
            };
        }

        // Ad, soyad veya telefon alanında geçen metne göre süzer.
        // Karşılaştırma büyük/küçük harf ve Türkçe karakter duyarsızdır.
        private static List<Ogrenci> OgrencileriFiltrele(List<Ogrenci> kaynak, string arama)
        {
            if (string.IsNullOrWhiteSpace(arama)) return kaynak;

            return kaynak.Where(o => Metin.OgrenciEslesiyorMu(o, arama)).ToList();
        }

        // Genel aramadan gelen yönlendirme için: verilen öğrenciyi listede
        // seçili hâle getirir ve görünür alana kaydırır.
        public void OgrenciSec(int ogrenciId)
        {
            // Sekme içi arama doluysa öğrenci süzülmüş olabilir; önce temizle
            if (txtAra.Text.Length > 0)
                txtAra.Text = string.Empty;   // TextChanged ListeyiYenile çağırır
            else
                ListeyiYenile();

            foreach (DataGridViewRow satir in dgvOgrenciler.Rows)
            {
                if ((int)satir.Cells["colId"].Value != ogrenciId) continue;

                satir.Selected = true;
                dgvOgrenciler.CurrentCell = satir.Cells["colAd"];
                dgvOgrenciler.FirstDisplayedScrollingRowIndex = satir.Index;
                dgvOgrenciler.Focus();
                break;
            }
        }

        private void DgvOgrenciler_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // Sadece hafta sütunlarını dinle
            string colName = dgvOgrenciler.Columns[e.ColumnIndex].Name;
            if (!HaftaSutunlari.Contains(colName)) return;

            int ogrenciId = (int)dgvOgrenciler.Rows[e.RowIndex].Cells["colId"].Value;
            bool degerAlindi = (bool)(dgvOgrenciler.Rows[e.RowIndex].Cells[colName].Value ?? false);

            int secilenAy = cmbAy.SelectedIndex + 1;
            int secilenYil = cmbYil.SelectedItem != null ? (int)cmbYil.SelectedItem : DateTime.Now.Year;

            int haftaNo = Array.IndexOf(HaftaSutunlari, colName) + 1; // 1,2,3,4

            VeriYoneticisi.HaftaDersKaydet(ogrenciId, secilenYil, secilenAy, haftaNo, degerAlindi);

            // Ödeme sütunu rengini güncelle (satırı yenilemeden)
            bool odeme = VeriYoneticisi.OdemeYapildiMi(ogrenciId, secilenYil, secilenAy);
            var row = dgvOgrenciler.Rows[e.RowIndex];
            row.Cells["colOdeme"].Style.ForeColor = odeme ? Color.FromArgb(30, 130, 60) : Color.FromArgb(190, 50, 50);
        }

        private void BtnOdemeYapildi_Click(object sender, EventArgs e)
        {
            if (dgvOgrenciler.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen bir öğrenci seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int id = (int)dgvOgrenciler.SelectedRows[0].Cells["colId"].Value;
            var ogr = VeriYoneticisi.Veriler.Ogrenciler.FirstOrDefault(o => o.Id == id);
            if (ogr == null) return;

            int secilenAy = cmbAy.SelectedIndex + 1;
            int secilenYil = (int)cmbYil.SelectedItem;

            if (VeriYoneticisi.OdemeYapildiMi(id, secilenYil, secilenAy))
            {
                string ayAdi = new DateTime(secilenYil, secilenAy, 1).ToString("MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
                MessageBox.Show($"{ogr.TamAd} için {ayAdi} ödemesi zaten alınmış. ✅", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string ayYaziAdi = new DateTime(secilenYil, secilenAy, 1)
                .ToString("MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));

            decimal tutar;
            decimal varsayilan = VeriYoneticisi.VarsayilanUcret(id);
            using (var tutarFormu = new TutarGirisFormu(ogr.TamAd, ayYaziAdi, varsayilan))
            {
                if (tutarFormu.ShowDialog(this) != DialogResult.OK) return;
                tutar = tutarFormu.Tutar;
            }

            VeriYoneticisi.OdemeYap(id, secilenYil, secilenAy, tutar);
            ListeyiYenile();
            MessageBox.Show($"{ogr.TamAd} için {VeriYoneticisi.TutarYazi(tutar)} ödeme alındı. ✅",
                "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void BtnOdemeIptal_Click(object sender, EventArgs e)
        {
            if (dgvOgrenciler.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen bir öğrenci seçin.", "Uyarı",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int id = (int)dgvOgrenciler.SelectedRows[0].Cells["colId"].Value;
            var ogr = VeriYoneticisi.Veriler.Ogrenciler.FirstOrDefault(o => o.Id == id);
            if (ogr == null) return;

            int secilenAy = cmbAy.SelectedIndex + 1;
            int secilenYil = (int)cmbYil.SelectedItem;

            if (!VeriYoneticisi.OdemeYapildiMi(id, secilenYil, secilenAy))
            {
                MessageBox.Show($"{ogr.TamAd} zaten ödeme yapmamış olarak işaretli.",
                    "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show($"{ogr.TamAd} için ödeme geri alınacak. Emin misiniz?",
                "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                VeriYoneticisi.OdemeGeriAl(id, secilenYil, secilenAy);
                ListeyiYenile();
                MessageBox.Show($"{ogr.TamAd} için ödeme geri alındı.",
                    "Tamam", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        // Seçili öğrencinin bilgilerini düzenleme formunda açar.
        // Ders ve ödeme geçmişi korunur; yalnızca kimlik bilgileri güncellenir.
        private void OgrenciDuzenle()
        {
            if (dgvOgrenciler.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen bir öğrenci seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int id = (int)dgvOgrenciler.SelectedRows[0].Cells["colId"].Value;
            var ogr = VeriYoneticisi.Veriler.Ogrenciler.FirstOrDefault(o => o.Id == id);
            if (ogr == null) return;

            string oncekiCalgi = ogr.Calgı;

            using (var form = new OgrenciKayitForm(ogr))
            {
                if (form.ShowDialog(this) != DialogResult.OK) return;
            }

            ListeyiYenile();

            // Çalgı değiştiyse öğrenci artık başka sekmeye ait; sekmeleri ana pencere yeniler
            if (!string.Equals(oncekiCalgi, ogr.Calgı, StringComparison.Ordinal))
                OgrenciGuncellendi?.Invoke(this, EventArgs.Empty);
        }
        private void BtnOgrenciSil_Click(object sender, EventArgs e)
        {
            if (dgvOgrenciler.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen bir öğrenci seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int id = (int)dgvOgrenciler.SelectedRows[0].Cells["colId"].Value;
            var ogr = VeriYoneticisi.Veriler.Ogrenciler.FirstOrDefault(o => o.Id == id);
            if (ogr == null) return;

            if (MessageBox.Show($"{ogr.TamAd} silinecek. Emin misiniz?",
                "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                VeriYoneticisi.OgrenciSil(id);
                ListeyiYenile();
            }
        }
    }
}
