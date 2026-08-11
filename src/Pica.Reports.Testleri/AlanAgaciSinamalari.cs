namespace Pica.Reports.Testleri;

/// <summary>
/// Alan ağacının sınamaları — hangi alanın hangi veri kümesine düştüğü.
/// </summary>
/// <remarks>
/// İki veri kümesi bağlı bir cetvelde (muhasebe fişleri listesi: <c>MuhFis</c>
/// ve <c>MuhFisOzet</c>) alanın yanlış kümeye düşmesi, sürüklendiğinde yanlış
/// bandın verisini basan bir kutu demek — ve bu kâğıtta boş bir hücre olarak
/// görünür, hata mesajı olarak değil.
/// </remarks>
public class AlanAgaciSinamalari
{
    private static CetvelDuzeni Duzen(params DuzenBandi[] bantlar) => new()
    {
        Anahtar = "d",
        Ad = "Düzen",
        Kaynak = "",
        Sayfalar = [new DuzenSayfasi { GenislikPt = 595, YukseklikPt = 842, Bantlar = [.. bantlar] }],
    };

    private static DuzenBandi Bant(string ad, string? kume, params DuzenNesnesi[] nesneler) => new()
    {
        Ad = ad,
        Tur = BantTuru.Veri,
        VeriKumesi = kume,
        YukseklikPt = 20,
        Nesneler = [.. nesneler],
    };

    private static DuzenNesnesi Kutu(string ad, string? metin = null, string? veriAlani = null)
    {
        var n = Ornek.Kutu(ad);
        n.Metin = metin;
        n.VeriAlani = veriAlani;
        return n;
    }

    [Theory]
    [InlineData("[MuhFis.\"Aciklama\"]", "MuhFis", "Aciklama")]
    [InlineData("[yevdefds.\"yevno\"]", "yevdefds", "yevno")]
    [InlineData("[borc]", null, "borc")]
    [InlineData("[ Bastarih ]", null, "Bastarih")]
    public void Ifade_kume_ve_alana_ayrilir(string ifade, string? kume, string? alan)
        => Assert.Equal((kume, alan), AlanKatalogu.Coz(ifade));

    [Theory]
    [InlineData("[Page#]")]                        // çizicinin kendi değişkeni
    [InlineData("[SUM(<MuhFis.\"Borc\">)]")]       // hesaplanan ifade
    [InlineData("[FormatFloat(',0.00', <ds.\"a\">)]")]
    public void Alan_olmayan_ifadeler_agaca_girmez(string ifade)
        => Assert.Equal((null, null), AlanKatalogu.Coz(ifade));

    [Fact]
    public void Iki_kume_ayri_ayri_listelenir()
    {
        var duzen = Duzen(
            Bant("Veri1", "MuhFis", Kutu("K1", "[MuhFis.\"Aciklama\"]"), Kutu("K2", "[MuhFis.\"FisNo\"]")),
            Bant("Veri2", "MuhFisOzet", Kutu("K3", "[MuhFisOzet.\"IhaleTuru\"]")));

        var kumeler = AlanKatalogu.Kumeler(duzen);

        Assert.Equal(["MuhFis", "MuhFisOzet"], kumeler.Select(k => k.Ad));
        Assert.Equal(["Aciklama", "FisNo"], kumeler[0].Alanlar);
        Assert.Equal(["IhaleTuru"], kumeler[1].Alanlar);
    }

    [Fact]
    public void Kumesiz_basvuru_bandin_kumesine_yazilir()
    {
        // [borc] hangi kümeden geldiğini söylemiyor ama bulunduğu bant
        // söylüyor: bant o kümeden besleniyorsa içindeki kutu da ondan okur.
        var duzen = Duzen(Bant("Veri1", "MuhFis", Kutu("K1", "[borc]")));

        var kume = Assert.Single(AlanKatalogu.Kumeler(duzen));

        Assert.Equal("MuhFis", kume.Ad);
        Assert.Equal(["borc"], kume.Alanlar);
    }

    [Fact]
    public void Bandi_kumesiz_olan_alanlar_ayri_baslikta_toplanir()
    {
        var duzen = Duzen(Bant("Baslik1", null, Kutu("K1", "[kurum]")));

        var kume = Assert.Single(AlanKatalogu.Kumeler(duzen));

        Assert.Equal(AlanKatalogu.Kumesiz, kume.Ad);
    }

    [Fact]
    public void Bagli_alan_da_agaca_girer()
    {
        // VeriAlani metinsiz kutularda tek kaynaktır; atlanırsa sütunların
        // yarısı ağaçta görünmezdi.
        var duzen = Duzen(Bant("Veri1", "MuhFis", Kutu("K1", veriAlani: "FisToplami")));

        Assert.Equal(["FisToplami"], AlanKatalogu.Kumeler(duzen)[0].Alanlar);
    }

    [Fact]
    public void Bos_veri_bandi_da_kume_olarak_gorunur()
    {
        // Kutusu olmayan ama bir kümeye bağlı bant: "bu cetvelde şu küme var"
        // bilgisi doğru ve alan sürüklemenin başlangıcı burası.
        var duzen = Duzen(Bant("Veri1", "MuhFisOzet"));

        Assert.Equal(["MuhFisOzet"], AlanKatalogu.Kumeler(duzen).Select(k => k.Ad));
        Assert.Empty(AlanKatalogu.Kumeler(duzen)[0].Alanlar);
    }

    [Fact]
    public void Ayni_alan_iki_kez_girmez()
    {
        var duzen = Duzen(Bant("Veri1", "MuhFis",
            Kutu("K1", "[MuhFis.\"Borc\"]"),
            Kutu("K2", "[borc]", veriAlani: "BORC")));

        Assert.Single(AlanKatalogu.Kumeler(duzen)[0].Alanlar);
    }

    [Fact]
    public void Uygulamanin_listesi_duzendekini_silmez()
    {
        // Düzende geçen ama uygulamanın bildirmediği alan hâlâ basılıyor;
        // ağaçtan düşerse kullanıcı onu bir daha bulamaz.
        var duzenden = new[] { new VeriKumesiTanimi("MuhFis", ["Aciklama"]) };
        var uygulamadan = new[] { new VeriKumesiTanimi("MuhFis", ["FisNo", "Tarih"]) };

        var kume = Assert.Single(AlanKatalogu.Birlestir(duzenden, uygulamadan));

        Assert.Equal(["Aciklama", "FisNo", "Tarih"], kume.Alanlar);
    }

    [Fact]
    public void Kumesiz_baslik_listenin_sonunda_durur()
    {
        var duzen = Duzen(
            Bant("Baslik1", null, Kutu("K1", "[kurum]")),
            Bant("Veri1", "MuhFis", Kutu("K2", "[MuhFis.\"Borc\"]")));

        Assert.Equal(["MuhFis", AlanKatalogu.Kumesiz], AlanKatalogu.Kumeler(duzen).Select(k => k.Ad));
    }
}

public class AlanSezgisiSinamalari
{
    [Theory]
    [InlineData("Borc", BicimTuru.Sayi)]
    [InlineData("FisToplami", BicimTuru.Sayi)]
    [InlineData("odenek", BicimTuru.Sayi)]
    [InlineData("Tarih", BicimTuru.Tarih)]
    [InlineData("VadeTarihi", BicimTuru.Tarih)]
    [InlineData("Aciklama", BicimTuru.Yok)]
    public void Addan_bicim_tahmin_edilir(string alan, BicimTuru beklenen)
        => Assert.Equal(beklenen, AlanSezgisi.Bicim(alan));

    [Fact]
    public void Tarih_sayidan_once_gelir()
    {
        // "Tarih" hem tarih hem — "toplam" gibi — sayı adı içerebilir;
        // sıralama belirleyici olmalı, yoksa aynı ad iki farklı biçim alırdı.
        Assert.Equal(BicimTuru.Tarih, AlanSezgisi.Bicim("ToplamTarih"));
    }

    [Fact]
    public void Bicime_yakisan_desen_verilir()
    {
        Assert.Equal("%2.2n", AlanSezgisi.Desen(BicimTuru.Sayi));
        Assert.Equal("dd.mm.yyyy", AlanSezgisi.Desen(BicimTuru.Tarih));
        Assert.Null(AlanSezgisi.Desen(BicimTuru.Yok));
    }
}
