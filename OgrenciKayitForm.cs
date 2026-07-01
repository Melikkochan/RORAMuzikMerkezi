using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace RORAMuzikMerkezi
{
    public class OgrenciKayitForm : Form
    {
        private TextBox txtAd, txtSoyad, txtTelefon;
        private ListBox lstCalgilar;
        private Button btnKaydet, btnIptal;
        private Label lblBaslik;

        public static readonly List<string> Calgilar = new List<string>
        {
            "Gitar", "Bağlama", "Keman", "Piyano", "Bateri", "Flüt",
            "Klarnet", "Saksofon", "Ud", "Kanun", "Darbuka", "Diğer","Konservatuar"
        };

        public OgrenciKayitForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Yeni Öğrenci Kaydı";
            this.Size = new Size(420, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(245, 245, 250);

            // Başlık
            lblBaslik = new Label
            {
                Text = "🎵 Yeni Öğrenci Kaydı",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 120),
                Location = new Point(15, 15),
                Size = new Size(380, 35)
            };

            // Ad
            var lblAd = new Label { Text = "Ad:", Location = new Point(15, 65), Size = new Size(80, 22), Font = new Font("Segoe UI", 10) };
            txtAd = new TextBox { Location = new Point(100, 62), Size = new Size(290, 28), Font = new Font("Segoe UI", 10) };

            // Soyad
            var lblSoyad = new Label { Text = "Soyad:", Location = new Point(15, 105), Size = new Size(80, 22), Font = new Font("Segoe UI", 10) };
            txtSoyad = new TextBox { Location = new Point(100, 102), Size = new Size(290, 28), Font = new Font("Segoe UI", 10) };

            // Telefon
            var lblTelefon = new Label { Text = "Telefon:", Location = new Point(15, 145), Size = new Size(80, 22), Font = new Font("Segoe UI", 10) };
            txtTelefon = new TextBox { Location = new Point(100, 142), Size = new Size(290, 28), Font = new Font("Segoe UI", 10) };

            // Çalgı seçimi
            var lblCalgi = new Label
            {
                Text = "Çalgı Kursu Seçin:",
                Location = new Point(15, 185),
                Size = new Size(380, 22),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            lstCalgilar = new ListBox
            {
                Location = new Point(15, 210),
                Size = new Size(375, 180),
                Font = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.FixedSingle,
                SelectionMode = SelectionMode.One
            };
            foreach (var c in Calgilar)
                lstCalgilar.Items.Add(c);

            // Butonlar
            btnKaydet = new Button
            {
                Text = "✅ Kaydet",
                Location = new Point(15, 405),
                Size = new Size(180, 38),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(50, 150, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.None
            };
            btnKaydet.FlatAppearance.BorderSize = 0;
            btnKaydet.Click += BtnKaydet_Click;

            btnIptal = new Button
            {
                Text = "❌ İptal",
                Location = new Point(210, 405),
                Size = new Size(180, 38),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(200, 70, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            btnIptal.FlatAppearance.BorderSize = 0;

            this.Controls.AddRange(new Control[] {
                lblBaslik, lblAd, txtAd, lblSoyad, txtSoyad,
                lblTelefon, txtTelefon, lblCalgi, lstCalgilar,
                btnKaydet, btnIptal
            });
        }

        private void BtnKaydet_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtAd.Text) || string.IsNullOrWhiteSpace(txtSoyad.Text))
            {
                MessageBox.Show("Ad ve Soyad alanları zorunludur.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (lstCalgilar.SelectedItem == null)
            {
                MessageBox.Show("Lütfen bir çalgı kursu seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            VeriYoneticisi.OgrenciEkle(
                txtAd.Text.Trim(),
                txtSoyad.Text.Trim(),
                txtTelefon.Text.Trim(),
                lstCalgilar.SelectedItem.ToString()
            );

            MessageBox.Show("Öğrenci başarıyla kaydedildi!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
