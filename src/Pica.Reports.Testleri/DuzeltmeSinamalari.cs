using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pica.Reports.Testleri;

/// <summary>
/// Düzeltmenin kayıpsızlığı.
/// </summary>
/// <remarks>
/// Tasarımcının kaydettiği şey düzenin kendisi değil, ham düzenle arasındaki
/// farktır. Fark çıkarma ile uygulama birbirinin tersi olmazsa kaydetmek
/// veri kaybetmek demektir — ve kaybın fark edilmesi zordur, çünkü ekranda
/// her şey doğru görünür, yanlış olan diskteki dosyadır.
/// </remarks>
public class DuzeltmeSinamalari
{
    private static readonly JsonSerializerOptions Secenekler = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public void Degismemis_duzen_bos_duzeltme_uretir()
    {
        // En önemli sınama: bir düzeni açıp hiçbir şey yapmadan kaydeden biri
        // sahte bir düzeltme dosyası oluşturmamalı. Ondalık kuyruğu uzun punto
        // değerleri (2,8346475) kayan nokta gürültüsüyle fark üretmeye açık.
        var ham = Ornek.Kutulu(Ornek.Kutu("K1", sol: 2.8346475, ust: 31.3547));
        var calisma = Ornek.Kutulu(Ornek.Kutu("K1", sol: 2.8346475, ust: 31.3547));

        var duzeltme = DuzenDuzeltmesi.Cikar(ham, calisma);

        Assert.True(duzeltme.Bos);
    }

    [Fact]
    public void Alan_degisikligi_cikarilip_geri_uygulanabilir()
    {
        var ham = Ornek.Kutulu(Ornek.Kutu("K1", sol: 10, metin: "eski"));
        var calisma = Ornek.Kutulu(Ornek.Kutu("K1", sol: 10, metin: "eski"));

        var kutu = Kutu(calisma, "K1");
        kutu.SolPt = 42.5;
        kutu.Metin = "[alan]";
        kutu.Kalin = true;

        var geri = Ornek.Kutulu(Ornek.Kutu("K1", sol: 10, metin: "eski"));
        DuzenDuzeltmesi.Cikar(ham, calisma).Uygula(geri);

        Assert.Equal(42.5, Kutu(geri, "K1").SolPt);
        Assert.Equal("[alan]", Kutu(geri, "K1").Metin);
        Assert.True(Kutu(geri, "K1").Kalin);
    }

    [Fact]
    public void Ozgun_degerine_dondurulen_alan_duzeltmeye_girmez()
    {
        var ham = Ornek.Kutulu(Ornek.Kutu("K1", sol: 10));
        var calisma = Ornek.Kutulu(Ornek.Kutu("K1", sol: 10));

        Kutu(calisma, "K1").SolPt = 99;
        Kutu(calisma, "K1").SolPt = 10;

        Assert.True(DuzenDuzeltmesi.Cikar(ham, calisma).Bos);
    }

    [Fact]
    public void Eklenen_kutu_butunuyle_tasinir()
    {
        var ham = Ornek.Kutulu(Ornek.Kutu("K1"));
        var calisma = Ornek.Kutulu(Ornek.Kutu("K1"));

        calisma.Sayfalar[0].Bantlar[0].Nesneler.Add(
            Ornek.Kutu("Yeni", sol: 5, ust: 6, en: 70, boy: 14, metin: "eklendi"));

        var geri = Ornek.Kutulu(Ornek.Kutu("K1"));
        DuzenDuzeltmesi.Cikar(ham, calisma).Uygula(geri);

        var eklenen = Kutu(geri, "Yeni");
        Assert.Equal(5, eklenen.SolPt);
        Assert.Equal(70, eklenen.GenislikPt);
        Assert.Equal("eklendi", eklenen.Metin);
    }

    [Fact]
    public void Silinen_kutu_duzeltmede_kalir()
    {
        var ham = Ornek.Kutulu(Ornek.Kutu("K1"), Ornek.Kutu("K2"));
        var calisma = Ornek.Kutulu(Ornek.Kutu("K1"), Ornek.Kutu("K2"));

        calisma.Sayfalar[0].Bantlar[0].Nesneler.RemoveAll(n => n.Ad == "K2");

        var geri = Ornek.Kutulu(Ornek.Kutu("K1"), Ornek.Kutu("K2"));
        DuzenDuzeltmesi.Cikar(ham, calisma).Uygula(geri);

        Assert.Single(geri.Sayfalar[0].Bantlar[0].Nesneler);
        Assert.Equal("K1", geri.Sayfalar[0].Bantlar[0].Nesneler[0].Ad);
    }

    [Fact]
    public void Eklenen_bant_kutulariyla_birlikte_tasinir()
    {
        var ham = Ornek.Kutulu(Ornek.Kutu("K1"));
        var calisma = Ornek.Kutulu(Ornek.Kutu("K1"));

        var yeni = Ornek.Bant("Toplam1", BantTuru.Alt, ust: 500, boy: 18);
        yeni.Nesneler.Add(Ornek.Kutu("TopKutu", sol: 3, en: 90, metin: "TOPLAM"));
        calisma.Sayfalar[0].Bantlar.Add(yeni);

        var geri = Ornek.Kutulu(Ornek.Kutu("K1"));
        var duzeltme = DuzenDuzeltmesi.Cikar(ham, calisma);

        // Bandın kutuları bandın içinde taşınır; ayrıca "eklenen kutu" olarak
        // da yazılsalardı düzeltme uygulanırken iki kez konurlardı.
        Assert.Empty(duzeltme.Eklenenler);

        duzeltme.Uygula(geri);

        var bant = geri.Sayfalar[0].Bantlar.Single(b => b.Ad == "Toplam1");
        Assert.Equal(BantTuru.Alt, bant.Tur);
        Assert.Equal(500, bant.UstPt);
        Assert.Equal("TOPLAM", bant.Nesneler.Single().Metin);
    }

    [Fact]
    public void Silinen_bant_duzeltmede_kalir()
    {
        var ham = Ornek.Kutulu(Ornek.Kutu("K1"));
        ham.Sayfalar[0].Bantlar.Add(Ornek.Bant("Fazla1", BantTuru.Alt, ust: 300, boy: 10));

        var calisma = Ornek.Kutulu(Ornek.Kutu("K1"));
        calisma.Sayfalar[0].Bantlar.Add(Ornek.Bant("Fazla1", BantTuru.Alt, ust: 300, boy: 10));
        calisma.Sayfalar[0].Bantlar.RemoveAll(b => b.Ad == "Fazla1");

        var geri = Ornek.Kutulu(Ornek.Kutu("K1"));
        geri.Sayfalar[0].Bantlar.Add(Ornek.Bant("Fazla1", BantTuru.Alt, ust: 300, boy: 10));

        DuzenDuzeltmesi.Cikar(ham, calisma).Uygula(geri);

        Assert.Single(geri.Sayfalar[0].Bantlar);
        Assert.Equal("Veri1", geri.Sayfalar[0].Bantlar[0].Ad);
    }

    [Fact]
    public void Eklenen_bant_kopyalanir_paylasilmaz()
    {
        // Aynı düzeltme iki düzene uygulanırsa birine eklenen kutu diğerinde
        // de belirmemeli.
        var ham = Ornek.Kutulu(Ornek.Kutu("K1"));
        var calisma = Ornek.Kutulu(Ornek.Kutu("K1"));
        calisma.Sayfalar[0].Bantlar.Add(Ornek.Bant("Yeni1", BantTuru.Alt, ust: 200, boy: 12));

        var duzeltme = DuzenDuzeltmesi.Cikar(ham, calisma);

        var a = Ornek.Kutulu(Ornek.Kutu("K1"));
        var b = Ornek.Kutulu(Ornek.Kutu("K1"));
        duzeltme.Uygula(a);
        duzeltme.Uygula(b);

        a.Sayfalar[0].Bantlar.Single(x => x.Ad == "Yeni1").Nesneler.Add(Ornek.Kutu("Sonradan"));

        Assert.Empty(b.Sayfalar[0].Bantlar.Single(x => x.Ad == "Yeni1").Nesneler);
    }

    [Fact]
    public void Bant_degisikligi_cikarilip_geri_uygulanabilir()
    {
        var ham = Ornek.Kutulu(Ornek.Kutu("K1"));
        var calisma = Ornek.Kutulu(Ornek.Kutu("K1"));

        calisma.Sayfalar[0].Bantlar[0].YukseklikPt = 55.5;
        calisma.Sayfalar[0].Bantlar[0].VeriKumesi = "yeniKume";

        var geri = Ornek.Kutulu(Ornek.Kutu("K1"));
        DuzenDuzeltmesi.Cikar(ham, calisma).Uygula(geri);

        Assert.Equal(55.5, geri.Sayfalar[0].Bantlar[0].YukseklikPt);
        Assert.Equal("yeniKume", geri.Sayfalar[0].Bantlar[0].VeriKumesi);
    }

    [Fact]
    public void Duzeltme_diskten_gecince_de_ayni_kalir()
    {
        // Enum'lar dosyaya ad olarak yazılıyor, alanların hepsi boş
        // geçilebiliyor; sözleşme bozulursa kayıt sessizce eksik okunur.
        var ham = Ornek.Kutulu(Ornek.Kutu("K1"));
        var calisma = Ornek.Kutulu(Ornek.Kutu("K1"));

        var kutu = Kutu(calisma, "K1");
        kutu.Yatay = YatayHiza.Sag;
        kutu.Dikey = DikeyHiza.Orta;
        kutu.Cerceve = CerceveKenari.Sol | CerceveKenari.Alt;
        kutu.Bicim = BicimTuru.Sayi;
        kutu.KelimeKaydir = true;

        var yazi = JsonSerializer.Serialize(DuzenDuzeltmesi.Cikar(ham, calisma), Secenekler);
        var okunan = JsonSerializer.Deserialize<DuzenDuzeltmesi>(yazi, Secenekler);

        var geri = Ornek.Kutulu(Ornek.Kutu("K1"));
        okunan!.Uygula(geri);

        var sonuc = Kutu(geri, "K1");
        Assert.Equal(YatayHiza.Sag, sonuc.Yatay);
        Assert.Equal(DikeyHiza.Orta, sonuc.Dikey);
        Assert.Equal(CerceveKenari.Sol | CerceveKenari.Alt, sonuc.Cerceve);
        Assert.Equal(BicimTuru.Sayi, sonuc.Bicim);
        Assert.True(sonuc.KelimeKaydir);
    }

    [Fact]
    public void Bulunamayan_kutu_sessizce_gecilmez()
    {
        // Düzen yeniden dönüştürülürken kutu adı değişmiş olabilir. O düzeltme
        // artık tutmuyor ve kutu kâğıtta boş çıkıyor; bunun fark edilmesinin
        // tek yolu uyarı listesi.
        var duzeltme = new DuzenDuzeltmesi
        {
            Nesneler = [new NesneDuzeltmesi { Bant = "Veri1", Nesne = "OlmayanKutu", Metin = "x" }],
        };

        var bulunamayan = duzeltme.Uygula(Ornek.Kutulu(Ornek.Kutu("K1")));

        Assert.Single(bulunamayan);
        Assert.Contains("OlmayanKutu", bulunamayan[0]);
    }

    private static DuzenNesnesi Kutu(CetvelDuzeni duzen, string ad)
        => duzen.Sayfalar.SelectMany(s => s.Bantlar).SelectMany(b => b.Nesneler).Single(n => n.Ad == ad);
}
