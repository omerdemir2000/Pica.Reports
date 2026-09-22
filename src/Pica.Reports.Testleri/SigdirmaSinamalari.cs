namespace Pica.Reports.Testleri;

/// <summary>
/// Sığdırma — metin sığmayınca puntoyu küçültme.
/// </summary>
/// <remarks>
/// Alan modelde vardı ama hiçbir yerde okunmuyordu: dar para kutuları metni
/// alt satıra kırıyor, tablonun satır hizası bozuluyordu. Sınamalar kesin
/// punto değerini değil, <b>davranışı</b> ölçüyor — kestirme tablosu
/// değiştiğinde sayılar da değişir, kural değişmemeli.
/// </remarks>
public class SigdirmaSinamalari
{
    private static DuzenNesnesi Para(double en, double punto, bool sigdir)
    {
        var n = Ornek.Kutu("Memo69", en: en, boy: 8);
        n.YaziTipi = "Arial Narrow";
        n.PuntoPt = punto;
        n.Sigdir = sigdir;
        n.KelimeKaydir = true;
        return n;
    }

    /// <summary>Kapalıyken punto olduğu gibi kalır.</summary>
    [Fact]
    public void Kapaliyken_punto_degismez()
        => Assert.Equal(5, Sigdirma.Punto(Para(24.13, 5, sigdir: false), "98.765,43"));

    /// <summary>Sığan metin büyütülmez: sığdırma daraltma ilkesidir.</summary>
    [Fact]
    public void Sigan_metin_buyutulmez()
        => Assert.Equal(5, Sigdirma.Punto(Para(120, 5, sigdir: true), "1,00"));

    /// <summary>Metin yoksa küçültülecek bir şey de yoktur.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Metinsiz_kutu_kucultulmez(string? metin)
        => Assert.Equal(5, Sigdirma.Punto(Para(24.13, 5, sigdir: true), metin));

    /// <summary>
    /// Bildirilen olayın şekli: dar para kutusuna sığmayan <c>98.765,43</c>
    /// alt satıra kırılıyordu; artık punto düşüyor.
    /// </summary>
    [Fact]
    public void Sigmayan_metin_kucultulur()
    {
        var kutu = Para(18, 5, sigdir: true);
        var punto = Sigdirma.Punto(kutu, "98.765,43");

        Assert.True(punto < 5, $"punto düşmedi: {punto}");

        // Küçültülmüş punto metni gerçekten sığdırmalı — yoksa kutu yine
        // kırpılır ve değişiklik hiçbir işe yaramamış olur.
        var alan = kutu.GenislikPt - 2 * Sigdirma.YanDolguPt;
        Assert.True(Sigdirma.Em("98.765,43", kutu) * punto <= alan + 0.01);
    }

    /// <summary>Punto hiçbir koşulda okunamayacak kadar düşmez.</summary>
    [Fact]
    public void Punto_tabana_kadar_iner_daha_asagi_inmez()
    {
        var punto = Sigdirma.Punto(Para(6, 9, sigdir: true), new string('8', 40));

        Assert.Equal(Sigdirma.EnAzPuntoPt, punto);
    }

    /// <summary>
    /// Sığdırma kelime kaydırmayı yener.
    /// </summary>
    /// <remarks>
    /// Şablonların bir kısmında ikisi birlikte işaretli geliyor. Kırma
    /// kazansaydı sığdırma hiçbir şey yapmamış olurdu — bildirilen hata tam
    /// olarak buydu.
    /// </remarks>
    [Fact]
    public void Sigdirma_kelime_kaydirmayi_yener()
    {
        Assert.Contains("white-space:pre;", Bicem.Yazi(Para(24.13, 5, sigdir: true)) + ";");
        Assert.Contains("white-space:pre-wrap", Bicem.Yazi(Para(24.13, 5, sigdir: false)));
    }

    /// <summary>Biçem, sığdırılmış puntoyu yazar — kutununkini değil.</summary>
    [Fact]
    public void Bicem_sigdirilmis_puntoyu_yazar()
    {
        var kutu = Para(18, 5, sigdir: true);

        Assert.NotEqual(Bicem.Yazi(kutu), Bicem.Yazi(kutu, "98.765,43"));
    }

    /// <summary>Dar kesim aynı metni daha küçük yazar.</summary>
    [Fact]
    public void Dar_kesim_daha_az_yer_kaplar()
    {
        var dar = Para(24.13, 5, sigdir: true);
        var genis = Para(24.13, 5, sigdir: true);
        genis.YaziTipi = "Arial";

        Assert.True(Sigdirma.Em("98.765,43", dar) < Sigdirma.Em("98.765,43", genis));
    }

    /// <summary>Çok satırlı metinde en uzun satır belirler.</summary>
    [Fact]
    public void Cok_satirli_metinde_en_uzun_satir_belirler()
    {
        var kutu = Para(24.13, 5, sigdir: true);

        Assert.Equal(Sigdirma.Punto(kutu, "98.765,43"),
                     Sigdirma.Punto(kutu, "1,00\n98.765,43\n2,00"));
    }
}
