namespace Pica.Reports.Testleri;

/// <summary>Sınamaların kullandığı küçük düzenler.</summary>
internal static class Ornek
{
    /// <summary>
    /// Bant sırasının dosya sırasından farklı olduğu bir düzen.
    /// </summary>
    /// <remarks>
    /// Bantlar bilerek karışık sırada ekleniyor ve dikey konumları
    /// (<c>UstPt</c>) o sırayla uyuşmuyor. Taşınan şablonların bir kısmı
    /// gerçekten böyle: muhasebe fişleri listesinde dosya sırası "veri, sayfa
    /// altı, sayfa başlığı, rapor sonu, başlık" iken kâğıttaki sıra bambaşka.
    /// </remarks>
    public static CetvelDuzeni KarisikSirali()
    {
        var sayfa = new DuzenSayfasi
        {
            GenislikPt = 595,
            YukseklikPt = 842,
            SolBoslukPt = 28,
            SagBoslukPt = 28,
            UstBoslukPt = 28,
            AltBoslukPt = 28,
            Bantlar =
            [
                Bant("Veri1", BantTuru.Veri, ust: 200, boy: 20),
                Bant("SayfaAlti1", BantTuru.SayfaSonu, ust: 700, boy: 30),
                Bant("SayfaBasligi1", BantTuru.SayfaBasligi, ust: 0, boy: 60),
                Bant("RaporSonu1", BantTuru.RaporSonu, ust: 400, boy: 25),
                Bant("Baslik1", BantTuru.Baslik, ust: 120, boy: 18),
            ],
        };

        return new CetvelDuzeni { Anahtar = "karisik", Ad = "Karışık", Kaynak = "sınama", Sayfalar = [sayfa] };
    }

    /// <summary>Ana veri bandına ayrıntı ve yan bant bağlı bir düzen.</summary>
    public static CetvelDuzeni Zincirli()
    {
        var ana = Bant("Veri1", BantTuru.Veri, ust: 100, boy: 20);
        ana.YanBant = "Yan1";

        var sayfa = new DuzenSayfasi
        {
            GenislikPt = 595,
            YukseklikPt = 842,
            Bantlar =
            [
                ana,
                Bant("Yan1", BantTuru.Yan, ust: 900, boy: 10),
                Bant("Ayrinti1", BantTuru.AltVeri, ust: 130, boy: 15),
            ],
        };

        return new CetvelDuzeni { Anahtar = "zincir", Ad = "Zincir", Kaynak = "sınama", Sayfalar = [sayfa] };
    }

    /// <summary>Tek bantta, verilen kutuları taşıyan düzen.</summary>
    public static CetvelDuzeni Kutulu(params DuzenNesnesi[] kutular)
    {
        var bant = Bant("Veri1", BantTuru.Veri, ust: 0, boy: 100);
        bant.Nesneler.AddRange(kutular);

        return new CetvelDuzeni
        {
            Anahtar = "kutulu",
            Ad = "Kutulu",
            Kaynak = "sınama",
            Sayfalar = [new DuzenSayfasi { GenislikPt = 595, YukseklikPt = 842, Bantlar = [bant] }],
        };
    }

    public static DuzenBandi Bant(string ad, BantTuru tur, double ust, double boy)
        => new() { Ad = ad, Tur = tur, UstPt = ust, YukseklikPt = boy };

    public static DuzenNesnesi Kutu(string ad, double sol = 0, double ust = 0,
                                    double en = 50, double boy = 12, string? metin = null)
        => new()
        {
            Ad = ad,
            Tur = NesneTuru.Yazi,
            SolPt = sol,
            UstPt = ust,
            GenislikPt = en,
            YukseklikPt = boy,
            Metin = metin,
        };
}
