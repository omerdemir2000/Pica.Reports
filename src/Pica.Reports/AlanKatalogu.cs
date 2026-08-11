using Pica.Reports.Duzen;

namespace Pica.Reports;

/// <summary>
/// Düzende kullanılan veri alanlarının listesi.
/// </summary>
/// <remarks>
/// <para>
/// Düzen <b>veriyi tanımaz</b>: hangi alanların geleceğini ekranın servisi
/// bilir, düzen yalnızca adlarını yazar. Dolayısıyla elde gerçek bir alan
/// listesi yok. Buradaki katalog, düzenin kendisinin zaten kullandığı
/// başvurulardan çıkarılıyor — bir kutuyu veriye bağlarken doğru adı hatırlamak
/// yerine listeden seçebilmek için.
/// </para>
/// <para>
/// <b>Eksiksiz değildir ve olamaz.</b> Düzende hiç geçmeyen bir alan burada
/// görünmez; katalog bir ipucudur, bir sözleşme değil.
/// </para>
/// </remarks>
public static class AlanKatalogu
{
    /// <summary>
    /// Basımda değeri veriden gelmeyen, çizicinin kendi ürettiği başvurular.
    /// </summary>
    /// <remarks>Alan listesine girmemeliler: bir alana bağlanacak şeyler değil.</remarks>
    private static readonly HashSet<string> Degiskenler =
        new(StringComparer.OrdinalIgnoreCase) { "Page#", "TotalPages#", "Line#", "Date", "Time" };

    /// <summary>Düzende geçen alan adları, alfabetik.</summary>
    public static List<string> Cikar(CetvelDuzeni duzen)
    {
        HashSet<string> alanlar = new(StringComparer.OrdinalIgnoreCase);

        foreach (var nesne in duzen.Sayfalar.SelectMany(s => s.Bantlar).SelectMany(b => b.Nesneler))
        {
            if (nesne.VeriAlani is { Length: > 0 } bagli) alanlar.Add(bagli);

            if (nesne.Metin is not { Length: > 0 } metin) continue;

            foreach (var parca in KutuMetni.Parcala(metin).Where(p => p.Ifade))
                if (Ad(parca.Metin) is { } ad)
                    alanlar.Add(ad);
        }

        return [.. alanlar.Order(StringComparer.CurrentCulture)];
    }

    /// <summary>
    /// Bir ifadeden alan adını çıkarır; alan başvurusu değilse <c>null</c>.
    /// </summary>
    /// <remarks>
    /// <c>[yevdefds."yevno"]</c> ile <c>[yevno]</c> aynı alanı gösterir — veri
    /// kümesi adı çözücüde de yok sayılıyor, katalogda da sayılmamalı, yoksa
    /// aynı alan listeye iki kez girerdi.
    /// </remarks>
    public static string? Ad(string ifade) => Coz(ifade).Alan;

    /// <summary>
    /// İfadeyi veri kümesi ve alan adına ayırır; alan başvurusu değilse ikisi de
    /// <c>null</c>.
    /// </summary>
    /// <remarks>
    /// <c>[MuhFis."Aciklama"]</c> → <c>("MuhFis", "Aciklama")</c>. Küme adı
    /// çıktı basılırken yok sayılıyor (alan adları düzen içinde benzersiz), ama
    /// <b>tasarımda gerekli</b>: iki veri kümesi bağlı bir cetvelde hangi alanın
    /// hangi kümeden geldiği ancak buradan bilinir.
    /// </remarks>
    public static (string? Kume, string? Alan) Coz(string ifade)
    {
        var ic = ifade.Trim().Trim('[', ']').Trim();

        // Hesaplanan ifadeler (FormatFloat, SUM...) alan değildir; taşınmadılar
        // ve bir kutuya bağlanacak bir şey de değiller.
        if (ic.Contains('(') || ic.Contains('<')) return (null, null);

        string? kume = null;
        var nokta = ic.LastIndexOf('.');

        if (nokta >= 0)
        {
            kume = ic[..nokta].Trim('"', '\'', ' ');
            ic = ic[(nokta + 1)..];
            if (kume.Length == 0) kume = null;
        }

        ic = ic.Trim('"', '\'', ' ');

        return ic.Length == 0 || Degiskenler.Contains(ic) ? (null, null) : (kume, ic);
    }

    /// <summary>Kümesi bilinmeyen alanların toplandığı başlık.</summary>
    public const string Kumesiz = "(küme adsız)";

    /// <summary>
    /// Düzenin veri kümeleri ve her birinin alanları.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Kaynak düzenin kendisi: bantların <c>VeriKumesi</c> alanı kümeleri,
    /// kutulardaki <c>[MuhFis."Aciklama"]</c> başvuruları da alanları veriyor.
    /// Kümesi yazılmamış başvurular (<c>[borc]</c>) <b>bulundukları bandın</b>
    /// kümesine yazılıyor — bant hangi kümeden besleniyorsa içindeki kutu da
    /// ondan okur.
    /// </para>
    /// <para>
    /// <b>Eksiksiz değildir ve olamaz</b> (bkz. sınıf açıklaması): düzende hiç
    /// geçmeyen bir alan burada görünmez. Barındıran uygulama gerçek listeyi
    /// biliyorsa onu ayrıca verebilir; ikisi birleştirilir.
    /// </para>
    /// </remarks>
    public static List<VeriKumesiTanimi> Kumeler(CetvelDuzeni duzen)
    {
        // Alan adları büyük/küçük harfe duyarsız eşleşiyor — çözücü de öyle
        // arıyor. Duyarlı bir küme kullanılsaydı "Borc" ile "borc" ağaçta iki
        // ayrı alan olarak görünürdü.
        Dictionary<string, HashSet<string>> kumeler = new(StringComparer.OrdinalIgnoreCase);

        foreach (var bant in duzen.Sayfalar.SelectMany(s => s.Bantlar))
        {
            // Boş da olsa küme görünsün: bir veri bandı bağlı ama kutusu yoksa
            // "bu cetvelde şu küme var" bilgisi yine de doğrudur.
            if (bant.VeriKumesi is { Length: > 0 } bantKumesi) Kume(bantKumesi);

            foreach (var nesne in bant.Nesneler)
            {
                if (nesne.VeriAlani is { Length: > 0 } bagli)
                    Kume(bant.VeriKumesi).Add(bagli);

                if (nesne.Metin is not { Length: > 0 } metin) continue;

                foreach (var parca in KutuMetni.Parcala(metin).Where(p => p.Ifade))
                {
                    var (kume, alan) = Coz(parca.Metin);
                    if (alan is not null) Kume(kume ?? bant.VeriKumesi).Add(alan);
                }
            }
        }

        return Sirala(kumeler);

        HashSet<string> Kume(string? ad)
        {
            var anahtar = string.IsNullOrEmpty(ad) ? Kumesiz : ad;

            if (!kumeler.TryGetValue(anahtar, out var liste))
                kumeler[anahtar] = liste = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return liste;
        }
    }

    /// <summary>Kümeleri ve alanlarını okunacak sıraya dizer.</summary>
    /// <remarks>
    /// Kümesiz alanlar en sona: adı olan kümeler daha çok işe yarıyor. Alanlar
    /// kullanıcının kültürüne göre sıralanıyor — listeye bakan insan.
    /// </remarks>
    private static List<VeriKumesiTanimi> Sirala(Dictionary<string, HashSet<string>> kumeler)
        => [.. kumeler
            .Select(k => new VeriKumesiTanimi(
                k.Key,
                [.. k.Value.Order(StringComparer.CurrentCulture)]))
            .OrderBy(k => k.Ad == Kumesiz)
            .ThenBy(k => k.Ad, StringComparer.CurrentCulture)];

    /// <summary>
    /// İki katalogu birleştirir — düzenden çıkarılanla uygulamanın verdiğini.
    /// </summary>
    /// <remarks>
    /// Aynı adlı kümenin alanları birleşir. Uygulamanın listesi düzendekini
    /// <b>silmez</b>: düzende geçen ama uygulamanın bildirmediği bir alan hâlâ
    /// basılıyor ve tasarımda görünmesi gerekir.
    /// </remarks>
    public static List<VeriKumesiTanimi> Birlestir(
        IEnumerable<VeriKumesiTanimi> a, IEnumerable<VeriKumesiTanimi>? b)
    {
        Dictionary<string, HashSet<string>> kumeler = new(StringComparer.OrdinalIgnoreCase);

        foreach (var kume in a.Concat(b ?? []))
        {
            if (!kumeler.TryGetValue(kume.Ad, out var liste))
                kumeler[kume.Ad] = liste = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var alan in kume.Alanlar) liste.Add(alan);
        }

        return Sirala(kumeler);
    }
}

/// <summary>Bir veri kümesi ve alanları — tasarımcının alan ağacı.</summary>
/// <param name="Ad">Kümenin adı (<c>MuhFis</c>) ya da <see cref="AlanKatalogu.Kumesiz"/>.</param>
/// <param name="Alanlar">Alan adları, alfabetik.</param>
public sealed record VeriKumesiTanimi(string Ad, IReadOnlyList<string> Alanlar);
