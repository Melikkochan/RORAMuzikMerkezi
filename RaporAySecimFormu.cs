using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace RORAMuzikMerkezi
{
    // Rapor alınacak ayı ve biçimi soran küçük diyalog. "Bu Ay" ve "Önceki Ay"
    // kısayolları menüde durmaya devam ediyor; bu form geçmiş bir ay gerektiğinde
    // kullanılıyor.
    public class RaporAySecimFormu : Form
    {
        private ComboBox cmbAy, cmbYil;

        public int SecilenYil { get; private set; }
        public int SecilenAy { get; private set; }
        public bool PdfIstendi { get; private set; }

        private static CultureInfo TrKultur { get { return new CultureInfo("tr-TR"); } }

        public RaporAySecimFormu()
        {
            // Yerleşim 96 DPI'a göre yazıldı; ölçekli ekranlarda
            // konum ve boyutları WinForms bu ayarla kendisi büyütür.
            this.AutoScaleDimensions = new System.Drawing.SizeF(Olcek.TasarimDpi, Olcek.TasarimDpi);
            this.AutoScaleMode = AutoScaleMode.Dpi;

            this.Text = "Rapor Ayı";
            this.Size = new Size(400, 235);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Renkler.Zemin;

            var lblBaslik = new Label
            {
                Text = "🗓️ Rapor Ayını Seçin",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Renkler.Lacivert,
                Location = new Point(15, 15),
                Size = new Size(360, 30)
            };

            var lblAy = new Label
            {
                Text = "Ay:",
                Font = new Font("Segoe UI", 10),
                Location = new Point(15, 62),
                Size = new Size(50, 24)
            };

            cmbAy = new ComboBox
            {
                Location = new Point(70, 59),
                Size = new Size(140, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10)
            };
            for (int i = 1; i <= 12; i++)
                cmbAy.Items.Add(new DateTime(2000, i, 1).ToString("MMMM", TrKultur));
            cmbAy.SelectedIndex = DateTime.Now.Month - 1;

            var lblYil = new Label
            {
                Text = "Yıl:",
                Font = new Font("Segoe UI", 10),
                Location = new Point(225, 62),
                Size = new Size(40, 24)
            };

            cmbYil = new ComboBox
            {
                Location = new Point(268, 59),
                Size = new Size(100, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10)
            };
            foreach (int y in YilAraligi())
                cmbYil.Items.Add(y);
            cmbYil.SelectedItem = DateTime.Now.Year;

            var lblAciklama = new Label
            {
                Text = "Seçtiğiniz ay için hangi biçimde rapor istediğinizi seçin.",
                Font = new Font("Segoe UI", 8),
                ForeColor = Renkler.MetinSolgun,
                Location = new Point(15, 95),
                Size = new Size(360, 20)
            };

            var btnMetin = new Button
            {
                Text = "📄 Metin",
                Location = new Point(15, 125),
                Size = new Size(115, 34),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Renkler.Lacivert,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnMetin.FlatAppearance.BorderSize = 0;
            btnMetin.Click += (s, e) => Bitir(false);

            var btnPdf = new Button
            {
                Text = "📕 PDF",
                Location = new Point(140, 125),
                Size = new Size(115, 34),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Renkler.LacivertOrta,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnPdf.FlatAppearance.BorderSize = 0;
            btnPdf.Click += (s, e) => Bitir(true);

            var btnIptal = new Button
            {
                Text = "❌ İptal",
                Location = new Point(265, 125),
                Size = new Size(103, 34),
                Font = new Font("Segoe UI", 9),
                BackColor = Renkler.Yuzey,
                ForeColor = Renkler.MetinSolgun,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            btnIptal.FlatAppearance.BorderSize = 0;

            this.Controls.AddRange(new Control[] {
                lblBaslik, lblAy, cmbAy, lblYil, cmbYil, lblAciklama, btnMetin, btnPdf, btnIptal
            });

            this.AcceptButton = btnMetin;
            this.CancelButton = btnIptal;
        }

        // Listelenecek yıllar, kayıtlardaki en eski yıldan gelecek yıla kadar.
        // Sabit bir aralık yerine veriye bakılıyor: eski kayıtları olan bir
        // merkezde o yıllar da seçilebilmeli, yeni kurulan bir merkezde ise
        // liste gereksiz yere uzamamalı.
        private static int[] YilAraligi()
        {
            int buYil = DateTime.Now.Year;
            int enEski = buYil;

            foreach (var ogrenci in VeriYoneticisi.Veriler.Ogrenciler)
            {
                foreach (var ders in ogrenci.DersKayitlari)
                    if (ders.Yil < enEski) enEski = ders.Yil;
                foreach (var odeme in ogrenci.OdemeKayitlari)
                    if (odeme.Yil < enEski) enEski = odeme.Yil;
            }

            return Enumerable.Range(enEski, (buYil + 1) - enEski + 1).ToArray();
        }

        private void Bitir(bool pdf)
        {
            SecilenAy = cmbAy.SelectedIndex + 1;
            SecilenYil = cmbYil.SelectedItem != null ? (int)cmbYil.SelectedItem : DateTime.Now.Year;
            PdfIstendi = pdf;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
