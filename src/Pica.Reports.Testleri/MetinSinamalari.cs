using System.Globalization;

namespace Pica.Reports.Testleri;

/// <summary>
/// Dil desteği.
/// </summary>
/// <remarks>
/// Varsayılan dil İngilizce, çevirisi Türkçe. Kültür uygulamadan geliyor
/// (<see cref="CultureInfo.CurrentUICulture"/>), kütüphanenin kendi ayarı yok;
/// sınamalar da kültürü geçici olarak değiştirip okuyor.
/// </remarks>
public class MetinSinamalari : IDisposable
{
    private readonly CultureInfo onceki = CultureInfo.CurrentUICulture;

    private static void Dil(string kod)
        => CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(kod);

    public void Dispose() => CultureInfo.CurrentUICulture = onceki;

    [Fact]
    public void Varsayilan_dil_ingilizce()
    {
        // Sözlükte karşılığı olmayan bir dil istenirse nötr kaynak, yani
        // İngilizce dönmeli.
        Dil("fr");

        Assert.Equal("Close", Metin.Al("Kapat"));
    }

    [Fact]
    public void Turkce_ceviri_uygulanir()
    {
        Dil("tr");

        Assert.Equal("Kapat", Metin.Al("Kapat"));
        Assert.Equal("Rapor düzenleri", Metin.Al("Liste_Baslik"));
    }

    [Fact]
    public void Ingilizce_metinler_okunur()
    {
        Dil("en");

        Assert.Equal("Report layouts", Metin.Al("Liste_Baslik"));
    }

    [Fact]
    public void Yer_tutucular_doldurulur()
    {
        Dil("en");
        Assert.Equal("3 layouts", Metin.Al("Liste_Sayi", 3));

        Dil("tr");
        Assert.Equal("3 düzen", Metin.Al("Liste_Sayi", 3));
    }

    [Fact]
    public void Eksik_anahtar_kendini_gosterir()
    {
        // Eksik çeviri ekranı bozmaz; köşeli parantez içinde anahtarı görürsünüz
        // ve neyin eksik olduğu belli olur.
        Assert.Equal("[BoyleBirAnahtarYok]", Metin.Al("BoyleBirAnahtarYok"));
    }

    [Fact]
    public void Model_etiketleri_de_cevriliyor()
    {
        Dil("en");
        Assert.Equal("Page footer", Etiketler.Bant(BantTuru.SayfaSonu));
        Assert.Equal("Barcode", Etiketler.Nesne(NesneTuru.Barkod));

        Dil("tr");
        Assert.Equal("Sayfa altı", Etiketler.Bant(BantTuru.SayfaSonu));
        Assert.Equal("Barkod", Etiketler.Nesne(NesneTuru.Barkod));
    }

    [Fact]
    public void Simgelem_adlari_cevrilmez()
    {
        // "Code 128" ve "EAN-13" simgelemlerin kendi adları; her dilde aynı.
        Dil("tr");
        Assert.Equal("Code 128", Etiketler.Barkod(BarkodTuru.Code128));

        Dil("en");
        Assert.Equal("EAN-13", Etiketler.Barkod(BarkodTuru.Ean13));
    }

    [Fact]
    public void Hizalama_adlari_cevriliyor()
    {
        Dil("en");
        Assert.Equal("Align left", Hizalama.Ad(HizalamaTuru.Sol));

        Dil("tr");
        Assert.Equal("Sola hizala", Hizalama.Ad(HizalamaTuru.Sol));
    }

    [Fact]
    public void Desteklenen_diller_bildiriliyor()
        => Assert.Equal(["en", "tr"], Metin.Diller.Select(d => d.TwoLetterISOLanguageName));
}
