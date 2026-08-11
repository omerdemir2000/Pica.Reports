using Pica.Reports.Basim;
using Pica.Reports.Veri;

namespace Pica.Reports.Testleri;

/// <summary>
/// Önizlemenin sayfa dizicisi.
/// </summary>
/// <remarks>
/// Dizici, kâğıdın kendisini üretmiyor; "hangi kutu hangi sayfanın neresinde"
/// sorusunu cevaplıyor. Sınamalar da bunu ölçüyor: sayfa sayısı, sayfa
/// başlığının yinelenmesi, sayfa altının kâğıdın altına yaslanması ve veri
/// bandının satır sayısı kadar yinelenmesi.
/// </remarks>
public class SayfaDiziciSinamalari
{
    private static DuzenNesnesi Kutu(string ad, string metin, double ust = 0)
    {
        var n = Ornek.Kutu(ad);
        n.Metin = metin;
        n.SolPt = 0;
        n.UstPt = ust;
        n.GenislikPt = 200;
        n.YukseklikPt = 12;
        return n;
    }

    private static DuzenBandi Bant(string ad, BantTuru tur, double ustPt, double boy,
                                   string? kume = null, params DuzenNesnesi[] nesneler)
        => new()
        {
            Ad = ad, Tur = tur, UstPt = ustPt, YukseklikPt = boy,
            VeriKumesi = kume,
            Nesneler = [.. nesneler],
        };

    /// <summary>Sayfa başlığı + veri bandı + sayfa altı olan küçük bir düzen.</summary>
    private static DuzenSayfasi Sayfa(double yukseklik = 200) => new()
    {
        GenislikPt = 300,
        YukseklikPt = yukseklik,
        SolBoslukPt = 10, SagBoslukPt = 10, UstBoslukPt = 10, AltBoslukPt = 10,
        Bantlar =
        [
            Bant("SayfaBasligi1", BantTuru.SayfaBasligi, 0, 20, null, Kutu("Baslik", "BAŞLIK")),
            Bant("Veri1", BantTuru.Veri, 30, 15, "Fisler", Kutu("Satir", "[Aciklama]")),
            Bant("SayfaSonu1", BantTuru.SayfaSonu, 60, 15, null, Kutu("Alt", "Sayfa [Page#]")),
        ],
    };

    private static RaporVerisi Veri(int satir)
        => new RaporVerisi().Ekle("Fisler",
            Enumerable.Range(1, satir).Select(i => new Dictionary<string, object?>
            {
                ["Aciklama"] = $"satır {i}",
                ["Tutar"] = 10m * i,
            }));

    [Fact]
    public void Veri_bandi_satir_sayisi_kadar_yinelenir()
    {
        var sayfalar = SayfaDizici.Diz(Sayfa(), Veri(3));

        var satirlar = sayfalar[0].Kutular.Where(k => k.Nesne.Ad == "Satir").ToList();

        Assert.Single(sayfalar);
        Assert.Equal(3, satirlar.Count);
        Assert.Equal(["satır 1", "satır 2", "satır 3"], satirlar.Select(k => k.Metin));
    }

    [Fact]
    public void Sayfa_dolunca_kirilir_ve_baslik_yinelenir()
    {
        // 200 pt boy, 10+10 boşluk, 20 başlık, 15 alt → satırlara 145 pt kalır,
        // satır 15 pt: sayfa başına 9 satır. 20 satır 3 sayfa eder.
        var sayfalar = SayfaDizici.Diz(Sayfa(), Veri(20));

        Assert.True(sayfalar.Count > 1, "sayfa kırılmalıydı");
        Assert.All(sayfalar, s => Assert.Contains(s.Kutular, k => k.Nesne.Ad == "Baslik"));
        Assert.All(sayfalar, s => Assert.Contains(s.Kutular, k => k.Nesne.Ad == "Alt"));

        // Bütün satırlar basılmış olmalı: kırılım satır düşürmemeli.
        var toplam = sayfalar.Sum(s => s.Kutular.Count(k => k.Nesne.Ad == "Satir"));
        Assert.Equal(20, toplam);
    }

    [Fact]
    public void Sayfa_alti_kagidin_altina_yaslanir()
    {
        var sayfa = Sayfa();
        var sayfalar = SayfaDizici.Diz(sayfa, Veri(2));

        var alt = sayfalar[0].Kutular.Single(k => k.Nesne.Ad == "Alt");

        // Yarım dolu sayfada bile alt bant kâğıdın altındadır: akışın bittiği
        // yerde asılı kalmaz.
        Assert.Equal(sayfa.YukseklikPt - sayfa.AltBoslukPt - 15, alt.UstPt, 3);
    }

    [Fact]
    public void Sayfa_numarasi_sayfadan_sayfaya_artar()
    {
        var sayfalar = SayfaDizici.Diz(Sayfa(), Veri(20));

        Assert.Equal("Sayfa 1", sayfalar[0].Kutular.Single(k => k.Nesne.Ad == "Alt").Metin);
        Assert.Equal("Sayfa 2", sayfalar[1].Kutular.Single(k => k.Nesne.Ad == "Alt").Metin);
    }

    [Fact]
    public void Veri_verilmezse_ornek_satirlarla_dizilir()
    {
        // Tasarım anında veri yok; boş önizleme göstermektense örnek satır.
        var sayfalar = SayfaDizici.Diz(Sayfa());

        Assert.Contains(sayfalar[0].Kutular, k => k.Nesne.Ad == "Satir");
    }

    [Fact]
    public void Kayitli_olmayan_kume_bos_kalir()
    {
        // "Fisler" bekleyen bant "Ozet" satırlarını basmamalı: kâğıtta fark
        // edilmesi güç bir hata olurdu.
        var veri = new RaporVerisi().Ekle("Ozet", new[] { new Dictionary<string, object?>() });

        var sayfalar = SayfaDizici.Diz(Sayfa(), veri);

        Assert.DoesNotContain(sayfalar[0].Kutular, k => k.Nesne.Ad == "Satir");
    }

    [Fact]
    public void Kutu_konumu_sayfanin_koseSine_gore_verilir()
    {
        var sayfa = Sayfa();
        var sayfalar = SayfaDizici.Diz(sayfa, Veri(1));

        var baslik = sayfalar[0].Kutular.First(k => k.Nesne.Ad == "Baslik");

        // Sol: sayfa boşluğu + kutunun banttaki solu. Üst: sayfa boşluğu.
        Assert.Equal(sayfa.SolBoslukPt, baslik.SolPt, 3);
        Assert.Equal(sayfa.UstBoslukPt, baslik.UstPt, 3);
    }

    [Fact]
    public void Toplam_sayfa_ikinci_gecise_dogru_yazilir()
    {
        var sayfa = Sayfa();
        sayfa.Bantlar[2].Nesneler[0].Metin = "[Page#] / [TotalPages#]";

        var sayfalar = SayfaDizici.Diz(sayfa, Veri(20));

        var son = sayfalar[^1].Kutular.Single(k => k.Nesne.Ad == "Alt").Metin;
        Assert.Equal($"{sayfalar.Count} / {sayfalar.Count}", son);
    }
}

public class KagitBoyuSinamalari
{
    [Fact]
    public void A4_olcusu_tanınır()
    {
        Assert.Equal("A4", KagitBoyu.Bul(595.28, 841.89)!.Ad);

        // Yatay A4 de A4'tür.
        Assert.Equal("A4", KagitBoyu.Bul(841.89, 595.28)!.Ad);
    }

    [Fact]
    public void Taninmayan_olcu_ozel_sayilir()
        => Assert.Null(KagitBoyu.Bul(500, 700));

    [Fact]
    public void Yon_degistirmek_en_ile_boyu_takas_eder()
    {
        var sayfa = new DuzenSayfasi { GenislikPt = 595.28, YukseklikPt = 841.89 };

        KagitBoyu.YonDegistir(sayfa, yatay: true);

        Assert.True(sayfa.Yatay);
        Assert.Equal(841.89, sayfa.GenislikPt, 2);
        Assert.Equal(595.28, sayfa.YukseklikPt, 2);

        // İkinci kez aynı yön istenirse bir şey olmamalı.
        KagitBoyu.YonDegistir(sayfa, yatay: true);
        Assert.Equal(841.89, sayfa.GenislikPt, 2);
    }

    [Fact]
    public void Kagit_uygulanirken_yon_korunur()
    {
        var sayfa = new DuzenSayfasi { GenislikPt = 841.89, YukseklikPt = 595.28, Yatay = true };

        KagitBoyu.A5.Uygula(sayfa);

        Assert.Equal(595.28, sayfa.GenislikPt, 2);
        Assert.Equal(419.53, sayfa.YukseklikPt, 2);
    }

    [Fact]
    public void Sayfa_ayari_duzeltmeye_girer()
    {
        var ham = Ornek.KarisikSirali();
        var calisma = Ornek.KarisikSirali();

        KagitBoyu.A5.Uygula(calisma.Sayfalar[0]);
        calisma.Sayfalar[0].SolBoslukPt = 40;

        var duzeltme = DuzenDuzeltmesi.Cikar(ham, calisma);
        var sayfaFarki = Assert.Single(duzeltme.Sayfalar);

        Assert.Equal(0, sayfaFarki.Sayfa);
        Assert.Equal(40, sayfaFarki.SolBoslukPt);

        // Uygulanınca geri gelir.
        var yeniden = Ornek.KarisikSirali();
        duzeltme.Uygula(yeniden);

        Assert.Equal(calisma.Sayfalar[0].GenislikPt, yeniden.Sayfalar[0].GenislikPt, 3);
        Assert.Equal(40, yeniden.Sayfalar[0].SolBoslukPt);
    }
}
