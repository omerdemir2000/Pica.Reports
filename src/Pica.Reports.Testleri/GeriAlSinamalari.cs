namespace Pica.Reports.Testleri;

public class GeriAlSinamalari
{
    [Fact]
    public void Alan_degisikligi_geri_alinip_yinelenebilir()
    {
        var yigin = new GeriAlYigini();
        var kutu = Ornek.Kutu("K1", sol: 10);

        var once = kutu.Kopya();
        kutu.SolPt = 50;
        yigin.Alan(kutu, once);

        Assert.True(yigin.GeriAl());
        Assert.Equal(10, kutu.SolPt);

        Assert.True(yigin.Yinele());
        Assert.Equal(50, kutu.SolPt);
    }

    [Fact]
    public void Kopya_butun_alanlari_tasir()
    {
        // Alan listesi elle yazılıyor (YazUzerine); biri unutulursa o alan
        // sessizce geri alınamaz olur.
        var kutu = Ornek.Kutu("K1");
        kutu.Metin = "a";
        kutu.Kalin = true;
        kutu.ZeminRengi = "#ff0000";
        kutu.Bicim = BicimTuru.Tarih;
        kutu.Cerceve = CerceveKenari.Tumu;
        kutu.Sigdir = true;

        var once = kutu.Kopya();

        kutu.Metin = "b";
        kutu.Kalin = false;
        kutu.ZeminRengi = "#00ff00";
        kutu.Bicim = BicimTuru.Sayi;
        kutu.Cerceve = CerceveKenari.Yok;
        kutu.Sigdir = false;

        kutu.YazUzerine(once);

        Assert.Equal("a", kutu.Metin);
        Assert.True(kutu.Kalin);
        Assert.Equal("#ff0000", kutu.ZeminRengi);
        Assert.Equal(BicimTuru.Tarih, kutu.Bicim);
        Assert.Equal(CerceveKenari.Tumu, kutu.Cerceve);
        Assert.True(kutu.Sigdir);
    }

    [Fact]
    public void Yapisal_degisiklik_geri_alinabilir()
    {
        var yigin = new GeriAlYigini();
        var duzen = Ornek.Kutulu(Ornek.Kutu("K1"));
        var bant = duzen.Sayfalar[0].Bantlar[0];
        var yeni = Ornek.Kutu("K2");

        bant.Nesneler.Add(yeni);
        yigin.Yapisal(() => bant.Nesneler.Remove(yeni), () => bant.Nesneler.Add(yeni));

        Assert.True(yigin.GeriAl());
        Assert.Single(bant.Nesneler);

        Assert.True(yigin.Yinele());
        Assert.Equal(2, bant.Nesneler.Count);
    }

    [Fact]
    public void Geri_alinmis_adimlarin_ustune_yazmak_ileri_yolu_kapatir()
    {
        var yigin = new GeriAlYigini();
        var kutu = Ornek.Kutu("K1", sol: 0);

        var a = kutu.Kopya(); kutu.SolPt = 10; yigin.Alan(kutu, a);
        var b = kutu.Kopya(); kutu.SolPt = 20; yigin.Alan(kutu, b);

        yigin.GeriAl();                       // 10
        Assert.True(yigin.Yinelenebilir);

        var c = kutu.Kopya(); kutu.SolPt = 99; yigin.Alan(kutu, c);

        Assert.False(yigin.Yinelenebilir);
    }

    [Fact]
    public void Temizle_yigini_bosaltir()
    {
        var yigin = new GeriAlYigini();
        var kutu = Ornek.Kutu("K1");

        yigin.Alan(kutu, kutu.Kopya());
        Assert.True(yigin.GeriAlinabilir);

        yigin.Temizle();

        Assert.False(yigin.GeriAlinabilir);
        Assert.False(yigin.Yinelenebilir);
    }
}
