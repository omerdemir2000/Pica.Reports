using Microsoft.AspNetCore.Components;
using Pica.Reports.Veri;

namespace Pica.Reports.Servisler;

/// <summary>
/// Rapor ekranlarını koddan açan araç — Delphi'deki <c>DesignReport</c>'un
/// web karşılığı.
/// </summary>
/// <remarks>
/// <para>
/// Kendi ekranınızda sorguyu çalıştırır, veriyi bağlar ve tasarım ya da
/// önizleme ekranını açarsınız:
/// </para>
/// <example>
/// <code>
/// var veri = new RaporVerisi()
///     .Ekle("Fisler", await baglanti.QueryAsync("select * from fis"))
///     .Degisken("KurumAdi", kurum.Ad);
///
/// await Rapor.TasarimAc("fatura", veri);   // tasarım ekranı
/// await Rapor.Onizle("fatura", veri);      // önizleme
/// </code>
/// </example>
/// <para>
/// Veri <b>gezinmeyi aşar</b>: araç kapsamlıdır (Blazor'da bir kullanıcı
/// devresi) ve bağlanan veriyi kendinde tutar, açılan ekran onu oradan alır.
/// Adres çubuğundan taşımak mümkün değildi — bir cetvelin satırları adrese
/// sığmaz ve sığsa da orada durmamalı.
/// </para>
/// </remarks>
public interface IRaporAraci
{
    /// <summary>Bir düzene veri bağlar; ekran açılmaz.</summary>
    /// <remarks>
    /// Aynı düzene ikinci kez bağlamak öncekinin yerine geçer: rapor her
    /// açılışta güncel veriyle basılmalı.
    /// </remarks>
    void Bagla(string anahtar, RaporVerisi veri);

    /// <summary>Düzene bağlı veri; bağlanmamışsa <c>null</c>.</summary>
    RaporVerisi? VeriAl(string anahtar);

    /// <summary>Tasarım ekranını açar.</summary>
    Task TasarimAc(string anahtar, RaporVerisi? veri = null);

    /// <summary>Önizleme ekranını açar.</summary>
    Task Onizle(string anahtar, RaporVerisi? veri = null);

    /// <summary>
    /// Önizlemeyi açar ve tarayıcının yazdırma kutusunu gösterir.
    /// </summary>
    /// <remarks>
    /// Yazdırılan şey önizlemenin kendisidir; kütüphane yazıcıya doğrudan bir
    /// şey göndermez, kâğıdın ölçüsünü <c>@@page</c> ile bildirip işi tarayıcıya
    /// bırakır.
    /// </remarks>
    Task Yazdir(string anahtar, RaporVerisi? veri = null);

    /// <summary>Düzen listesini açar.</summary>
    Task ListeAc();
}

/// <inheritdoc cref="IRaporAraci"/>
public sealed class RaporAraci(NavigationManager gezinme) : IRaporAraci
{
    /// <summary>Hazır ekranların adres öneki.</summary>
    /// <remarks>
    /// Sabit: <c>@page</c> yönergesi derleme anında yazılıyor, ayardan
    /// okunamıyor. Kendi adres düzenini isteyen uygulama hazır ekranları
    /// kullanmaz, kendi sayfasına bileşeni koyar.
    /// </remarks>
    public const string Onek = "pica";

    private readonly Dictionary<string, RaporVerisi> veriler = new(StringComparer.OrdinalIgnoreCase);

    public void Bagla(string anahtar, RaporVerisi veri) => veriler[anahtar] = veri;

    public RaporVerisi? VeriAl(string anahtar) => veriler.GetValueOrDefault(anahtar);

    public Task TasarimAc(string anahtar, RaporVerisi? veri = null)
        => Ac($"/{Onek}/tasarim/{anahtar}", anahtar, veri);

    public Task Onizle(string anahtar, RaporVerisi? veri = null)
        => Ac($"/{Onek}/onizleme/{anahtar}", anahtar, veri);

    /// <remarks>
    /// Yazdırma isteği adres çubuğunda taşınıyor (<c>?yazdir=1</c>): önizleme
    /// ekranı açıldığında kendini yazdırma kutusuna verir. Böylece bağlantı
    /// paylaşılabilir ve yenilendiğinde aynı şeyi yapar.
    /// </remarks>
    public Task Yazdir(string anahtar, RaporVerisi? veri = null)
        => Ac($"/{Onek}/onizleme/{anahtar}?yazdir=1", anahtar, veri);

    public Task ListeAc()
    {
        gezinme.NavigateTo($"/{Onek}/duzenler");
        return Task.CompletedTask;
    }

    private Task Ac(string adres, string anahtar, RaporVerisi? veri)
    {
        if (veri is not null) Bagla(anahtar, veri);

        gezinme.NavigateTo(adres);
        return Task.CompletedTask;
    }
}
