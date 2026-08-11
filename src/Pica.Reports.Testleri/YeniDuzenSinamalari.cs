namespace Pica.Reports.Testleri;

public class AnahtarlamaSinamalari
{
    [Theory]
    [InlineData("Aylık Mizan", "aylik-mizan")]
    [InlineData("Mizan  (ayrıntılı)", "mizan-ayrintili")]      // tire tireyi izlemez
    [InlineData("  Yevmiye Defteri  ", "yevmiye-defteri")]     // baştaki ve sondaki boşluk
    [InlineData("Ödeme Emri 2027", "odeme-emri-2027")]
    [InlineData("ŞUBE ÇIKIŞI", "sube-cikisi")]
    [InlineData("Gönderme_Emri", "gonderme-emri")]
    public void Addan_anahtar_uretilir(string ad, string beklenen)
        => Assert.Equal(beklenen, YeniDuzen.Anahtarla(ad));

    [Fact]
    public void Turkce_buyuk_I_harfi_bolmez()
    {
        // Dizenin tamamına ToLowerInvariant uygulansaydı "İ" burada "i" ve
        // birleşen noktaya ayrılır, nokta tireye dönüşür ve anahtar
        // "i-stanbul" olurdu.
        Assert.Equal("istanbul-subesi", YeniDuzen.Anahtarla("İstanbul Şubesi"));
    }

    [Fact]
    public void Karsiligi_olmayan_ad_bos_doner()
    {
        // Çağıran karşılar: kullanıcıya "anahtar boş olamaz" denir, uydurma bir
        // ad üretilmez — dosya adını kullanıcı görecek.
        Assert.Equal("", YeniDuzen.Anahtarla("・・・"));
    }
}

public class BosDuzenSinamalari
{
    [Fact]
    public void Bos_duzen_tek_A4_sayfa_acar()
    {
        var duzen = YeniDuzen.Bos("aylik-mizan", "Aylık Mizan");

        var sayfa = Assert.Single(duzen.Sayfalar);
        Assert.Equal(YeniDuzen.A4GenislikPt, sayfa.GenislikPt);
        Assert.Equal(YeniDuzen.A4YukseklikPt, sayfa.YukseklikPt);
        Assert.False(sayfa.Yatay);
    }

    [Fact]
    public void Bant_olmadan_kutu_eklenemedigi_icin_iki_bant_gelir()
    {
        var sayfa = YeniDuzen.Bos("k", "Ad").Sayfalar[0];

        Assert.Equal(
            [BantTuru.RaporBasligi, BantTuru.Veri],
            sayfa.Bantlar.Select(b => b.Tur));

        // Bantların dikey konumu basım sırasıdır: başlık verinin üstünde.
        Assert.True(sayfa.Bantlar[0].UstPt < sayfa.Bantlar[1].UstPt);
    }

    [Fact]
    public void Baslik_kutusu_duzenin_adini_tasir()
    {
        var kutu = Assert.Single(YeniDuzen.Bos("k", "Aylık Mizan").Sayfalar[0].Bantlar[0].Nesneler);

        Assert.Equal("Aylık Mizan", kutu.Metin);
        Assert.Equal(NesneTuru.Yazi, kutu.Tur);
    }

    [Fact]
    public void Baslik_kutusu_kenar_bosluklarinin_icinde_kalir()
    {
        var duzen = YeniDuzen.Bos("k", "Ad");
        var sayfa = duzen.Sayfalar[0];
        var kutu = sayfa.Bantlar[0].Nesneler[0];

        // Kutu konumu bandın soluna göredir, bant da yazılabilir alandan başlar:
        // genişliği taşarsa ilk PDF'te kutu kâğıdın dışına çıkardı.
        Assert.True(kutu.GenislikPt <= sayfa.GenislikPt - sayfa.SolBoslukPt - sayfa.SagBoslukPt);
    }

    [Fact]
    public void Anahtar_ve_ad_duzene_yazilir()
    {
        // Depo dosya adıyla düzenin kendi anahtarının aynı olmasını bekler.
        var duzen = YeniDuzen.Bos("aylik-mizan", "Aylık Mizan");

        Assert.Equal("aylik-mizan", duzen.Anahtar);
        Assert.Equal("Aylık Mizan", duzen.Ad);
        Assert.Empty(duzen.Kaynak);
    }
}
