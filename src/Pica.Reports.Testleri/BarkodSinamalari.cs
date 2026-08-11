namespace Pica.Reports.Testleri;

/// <summary>
/// Barkod kodlamasının sınamaları.
/// </summary>
/// <remarks>
/// Barkodda "gözle bakıp doğru görünüyor" diye bir şey yok: desen tablosundaki
/// tek harflik bir hata, okunduğunda başka bir şey söyleyen ya da hiç okunmayan
/// bir etiket üretir ve bu ancak sahada anlaşılır. Sınamalar bu yüzden hem
/// yapısal değişmezleri (her simge 11 modül, sağlama kuralı) hem de bilinen
/// örnekleri kontrol ediyor.
/// </remarks>
public class Code128Sinamalari
{
    /// <summary>Metnin kodundaki modül dizisi.</summary>
    private static bool[] Kod(string metin)
        => Barkod.Kodla(metin, BarkodTuru.Code128)!.Value.Moduller;

    [Fact]
    public void Bos_deger_kodlanmaz()
    {
        Assert.Null(Barkod.Kodla(null, BarkodTuru.Code128));
        Assert.Null(Barkod.Kodla("   ", BarkodTuru.Code128));
    }

    [Fact]
    public void Ascii_disi_karakter_kodlanmaz()
    {
        // Code 128'in B kümesi 32..126 arasını taşır. Türkçe harfler
        // kodlanamaz; uydurup basmak yerine kutu boş kalır.
        Assert.Null(Barkod.Kodla("ŞUBE", BarkodTuru.Code128));
    }

    [Fact]
    public void Uzunluk_simge_basina_11_modul()
    {
        // START + 3 veri + sağlama = 5 simge × 11 modül, artı 13 modüllük STOP.
        var kod = Kod("ABC");

        Assert.Equal(5 * 11 + 13, kod.Length);
    }

    [Fact]
    public void Barkod_koyu_cubukla_baslar_ve_biter()
    {
        var kod = Kod("PBM2027");

        Assert.True(kod[0]);
        Assert.True(kod[^1]);
    }

    [Fact]
    public void Bitis_simgesi_her_zaman_ayni()
    {
        // STOP deseni 2331112: 2 koyu, 3 açık, 3 koyu, 1 açık, 1 koyu, 1 açık,
        // 2 koyu. Sonu değişmiş bir barkod hiç okunmaz.
        bool[] beklenen =
        [
            true, true, false, false, false, true, true, true,
            false, true, false, true, true,
        ];

        Assert.Equal(beklenen, Kod("1")[^13..]);
    }

    [Fact]
    public void Ayni_deger_ayni_kodu_verir()
    {
        // Kodlamada rastgelelik ya da duruma bağlılık yok; olsaydı aynı düzen
        // her basımda başka bir barkod üretirdi.
        Assert.Equal(Kod("12345"), Kod("12345"));
    }

    [Fact]
    public void Farkli_deger_farkli_kod_verir()
        => Assert.NotEqual(Kod("12345"), Kod("12346"));

    [Fact]
    public void Desen_tablosu_bozulmamis()
    {
        // Her simge 11 modüldür (bitiş 13) ve altı öğeden oluşur. Tabloya
        // düşen bir yazım hatası neredeyse her zaman bu iki kuraldan birini
        // bozar; barkodun kendisine bakmadan yakalanır.
        var alan = typeof(Barkod).GetField("Code128Desenleri",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var desenler = (string[])alan!.GetValue(null)!;

        Assert.Equal(107, desenler.Length);

        for (var i = 0; i < desenler.Length; i++)
        {
            var desen = desenler[i];
            var son = i == desenler.Length - 1;

            Assert.Equal(son ? 7 : 6, desen.Length);
            Assert.All(desen, c => Assert.InRange(c, '1', '4'));
            Assert.Equal(son ? 13 : 11, desen.Sum(c => c - '0'));
        }

        // Standartta sabit olan dört simge: START A/B/C ve STOP.
        Assert.Equal("211412", desenler[103]);
        Assert.Equal("211214", desenler[104]);
        Assert.Equal("211232", desenler[105]);
        Assert.Equal("2331112", desenler[106]);
    }
}

public class Ean13Sinamalari
{
    [Fact]
    public void On_iki_hanenin_saglamasi_hesaplanir()
    {
        // 5901234123457 yaygın bir örnek koddur; sağlama hanesi 7'dir.
        var kod = Barkod.Kodla("590123412345", BarkodTuru.Ean13);

        Assert.Equal("5901234123457", kod!.Value.Yazi);
    }

    [Fact]
    public void On_uc_hane_verilirse_saglama_dogrulanir()
    {
        Assert.NotNull(Barkod.Kodla("5901234123457", BarkodTuru.Ean13));

        // Son hane yanlış: kodlanmaz. Basılsaydı kasada okunmayan bir etiket
        // çıkardı ve hata ancak orada görülürdü.
        Assert.Null(Barkod.Kodla("5901234123456", BarkodTuru.Ean13));
    }

    [Fact]
    public void Hane_sayisi_tutmuyorsa_kodlanmaz()
    {
        Assert.Null(Barkod.Kodla("12345", BarkodTuru.Ean13));
        Assert.Null(Barkod.Kodla("12345678901234", BarkodTuru.Ean13));
    }

    [Fact]
    public void Rakam_disi_karakterler_atilir()
    {
        // Elle yazılan kodlarda tire ve boşluk oluyor; kodu reddetmek yerine
        // ayıklamak kullanıcının işine yarıyor.
        var kod = Barkod.Kodla("590-1234 123457", BarkodTuru.Ean13);

        Assert.Equal("5901234123457", kod!.Value.Yazi);
    }

    [Fact]
    public void Uzunluk_ve_korumalar_standarta_uyar()
    {
        var kod = Barkod.Kodla("5901234123457", BarkodTuru.Ean13)!.Value.Moduller;

        // 3 + 6×7 + 5 + 6×7 + 3 = 95 modül.
        Assert.Equal(95, kod.Length);

        // Sol koruma 101, orta koruma 01010, sağ koruma 101.
        Assert.Equal([true, false, true], kod[..3]);
        Assert.Equal([false, true, false, true, false], kod[45..50]);
        Assert.Equal([true, false, true], kod[^3..]);
    }
}

public class BarkodSvgSinamalari
{
    [Fact]
    public void Kodlanamayan_deger_svg_uretmez()
        => Assert.Null(Barkod.Svg("ŞUBE", BarkodTuru.Code128));

    [Fact]
    public void Svg_kutunun_oraniyla_kurulur()
    {
        // viewBox kutuyla aynı oranda olmalı: PDF motoru SVG'yi kendi oranını
        // koruyarak yerleştiriyor, oran tutmazsa barkod kutunun içinde küçülüp
        // bir köşeye yaslanır.
        var svg = Barkod.Svg("12345", BarkodTuru.Code128, yazi: false, enBoyOrani: 4)!;

        var kutu = svg.Split("viewBox=\"")[1].Split('"')[0].Split(' ');
        var en = double.Parse(kutu[2], System.Globalization.CultureInfo.InvariantCulture);
        var boy = double.Parse(kutu[3], System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(4, en / boy, 3);
    }

    [Fact]
    public void Sayilar_noktayla_yazilir()
    {
        // Türkçe kültürde virgülle biçimlenen bir koordinat SVG'yi bozar ve
        // barkod hiç çizilmez.
        var eski = System.Threading.Thread.CurrentThread.CurrentCulture;
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("tr-TR");

        try
        {
            var svg = Barkod.Svg("12345", BarkodTuru.Code128, enBoyOrani: 3.7)!;
            Assert.DoesNotContain(",", svg);
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = eski;
        }
    }

    [Fact]
    public void Yazi_kapaliyken_metin_dugumu_yok()
    {
        Assert.DoesNotContain("<text", Barkod.Svg("12345", BarkodTuru.Code128, yazi: false));
        Assert.Contains("<text", Barkod.Svg("12345", BarkodTuru.Code128, yazi: true));
    }

    [Fact]
    public void Metindeki_isaretler_kacirilir()
    {
        // Barkodun altındaki yazı doğrudan SVG'ye giriyor; kaçırılmazsa
        // "A&B" belgeyi bozardı.
        var svg = Barkod.Svg("A&B", BarkodTuru.Code128)!;

        Assert.Contains("A&amp;B", svg);
    }
}
