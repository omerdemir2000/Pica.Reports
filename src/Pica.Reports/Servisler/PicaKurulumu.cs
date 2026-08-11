using Microsoft.Extensions.DependencyInjection;
using Pica.Reports.Duzen;

namespace Pica.Reports.Servisler;

/// <summary>Kütüphanenin kurulum ayarları.</summary>
public sealed class PicaSecenekleri
{
    /// <summary>
    /// Düzen dosyalarının klasörü. Göreli verilirse uygulamanın çalışma
    /// dizinine göre çözülür.
    /// </summary>
    /// <remarks>
    /// ASP.NET Core uygulamalarında çalışma dizini içerik köküdür, yani
    /// <c>"Raporlar"</c> demek proje klasöründeki <c>Raporlar</c> demektir.
    /// Kütüphane barındırma soyutlamalarına (<c>IWebHostEnvironment</c>)
    /// başvurmuyor: WebAssembly'de o tür yok ve tek bir dize her yerde çalışır.
    /// </remarks>
    public string Duzenler { get; set; } = "Duzenler";

    /// <summary>
    /// Hazır ekranlar kaydedilsin mi? Kapatılırsa yalnızca servisler gelir.
    /// </summary>
    /// <remarks>
    /// Hazır ekranlar <c>/pica/…</c> adreslerinde durur ve uygulamanın kendi
    /// yönlendiricisine <b>ek derleme</b> olarak tanıtılmalıdır (bkz. README).
    /// Kendi sayfalarını yazan uygulamalar bunu kapatabilir.
    /// </remarks>
    public bool HazirEkranlar { get; set; } = true;
}

/// <summary>Kütüphaneyi uygulamaya bağlayan kurulum.</summary>
public static class PicaKurulumu
{
    /// <summary>
    /// Düzen deposunu ve rapor aracını kaydeder.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Depo <b>tekil</b> (singleton): dosya sistemine bakıyor, durumu yok.
    /// Rapor aracı <b>kapsamlı</b> (scoped): Blazor'da kapsam bir kullanıcı
    /// devresidir, dolayısıyla bir kullanıcının rapora bağladığı veri başka
    /// kullanıcıya sızmaz.
    /// </para>
    /// <example>
    /// <code>
    /// builder.Services.AddPicaReports(o => o.Duzenler = "Raporlar");
    /// </code>
    /// </example>
    /// </remarks>
    public static IServiceCollection AddPicaReports(
        this IServiceCollection hizmetler, Action<PicaSecenekleri>? ayar = null)
    {
        var secenekler = new PicaSecenekleri();
        ayar?.Invoke(secenekler);

        hizmetler.AddSingleton(secenekler);

        // Uygulama kendi deposunu zaten kaydettiyse ona dokunulmuyor:
        // TryAdd, "hazır olanı kullan ama benimkini ezme" demenin yolu.
        hizmetler.AddSingleton<IDuzenDeposu>(_ => new DosyaDuzenDeposu(Klasor(secenekler.Duzenler)));

        hizmetler.AddScoped<IRaporAraci, RaporAraci>();

        return hizmetler;
    }

    /// <summary>
    /// Kendi deposunu veren uygulamalar için: yalnızca rapor aracını kaydeder.
    /// </summary>
    public static IServiceCollection AddPicaReports<TDepo>(
        this IServiceCollection hizmetler, Action<PicaSecenekleri>? ayar = null)
        where TDepo : class, IDuzenDeposu
    {
        var secenekler = new PicaSecenekleri();
        ayar?.Invoke(secenekler);

        hizmetler.AddSingleton(secenekler);
        hizmetler.AddScoped<IDuzenDeposu, TDepo>();
        hizmetler.AddScoped<IRaporAraci, RaporAraci>();

        return hizmetler;
    }

    private static string Klasor(string yol)
        => Path.IsPathRooted(yol) ? yol : Path.Combine(Directory.GetCurrentDirectory(), yol);
}
