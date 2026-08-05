using System;
using System.Drawing;
using System.Windows.Forms;

namespace RORAMuzikMerkezi
{
    public class SplashForm : Form
    {
        private Timer timer;
        private ProgressBar progressBar;

        public SplashForm()
        {
            this.Text = "RORA Sanat Merkezi";
            this.Size = new Size(560, 340);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(35, 45, 100);

            PictureBox logo = new PictureBox
            {
                Image = Varliklar.Resim("rora1.png"),
                Location = new Point(180, 20),
                Size = new Size(200, 80),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            this.Controls.Add(logo);

            Label lblBaslik = new Label
            {
                Text = "RORA SANAT MERKEZİ",
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(30, 100),
                Size = new Size(500, 45),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblAltBaslik = new Label
            {
                Text = "Öğrenci Takip Sistemi",
                Font = new Font("Segoe UI", 13),
                ForeColor = Color.FromArgb(220, 225, 255),
                Location = new Point(30, 150),
                Size = new Size(500, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblGelisitirici = new Label
            {
                Text = "Geliştirici: Melik KOÇHAN",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 220, 120),
                Location = new Point(30, 205),
                Size = new Size(500, 28),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblYukleniyor = new Label
            {
                Text = "Program hazırlanıyor...",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                Location = new Point(30, 245),
                Size = new Size(500, 22),
                TextAlign = ContentAlignment.MiddleCenter
            };

            progressBar = new ProgressBar
            {
                Location = new Point(90, 275),
                Size = new Size(380, 18),
                Style = ProgressBarStyle.Continuous
            };

            timer = new Timer();
            timer.Interval = 25;
            timer.Tick += Timer_Tick;

            this.Controls.Add(logo);
            this.Controls.Add(lblBaslik);
            this.Controls.Add(lblAltBaslik);
            this.Controls.Add(lblGelisitirici);
            this.Controls.Add(lblYukleniyor);
            this.Controls.Add(progressBar);

            this.Load += (s, e) => timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (progressBar.Value < 100)
            {
                progressBar.Value += 1+3/4;
            }
            else
            {
                timer.Stop();
                this.Close();
            }
        }
    }
}