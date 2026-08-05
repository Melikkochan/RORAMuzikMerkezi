using System;
using System.Text;

namespace RORAMuzikMerkezi
{
    // Arama karşılaştırmaları için ortak metin yardımcıları.
    //
    // Bu mantık önce yalnızca çalgı sekmesindeki aramada vardı. Genel arama
    // eklenince iki yerde farklı davranış oluşmasın diye buraya taşındı:
    // her iki arama da aynı kuralları kullanır.
    public static class Metin
    {
        // Aramayı Türkçe karakterlere ve büyük/küçük harfe duyarsız hâle getirir.
        //
        // Gerekçe: Türkçede I/ı ile İ/i ayrı harf çiftleridir. Kültüre duyarlı
        // karşılaştırmada "CIVAN" ile "civan", "BİLAL" ile "BILAL" eşleşmez.
        // Bu teknik olarak doğrudur ama arama kutusunda kullanıcıyı engeller:
        // klavyesine göre farklı i yazan kullanıcı öğrenciyi bulamaz.
        // Bu yüzden i, ı, İ, I hepsi tek bir harfe indirgenir; noktalı ve
        // kuyruklu diğer harfler de aynı biçimde sadeleştirilir.
        //
        // Sadeleştirme yalnızca karşılaştırmada kullanılır; kayıtlı veriye ve
        // ekranda gösterilen metne dokunulmaz.
        public static string AramaIcinNormalize(string metin)
        {
            if (string.IsNullOrEmpty(metin)) return string.Empty;

            var sb = new StringBuilder(metin.Length);
            foreach (char karakter in metin)
            {
                char k = karakter;
                switch (k)
                {
                    case 'İ': case 'I': case 'ı': k = 'i'; break;
                    case 'Ş': case 'ş': k = 's'; break;
                    case 'Ğ': case 'ğ': k = 'g'; break;
                    case 'Ü': case 'ü': k = 'u'; break;
                    case 'Ö': case 'ö': k = 'o'; break;
                    case 'Ç': case 'ç': k = 'c'; break;
                }
                sb.Append(char.ToLowerInvariant(k));
            }
            return sb.ToString();
        }

        // metin, aranan ifadeyi içeriyor mu? Karşılaştırma normalize edilmiş
        // hâller üzerinden yapılır.
        public static bool Iceriyor(string metin, string aranan)
        {
            if (string.IsNullOrEmpty(metin)) return false;
            if (string.IsNullOrEmpty(aranan)) return true;

            return AramaIcinNormalize(metin)
                .IndexOf(AramaIcinNormalize(aranan), StringComparison.Ordinal) >= 0;
        }

        // Bir öğrencinin arama ölçütüyle eşleşip eşleşmediği.
        // Ad, soyad ve telefon alanlarında arar.
        public static bool OgrenciEslesiyorMu(Ogrenci ogrenci, string arama)
        {
            if (ogrenci == null) return false;
            if (string.IsNullOrWhiteSpace(arama)) return true;

            return Iceriyor(ogrenci.TamAd, arama) || Iceriyor(ogrenci.Telefon, arama);
        }
    }
}
