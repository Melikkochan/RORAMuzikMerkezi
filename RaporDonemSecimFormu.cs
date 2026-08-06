using System;
using System.Drawing;
using System.Windows.Forms;

namespace RORAMuzikMerkezi
{
    // Dönemsel rapor için dönemi ve biçimi soran diyalog.
    //
    // İki mod var: bir yılın tamamı ya da serbest ay aralığı. Yıl ayrı bir
    // seçenek olarak duruyor çünkü en sık istenen o; aralık için dört kutu
    // doldurmak zorunda bırakmak gereksiz sürtünme olurdu.
    public class RaporDonemSecimFormu : Form
    {
        private RadioButton rbYil, rbAralik;
        private ComboBox cmbYil;
        private ComboBox cmbBasAy, cmbBasYil, cmbBitAy, cmbBitYil;

        public Donem SecilenDonem { get; private set; }
        public bool PdfIstendi { get; private set; }

        public RaporDonemSecimFormu()
        {
            // Yerleşim 96 DPI'a göre yazıldı; ölçekli ekranlarda
            // konum ve boyutları WinForms bu ayarla kendisi büyütür.
            this.AutoScaleDimensions = new SizeF(Olcek.TasarimDpi, Olcek.TasarimDpi);
            this.AutoScaleMode = AutoScaleMode.Dpi;

            this.Text = "Dönem Raporu";
            this.Size = new Size(420, 335);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Renkler.Zemin;

            var lblBaslik = new Label
            {
                Text = "📆 Rapor Dönemini Seçin",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Renkler.Lacivert,
                Location = new Point(15, 15),
                Size = new Size(380, 30)
            };

            // ---- Yıl ----
            rbYil = new RadioButton
            {
                Text = "Yıl",
                Checked = true,
                Font = new Font("Segoe UI", 10),
                Location = new Point(15, 58),
                Size = new Size(60, 26)
            };

            cmbYil = new ComboBox
            {
                Location = new Point(85, 56),
                Size = new Size(110, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10)
            };
            foreach (int y in Donem.SecilebilirYillar()) cmbYil.Items.Add(y);

            // ---- Aralık ----
            rbAralik = new RadioButton
            {
                Text = "Tarih aralığı",
                Font = new Font("Segoe UI", 10),
                Location = new Point(15, 94),
                Size = new Size(150, 26)
            };

            var lblBas = new Label
            {
                Text = "Başlangıç:",
                Font = new Font("Segoe UI", 9),
                ForeColor = Renkler.MetinSolgun,
                Location = new Point(32, 130),
                Size = new Size(70, 24)
            };

            cmbBasAy = AyKutusu(new Point(106, 127));
            cmbBasYil = YilKutusu(new Point(226, 127));

            var lblBit = new Label
            {
                Text = "Bitiş:",
                Font = new Font("Segoe UI", 9),
                ForeColor = Renkler.MetinSolgun,
                Location = new Point(32, 164),
                Size = new Size(70, 24)
            };

            cmbBitAy = AyKutusu(new Point(106, 161));
            cmbBitYil = YilKutusu(new Point(226, 161));

            // Varsayılan aralık: bu yılın başından içinde bulunulan aya kadar.
            // Kullanıcı diyalogu açtığında çoğunlukla "şimdiye kadar ne oldu"
            // sorusunu soruyor.
            cmbBasAy.SelectedIndex = 0;
            cmbBitAy.SelectedIndex = DateTime.Now.Month - 1;
            VarsayilanYiliSec(cmbYil);
            VarsayilanYiliSec(cmbBasYil);
            VarsayilanYiliSec(cmbBitYil);

            var lblAciklama = new Label
            {
                Text = "Rapor, dönem boyunca ay ay tahsilatı ve çalgı bazında dağılımı gösterir.",
                Font = new Font("Segoe UI", 8),
                ForeColor = Renkler.MetinSolgun,
                Location = new Point(15, 199),
                Size = new Size(380, 32)
            };

            var btnMetin = new Button
            {
                Text = "📄 Metin",
                Location = new Point(15, 236),
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
                Location = new Point(140, 236),
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
                Location = new Point(265, 236),
                Size = new Size(115, 34),
                Font = new Font("Segoe UI", 9),
                BackColor = Renkler.Yuzey,
                ForeColor = Renkler.MetinSolgun,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            btnIptal.FlatAppearance.BorderSize = 0;

            rbYil.CheckedChanged += (s, e) => KutulariGuncelle();
            rbAralik.CheckedChanged += (s, e) => KutulariGuncelle();

            this.Controls.AddRange(new Control[] {
                lblBaslik, rbYil, cmbYil, rbAralik, lblBas, cmbBasAy, cmbBasYil,
                lblBit, cmbBitAy, cmbBitYil, lblAciklama, btnMetin, btnPdf, btnIptal
            });

            KutulariGuncelle();

            this.AcceptButton = btnMetin;
            this.CancelButton = btnIptal;
        }

        private ComboBox AyKutusu(Point konum)
        {
            var kutu = new ComboBox
            {
                Location = konum,
                Size = new Size(112, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            for (int i = 1; i <= 12; i++) kutu.Items.Add(Donem.AyAdi(i));
            return kutu;
        }

        private ComboBox YilKutusu(Point konum)
        {
            var kutu = new ComboBox
            {
                Location = konum,
                Size = new Size(90, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            foreach (int y in Donem.SecilebilirYillar()) kutu.Items.Add(y);
            return kutu;
        }

        // Bu yıl listede yoksa (kayıtlar gelecekteyse) son öğeye düşülür;
        // SelectedItem'ı boş bırakmak sonradan null kontrolü gerektirirdi.
        private static void VarsayilanYiliSec(ComboBox kutu)
        {
            kutu.SelectedItem = DateTime.Now.Year;
            if (kutu.SelectedIndex < 0 && kutu.Items.Count > 0)
                kutu.SelectedIndex = kutu.Items.Count - 1;
        }

        // Seçili olmayan modun kutuları kapatılıyor: hangi değerlerin rapora
        // gireceği tek bakışta belli olsun.
        private void KutulariGuncelle()
        {
            bool yil = rbYil.Checked;
            cmbYil.Enabled = yil;
            cmbBasAy.Enabled = cmbBasYil.Enabled = !yil;
            cmbBitAy.Enabled = cmbBitYil.Enabled = !yil;
        }

        private static int SeciliYil(ComboBox kutu)
        {
            return kutu.SelectedItem != null ? (int)kutu.SelectedItem : DateTime.Now.Year;
        }

        private void Bitir(bool pdf)
        {
            SecilenDonem = rbYil.Checked
                ? Donem.Yil(SeciliYil(cmbYil))
                : Donem.Aralik(SeciliYil(cmbBasYil), cmbBasAy.SelectedIndex + 1,
                               SeciliYil(cmbBitYil), cmbBitAy.SelectedIndex + 1);

            PdfIstendi = pdf;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
