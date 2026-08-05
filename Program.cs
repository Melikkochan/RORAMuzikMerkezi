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

        // ---- Yüksek DPI ----

        [DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr deger);

        [DllImport("shcore.dll")]
        private static extern int SetProcessDpiAwareness(int deger);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        private static readonly IntPtr EkranBasinaV2 = new IntPtr(-4);   // PER_MONITOR_AWARE_V2
        private const int EkranBasina = 2;                                // PROCESS_PER_MONITOR_DPI_AWARE

        // Windows'a, ölçeklemeyi uygulamanın kendisinin yaptığını bildirir.
        // Bu bildirim olmadan Windows pencereyi bitmap gibi büyütüyor ve
        // yazılar ölçekli ekranlarda (%125, %150) bulanık çıkıyor.
        //
        // Normalde uygulama manifestosuyla bildirilir. Bu projede ClickOnce
        // manifestosu üretildiği için MSBuild Win32 manifestosunu bastırıyor
        // (csc /nowin32manifest) ve csproj'dan geçersiz kılınamıyor. Bildirim
        // bu yüzden çalışma anında, ilk pencere oluşmadan önce yapılıyor;
        // işletim sistemi açısından sonuç aynı.
        //
        // Yeniden eskiye doğru denenir. Desteklenmeyen sürümde giriş noktası
        // bulunamaz, bir alt yönteme düşülür; hiçbiri yoksa uygulama eskisi
        // gibi çalışmaya devam eder, yalnızca bulanıklık kalır.
        private static void DpiFarkindaligiBildir()
        {
            try { if (SetProcessDpiAwarenessContext(EkranBasinaV2)) return; }
            catch (EntryPointNotFoundException) { } catch (DllNotFoundException) { }

            try { if (SetProcessDpiAwareness(EkranBasina) == 0) return; }
            catch (EntryPointNotFoundException) { } catch (DllNotFoundException) { }

            try { SetProcessDPIAware(); }
            catch (EntryPointNotFoundException) { } catch (DllNotFoundException) { }
        }

        [STAThread]
        static void Main()
        {
            // Her şeyden önce: pencere oluşturulduktan sonra bildirmek etkisiz.
            DpiFarkindaligiBildir();

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