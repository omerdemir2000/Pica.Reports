namespace Pica.Reports.Testleri;

public class HizalamaSinamalari
{
    private static List<DuzenNesnesi> Uc() =>
    [
        Ornek.Kutu("A", sol: 0, ust: 0, en: 10, boy: 10),
        Ornek.Kutu("B", sol: 20, ust: 40, en: 30, boy: 20),
        Ornek.Kutu("C", sol: 100, ust: 90, en: 10, boy: 10),
    ];

    [Fact]
    public void Sola_hizalama_en_soldaki_kutuyu_capa_alir()
    {
        // Bandın soluna değil: üç kutuluk bir düzeltme bütün sütunu kâğıdın
        // kenarına yapıştırmamalı.
        var sonuc = Hizalama.Hesapla(HizalamaTuru.Sol, Uc());

        Assert.All(sonuc, s => Assert.Equal(0, s.SolPt));
        Assert.Equal(2, sonuc.Count);   // A zaten 0'daydı, sonuca girmez
    }

    [Fact]
    public void Saga_hizalama_kutunun_sag_kenarini_esitler()
    {
        var sonuc = Hizalama.Hesapla(HizalamaTuru.Sag, Uc());

        // En sağdaki kenar 110 (C: 100 + 10). Hizalanan her kutunun sağ kenarı
        // oraya gelmeli — solu değil, çünkü genişlikleri farklı.
        Assert.NotEmpty(sonuc);
        Assert.All(sonuc, s => Assert.Equal(110, s.SolPt + s.GenislikPt, 3));
    }

    [Fact]
    public void Ortalama_kapsayan_dikdortgenin_ortasini_alir()
    {
        // Ortaların ortalaması alınsaydı geniş kutu sonucu kendine çekerdi.
        var sonuc = Hizalama.Hesapla(HizalamaTuru.YatayOrta, Uc());

        // Kapsam 0..110, orta 55.
        foreach (var s in sonuc)
            Assert.Equal(55 - Genislik(s) / 2, s.SolPt, 3);
    }

    [Fact]
    public void Esitleme_en_buyuge_gore_yapilir()
    {
        var sonuc = Hizalama.Hesapla(HizalamaTuru.AyniEn, Uc());

        Assert.All(sonuc, s => Assert.Equal(30, s.GenislikPt));
    }

    [Fact]
    public void Dagitma_uclari_yerinde_birakir_ve_araliklari_esitler()
    {
        var sonuc = Hizalama.Hesapla(HizalamaTuru.YatayDagit, Uc());

        // Uçtaki iki kutu yerinde kaldığı için sonuca hiç girmez; kalan tek
        // kutu ortadaki.
        var b = Assert.Single(sonuc);
        Assert.Equal("B", b.Nesne.Ad);

        // Açıklık 110 (0'dan C'nin sağ kenarına), kutuların topladığı 50,
        // boşluk (110 - 50) / 2 = 30. B: 0 + 10 + 30 = 40.
        Assert.Equal(40, b.SolPt, 3);
    }

    [Fact]
    public void Dagitma_en_az_uc_kutu_ister()
    {
        Assert.Equal(3, Hizalama.EnAzKutu(HizalamaTuru.YatayDagit));
        Assert.Empty(Hizalama.Hesapla(HizalamaTuru.YatayDagit, [.. Uc().Take(2)]));
    }

    [Fact]
    public void Yeri_degismeyen_kutu_sonuca_girmez()
    {
        // Geri alma yığınına hiçbir şeyi değişmemiş bir adım yazmanın anlamı
        // yok: kullanıcı Ctrl+Z'ye basıp hiçbir şey olmadığını görürdü.
        List<DuzenNesnesi> ayni =
        [
            Ornek.Kutu("A", sol: 5, en: 10),
            Ornek.Kutu("B", sol: 5, en: 10),
        ];

        Assert.Empty(Hizalama.Hesapla(HizalamaTuru.Sol, ayni));
    }

    private static double Genislik(Hizalama.Sonuc s) => s.GenislikPt;
}
