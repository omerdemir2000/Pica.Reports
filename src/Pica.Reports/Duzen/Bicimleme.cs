using System.Globalization;
using System.Text;

namespace Pica.Reports.Duzen;

/// <summary>
/// Bir değerin kutunun biçim ayarına göre yazıya çevrilmesi.
/// </summary>
/// <remarks>
/// <para>
/// Kod çizicinin içindeydi ve orada kalamazdı: <b>tasarımcının da aynı biçimi
/// uygulaması gerekiyor</b>. Örnek veriyle çalışan önizleme, kutuya
/// <c>%2.2n</c> yazan kullanıcıya "1.234,56" göstermeli; iki taraf ayrı ayrı
/// biçimleseydi tasarımda görünenle kâğıda basılan ayrışırdı.
/// </para>
/// <para>
/// Desenler <b>Delphi'nindir</b>, .NET'in değil: şablonlar oradan taşındı ve
/// düzeltme dosyalarında da o desenler yazılı. Çeviri burada yapılıyor.
/// </para>
/// </remarks>
public static class Bicimleme
{
    /// <summary>Değeri kutunun biçim ayarına göre yazar.</summary>
    public static string Bicimle(object? deger, DuzenNesnesi nesne)
    {
        if (deger is null) return "";
        if (nesne.Bicim == BicimTuru.Yok || string.IsNullOrEmpty(nesne.BicimDeseni))
            return Metinle(deger);

        var kultur = Kultur(nesne);

        return nesne.Bicim switch
        {
            // Desen tanınmıyorsa (SayiDeseni null döner) uydurma bir biçim
            // seçilmez, varsayılan yazıma düşülür: tanınmayan deseni sessizce
            // iki ondalığa çevirmek, kâğıtta "30" yerine "30,00" basıp hatayı
            // görünmez kılıyordu.
            BicimTuru.Sayi when SayiyaCevir(deger) is { } s && SayiDeseni(nesne.BicimDeseni) is { } d
                => s.ToString(d, kultur),

            BicimTuru.Tarih or BicimTuru.Saat when TariheCevir(deger) is { } t
                => t.ToString(TarihDeseni(nesne.BicimDeseni), kultur),

            _ => Metinle(deger),
        };
    }

    /// <summary>Biçimi olmayan değerin varsayılan yazımı.</summary>
    public static string Metinle(object? deger) => deger switch
    {
        null => "",
        DateTime t => t.ToString("dd.MM.yyyy"),
        DateOnly t => t.ToString("dd.MM.yyyy"),
        decimal d => d.ToString("#,##0.00"),
        double d => d.ToString("#,##0.00"),
        _ => deger.ToString() ?? "",
    };

    /// <summary>
    /// Ayraçlar kutunun kendi ayarından gelir; şablonların bir kısmı nokta,
    /// bir kısmı virgül yazıyor ve kâğıtta ikisi karışmamalı.
    /// </summary>
    public static CultureInfo Kultur(DuzenNesnesi nesne)
    {
        var k = (CultureInfo)CultureInfo.CurrentCulture.Clone();

        if (!string.IsNullOrEmpty(nesne.OndalikAyraci))
            k.NumberFormat.NumberDecimalSeparator = nesne.OndalikAyraci;

        if (!string.IsNullOrEmpty(nesne.BinlikAyraci))
            k.NumberFormat.NumberGroupSeparator = nesne.BinlikAyraci;

        return k;
    }

    /// <summary>
    /// Delphi <c>Format</c> desenini .NET desenine çevirir; desen
    /// tanınmıyorsa <c>null</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Desen <c>%[genişlik].[ondalık]tür</c> biçimindedir. Taşınan 137 düzende
    /// geçen üç desen var: <c>%2.2n</c> (264 kutu), <c>%g</c> (60 kutu),
    /// <c>%2.2f</c> (7 kutu). <c>n</c> binlik ayraç ister, <c>f</c> istemez,
    /// <c>m</c> para birimi, <c>d</c> tam sayıdır. Genişlik alanı Delphi'de en
    /// az karakter sayısıdır; .NET'te karşılığı olmadığı ve kutular zaten
    /// hizalandığı için yok sayılır.
    /// </para>
    /// <para>
    /// <b><c>%g</c> "genel"dir: anlamsız ondalık sıfırlar atılır</b> ve binlik
    /// ayraç konmaz. Puantaj katsayıları ve gün sayıları bu desenle yazılıyor —
    /// tam gün <c>30</c>, yarım gün <c>29,5</c> basmalı. Desteklenmediği sürece
    /// bu kutular iki ondalığa düşürülüp <c>30,00</c> basıyordu.
    /// </para>
    /// <para>
    /// <b>Tanınmayan desende <c>null</c> dönülür</b>, varsayılan bir biçim
    /// uydurulmaz: çağıran (<see cref="Bicimle"/>) o zaman
    /// <see cref="Metinle"/>'ye düşer. Uydurulan biçim, desteklenmeyen bir
    /// deseni kâğıtta makul görünen ama yanlış bir sayıya çeviriyordu; hatanın
    /// görünür olması, sessizce yanlış basmaktan iyidir.
    /// </para>
    /// </remarks>
    public static string? SayiDeseni(string desen)
    {
        var d = desen.Trim();
        if (d.Length < 2 || d[0] != '%') return null;

        // % ile tür harfi arasında yalnızca genişlik ve ondalık olabilir.
        // "saçma" gibi bir metni desen sayıp son harfine bakmak, tanınmayan
        // deseni tanınmış gibi göstermek olurdu.
        var govde = d[1..^1];
        if (!govde.All(c => char.IsAsciiDigit(c) || c is '.' or '-' or '+')) return null;

        int? verilen = null;
        var nokta = govde.IndexOf('.');
        if (nokta >= 0 && nokta + 1 < govde.Length && char.IsAsciiDigit(govde[nokta + 1]))
            verilen = govde[nokta + 1] - '0';

        return char.ToLowerInvariant(d[^1]) switch
        {
            'n' or 'm' => "#,##0" + Kesir('0', verilen ?? 2),
            'f' => "0" + Kesir('0', verilen ?? 2),
            'd' => "#,##0",

            // '#' — basılacak basamak varsa basılır, yoksa hiç: "%g"nin işi bu.
            'g' => "0" + Kesir('#', verilen ?? 2),

            _ => null,
        };

        static string Kesir(char basamak, int adet)
            => adet > 0 ? "." + new string(basamak, adet) : "";
    }

    /// <summary>
    /// Delphi tarih deseni küçük harflidir (<c>dd.mm.yyyy</c>); .NET'te ay
    /// büyük <c>MM</c>'dir, küçük <c>mm</c> dakika demektir.
    /// </summary>
    public static string TarihDeseni(string desen)
    {
        var sb = new StringBuilder(desen.Length);
        var saatBolumu = false;

        foreach (var c in desen)
        {
            if (c is 'h' or 'H') saatBolumu = true;
            if (c is 'd' or 'y' or '/' or '.' or '-' or ':' or ' ') saatBolumu &= c is ':' or ' ';

            sb.Append(c == 'm' && !saatBolumu ? 'M' : c);
        }

        return sb.ToString();
    }

    public static decimal? SayiyaCevir(object deger) => deger switch
    {
        decimal d => d,
        double d => (decimal)d,
        float f => (decimal)f,
        int i => i,
        long l => l,
        string s when decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out var v) => v,
        _ => null,
    };

    public static DateTime? TariheCevir(object deger) => deger switch
    {
        DateTime t => t,
        DateOnly t => t.ToDateTime(TimeOnly.MinValue),
        string s when DateTime.TryParse(s, CultureInfo.CurrentCulture, out var v) => v,
        _ => null,
    };
}
