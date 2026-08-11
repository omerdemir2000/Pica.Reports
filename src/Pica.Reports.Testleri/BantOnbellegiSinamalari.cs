namespace Pica.Reports.Testleri;

/// <summary>
/// Tuvalin bant sırası önbelleği.
/// </summary>
/// <remarks>
/// Bu önbellek yalnızca sayfa nesnesine bakıyordu ve bant eklemek/silmek
/// listeyi <b>yerinde</b> değiştirdiği için ekran hiç tazelenmiyordu: eklenen
/// bant görünmüyor, silinen duruyordu. Dışarıdan "düğme çalışmıyor" gibi
/// görünen, aslında doğru çalışan bir hataydı — sınamalar o yüzden burada.
/// </remarks>
public class BantOnbellegiSinamalari
{
    private static DuzenSayfasi Sayfa(params BantTuru[] turler)
    {
        var sayfa = new DuzenSayfasi { GenislikPt = 595, YukseklikPt = 842 };

        for (var i = 0; i < turler.Length; i++)
            sayfa.Bantlar.Add(new DuzenBandi
            {
                Ad = $"Bant{i + 1}",
                Tur = turler[i],
                UstPt = i * 40,
                YukseklikPt = 20,
            });

        return sayfa;
    }

    [Fact]
    public void Eklenen_bant_siraya_girer()
    {
        var sayfa = Sayfa(BantTuru.RaporBasligi, BantTuru.Veri);
        var onbellek = new BantOnbellegi();

        Assert.Equal(2, onbellek.Sira(sayfa).Count);

        sayfa.Bantlar.Add(new DuzenBandi { Ad = "Yeni", Tur = BantTuru.Alt, UstPt = 200, YukseklikPt = 20 });

        Assert.Equal(3, onbellek.Sira(sayfa).Count);
        Assert.Contains(onbellek.Sira(sayfa), b => b.Bant.Ad == "Yeni");
    }

    [Fact]
    public void Silinen_bant_siradan_duser()
    {
        var sayfa = Sayfa(BantTuru.RaporBasligi, BantTuru.Veri);
        var onbellek = new BantOnbellegi();

        onbellek.Sira(sayfa);
        sayfa.Bantlar.RemoveAt(0);

        var sira = onbellek.Sira(sayfa);

        Assert.Single(sira);
        Assert.Equal(BantTuru.Veri, sira[0].Bant.Tur);
    }

    [Fact]
    public void Bant_geri_alinca_yerine_doner()
    {
        // Geri alma silinen bandı ESKİ YERİNE koyuyor; önbellek bunu da
        // görmeli, yoksa geri alınan bant ekranda belirmez.
        var sayfa = Sayfa(BantTuru.RaporBasligi, BantTuru.Veri);
        var onbellek = new BantOnbellegi();

        var bant = sayfa.Bantlar[0];
        onbellek.Sira(sayfa);

        sayfa.Bantlar.RemoveAt(0);
        Assert.Single(onbellek.Sira(sayfa));

        sayfa.Bantlar.Insert(0, bant);
        Assert.Equal(2, onbellek.Sira(sayfa).Count);
    }

    [Fact]
    public void Degismeyen_duzende_ayni_liste_doner()
    {
        // Önbellek işini yapıyor mu: değişiklik yokken yeniden hesaplamamalı.
        var sayfa = Sayfa(BantTuru.Veri);
        var onbellek = new BantOnbellegi();

        Assert.Same(onbellek.Sira(sayfa), onbellek.Sira(sayfa));
    }

    [Fact]
    public void Baska_sayfaya_gecince_tazelenir()
    {
        var onbellek = new BantOnbellegi();
        var ilk = Sayfa(BantTuru.Veri);
        var ikinci = Sayfa(BantTuru.RaporBasligi, BantTuru.Veri, BantTuru.Alt);

        Assert.Single(onbellek.Sira(ilk));
        Assert.Equal(3, onbellek.Sira(ikinci).Count);
    }

    [Fact]
    public void Sayfa_yoksa_sira_bostur()
    {
        var onbellek = new BantOnbellegi();

        onbellek.Sira(Sayfa(BantTuru.Veri));

        Assert.Empty(onbellek.Sira(null));
    }
}

/// <summary>
/// Bandın fareyle inebileceği alt sınır.
/// </summary>
/// <remarks>
/// İki kural var ve ikisi de kullanıcıyı yanıltmamak için: bant sıfıra kadar
/// küçültülemez (küçültülse tutulacak kenarı kalmaz, bir daha büyütülemez) ve
/// içindeki kutuların altına inemez (basılacak yükseklik zaten en alttaki
/// kutuya göre hesaplanıyor; daha aşağı çekmek kâğıtta hiçbir şey değiştirmez).
/// </remarks>
public class BantAltSiniriSinamalari
{
    private static DuzenBandi Bant(params (double Ust, double Boy)[] kutular)
    {
        var bant = new DuzenBandi { Ad = "Veri1", Tur = BantTuru.Veri, YukseklikPt = 40 };

        for (var i = 0; i < kutular.Length; i++)
        {
            var n = Ornek.Kutu($"K{i + 1}");
            n.UstPt = kutular[i].Ust;
            n.YukseklikPt = kutular[i].Boy;
            bant.Nesneler.Add(n);
        }

        return bant;
    }

    [Fact]
    public void Kutusuz_bant_sabit_alt_sinira_iner()
        => Assert.Equal(BantSirasi.EnAzYukseklik, BantSirasi.EnAz(Bant()));

    [Fact]
    public void Alt_sinir_en_alttaki_kutudur()
    {
        // 10 + 14 = 24: kutunun alt kenarı.
        Assert.Equal(24, BantSirasi.EnAz(Bant((0, 12), (10, 14))));
    }

    [Fact]
    public void Kucuk_kutu_sabit_siniri_dusurmez()
    {
        // Kutu 2 puntoluk bile olsa bant 4'ün altına inmez.
        Assert.Equal(BantSirasi.EnAzYukseklik, BantSirasi.EnAz(Bant((0, 2))));
    }

    [Fact]
    public void Icerik_yuksekligi_bildirilenden_bagimsiz()
    {
        var bant = Bant((0, 60));
        bant.YukseklikPt = 10;

        // Bildirilen 10 ama kutu 60: basılacak yükseklik 60, alt sınır da 60.
        Assert.Equal(60, BantSirasi.IcerikYuksekligi(bant));
        Assert.Equal(60, BantSirasi.Yukseklik(bant));
        Assert.Equal(60, BantSirasi.EnAz(bant));
    }
}
