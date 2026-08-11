namespace Pica.Reports.Duzen;

/// <param name="Metin">Parçanın içeriği. İfade parçalarında köşeli parantezler dahildir.</param>
/// <param name="Ifade">Basımda değeriyle değiştirilecek bir başvuru mu?</param>
public readonly record struct MetinParcasi(string Metin, bool Ifade);

/// <summary>
/// Kutu metnini düz yazı ve ifade parçalarına ayırır.
/// </summary>
/// <remarks>
/// <para>
/// FastReport kutu metnini ikisinin karışımı olarak tutar:
/// <c>Sayfa [Page#] / [TotalPages#]</c>. Tasarımcının ikisini ayırt etmesi
/// gerekir, çünkü kâğıtta görünecek şey bakımından bambaşkadırlar: düz yazı
/// olduğu gibi basılır, ifade veriden gelen bir değerle değişir. Aynı görünen
/// iki kutudan birinin sabit metin taşıdığını başka türlü anlamanın yolu yok.
/// </para>
/// <para>
/// <b>Bu bir ayrıştırıcı değil, ayıklayıcıdır.</b> İfadenin ne anlama geldiğini
/// bilmez, yalnızca nerede başlayıp bittiğini bulur — ve bunu basımı yapan
/// çözücüyle <i>tıpatıp aynı</i> kuralla yapar: ilk <c>[</c>, ondan sonraki ilk
/// <c>]</c>. İç içe parantez aranmaz; <c>[FormatFloat(',0.00',
/// &lt;ds."alacak"&gt;)]</c> gibi ifadelerde de doğru sonuç verir çünkü içeride
/// kapanan köşeli parantez yoktur. Kural değişirse tasarımcı, çıktıda ifade
/// sayılmayan bir şeyi ifade diye gösterirdi.
/// </para>
/// </remarks>
public static class KutuMetni
{
    /// <summary>
    /// Kutunun basılacak metnini parçalarına ayırır.
    /// </summary>
    /// <remarks>
    /// Metin boş ama kutu bir alana bağlıysa (<see cref="DuzenNesnesi.VeriAlani"/>)
    /// o alan tek parça olarak döner: kâğıda basılacak olan odur. Tasarımcıda
    /// boş görünseydi kutunun neden dolu bastığı anlaşılmazdı.
    /// </remarks>
    public static List<MetinParcasi> Parcala(DuzenNesnesi nesne)
        => string.IsNullOrEmpty(nesne.Metin)
            ? string.IsNullOrEmpty(nesne.VeriAlani)
                ? []
                : [new MetinParcasi($"[{nesne.VeriAlani}]", true)]
            : Parcala(nesne.Metin);

    /// <summary>Verilen metni parçalarına ayırır.</summary>
    public static List<MetinParcasi> Parcala(string metin)
    {
        List<MetinParcasi> parcalar = [];
        var i = 0;

        while (i < metin.Length)
        {
            var ac = metin.IndexOf('[', i);
            if (ac < 0) { Ekle(metin[i..], false); break; }

            var kapa = metin.IndexOf(']', ac + 1);

            // Kapanmayan parantez ifade değildir; kalan her şey düz yazıdır.
            // Çözücü de böyle davranır, dolayısıyla kâğıda da öyle basılır.
            if (kapa < 0) { Ekle(metin[i..], false); break; }

            Ekle(metin[i..ac], false);
            Ekle(metin[ac..(kapa + 1)], true);

            i = kapa + 1;
        }

        return parcalar;

        void Ekle(string s, bool ifade)
        {
            if (s.Length > 0) parcalar.Add(new MetinParcasi(s, ifade));
        }
    }
}
