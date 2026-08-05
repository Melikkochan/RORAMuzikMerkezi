using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace RORAMuzikMerkezi
{
    // Çalgı başına varsayılan aylık ücretin tanımlandığı ekran.
    // Buradaki değerler ödeme alınırken tutar kutusuna varsayılan olarak gelir;
    // öğrenciye özel bir ücret tanımlıysa o öncelikli olur.
    public class UcretTanimFormu : Form
    {
        private DataGridView dgvUcretler;
        private Button btnKaydet, btnKapat;

        private static CultureInfo TrKultur
        {
            get { return new CultureInfo("tr-TR"); }
        }

        public UcretTanimFormu()
        {
            InitializeComponent();
            ListeyiDoldur();
        }

        private void InitializeComponent()
        {
            this.Text = "Çalgı Ücret Tanımları";
            this.Size = new Size(460, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(245, 245, 250);

            var lblBaslik = new Label
            {
                Text = "💵 Çalgı Ücret Tanımları",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 120),
                Location = new Point(15, 15),
                Size = new Size(420, 32)
            };

            var lblAciklama = new Label
            {
                Text = "Burada tanımlanan tutar, ödeme alınırken varsayılan olarak gelir.\n" +
                       "Boş bırakılan çalgı için varsayılan uygulanmaz.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(100, 100, 140),
                Location = new Point(15, 50),
                Size = new Size(420, 40)
            };

            dgvUcretler = new DataGridView
            {
                Location = new Point(15, 95),
                Size = new Size(415, 370),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                GridColor = Color.FromArgb(220, 220, 235),
                Font = new Font("Segoe UI", 10),
                ColumnHeadersHeight = 34,
                RowTemplate = { Height = 30 },
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            dgvUcretler.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(240, 240, 250),
                ForeColor = Color.FromArgb(60, 60, 120),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(5)
            };

            var colCalgi = new DataGridViewTextBoxColumn
            {
                Name = "colCalgi",
                HeaderText = "Çalgı",
                FillWeight = 60,
                ReadOnly = true
            };

            var colUcret = new DataGridViewTextBoxColumn
            {
                Name = "colUcret",
                HeaderText = "Aylık Ücret (₺)",
                FillWeight = 40
            };
            colUcret.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            dgvUcretler.Columns.AddRange(new DataGridViewColumn[] { colCalgi, colUcret });

            btnKaydet = new Button
            {
                Text = "✅ Kaydet",
                Location = new Point(15, 478),
                Size = new Size(200, 36),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(50, 150, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnKaydet.FlatAppearance.BorderSize = 0;
            btnKaydet.Click += BtnKaydet_Click;

            btnKapat = new Button
            {
                Text = "Kapat",
                Location = new Point(230, 478),
                Size = new Size(200, 36),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(150, 150, 170),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            btnKapat.FlatAppearance.BorderSize = 0;

            this.Controls.AddRange(new Control[] { lblBaslik, lblAciklama, dgvUcretler, btnKaydet, btnKapat });
            this.CancelButton = btnKapat;
        }

        private void ListeyiDoldur()
        {
            dgvUcretler.Rows.Clear();

            // Sabit çalgı listesi ile kayıtlı öğrencilerin çalgıları birleştirilir;
            // böylece listede olmayan eski bir çalgı da düzenlenebilir kalır.
            var calgilar = OgrenciKayitForm.Calgilar
                .Union(VeriYoneticisi.Veriler.Ogrenciler.Select(o => o.Calgı))
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            foreach (var calgi in calgilar)
            {
                decimal? ucret = VeriYoneticisi.CalgiUcretiGetir(calgi);
                dgvUcretler.Rows.Add(calgi, ucret.HasValue ? ucret.Value.ToString("N2", TrKultur) : string.Empty);
            }
        }

        private void BtnKaydet_Click(object sender, EventArgs e)
        {
            // Önce tüm satırları doğrula; tek bir hatalı değer varsa hiçbir şey kaydedilmesin
            foreach (DataGridViewRow satir in dgvUcretler.Rows)
            {
                string metin = Convert.ToString(satir.Cells["colUcret"].Value ?? string.Empty).Trim();
                if (metin.Length == 0) continue;

                decimal deger;
                if (!TutarAyristir(metin, out deger))
                {
                    MessageBox.Show(
                        $"{satir.Cells["colCalgi"].Value} için girilen tutar geçerli değil: \"{metin}\"" +
                        Environment.NewLine + Environment.NewLine +
                        "Örnek: 500 veya 500,50. Boş bırakırsanız varsayılan uygulanmaz.",
                        "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dgvUcretler.CurrentCell = satir.Cells["colUcret"];
                    return;
                }
            }

            foreach (DataGridViewRow satir in dgvUcretler.Rows)
            {
                string calgi = Convert.ToString(satir.Cells["colCalgi"].Value);
                string metin = Convert.ToString(satir.Cells["colUcret"].Value ?? string.Empty).Trim();

                if (metin.Length == 0)
                {
                    VeriYoneticisi.CalgiUcretiAyarla(calgi, null);
                }
                else
                {
                    decimal deger;
                    TutarAyristir(metin, out deger);
                    VeriYoneticisi.CalgiUcretiAyarla(calgi, deger);
                }
            }

            MessageBox.Show("Ücret tanımları kaydedildi.", "Kaydedildi",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        // Kullanıcı ondalık ayırıcı olarak hem virgül hem nokta yazabilir
        private static bool TutarAyristir(string metin, out decimal deger)
        {
            metin = metin.Replace(".", ",");
            bool okundu = decimal.TryParse(metin, NumberStyles.Number, TrKultur, out deger);
            return okundu && deger >= 0m;
        }
    }
}
