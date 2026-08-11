using Pica.Reports.Duzen;

namespace Pica.Reports;

/// <summary>
/// Alan adına bakarak türünü tahmin eder.
/// </summary>
/// <remarks>
/// <para>
/// <b>Tahmindir, bilgi değil.</b> Düzen veriyi tanımaz: alanların gerçek türünü
/// yalnızca ekranın servisi bilir ve tasarımcıya o bilgi gelmez. Elde bir tek
/// ad var.
/// </para>
/// <para>
/// Yine de işe yarıyor: alan ağacından <c>Borc</c> sürükleyen biri sağa
/// yaslanmış, iki ondalıklı bir kutu bekliyor; <c>Tarih</c> sürükleyen
/// <c>dd.mm.yyyy</c> bekliyor. Yanlış tahminin bedeli iki tıklama — özellik
/// panelinden biçim değiştirilir. Tahmin edilmeseydi <b>her</b> alan için o iki
/// tıklama gerekirdi.
/// </para>
/// <para>
/// Adlar Delphi şablonlarından geliyor ve Türkçe; aksanlı ve aksansız yazımları
/// birlikte aranıyor (<c>borc</c>/<c>borç</c>).
/// </para>
/// </remarks>
public static class AlanSezgisi
{
    public static bool Parasal(string alan) => Gecer(alan, ParaAdlari);

    public static bool Tarihsel(string alan) => Gecer(alan, TarihAdlari);

    /// <summary>Sıra numarası, fiş no gibi tam sayı alanları.</summary>
    public static bool Sirali(string alan) => Gecer(alan, SiraAdlari);

    /// <summary>Alana yakışan biçim; bilinmiyorsa <see cref="BicimTuru.Yok"/>.</summary>
    public static BicimTuru Bicim(string alan)
    {
        if (Tarihsel(alan)) return BicimTuru.Tarih;
        if (Parasal(alan)) return BicimTuru.Sayi;

        return BicimTuru.Yok;
    }

    /// <summary>Biçime yakışan Delphi deseni; biçimsiz alanda <c>null</c>.</summary>
    public static string? Desen(BicimTuru bicim) => bicim switch
    {
        BicimTuru.Sayi => "%2.2n",
        BicimTuru.Tarih => "dd.mm.yyyy",
        BicimTuru.Saat => "hh:nn",
        _ => null,
    };

    private static bool Gecer(string alan, string[] adlar)
        => adlar.Any(a => alan.Contains(a, StringComparison.OrdinalIgnoreCase));

    private static readonly string[] ParaAdlari =
    [
        "borc", "borç", "alacak", "tutar", "toplam", "bakiye", "miktar", "fiyat",
        "kdv", "meblag", "meblağ", "odenen", "ödenen", "kalan", "nakil", "genel",
        "gelir", "gider", "odenek", "ödenek", "harcama", "avans", "kesinti",
    ];

    private static readonly string[] TarihAdlari = ["tarih", "date", "vade", "gun", "gün"];

    private static readonly string[] SiraAdlari =
    [
        "sira", "sıra", "no", "numara", "sayi", "sayı", "fis", "fiş",
    ];
}
