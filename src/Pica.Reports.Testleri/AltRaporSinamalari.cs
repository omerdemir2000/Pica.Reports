using Pica.Reports.Basim;
using Pica.Reports.Veri;

namespace Pica.Reports.Testleri;

/// <summary>
/// Alt rapor: bir bandın içindeki kutunun yerine başka bir sayfanın bantlarının
/// akışa girmesi.
/// </summary>
/// <remarks>
/// Ölçülen şey <b>sıra ve yer</b>: gömülen bantlar kutunun durduğu noktada mı
/// akıyor, yer tutucu kutu kâğıttan düşüyor mu, hedefin kendi sayfa başlığı
/// dışarıda mı kalıyor, gömülen içerik sayfayı kırıyor mu ve döngü kesiliyor mu.
/// </remarks>
public class AltRaporSinamalari
{
    private static DuzenNesnesi Kutu(string ad, string? metin = null)
    {
        var n = Ornek.Kutu(ad);
        n.Metin = metin ?? ad;
        n.GenislikPt = 200;
        n.YukseklikPt = 12;
        return n;
    }

    /// <summary>Basılmayan yer tutucu: hedefini adıyla gösterir.</summary>
    private static DuzenNesnesi AltRaporKutusu(string ad, string hedef)
    {
        var n = Kutu(ad, $"[alt rapor → {hedef}]");
        n.AltRaporSayfasi = hedef;
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

    /// <summary>
    /// İki sayfalık düzen: birincinin gövdesindeki kutu ikinciyi gösterir.
    /// </summary>
    /// <remarks>
    /// Ana sayfada gömmenin önünde ve ardında birer bant var — gömülen akışın
    /// ikisinin ARASINA girdiği ancak böyle görünür.
    /// </remarks>
    private static CetvelDuzeni Duzen(double yukseklik = 400) => new()
    {
        Anahtar = "alt-rapor",
        Ad = "Alt Raporlu",
        Kaynak = "sınama",
        Sayfalar =
        [
            new DuzenSayfasi
            {
                Ad = "Ana",
                GenislikPt = 300,
                YukseklikPt = yukseklik,
                SolBoslukPt = 10, SagBoslukPt = 10, UstBoslukPt = 10, AltBoslukPt = 10,
                Bantlar =
                [
                    Bant("AnaSayfaBasligi", BantTuru.SayfaBasligi, 0, 20, null, Kutu("SayfaBasi")),
                    Bant("AnaBaslik", BantTuru.Baslik, 30, 15, null, Kutu("Once")),
                    Bant("AnaGomme", BantTuru.Baslik, 40, 10, null, AltRaporKutusu("Gomme", "Ek")),
                    Bant("AnaAlt", BantTuru.Alt, 50, 15, null, Kutu("Sonra")),
                    Bant("AnaSayfaSonu", BantTuru.SayfaSonu, 90, 15, null, Kutu("SayfaAlti")),
                ],
            },
            new DuzenSayfasi
            {
                Ad = "Ek",
                GenislikPt = 300,
                YukseklikPt = yukseklik,
                SolBoslukPt = 40, SagBoslukPt = 40, UstBoslukPt = 40, AltBoslukPt = 40,
                Bantlar =
                [
                    Bant("EkSayfaBasligi", BantTuru.SayfaBasligi, 0, 20, null, Kutu("EkSayfaBasi")),
                    Bant("EkVeri", BantTuru.Veri, 30, 12, "Hareket", Kutu("EkSatir", "[Aciklama]")),
                    Bant("EkSayfaSonu", BantTuru.SayfaSonu, 60, 20, null, Kutu("EkSayfaAlti")),
                ],
            },
        ],
    };

    private static RaporVerisi Veri(int satir)
        => new RaporVerisi().Ekle("Hareket",
            Enumerable.Range(1, satir).Select(i => new Dictionary<string, object?>
            {
                ["Aciklama"] = $"hareket {i}",
            }));

    private static List<string> Sirasiyla(BasilanSayfa sayfa)
        => [.. sayfa.Kutular.Select(k => k.Nesne.Ad)];

    [Fact]
    public void Hedef_sayfanin_bantlari_kutunun_yerinde_akisa_girer()
    {
        var sayfalar = SayfaDizici.Diz(Duzen(), 0, Veri(3));

        var tek = Assert.Single(sayfalar);

        // Gömülen satırlar önceki ile sonraki bandın ARASINDA: yer tutucu kutu
        // akışın neresinde duruyorsa alt rapor oradan başlar.
        Assert.Equal(
            ["SayfaBasi", "Once", "EkSatir", "EkSatir", "EkSatir", "Sonra", "SayfaAlti"],
            Sirasiyla(tek));

        Assert.Equal(["hareket 1", "hareket 2", "hareket 3"],
            tek.Kutular.Where(k => k.Nesne.Ad == "EkSatir").Select(k => k.Metin));
    }

    [Fact]
    public void Yer_tutucu_kutu_basilmaz()
    {
        var sayfalar = SayfaDizici.Diz(Duzen(), 0, Veri(2));

        // Kutunun kendisi kâğıda çıksaydı köşeli parantezli yer tutucu metin
        // gömülen içeriğin üstünde görünürdü.
        Assert.DoesNotContain(sayfalar[0].Kutular, k => k.Nesne.Ad == "Gomme");
    }

    [Fact]
    public void Gomulen_bandin_yuksekligi_akisi_ilerletir()
    {
        var sayfalar = SayfaDizici.Diz(Duzen(), 0, Veri(2));
        var kutular = sayfalar[0].Kutular;

        double Ust(string ad) => kutular.First(k => k.Nesne.Ad == ad).UstPt;

        // 10 üst boşluk + 20 sayfa başlığı + 15 başlık bandı = 45. Yer tutucunun
        // bandı yerini KORUR (12 punto — kutunun boyu bandın bildirdiğinden
        // büyük, bkz. BantSirasi.Yukseklik): FastReport da alt raporu bandı
        // bastıktan sonra çalıştırır. Gömülen akış 57'den başlar.
        Assert.Equal(30, Ust("Once"), 3);
        Assert.Equal(57, Ust("EkSatir"), 3);

        // İki satır 12'şer punto: ana akış 81'den devam eder.
        Assert.Equal(81, Ust("Sonra"), 3);
    }

    [Fact]
    public void Gomulen_kutu_ANA_sayfanin_kenar_boslugunu_kullanir()
    {
        var sayfalar = SayfaDizici.Diz(Duzen(), 0, Veri(1));

        // Kâğıt ana sayfanındır: hedef sayfanın 40 puntoluk kendi boşluğu değil,
        // ana sayfanın 10 puntosu geçerli. Yoksa gömülen bantlar ötekilerden
        // kaymış çıkardı.
        Assert.Equal(10, sayfalar[0].Kutular.First(k => k.Nesne.Ad == "EkSatir").SolPt, 3);
    }

    [Fact]
    public void Hedefin_kendi_sayfa_basligi_ve_sonu_basilmaz()
    {
        var sayfalar = SayfaDizici.Diz(Duzen(), 0, Veri(3));

        // O bantlar kâğıdın kenarına ait ve kâğıt ana sayfanın: gömülü akışın
        // ortasında bir "sayfa altı" basmak kâğıdı ikiye bölerdi.
        Assert.All(sayfalar, s => Assert.DoesNotContain(s.Kutular, k => k.Nesne.Ad == "EkSayfaBasi"));
        Assert.All(sayfalar, s => Assert.DoesNotContain(s.Kutular, k => k.Nesne.Ad == "EkSayfaAlti"));
    }

    [Fact]
    public void Gomulen_icerik_sayfa_kirilimina_katilir()
    {
        // 200 punto kâğıt: gömülen 20 satır tek sayfaya sığmaz. Ekstre detayı
        // uzundur, kırılmazsa kâğıdın altından taşardı.
        var sayfalar = SayfaDizici.Diz(Duzen(yukseklik: 200), 0, Veri(20));

        Assert.True(sayfalar.Count > 1, "gömülen içerik sayfayı kırmalıydı");

        // Satır düşmemeli.
        Assert.Equal(20, sayfalar.Sum(s => s.Kutular.Count(k => k.Nesne.Ad == "EkSatir")));

        // Kırılan sayfanın üstünde ANA sayfanın başlığı yinelenir.
        Assert.All(sayfalar, s => Assert.Contains(s.Kutular, k => k.Nesne.Ad == "SayfaBasi"));
        Assert.All(sayfalar, s => Assert.Contains(s.Kutular, k => k.Nesne.Ad == "SayfaAlti"));

        // Gövde bandı yinelenmez: o bir kez basılır, sayfa başlığı gibi değil.
        Assert.Equal(1, sayfalar.Sum(s => s.Kutular.Count(k => k.Nesne.Ad == "Once")));

        // Ana akış gömme bittikten sonra sürer — son sayfada.
        Assert.Contains(sayfalar[^1].Kutular, k => k.Nesne.Ad == "Sonra");
    }

    [Fact]
    public void Dongu_kesilir()
    {
        // Ek sayfası da Ana'yı gösteriyor: A → B → A. Kesilmezse akış sonsuza
        // kadar açılır, tarayıcı kilitlenirdi.
        var duzen = Duzen();
        duzen.Sayfalar[1].Bantlar.Add(
            Bant("EkGomme", BantTuru.Alt, 40, 10, null, AltRaporKutusu("GeriDonus", "Ana")));

        var sayfalar = SayfaDizici.Diz(duzen, 0, Veri(2));

        // Zincirde duran sayfa ikinci kez açılmaz: her bant bir kez basılmış olur.
        Assert.Single(sayfalar);
        Assert.Equal(1, sayfalar[0].Kutular.Count(k => k.Nesne.Ad == "Once"));
        Assert.Equal(2, sayfalar[0].Kutular.Count(k => k.Nesne.Ad == "EkSatir"));
    }

    [Fact]
    public void Kendini_gosteren_sayfa_kendini_gommez()
    {
        var duzen = Duzen();
        duzen.Sayfalar[0].Bantlar[2].Nesneler[0].AltRaporSayfasi = "Ana";

        var sayfalar = SayfaDizici.Diz(duzen, 0, Veri(2));

        Assert.Single(sayfalar);
        Assert.Equal(1, sayfalar[0].Kutular.Count(k => k.Nesne.Ad == "Once"));
    }

    [Fact]
    public void Bulunamayan_hedef_akisi_bozmaz()
    {
        // Sayfa adı değişmiş ya da düzen tek sayfa gelmiş olabilir: gömülecek
        // bir şey yoksa akış olduğu gibi sürer, yer tutucu yine basılmaz.
        var duzen = Duzen();
        duzen.Sayfalar[0].Bantlar[2].Nesneler[0].AltRaporSayfasi = "OlmayanSayfa";

        var sayfalar = SayfaDizici.Diz(duzen, 0, Veri(2));

        Assert.Equal(["SayfaBasi", "Once", "Sonra", "SayfaAlti"], Sirasiyla(sayfalar[0]));
    }

    [Fact]
    public void Ayni_alt_rapor_iki_bantta_iki_kez_akar()
    {
        // Döngü koruması zincire bakıyor, "bir kez ziyaret edildi"ye değil: aynı
        // alt raporun iki ayrı bantta olması olağan (mutabakat yazısının iki
        // nüshası), döngü değil.
        var duzen = Duzen();
        duzen.Sayfalar[0].Bantlar.Add(
            Bant("AnaGomme2", BantTuru.Alt, 60, 10, null, AltRaporKutusu("Gomme2", "Ek")));

        var sayfalar = SayfaDizici.Diz(duzen, 0, Veri(2));

        Assert.Equal(4, sayfalar.Sum(s => s.Kutular.Count(k => k.Nesne.Ad == "EkSatir")));
    }

    [Fact]
    public void Veri_bandina_gomulen_alt_rapor_her_satirda_akar()
    {
        // Ana bandın satır bağlamı gömmeden sonra da durmalı: gömülen gövde
        // kendi veri bandını bitirince çözücünün satırını boşaltıyor ve ana
        // bandın ardından gelen ayrıntı bandı boş çıkardı.
        var duzen = Duzen();
        var ana = duzen.Sayfalar[0];

        // Gömme yalnızca veri bandında olsun: sayılan satırlar oradan gelsin.
        ana.Bantlar.RemoveAll(b => b.Ad == "AnaGomme");

        ana.Bantlar.Add(Bant("AnaVeri", BantTuru.Veri, 70, 12, "Fis",
            Kutu("FisNo", "[Fisno]"), AltRaporKutusu("SatirGomme", "Ek")));
        ana.Bantlar.Add(Bant("AnaAyrinti", BantTuru.AltVeri, 75, 12, null, Kutu("FisTekrar", "[Fisno]")));

        var veri = Veri(1).Ekle("Fis",
            Enumerable.Range(1, 2).Select(i => new Dictionary<string, object?> { ["Fisno"] = $"F{i}" }));

        var kutular = SayfaDizici.Diz(duzen, 0, veri).SelectMany(s => s.Kutular).ToList();

        // İki satır, her birinde bir gömme.
        Assert.Equal(2, kutular.Count(k => k.Nesne.Ad == "EkSatir"));

        // Ayrıntı bandı gömmeden SONRA basılıyor ve satırını hâlâ görüyor.
        Assert.Equal(["F1", "F2"],
            kutular.Where(k => k.Nesne.Ad == "FisTekrar").Select(k => k.Metin));
    }

    [Fact]
    public void Alt_rapor_hedefi_ayri_bir_cetvel_sayilmaz()
    {
        var duzen = Duzen();

        // Önizlemenin cetvel seçicisi buna bakıyor: gömülen sayfa kendi başına
        // bir cetvel değil.
        Assert.False(duzen.AltRaporMu(duzen.Sayfalar[0]));
        Assert.True(duzen.AltRaporMu(duzen.Sayfalar[1]));
    }

    [Theory]
    [InlineData("AnaSayfaBasligi")]
    [InlineData("AnaSayfaSonu")]
    public void Kagidin_kenarindaki_bant_alt_rapor_gommez(string bantAdi)
    {
        // Kenar bantları sayfa kırılırken basılıyor; oradan akışa girmek
        // kırılımın ortasında yeniden kırmak, yani kendini çağırmak olurdu.
        var duzen = Duzen();
        var ana = duzen.Sayfalar[0];

        ana.Bantlar.RemoveAll(b => b.Ad == "AnaGomme");
        ana.Bantlar.First(b => b.Ad == bantAdi).Nesneler.Add(AltRaporKutusu("KenarGomme", "Ek"));

        var sayfalar = SayfaDizici.Diz(duzen, 0, Veri(2));

        Assert.DoesNotContain(sayfalar.SelectMany(s => s.Kutular), k => k.Nesne.Ad == "EkSatir");
    }

    [Fact]
    public void Yer_tutucunun_metni_alan_agacina_girmez()
    {
        // "[alt rapor → Ek]" bir alan başvurusuna benziyor ama değil: taranırsa
        // alan ağacında gerçek alanların arasında uydurma bir satır belirir.
        var alanlar = AlanKatalogu.Cikar(Duzen());

        Assert.DoesNotContain(alanlar, a => a.Contains("alt rapor"));
        Assert.Contains("Aciklama", alanlar);
    }

    [Fact]
    public void Geri_alma_alt_rapor_hedefini_geri_getirir()
    {
        // YazUzerine elle yazılmış: yeni alan unutulursa tasarımcıda geri alma
        // kutuyu eksik döndürür.
        var kutu = AltRaporKutusu("Gomme", "Ek");
        var kopya = kutu.Kopya();

        kutu.AltRaporSayfasi = "Baska";
        kutu.YazUzerine(kopya);

        Assert.Equal("Ek", kutu.AltRaporSayfasi);
    }
}
