using System.Globalization;
using System.Resources;

namespace Pica.Reports;

/// <summary>
/// Arayüz metinleri — kullanıcının gördüğü her şey buradan geçer.
/// </summary>
/// <remarks>
/// <para>
/// <b>Varsayılan dil İngilizce</b> (<c>Kaynak.resx</c>), Türkçesi
/// <c>Kaynak.tr.resx</c>. .NET'in kendi kaynak düzeni kullanılıyor: nötr
/// kaynaklar derlemenin içine gömülü, çeviriler uydu derlemelere
/// (<c>tr/Pica.Reports.resources.dll</c>) çıkıyor. Yeni bir dil eklemek yeni
/// bir <c>Kaynak.&lt;dil&gt;.resx</c> dosyası demek; kod değişmiyor.
/// </para>
/// <para>
/// Dil <see cref="CultureInfo.CurrentUICulture"/>'dan geliyor, bir ayardan
/// değil: barındıran uygulama kültürü nasıl belirliyorsa (istek yerelleştirme
/// ara katmanı, çerez, kullanıcı tercihi) tasarımcı da onu izler. Kütüphanenin
/// kendi dil ayarı olsaydı iki ayar birbiriyle çelişirdi.
/// </para>
/// <para>
/// <b>Kod ve API Türkçe kalır</b> (bkz. CONTRIBUTING). Yerelleşen şey
/// arayüzdür; sınıf ve üye adları değil. Hata iletileri de çevrilmiyor:
/// onların okuru geliştirici, yeri günlük dosyası.
/// </para>
/// </remarks>
public static class Metin
{
    private static readonly ResourceManager Kaynak =
        new("Pica.Reports.Kaynak", typeof(Metin).Assembly);

    /// <summary>
    /// Anahtarın karşılığı; bulunamazsa anahtarın kendisi.
    /// </summary>
    /// <remarks>
    /// Eksik çeviri ekranı bozmaz, kendini gösterir: köşeli parantez içinde
    /// anahtar görürseniz o metin kaynak dosyasına eklenmemiş demektir.
    /// </remarks>
    public static string Al(string anahtar)
    {
        try
        {
            return Kaynak.GetString(anahtar, CultureInfo.CurrentUICulture) ?? "[" + anahtar + "]";
        }
        catch (MissingManifestResourceException)
        {
            return "[" + anahtar + "]";
        }
    }

    /// <summary>Yer tutuculu metin — <c>{0}</c>, <c>{1}</c>…</summary>
    public static string Al(string anahtar, params object?[] degerler)
        => string.Format(CultureInfo.CurrentUICulture, Al(anahtar), degerler);

    // Kısayol: bileşenlerde @Metin.T("Kapat") diye okunuyor, Al() ile aynı.
    // İki ad var çünkü Razor içinde tek harflik ad okumayı kolaylaştırıyor,
    // C# tarafında Al() daha açık duruyor.

    /// <inheritdoc cref="Al(string)"/>
    public static string T(string anahtar) => Al(anahtar);

    /// <inheritdoc cref="Al(string, object[])"/>
    public static string T(string anahtar, params object?[] degerler) => Al(anahtar, degerler);

    /// <summary>Kütüphanenin çevirisi olan diller.</summary>
    /// <remarks>
    /// Listeyi kod tutuyor: uydu derlemeleri çalışma anında saymak mümkün ama
    /// tek dosyalık yayınlarda (single-file) güvenilir değil.
    /// </remarks>
    public static IReadOnlyList<CultureInfo> Diller { get; } =
    [
        CultureInfo.GetCultureInfo("en"),
        CultureInfo.GetCultureInfo("tr"),
    ];
}
