using System.Globalization;

namespace Pica.Reports.Testleri;

/// <summary>
/// Örnek (test) veriyle biçimli önizlemenin sınamaları.
/// </summary>
/// <remarks>
/// Önizlemenin işi kullanıcıya "kâğıtta ne görünecek" sorusunu cevaplamak.
/// Yanlış cevap verirse zararlıdır: tasarımcıda düzgün görünen desen kâğıtta
/// başka bir şey basar. Bu yüzden biçim kuralları çizicinin kurallarıyla
/// birebir aynı olmalı.
/// </remarks>
public class OrnekVeriSinamalari
{
    private static DuzenNesnesi Kutu(string? metin, BicimTuru bicim = BicimTuru.Yok, string? desen = null)
    {
        var n = Ornek.Kutu("K1");
        n.Metin = metin;
        n.Bicim = bicim;
        n.BicimDeseni = desen;
        return n;
    }

    [Fact]
    public void Duz_yazi_oldugu_gibi_kalir()
        => Assert.Equal("TOPLAM", OrnekVeri.Yaz(Kutu("TOPLAM")));

    [Fact]
    public void Sayi_bicimi_desene_gore_uygulanir()
    {
        // %2.2n: binlik ayraçlı, iki ondalıklı. Kutunun tamamı tek ifade
        // olduğu için biçim uygulanır.
        var yazi = OrnekVeri.Yaz(Kutu("[borc]", BicimTuru.Sayi, "%2.2n"), satir: 1);

        Assert.Equal(1234.56m.ToString("#,##0.00", CultureInfo.CurrentCulture), yazi);
    }

    [Fact]
    public void Ondalik_ayraci_kutudan_gelir()
    {
        var kutu = Kutu("[tutar]", BicimTuru.Sayi, "%2.2n");
        kutu.OndalikAyraci = ",";
        kutu.BinlikAyraci = ".";

        Assert.Equal("1.234,56", OrnekVeri.Yaz(kutu));
    }

    [Fact]
    public void Tarih_deseni_delphi_yaziliyla_calisir()
    {
        // Delphi deseni küçük harflidir; .NET'te küçük mm dakikadır ve
        // çevrilmeseydi "15.05.2027" gibi bir tarih çıkardı.
        var yazi = OrnekVeri.Yaz(Kutu("[tarih]", BicimTuru.Tarih, "dd.mm.yyyy"));

        Assert.Equal("15.03.2027", yazi);
    }

    [Fact]
    public void Gomulu_ifadeye_bicim_uygulanmaz()
    {
        // Çizicinin kuralı: kutunun tamamı tek ifadeyse biçim uygulanır,
        // metnin içine gömülü ifadede uygulanmaz. Önizleme ayrı davransaydı
        // yanıltırdı.
        var yazi = OrnekVeri.Yaz(Kutu("Tutar: [borc] TL", BicimTuru.Sayi, "%2.0n"));

        Assert.StartsWith("Tutar: ", yazi);
        Assert.EndsWith(" TL", yazi);
        Assert.Contains("1", yazi);
    }

    [Fact]
    public void Sayfa_degiskenleri_ornek_deger_alir()
    {
        Assert.Equal("Sayfa 1 / 3", OrnekVeri.Yaz(Kutu("Sayfa [Page#] / [TotalPages#]")));
        Assert.Equal("7", OrnekVeri.Yaz(Kutu("[Line#]"), satir: 7));
    }

    [Fact]
    public void Toplam_ifadesi_sayi_uretir()
    {
        // SUM(...) taşınmıyor ama önizlemede bir sayı görünmeli: toplam
        // kutusunun genişliği ancak dolu bir sayıyla denenebilir.
        var yazi = OrnekVeri.Yaz(Kutu("[SUM(<ds.\"borc\">)]", BicimTuru.Sayi, "%2.2n"));

        Assert.Contains("98", yazi);
    }

    [Fact]
    public void Metin_bossa_bagli_alan_kullanilir()
    {
        var kutu = Kutu(null, BicimTuru.Sayi, "%2.2n");
        kutu.VeriAlani = "alacak";

        Assert.Equal(1234.56m.ToString("#,##0.00", CultureInfo.CurrentCulture), OrnekVeri.Yaz(kutu));
    }

    [Fact]
    public void Satirdan_satira_deger_degisir()
    {
        // Bütün satırlar aynı olsaydı veri bandının yinelendiği görünmez,
        // sütun genişliği de tek bir değere göre ayarlanırdı.
        Assert.NotEqual(OrnekVeri.Yaz(Kutu("[borc]"), 1), OrnekVeri.Yaz(Kutu("[borc]"), 2));
    }

    [Fact]
    public void Ayni_girdi_ayni_ciktiyi_verir()
    {
        // Rastgelelik ya da saate bağlılık yok: iki önizleme karşılaştırılabilir.
        Assert.Equal(OrnekVeri.Yaz(Kutu("[tarih]"), 3), OrnekVeri.Yaz(Kutu("[tarih]"), 3));
    }

    [Theory]
    [InlineData("borc")]
    [InlineData("AlacakToplam")]
    [InlineData("odenek")]
    public void Parasal_adlar_sayi_uretir(string alan)
        => Assert.IsType<decimal>(OrnekVeri.Deger(alan));

    [Theory]
    [InlineData("tarih")]
    [InlineData("VadeTarihi")]
    public void Tarih_adlari_tarih_uretir(string alan)
        => Assert.IsType<DateTime>(OrnekVeri.Deger(alan));

    [Fact]
    public void Taninmayan_ad_yaziya_duser_ve_adi_tasir()
    {
        // Örnek çıktıda bile hangi kutunun neye bağlı olduğu görünsün.
        Assert.Equal("aciklama 1", OrnekVeri.Deger("aciklama"));
    }
}

public class BicimlemeSinamalari
{
    [Theory]
    [InlineData("%2.2n", "#,##0.00")]
    [InlineData("%2.0n", "#,##0")]
    [InlineData("%1.3f", "0.000")]
    [InlineData("%d", "#,##0")]
    [InlineData("", "#,##0.00")]           // desensiz: varsayılan
    [InlineData("saçma", "#,##0.00")]      // tanınmayan: varsayılan
    public void Delphi_sayi_deseni_cevrilir(string delphi, string beklenen)
        => Assert.Equal(beklenen, Bicimleme.SayiDeseni(delphi));

    [Theory]
    [InlineData("dd.mm.yyyy", "dd.MM.yyyy")]
    [InlineData("dd/mm/yy", "dd/MM/yy")]
    // Saat bölümündeki mm DAKİKADIR ve büyütülmemeli.
    [InlineData("dd.mm.yyyy hh:mm", "dd.MM.yyyy hh:mm")]
    public void Delphi_tarih_deseni_cevrilir(string delphi, string beklenen)
        => Assert.Equal(beklenen, Bicimleme.TarihDeseni(delphi));

    [Fact]
    public void Bicimsiz_deger_varsayilan_yazimla_gelir()
    {
        Assert.Equal("", Bicimleme.Metinle(null));
        Assert.Equal("15.03.2027", Bicimleme.Metinle(new DateTime(2027, 3, 15)));
    }
}
