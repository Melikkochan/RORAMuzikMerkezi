using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace RORAMuzikMerkezi
{
    // Bir öğrencinin ay ay geçmişi: kaç ders aldı, ödeme yaptı mı, ne kadar
    // ödedi.
    //
    // Bu bilgi zaten kayıtlıydı ama toplanabilmesi için çalgı sekmesindeki ay
    // seçicisini tek tek geri almak gerekiyordu. Veliyle konuşurken sorulan
    // şey ise tam olarak bu: "geçen dönem kaç ders aldı, hangi ay ödeme
    // yapılmadı".
    public class OgrenciGecmisFormu : Form
    {
        // Bir ayın durumu. "Ödeme kaydı hiç açılmamış" ile "kayıt var ama
        // ödenmemiş" ayrı tutuluyor; ilki henüz işlenmemiş bir ay, ikincisi
        // gerçekten bekleyen bir ödeme.
        private enum OdemeDurumu { Odendi, Bekliyor, KayitYok }

        private class AySatiri
        {
            public int Yil;
            public int Ay;
            public int DersSayisi;
            public OdemeDurumu Durum;
            public decimal Tutar;

            // Ödemesi eksik sayılması için o ayda bir hareket olmalı: ya ders
            // işlenmiş ya da ödeme kaydı açılmış olmalı. Hiç hareketi olmayan
            // bir ayı borç gibi göstermek yanıltıcı olurdu.
            public bool OdemeEksik
            {
                get { return Durum == OdemeDurumu.Bekliyor || (Durum == OdemeDurumu.KayitYok && DersSayisi > 0); }
            }
        }

        private readonly Ogrenci ogrenci;
        private DataGridView dgvGecmis;

        private static CultureInfo TrKultur { get { return new CultureInfo("tr-TR"); } }

        private const int BaslikYuksekligi = 34;
        private const int SatirYuksekligi = 32;

        public OgrenciGecmisFormu(Ogrenci ogrenci)
        {
            if (ogrenci == null) throw new ArgumentNullException("ogrenci");
            this.ogrenci = ogrenci;

            // Yerleşim 96 DPI'a göre yazıldı; ölçekli ekranlarda
            // konum ve boyutları WinForms bu ayarla kendisi büyütür.
            this.AutoScaleDimensions = new SizeF(Olcek.TasarimDpi, Olcek.TasarimDpi);
            this.AutoScaleMode = AutoScaleMode.Dpi;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = $"Geçmiş — {ogrenci.TamAd}";
            this.Size = new Size(700, 620);
            this.MinimumSize = new Size(560, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Renkler.Zemin;

            var ikon = Varliklar.Ikon("favicon.ico");
            if (ikon != null) this.Icon = ikon;

            var satirlar = SatirlariHesapla();

            // ---- Üst bilgi ----
            var panelUst = new Panel
            {
                Dock = DockStyle.Top,
                Height = 96,
                BackColor = Renkler.Lacivert
            };

            var lblAd = new Label
            {
                Text = ogrenci.TamAd,
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Renkler.MetinTers,
                Location = new Point(18, 14),
                Size = new Size(640, 30),
                BackColor = Color.Transparent
            };

            var lblAyrinti = new Label
            {
                Text = Ayrinti(),
                Font = Yazilar.Kucuk,
                ForeColor = Color.FromArgb(205, 212, 235),
                Location = new Point(20, 48),
                Size = new Size(640, 20),
                BackColor = Color.Transparent
            };

            var lblOzet = new Label
            {
                Text = Ozet(satirlar),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Renkler.Altin,
                Location = new Point(20, 70),
                Size = new Size(640, 20),
                BackColor = Color.Transparent
            };

            panelUst.Controls.AddRange(new Control[] { lblAd, lblAyrinti, lblOzet });

            // ---- Tablo ----
            dgvGecmis = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Renkler.Yuzey,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                EnableHeadersVisualStyles = false
            };

            dgvGecmis.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Renkler.Lacivert,
                ForeColor = Renkler.MetinTers,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                SelectionBackColor = Renkler.Lacivert,
                SelectionForeColor = Renkler.MetinTers
            };

            dgvGecmis.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Renkler.Yuzey,
                ForeColor = Renkler.Metin,
                Font = Yazilar.Govde,
                SelectionBackColor = Renkler.SatirSecili,
                SelectionForeColor = Renkler.Metin,
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };

            dgvGecmis.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colAy", HeaderText = "Ay", FillWeight = 34 },
                new DataGridViewTextBoxColumn { Name = "colDers", HeaderText = "Ders", FillWeight = 18 },
                new DataGridViewTextBoxColumn { Name = "colOdeme", HeaderText = "Ödeme", FillWeight = 24 },
                new DataGridViewTextBoxColumn { Name = "colTutar", HeaderText = "Tutar", FillWeight = 24 }
            });
            dgvGecmis.Columns["colAy"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvGecmis.Columns["colTutar"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            SatirlariDoldur(satirlar);

            // ---- Alt panel ----
            var panelAlt = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Renkler.Zemin
            };

            var lblAciklama = new Label
            {
                Text = "💡 Ödemesi bekleyen aylar kırmızı, hareketi olmayan aylar soluk gösterilir.",
                Font = Yazilar.Kucuk,
                ForeColor = Renkler.MetinSolgun,
                Location = new Point(16, 16),
                Size = new Size(440, 22)
            };

            var btnKapat = new Button
            {
                Text = "Kapat",
                Size = new Size(110, 32),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Renkler.Lacivert,
                ForeColor = Renkler.MetinTers,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                DialogResult = DialogResult.OK
            };
            btnKapat.FlatAppearance.BorderSize = 0;
            btnKapat.Location = new Point(panelAlt.ClientSize.Width - 126, 10);

            panelAlt.Controls.AddRange(new Control[] { lblAciklama, btnKapat });

            // Tablo, panellerden sonra eklenirse Dock sırası doğru oluşur.
            this.Controls.Add(dgvGecmis);
            this.Controls.Add(panelAlt);
            this.Controls.Add(panelUst);

            this.AcceptButton = btnKapat;
            this.CancelButton = btnKapat;
        }

        // DataGridView'in satır ve başlık yükseklikleri AutoScaleMode ile
        // ölçeklenmiyor; tutamaç oluştuğunda tasarım değerlerinden yeniden
        // hesaplanıyor.
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (dgvGecmis == null) return;

            dgvGecmis.ColumnHeadersHeight = Olcek.Piksel(this, BaslikYuksekligi);
            dgvGecmis.RowTemplate.Height = Olcek.Piksel(this, SatirYuksekligi);
            foreach (DataGridViewRow satir in dgvGecmis.Rows)
                satir.Height = Olcek.Piksel(this, SatirYuksekligi);
        }

        private string Ayrinti()
        {
            string telefon = string.IsNullOrWhiteSpace(ogrenci.Telefon) ? "telefon yok" : ogrenci.Telefon;
            return $"{ogrenci.Calgı}  ·  {telefon}  ·  Kayıt: {ogrenci.KayitTarihi.ToString("dd.MM.yyyy", TrKultur)}";
        }

        private string Ozet(List<AySatiri> satirlar)
        {
            decimal toplam = VeriYoneticisi.OgrenciToplamTahsilat(ogrenci.Id);
            int ders = VeriYoneticisi.OgrenciToplamDers(ogrenci.Id);
            int eksik = satirlar.Count(s => s.OdemeEksik);

            string eksikYazi = eksik == 0 ? "Ödemesi eksik ay yok" : $"Ödemesi eksik: {eksik} ay";
            return $"Toplam tahsilat: {VeriYoneticisi.TutarYazi(toplam)}   ·   Toplam ders: {ders}   ·   {eksikYazi}";
        }

        // Gösterilecek aylar: öğrencinin kayıt tarihinden (ya da daha eski bir
        // kaydı varsa ondan) bugüne kadar kesintisiz. Hareketsiz aylar da
        // listeleniyor; bir ayın hiç görünmemesi ile boş görünmesi farklı
        // şeyler ve ödeme boşluğu ancak kesintisiz listede fark ediliyor.
        private List<AySatiri> SatirlariHesapla()
        {
            DateTime bas = new DateTime(ogrenci.KayitTarihi.Year, ogrenci.KayitTarihi.Month, 1);
            DateTime bugun = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DateTime son = bugun;

            foreach (var d in ogrenci.DersKayitlari)
            {
                var nokta = GuvenliAy(d.Yil, d.Ay);
                if (nokta == null) continue;
                if (nokta.Value < bas) bas = nokta.Value;
                if (nokta.Value > son) son = nokta.Value;
            }
            foreach (var o in ogrenci.OdemeKayitlari)
            {
                var nokta = GuvenliAy(o.Yil, o.Ay);
                if (nokta == null) continue;
                if (nokta.Value < bas) bas = nokta.Value;
                if (nokta.Value > son) son = nokta.Value;
            }

            var liste = new List<AySatiri>();
            for (var ay = bas; ay <= son; ay = ay.AddMonths(1))
            {
                bool odendi = VeriYoneticisi.OdemeYapildiMi(ogrenci.Id, ay.Year, ay.Month);
                bool kayitVar = VeriYoneticisi.OdemeKaydiVarMi(ogrenci.Id, ay.Year, ay.Month);

                liste.Add(new AySatiri
                {
                    Yil = ay.Year,
                    Ay = ay.Month,
                    DersSayisi = VeriYoneticisi.AylikDersSayisi(ogrenci.Id, ay.Year, ay.Month),
                    Durum = odendi ? OdemeDurumu.Odendi : (kayitVar ? OdemeDurumu.Bekliyor : OdemeDurumu.KayitYok),
                    Tutar = VeriYoneticisi.OdemeTutari(ogrenci.Id, ay.Year, ay.Month)
                });
            }

            // En yeni ay üstte: geçmişe bakan kişi önce son duruma bakıyor.
            liste.Reverse();
            return liste;
        }

        // Kayıtlardaki yıl/ay değerleri elle düzenlenmiş bir XML dosyasından
        // gelmiş olabilir; geçersiz bir tarih tüm ekranı çökertmesin.
        private static DateTime? GuvenliAy(int yil, int ay)
        {
            if (yil < 1 || yil > 9999 || ay < 1 || ay > 12) return null;
            return new DateTime(yil, ay, 1);
        }

        private void SatirlariDoldur(List<AySatiri> satirlar)
        {
            foreach (var s in satirlar)
            {
                string odemeYazi;
                switch (s.Durum)
                {
                    case OdemeDurumu.Odendi: odemeYazi = "Ödendi"; break;
                    case OdemeDurumu.Bekliyor: odemeYazi = "Ödenmedi"; break;
                    default: odemeYazi = "—"; break;
                }

                int satirNo = dgvGecmis.Rows.Add(
                    new DateTime(s.Yil, s.Ay, 1).ToString("MMMM yyyy", TrKultur),
                    s.DersSayisi > 0 ? $"{s.DersSayisi} / 4" : "—",
                    odemeYazi,
                    s.Durum == OdemeDurumu.Odendi ? VeriYoneticisi.TutarYazi(s.Tutar) : "—");

                var satir = dgvGecmis.Rows[satirNo];

                if (s.Durum == OdemeDurumu.Odendi)
                {
                    satir.Cells["colOdeme"].Style.ForeColor = Renkler.Olumlu;
                    satir.Cells["colOdeme"].Style.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                }
                else if (s.OdemeEksik)
                {
                    // Bekleyen ödeme, listeye bakan kişinin aradığı tek şey
                    // olabilir; satırın tamamı ayrışıyor.
                    satir.DefaultCellStyle.BackColor = Renkler.OlumsuzSolgun;
                    satir.Cells["colOdeme"].Style.ForeColor = Renkler.Olumsuz;
                    satir.Cells["colOdeme"].Style.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                }
                else
                {
                    // Hareketi olmayan ay: borç değil, yalnızca boş.
                    satir.DefaultCellStyle.ForeColor = Renkler.MetinSolgun;
                }
            }
        }
    }
}
