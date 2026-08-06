using System;
using System.Windows.Forms;

namespace RORAMuzikMerkezi
{
    // Arayüz 96 DPI'a, yani %100 ölçeğe göre yazıldı. Formlar ve denetimler
    // AutoScaleMode.Dpi ile işaretlendiği için konum ve boyutları WinForms
    // kendisi ölçekliyor.
    //
    // Ölçeklenmeyen iki şey kalıyor: kod içinde çalışma anında hesaplanan
    // piksel değerleri ve DataGridView'in satır/başlık yükseklikleri. Bu
    // sınıf onlar için; tasarım pikselini içinde bulunulan ekranın ölçeğine
    // çevirir.
    public static class Olcek
    {
        // Yerleşimin yazıldığı ölçek. Değiştirilmemeli; tasarım değerleri
        // buna göre yorumlanıyor.
        public const float TasarimDpi = 96f;

        public static float Kat(Control denetim)
        {
            if (denetim == null) return 1f;
            return denetim.DeviceDpi / TasarimDpi;
        }

        // Tasarım pikselini geçerli ekranın ölçeğine çevirir. Denetimin
        // tutamacı oluşmadan önce çağrılırsa DeviceDpi güvenilir olmayabilir;
        // bu yüzden çağıranlar OnHandleCreated sonrasını bekliyor.
        public static int Piksel(Control denetim, int tasarimPikseli)
        {
            return (int)Math.Round(tasarimPikseli * Kat(denetim));
        }
    }
}
