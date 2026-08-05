using System;
using System.Drawing;
using System.IO;

namespace RORAMuzikMerkezi
{
    // Uygulama görselleri (ikon, logo) çalışma dizinine göre değil,
    // uygulamanın bulunduğu klasöre göre çözümlenir. Böylece program
    // farklı bir çalışma dizini ayarlanmış bir kısayoldan başlatılsa da
    // görseller bulunur. Dosya bulunamazsa null döner; uygulama görselsiz
    // açılmaya devam eder, çökmez.
    public static class Varliklar
    {
        private static string TamYol(string dosyaAdi)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dosyaAdi);
        }

        public static Icon Ikon(string dosyaAdi)
        {
            try
            {
                string yol = TamYol(dosyaAdi);
                return File.Exists(yol) ? new Icon(yol) : null;
            }
            catch
            {
                return null;
            }
        }

        public static Image Resim(string dosyaAdi)
        {
            try
            {
                string yol = TamYol(dosyaAdi);
                return File.Exists(yol) ? Image.FromFile(yol) : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
