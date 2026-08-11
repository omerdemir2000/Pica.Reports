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
            BicimTuru.Sayi when SayiyaCevir(deger) is { } s
                => s.ToString(SayiDeseni(nesne.BicimDeseni), kultur),

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
    /// Delphi <c>Format</c> desenini .NET desenine çevirir.
    /// </summary>
    /// <remarks>
    /// Şablonlarda geçen tek biçim <c>%[genişlik].[ondalık]n</c>'dir
    /// (<c>%2.2n</c> = binlik ayraçlı, iki ondalıklı). <c>n</c> binlik ayraç
    /// ister, <c>f</c> istemez, <c>m</c> para birimidir. Genişlik alanı
    /// Delphi'de en az karakter sayısıdır; .NET'te karşılığı olmadığı ve
    /// kutular zaten hizalandığı için yok sayılır.
    /// </remarks>
    public static string SayiDeseni(string desen)
    {
        var d = desen.Trim();
        if (d.Length == 0 || d[0] != '%') return "#,##0.00";

        var harf = char.ToLowerInvariant(d[^1]);
        var ondalik = 2;

        var nokta = d.IndexOf('.');
        if (nokta >= 0 && nokta + 1 < d.Length && char.IsDigit(d[nokta + 1]))
            ondalik = d[nokta + 1] - '0';

        var kesir = ondalik > 0 ? "." + new string('0', ondalik) : "";

        return harf switch
        {
            'n' or 'm' => "#,##0" + kesir,
            'f' => "0" + kesir,
            'd' => "#,##0",
            _ => "#,##0" + kesir,
        };
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
