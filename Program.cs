using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace RORAMuzikMerkezi
{
    static class Program
    {
        // Uygulama tek örnek çalışır. İkinci bir örnek açılırsa her iki örnek de
        // tüm veriyi belleğine alır ve sonradan kaydeden, diğerinin değişikliklerini
        // sessizce ezer. Mutex bunu kaynağında engeller.
        private const string TekOrnekAnahtari = @"Local\RORAMuzikMerkezi_TekOrnek";

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr pencere);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr pencere, int komut);

        private const int SW_RESTORE = 9;

        [STAThread]
        static void Main()
        {
            bool ilkOrnek;
            using (var kilit = new Mutex(true, TekOrnekAnahtari, out ilkOrnek))
            {
                if (!ilkOrnek)
                {
                    MevcutPencereyiOneGetir();
                    MessageBox.Show(
                        "RORA Sanat Merkezi zaten açık." + Environment.NewLine + Environment.NewLine +
                        "Uygulama aynı anda iki kez çalıştırılırsa, sonradan kaydeden pencere" + Environment.NewLine +
                        "diğerinin yaptığı değişiklikleri siler. Bu nedenle tek örnek çalışır.",
                        "Uygulama Zaten Açık", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                using (SplashForm splash = new SplashForm())
                {
                    splash.ShowDialog();
                }

                Application.Run(new MainForm());

                GC.KeepAlive(kilit);
            }
        }

        // Zaten açık olan pencereyi simge durumundan çıkarıp öne getirir,
        // böylece kullanıcı uygulamanın nerede olduğunu arar durumda kalmaz.
        private static void MevcutPencereyiOneGetir()
        {
            try
            {
                var benim = Process.GetCurrentProcess();
                foreach (var surec in Process.GetProcessesByName(benim.ProcessName))
                {
                    if (surec.Id == benim.Id) continue;
                    if (surec.MainWindowHandle == IntPtr.Zero) continue;

                    ShowWindow(surec.MainWindowHandle, SW_RESTORE);
                    SetForegroundWindow(surec.MainWindowHandle);
                    break;
                }
            }
            catch
            {
                // Pencereyi öne getirememek engelleyici değil; kullanıcı yine
                // uyarıyı görür ve ikinci örnek açılmaz.
            }
        }
    }
}