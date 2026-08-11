using System.Reflection;

namespace Pica.Reports;

/// <summary>
/// Ürünün kimliği — ad, sürüm, üretici.
/// </summary>
/// <remarks>
/// <para>
/// Barındıran uygulamanın ekranına <b>kendiliğinden hiçbir şey yazılmıyor</b>:
/// kütüphane başkasının uygulamasının içinde çalışıyor ve oraya izinsiz bir
/// künye koymak doğru olmaz. Kütüphanenin KENDİ hazır ekranları
/// (<c>/pica/…</c>) künyeyi gösteriyor; bileşenleri kendi sayfanıza gömerseniz
/// göstermek de göstermemek de sizin kararınız.
/// </para>
/// <para>
/// Sürüm derlemenin kendisinden okunuyor: iki yerde yazılsa biri er geç eski
/// kalırdı.
/// </para>
/// </remarks>
public static class Urun
{
    public const string Ad = "Pica.Reports";

    public const string Firma = "Papirus Yazılım Ltd. Şti.";

    public const string Adres = "https://www.papirusbilisim.com";

    /// <summary>Paket sürümü — <c>0.9.0</c>.</summary>
    public static string Surum { get; } =
        typeof(Urun).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion.Split('+')[0]
        ?? typeof(Urun).Assembly.GetName().Version?.ToString(3)
        ?? "";

    /// <summary>Tek satırlık künye: <c>Pica.Reports 0.9.0 · Papirus Yazılım Ltd. Şti.</c></summary>
    public static string Kunye => $"{Ad} {Surum} · {Firma}";
}
