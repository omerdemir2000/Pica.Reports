namespace Pica.Reports.Testleri;

public class KutuMetniSinamalari
{
    [Fact]
    public void Duz_yazi_ile_ifade_ayrilir()
    {
        var parcalar = KutuMetni.Parcala("Sayfa [Page#] / [TotalPages#]");

        Assert.Equal(
            [("Sayfa ", false), ("[Page#]", true), (" / ", false), ("[TotalPages#]", true)],
            parcalar.Select(p => (p.Metin, p.Ifade)));
    }

    [Fact]
    public void Kapanmayan_parantez_ifade_sayilmaz()
    {
        // Çözücü de böyle davranır; tasarımcı ifade diye gösterseydi kâğıtta
        // düz yazı olarak çıkan bir şeyi veri gibi işaretlerdi.
        var parcalar = KutuMetni.Parcala("Tutar [eksik");

        Assert.Single(parcalar);
        Assert.False(parcalar[0].Ifade);
        Assert.Equal("Tutar [eksik", parcalar[0].Metin);
    }

    [Fact]
    public void Ic_ice_gorunen_ifadede_ilk_kapanis_gecerlidir()
    {
        var parcalar = KutuMetni.Parcala("[FormatFloat(',0.00', <ds.\"alacak\">)].- TL");

        Assert.True(parcalar[0].Ifade);
        Assert.Equal("[FormatFloat(',0.00', <ds.\"alacak\">)]", parcalar[0].Metin);
        Assert.Equal(".- TL", parcalar[1].Metin);
    }

    [Fact]
    public void Metin_bossa_bagli_alan_gosterilir()
    {
        // Kâğıda basılacak olan odur; tasarımcıda boş görünseydi kutunun neden
        // dolu bastığı anlaşılmazdı.
        var nesne = Ornek.Kutu("K1");
        nesne.Metin = null;
        nesne.VeriAlani = "borc";

        var parcalar = KutuMetni.Parcala(nesne);

        Assert.Single(parcalar);
        Assert.True(parcalar[0].Ifade);
        Assert.Equal("[borc]", parcalar[0].Metin);
    }
}

public class AlanKataloguSinamalari
{
    [Theory]
    [InlineData("[yevno]", "yevno")]
    [InlineData("[yevdefds.\"yevno\"]", "yevno")]     // veri kümesi adı yok sayılır
    [InlineData("[ Bastarih ]", "Bastarih")]
    [InlineData("[Page#]", null)]                     // çizicinin kendi değişkeni
    [InlineData("[Date]", null)]
    [InlineData("[SUM(<ds.\"borc\">)]", null)]        // hesaplanan ifade, alan değil
    public void Ad_yalnizca_alan_basvurularini_dondurur(string ifade, string? beklenen)
        => Assert.Equal(beklenen, AlanKatalogu.Ad(ifade));

    [Fact]
    public void Katalog_duzende_gecen_adlari_tekilleyerek_toplar()
    {
        var duzen = Ornek.Kutulu(
            Ornek.Kutu("K1", metin: "[yevdefds.\"yevno\"] — [bastarih]"),
            Ornek.Kutu("K2", metin: "[yevno]"),                       // aynı alan, farklı yazım
            Ornek.Kutu("K3", metin: "Sayfa [Page#]"));                // değişken, alan değil

        Assert.Equal(["bastarih", "yevno"], AlanKatalogu.Cikar(duzen));
    }

    [Fact]
    public void Bagli_alan_da_kataloga_girer()
    {
        var kutu = Ornek.Kutu("K1");
        kutu.VeriAlani = "alacak";

        Assert.Contains("alacak", AlanKatalogu.Cikar(Ornek.Kutulu(kutu)));
    }
}
