using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace RORAMuzikMerkezi
{
    // Ödeme alınırken tahsil edilen tutarı soran küçük diyalog.
    // Windows Forms'ta hazır bir giriş kutusu bulunmadığı için ayrı bir form
    // olarak yazıldı; böylece Microsoft.VisualBasic referansı eklemek gerekmiyor.
    public class TutarGirisFormu : Form
    {
        private TextBox txtTutar;
        private Button btnTamam, btnIptal;

        public decimal Tutar { get; private set; }

        public TutarGirisFormu(string ogrenciAdi, string ayAdi, decimal baslangicTutari = 0m)
        {
            // Yerleşim 96 DPI'a göre yazıldı; ölçekli ekranlarda
            // konum ve boyutları WinForms bu ayarla kendisi büyütür.
            this.AutoScaleDimensions = new System.Drawing.SizeF(Olcek.TasarimDpi, Olcek.TasarimDpi);
            this.AutoScaleMode = AutoScaleMode.Dpi;

            this.Text = "Ödeme Tutarı";
            this.Size = new Size(400, 210);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Renkler.Zemin;

            var lblBaslik = new Label
            {
                Text = "💰 Ödeme Tutarı",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Renkler.Lacivert,
                Location = new Point(15, 15),
                Size = new Size(360, 30)
            };

            var lblBilgi = new Label
            {
                Text = $"{ogrenciAdi} — {ayAdi}",
                Font = new Font("Segoe UI", 10),
                ForeColor = Renkler.MetinSolgun,
                Location = new Point(15, 48),
                Size = new Size(360, 24)
            };

            var lblTutar = new Label
            {
                Text = "Tutar (₺):",
                Font = new Font("Segoe UI", 10),
                Location = new Point(15, 88),
                Size = new Size(80, 24)
            };

            txtTutar = new TextBox
            {
                Location = new Point(100, 85),
                Size = new Size(160, 28),
                Font = new Font("Segoe UI", 11),
                Text = baslangicTutari > 0m ? baslangicTutari.ToString("N2", TrKultur) : string.Empty
            };
            txtTutar.SelectAll();

            var lblAciklama = new Label
            {
                Text = "Boş bırakılırsa 0 kaydedilir.",
                Font = new Font("Segoe UI", 8),
                ForeColor = Renkler.MetinSolgun,
                Location = new Point(100, 115),
                Size = new Size(260, 20)
            };

            btnTamam = new Button
            {
                Text = "✅ Tamam",
                Location = new Point(100, 140),
                Size = new Size(120, 32),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Renkler.Lacivert,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnTamam.FlatAppearance.BorderSize = 0;
            btnTamam.Click += BtnTamam_Click;

            btnIptal = new Button
            {
                Text = "❌ İptal",
                Location = new Point(230, 140),
                Size = new Size(120, 32),
                Font = new Font("Segoe UI", 9),
                BackColor = Renkler.Yuzey,
                ForeColor = Renkler.MetinSolgun,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            btnIptal.FlatAppearance.BorderSize = 0;

            this.Controls.AddRange(new Control[] {
                lblBaslik, lblBilgi, lblTutar, txtTutar, lblAciklama, btnTamam, btnIptal
            });

            this.AcceptButton = btnTamam;
            this.CancelButton = btnIptal;
        }

        private static CultureInfo TrKultur
        {
            get { return new CultureInfo("tr-TR"); }
        }

        private void BtnTamam_Click(object sender, EventArgs e)
        {
            string girdi = txtTutar.Text.Trim();

            if (string.IsNullOrEmpty(girdi))
            {
                Tutar = 0m;
                this.DialogResult = DialogResult.OK;
                this.Close();
                return;
            }

            // Kullanıcı ondalık ayırıcı olarak hem virgül hem nokta yazabilir
            girdi = girdi.Replace(".", ",");

            decimal deger;
            bool okundu = decimal.TryParse(girdi, NumberStyles.Number, TrKultur, out deger);

            if (!okundu || deger < 0m)
            {
                MessageBox.Show("Geçerli bir tutar girin. Örnek: 500 veya 500,50",
                    "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTutar.Focus();
                txtTutar.SelectAll();
                return;
            }

            Tutar = deger;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
