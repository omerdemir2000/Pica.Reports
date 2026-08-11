namespace Pica.Reports.Testleri;

/// <summary>
/// Basım sırasının doğruluğu.
/// </summary>
/// <remarks>
/// Bu sınıfın koruduğu şey aracın en temel iddiası: tuvaldeki sıra kâğıttaki
/// sıradır. Bozulursa tasarımcı yanlış bir şey göstermeye başlar ve bunun fark
/// edilmesi zordur — çıktı doğru kalır, çıktıya dair söylenen şey yanlış olur.
/// </remarks>
public class BantSirasiSinamalari
{
    [Fact]
    public void Basim_sirasi_dosya_sirasi_degildir()
    {
        var sayfa = Ornek.KarisikSirali().Sayfalar[0];

        var dosya = sayfa.Bantlar.Select(b => b.Ad);
        var basim = BantSirasi.BasimSirasi(sayfa).Select(s => s.Bant.Ad);

        Assert.Equal(["Veri1", "SayfaAlti1", "SayfaBasligi1", "RaporSonu1", "Baslik1"], dosya);
        Assert.Equal(["SayfaBasligi1", "Baslik1", "Veri1", "RaporSonu1", "SayfaAlti1"], basim);
    }

    [Fact]
    public void Roller_bandin_kagittaki_gorevini_soyler()
    {
        var sira = BantSirasi.BasimSirasi(Ornek.KarisikSirali().Sayfalar[0]);

        Assert.Equal(BantRolu.SayfaBasi, sira.Single(s => s.Bant.Ad == "SayfaBasligi1").Rol);
        Assert.Equal(BantRolu.SayfaAlti, sira.Single(s => s.Bant.Ad == "SayfaAlti1").Rol);
        Assert.Equal(BantRolu.Icerik, sira.Single(s => s.Bant.Ad == "Veri1").Rol);
    }

    [Fact]
    public void Ayrinti_ve_yan_bantlar_ana_bandin_pesine_takilir()
    {
        var sayfa = Ornek.Zincirli().Sayfalar[0];
        var sira = BantSirasi.BasimSirasi(sayfa);

        // Yan bant dosyada ayrıntıdan ÖNCE duruyor ve UstPt'si 900 — yani
        // dikey konuma göre en sonda. Buna rağmen sahibinin hemen ardına
        // basılır, çünkü yan bandın kâğıtta kendi yeri yoktur.
        Assert.Equal(["Veri1", "Yan1", "Ayrinti1"], sira.Select(s => s.Bant.Ad));
    }

    [Fact]
    public void Girinti_bagimlilik_derinligini_gosterir()
    {
        var sira = BantSirasi.BasimSirasi(Ornek.Zincirli().Sayfalar[0]);

        Assert.Equal(1, sira.Single(s => s.Bant.Ad == "Veri1").Girinti);
        Assert.Equal(2, sira.Single(s => s.Bant.Ad == "Yan1").Girinti);
        Assert.Equal(2, sira.Single(s => s.Bant.Ad == "Ayrinti1").Girinti);
    }

    [Fact]
    public void Yukseklik_bildirilen_deger_ile_kutularin_en_altindan_buyuk_olanidir()
    {
        // Şablonların bir kısmında bandın Height'ı 0'dır ama içinde yüksekliği
        // olan kutular vardır; bildirilen değere güvenilseydi o bantlar hem
        // kâğıtta hem tasarımcıda görünmezdi.
        var bant = Ornek.Bant("B", BantTuru.Veri, ust: 0, boy: 0);
        bant.Nesneler.Add(Ornek.Kutu("K", ust: 10, boy: 25));

        Assert.Equal(35, BantSirasi.Yukseklik(bant));

        bant.YukseklikPt = 60;
        Assert.Equal(60, BantSirasi.Yukseklik(bant));
    }

    [Fact]
    public void Kendini_gosteren_yan_bant_sonsuz_donguye_girmez()
    {
        var a = Ornek.Bant("A", BantTuru.Veri, ust: 0, boy: 10);
        var b = Ornek.Bant("B", BantTuru.Yan, ust: 20, boy: 10);

        a.YanBant = "B";
        b.YanBant = "B";   // kendini gösteriyor

        var sayfa = new DuzenSayfasi { Bantlar = [a, b] };

        // Koruma sayacı kesmezse bu çağrı hiç dönmezdi.
        var zincir = BantSirasi.YanZinciri(sayfa, a).ToList();

        Assert.True(zincir.Count <= 20);
        Assert.Equal("A", zincir[0].Ad);
    }
}
