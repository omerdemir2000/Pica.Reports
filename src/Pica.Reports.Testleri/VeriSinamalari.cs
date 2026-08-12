using System.Globalization;
using Pica.Reports.Veri;

namespace Pica.Reports.Testleri;

/// <summary>
/// Veri kaynağı kaydının sınamaları.
/// </summary>
/// <remarks>
/// Kütüphanenin dışarıya bakan sözü şu: "satırlarınızı verin, ben adlarını
/// bulurum". Dapper satırları sözlüktür, kendi sınıflarınız nesnedir; ikisi de
/// aynı kapıdan girer ve <b>ayırt etme satırın şekline bakılarak</b> yapılır —
/// Dapper <c>dynamic</c> döndürdüğü için derleme anında tür bilgisi yok.
/// </remarks>
public class VeriKumesiSinamalari
{
    private sealed class Fis
    {
        public int FisNo { get; set; }
        public string Aciklama { get; set; } = "";
        public decimal Tutar { get; set; }
    }

    private static Dictionary<string, object?> Sozluk(int no, string aciklama, decimal tutar)
        => new() { ["FisNo"] = no, ["Aciklama"] = aciklama, ["Tutar"] = tutar };

    [Fact]
    public void Sozluk_satirlari_alanlarini_verir()
    {
        var kume = VeriKumesi.Olustur("Fisler", new[] { Sozluk(1, "a", 10m), Sozluk(2, "b", 20m) });

        Assert.Equal(["FisNo", "Aciklama", "Tutar"], kume.Alanlar);
        Assert.Equal(2, kume.Satirlar.Count);
        Assert.Equal(20m, kume.Satirlar[1]["Tutar"]);
    }

    [Fact]
    public void Nesne_satirlari_ozelliklerinden_okunur()
    {
        var kume = VeriKumesi.Olustur("Fisler", new[]
        {
            new Fis { FisNo = 7, Aciklama = "kırtasiye", Tutar = 125.5m },
        });

        Assert.Equal(["FisNo", "Aciklama", "Tutar"], kume.Alanlar);
        Assert.Equal("kırtasiye", kume.Satirlar[0]["Aciklama"]);
    }

    [Fact]
    public void Bos_liste_alanlarini_turden_cikarir()
    {
        // Sorgu hiç satır döndürmese de alan ağacı dolu kalmalı: tasarım
        // yapılabilsin diye.
        var kume = VeriKumesi.Olustur("Fisler", Array.Empty<Fis>());

        Assert.Empty(kume.Satirlar);
        Assert.Equal(["FisNo", "Aciklama", "Tutar"], kume.Alanlar);
    }

    [Fact]
    public void Alan_adi_buyuk_kucuk_harfe_duyarsiz()
    {
        var kume = VeriKumesi.Olustur("Fisler", new[] { Sozluk(1, "a", 10m) });

        Assert.Equal(10m, kume.Satirlar[0]["TUTAR"]);
        Assert.True(kume.Satirlar[0].Var("tutar"));
    }

    [Fact]
    public void Farkli_satirlarin_alanlari_birlesir()
    {
        var kume = VeriKumesi.Olustur("Karma", new[]
        {
            new Dictionary<string, object?> { ["A"] = 1 },
            new Dictionary<string, object?> { ["B"] = 2 },
        });

        Assert.Equal(["A", "B"], kume.Alanlar);
    }
}

public class RaporVerisiSinamalari
{
    private static RaporVerisi Iki() => new RaporVerisi()
        .Ekle("Fisler", new[] { new Dictionary<string, object?> { ["Tutar"] = 100m } })
        .Ekle("Ozet", new[] { new Dictionary<string, object?> { ["Sayi"] = 3 } });

    [Fact]
    public void Kume_adiyla_bulunur()
    {
        var veri = Iki();

        Assert.Equal("Fisler", veri.Kume("Fisler")!.Ad);
        Assert.Equal("Ozet", veri.Kume("ozet")!.Ad);   // ad büyük/küçük harfe duyarsız
        Assert.Null(veri.Kume("Yok"));
    }

    [Fact]
    public void Adsiz_istek_tek_kume_varsa_karsilanir()
    {
        // Taşınan düzenlerin çoğu tek kümeyle çalışıyor ve bandına küme adı
        // yazmıyor; tahmin ancak tek aday varken doğru olur.
        var tek = new RaporVerisi().Ekle("Fisler", new[] { new Dictionary<string, object?>() });

        Assert.NotNull(tek.Kume(null));
        Assert.Null(Iki().Kume(null));
    }

    [Fact]
    public void Ayni_adla_ikinci_kayit_oncekini_degistirir()
    {
        // Rapor her açılışta güncel veriyle basılmalı.
        var veri = new RaporVerisi()
            .Ekle("Fisler", new[] { new Dictionary<string, object?> { ["A"] = 1 } })
            .Ekle("Fisler", new[] { new Dictionary<string, object?> { ["A"] = 2 } });

        Assert.Single(veri.Kumeler);
        Assert.Equal(2, veri.Kume("Fisler")!.Satirlar[0]["A"]);
    }

    [Fact]
    public void Tanit_verisiz_kume_acar()
    {
        // Sorgu çalıştırmadan tasarım yapılabilsin diye.
        var veri = new RaporVerisi().Tanit("Fisler", "FisNo", "Tutar");

        Assert.Empty(veri.Kume("Fisler")!.Satirlar);
        Assert.Equal(["FisNo", "Tutar"], veri.Kume("Fisler")!.Alanlar);
    }

    [Fact]
    public void Katalog_alan_agacina_verilir()
    {
        var katalog = Iki().Katalog();

        Assert.Equal(2, katalog.Count);
        Assert.Contains(katalog, k => k.Ad == "Fisler" && k.Alanlar.Contains("Tutar"));
    }
}

public class DegerCozucuSinamalari
{
    private static DuzenNesnesi Kutu(string metin, BicimTuru bicim = BicimTuru.Yok, string? desen = null)
    {
        var n = Ornek.Kutu("K1");
        n.Metin = metin;
        n.Bicim = bicim;
        n.BicimDeseni = desen;
        return n;
    }

    private static VeriSatiri Satir(params (string Alan, object? Deger)[] alanlar)
        => new(alanlar.ToDictionary(a => a.Alan, a => a.Deger, StringComparer.OrdinalIgnoreCase));

    [Fact]
    public void Satir_alani_okunur_ve_bicimlenir()
    {
        var cozucu = new DegerCozucu(new RaporVerisi(), ornek: false)
        {
            Satir = Satir(("Tutar", 1234.5m)),
        };

        var kutu = Kutu("[Fisler.\"Tutar\"]", BicimTuru.Sayi, "%2.2n");
        kutu.OndalikAyraci = ",";
        kutu.BinlikAyraci = ".";

        Assert.Equal("1.234,50", cozucu.Yaz(kutu));
    }

    [Fact]
    public void Rapor_degiskeni_satir_disinda_okunur()
    {
        var veri = new RaporVerisi().Degisken("KurumAdi", "Papirus");
        var cozucu = new DegerCozucu(veri, ornek: false);

        Assert.Equal("Papirus", cozucu.Yaz(Kutu("[KurumAdi]")));
    }

    [Fact]
    public void Cozulemeyen_basvuru_veri_kipinde_oldugu_gibi_kalir()
    {
        // Boş bırakmak, eksiği sessizce doğru göstermek olurdu.
        var cozucu = new DegerCozucu(new RaporVerisi(), ornek: false);

        Assert.Equal("[Yokolan]", cozucu.Yaz(Kutu("[Yokolan]")));
    }

    [Fact]
    public void Ornek_kipinde_bos_alan_uydurma_degerle_dolar()
    {
        var cozucu = new DegerCozucu(null, ornek: true) { SatirSirasi = 1 };

        Assert.NotEqual("[Borc]", cozucu.Yaz(Kutu("[Borc]")));
    }

    [Fact]
    public void Sayfa_toplami_satirlardan_birikir_ve_sayfa_sonunda_devreder()
    {
        var cozucu = new DegerCozucu(new RaporVerisi(), ornek: false);

        cozucu.SatirIsle(Satir(("Borc", 100m)));
        cozucu.SatirIsle(Satir(("Borc", 50m)));

        var sayfaToplami = Kutu("[SUM(<Yevmiye.\"Borc\">, Veri1, 2)]");
        Assert.Equal(Bicimli(150m), cozucu.Yaz(sayfaToplami));

        cozucu.SayfayiKapat();

        // Nakli yekûn: kapanan sayfanın toplamı devreder, sayfa toplamı sıfırlanır.
        Assert.Equal(Bicimli(150m), cozucu.Yaz(Kutu("[Nakil_Borc]")));
        Assert.Equal(Bicimli(0m), cozucu.Yaz(sayfaToplami));
    }

    [Fact]
    public void Rapor_toplami_kumenin_tamamini_gezer()
    {
        var veri = new RaporVerisi().Ekle("Fisler", new[]
        {
            new Dictionary<string, object?> { ["Tutar"] = 10m },
            new Dictionary<string, object?> { ["Tutar"] = 15m },
        });

        var cozucu = new DegerCozucu(veri, ornek: false);

        Assert.Equal(Bicimli(25m), cozucu.Yaz(Kutu("[SUM(<Fisler.\"Tutar\">)]")));
    }

    /// <summary>Beklenen çıktı, o anki kültürün ondalık ayracıyla.</summary>
    /// <remarks>
    /// Ayraç <b>elle yazılamaz</b>: kütüphane sayıyı kültürden geçiriyor (kendi
    /// dil ayarı yok, barındıran uygulamanınkini kullanıyor) ve "150,00" yazan
    /// bir sınama yalnızca Türkçe makinede geçerdi. CI ubuntu üzerinde koşuyor,
    /// orada ayraç nokta. Aynı hesap <c>OrnekVeriSinamalari</c> içinde de var.
    /// </remarks>
    private static string Bicimli(decimal d) => d.ToString("#,##0.00", CultureInfo.CurrentCulture);

    [Fact]
    public void Sayfa_numarasi_ve_toplam_yazilir()
    {
        var cozucu = new DegerCozucu(null, ornek: false) { SayfaNo = 2, ToplamSayfa = 5 };

        Assert.Equal("Sayfa 2 / 5", cozucu.Yaz(Kutu("Sayfa [Page#] / [TotalPages#]")));
    }
}
