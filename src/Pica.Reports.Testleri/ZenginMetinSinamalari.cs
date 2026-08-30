using System.Globalization;

namespace Pica.Reports.Testleri;

/// <summary>
/// Zengin metin süzgecinin sınamaları.
/// </summary>
/// <remarks>
/// Bu süzgeç bir <b>güvenlik sınırı</b>: içeriği veritabanından geliyor ve
/// tarayıcıya im olarak basılıyor. Süzülmeyen bir <c>&lt;script&gt;</c> raporu
/// açan herkeste çalışır — "gözle bakınca düzgün görünüyor" burada ölçü değil,
/// kaçan tek bir öznitelik yeter. Sınamalar hem korunması gerekeni (biçimleme)
/// hem de geçmemesi gerekeni (kod, olay, adres) kontrol ediyor.
/// </remarks>
public class ZenginMetinSinamalari
{
    // ─────────────────────────────────────────────────────── korunanlar

    [Fact]
    public void Bicimleme_etiketleri_korunur()
    {
        Assert.Equal("<b>kalın</b>", ZenginMetin.Temizle("<b>kalın</b>"));
        Assert.Equal("<i>eğik</i>", ZenginMetin.Temizle("<i>eğik</i>"));
        Assert.Equal("<u>altı çizili</u>", ZenginMetin.Temizle("<u>altı çizili</u>"));
        Assert.Equal("<p>paragraf</p>", ZenginMetin.Temizle("<p>paragraf</p>"));
    }

    [Fact]
    public void Tablo_ve_liste_korunur()
    {
        // BI-RADS gibi iki sütunlu radyoloji listeleri tablo olarak geliyor;
        // tablo düşerse sütunlar yan yana yapışır ve metin okunmaz olur.
        const string im = "<table><tr><td>3</td><td>Heterojen dens</td></tr></table>";
        Assert.Equal(im, ZenginMetin.Temizle(im));

        Assert.Equal("<ul><li>bir</li></ul>", ZenginMetin.Temizle("<ul><li>bir</li></ul>"));
    }

    [Fact]
    public void Class_ozniteligi_korunur()
    {
        // Barındıran uygulamanın biçemi buna bağlanabiliyor (bir RTF çeviricisi
        // tabloya sınıf veriyor); tek tanınan öznitelik budur.
        Assert.Equal("<table class=\"rtf-tablo\"></table>",
                     ZenginMetin.Temizle("<table class=\"rtf-tablo\"></table>"));
    }

    [Fact]
    public void Duz_metin_oldugu_gibi_gecer()
    {
        // Veri alanından gelen sıradan bir metin bu kutuda da doğru basmalı;
        // kullanıcı türü değiştirmek zorunda kalmasın.
        Assert.Equal("Hasta iyileşti.", ZenginMetin.Temizle("Hasta iyileşti."));
    }

    [Fact]
    public void Bos_deger_bos_doner()
    {
        Assert.Equal("", ZenginMetin.Temizle(null));
        Assert.Equal("", ZenginMetin.Temizle(""));
    }

    // ─────────────────────────────────────────────────────── süzülenler

    [Fact]
    public void Script_govdesiyle_birlikte_atilir()
    {
        // Etiketin düşmesi yetmez: gövdesi de gitmeli, yoksa kodun kendisi
        // kâğıda metin olarak basılırdı.
        var temiz = ZenginMetin.Temizle("önce <script>alert(1)</script> sonra");

        Assert.DoesNotContain("script", temiz, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alert", temiz, StringComparison.OrdinalIgnoreCase);

        // Çevresindeki metin kalır. Boşluklar imde olduğu gibi geçer —
        // etiketin iki yanındaki boşluk sayısı korunur, tarayıcı zaten tek
        // boşluğa indirir; süzgecin işi biçim düzeltmek değil.
        Assert.StartsWith("önce", temiz);
        Assert.EndsWith("sonra", temiz);
    }

    [Fact]
    public void Style_govdesiyle_birlikte_atilir()
        => Assert.Equal("x", ZenginMetin.Temizle("<style>body{display:none}</style>x"));

    [Fact]
    public void Olay_oznitelikleri_gecmez()
    {
        var temiz = ZenginMetin.Temizle("<b onclick=\"kotu()\" onmouseover=\"x\">metin</b>");

        Assert.Equal("<b>metin</b>", temiz);
        Assert.DoesNotContain("onclick", temiz, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Style_ozniteligi_gecmez()
    {
        // style, url() ve expression() ile bir saldırı yüzeyi; ayrıca kutunun
        // kendi biçemini ezip kâğıdı bozardı.
        var temiz = ZenginMetin.Temizle("<span style=\"position:fixed;top:0\">x</span>");

        Assert.Equal("<span>x</span>", temiz);
        Assert.DoesNotContain("style", temiz, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Baglanti_ve_resim_etiketi_dusrer_metni_kalir()
    {
        // Kâğıda basılan raporda tıklanacak bağlantı olmaz; resim ise kendi
        // kutu türüdür. Etiket düşer ama METİN KALIR — bilinmeyen bir
        // sarmalayıcı yüzünden içerik kaybolmamalı.
        Assert.Equal("Rapor", ZenginMetin.Temizle("<a href=\"javascript:kotu()\">Rapor</a>"));
        Assert.Equal("", ZenginMetin.Temizle("<img src=\"x\" onerror=\"kotu()\" />"));
    }

    [Fact]
    public void Iframe_govdesiyle_atilir()
        => Assert.Equal("a", ZenginMetin.Temizle("a<iframe src=\"http://x\">içerik</iframe>"));

    // ───────────────────────────────────────────────── bozuk im dayanıklılığı

    [Fact]
    public void Kapatilmamis_etiket_sonda_kapatilir()
    {
        // Kapatılmayan bir etiket, süzgeç kapatmasa, kâğıdın geri kalanını
        // kendi biçimine sokardı.
        Assert.Equal("<b>metin</b>", ZenginMetin.Temizle("<b>metin"));
        Assert.Equal("<p><b>iç</b></p>", ZenginMetin.Temizle("<p><b>iç"));
    }

    [Fact]
    public void Eslesmeyen_kapanis_atilir()
        => Assert.Equal("metin", ZenginMetin.Temizle("metin</p>"));

    [Fact]
    public void Ic_ice_gecmis_etiketler_sirayla_kapanir()
    {
        // </b> gelince yalnız b değil, onun içinde açık kalan i de kapanmalı;
        // yoksa çıktı geçersiz im olur.
        Assert.Equal("<b><i>x</i></b>", ZenginMetin.Temizle("<b><i>x</b>"));
    }

    [Fact]
    public void Kapanmayan_kucuktur_isareti_metin_sayilir()
    {
        // Atılsaydı içerik sessizce kaybolurdu.
        Assert.Equal("2 &lt; 3", ZenginMetin.Temizle("2 < 3"));
    }

    [Fact]
    public void Yorum_ve_bildirim_atilir()
    {
        Assert.Equal("x", ZenginMetin.Temizle("<!-- gizli -->x"));
        Assert.Equal("x", ZenginMetin.Temizle("<!DOCTYPE html>x"));
    }

    [Fact]
    public void Br_kendi_kendini_kapatir()
    {
        var temiz = ZenginMetin.Temizle("bir<br>iki");

        Assert.Contains("<br />", temiz);
        Assert.DoesNotContain("</br>", temiz);
    }

    // ───────────────────────────────────────────────────────── kaçışlar

    [Fact]
    public void Metindeki_ozel_harfler_kacirilir()
        => Assert.Equal("a &amp; b", ZenginMetin.Temizle("a & b"));

    [Fact]
    public void Var_olan_kacislar_iki_kez_kacirilmaz()
    {
        // "&nbsp;" zaten kaçırılmış bir metindir; yeniden kaçırılırsa kâğıda
        // "&amp;nbsp;" diye basardı.
        Assert.Equal("a&nbsp;b", ZenginMetin.Temizle("a&nbsp;b"));
        Assert.Equal("&#39;", ZenginMetin.Temizle("&#39;"));
    }

    // ───────────────────────────────────────────────────────── düz metin

    [Fact]
    public void DuzMetin_etiketleri_atar()
        => Assert.Equal("kalın metin", ZenginMetin.DuzMetin("<p><b>kalın</b> metin</p>"));

    [Fact]
    public void DuzMetin_bosluklari_teke_indirir()
        => Assert.Equal("a b", ZenginMetin.DuzMetin("<p>a</p>\n\n   <p>b</p>"));

    [Fact]
    public void DuzMetin_kacislari_cozer()
        => Assert.Equal("a & b", ZenginMetin.DuzMetin("a &amp; b"));

    [Fact]
    public void DuzMetin_bos_degerde_bos_doner()
        => Assert.Equal("", ZenginMetin.DuzMetin(null));
}

/// <summary>Onay kutusunun sınamaları.</summary>
/// <remarks>
/// Bir onay kutusunda "yaklaşık doğru" diye bir şey yok: yanlış yorumlanan bir
/// değer, hastanın vermediği bir onayı verilmiş göstermek demektir.
/// </remarks>
public class OnayKutusuSinamalari
{
    [Theory]
    [InlineData("1")]
    [InlineData("true")]
    [InlineData("TRUE")]
    [InlineData("True")]
    [InlineData("t")]
    [InlineData("E")]
    [InlineData("e")]
    [InlineData("evet")]
    [InlineData("EVET")]
    [InlineData("yes")]
    [InlineData("Y")]
    [InlineData("X")]
    [InlineData("x")]
    [InlineData("var")]
    [InlineData("  1  ")]
    public void Isaretli_sayilan_degerler(string deger)
        => Assert.True(OnayKutusu.Isaretli(deger));

    [Theory]
    [InlineData("0")]
    [InlineData("false")]
    [InlineData("H")]
    [InlineData("hayır")]
    [InlineData("no")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Isaretsiz_sayilan_degerler(string? deger)
        => Assert.False(OnayKutusu.Isaretli(deger));

    [Fact]
    public void Sifirdan_farkli_sayi_isaretlidir()
    {
        // Sayısal sütunlarda 1 dışında değerler de gelebiliyor.
        Assert.True(OnayKutusu.Isaretli("2"));
        Assert.True(OnayKutusu.Isaretli("-1"));
        Assert.False(OnayKutusu.Isaretli("0"));
        Assert.False(OnayKutusu.Isaretli("0.0"));
    }

    [Fact]
    public void Turkce_yerelde_de_dogru_yorumlanir()
    {
        // ⚠ Türkçede "I".ToLower() "ı" verir; kültüre bağlı bir karşılaştırma
        // "TRUE" gibi değerleri kaçırırdı. Karşılaştırma ordinal olmalı.
        var eski = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

            Assert.True(OnayKutusu.Isaretli("TRUE"));
            Assert.True(OnayKutusu.Isaretli("Yes"));
            Assert.True(OnayKutusu.Isaretli("EVET"));
        }
        finally { CultureInfo.CurrentCulture = eski; }
    }

    [Fact]
    public void Cerceve_her_zaman_cizilir()
    {
        // Boş bir kare "işaretlenmemiş" bilgisidir ve kâğıtta görünmeli.
        Assert.Contains("<rect", OnayKutusu.Svg(false));
        Assert.Contains("<rect", OnayKutusu.Svg(true));
    }

    [Fact]
    public void Isaretsiz_kutuda_im_yok()
    {
        var svg = OnayKutusu.Svg(false);

        Assert.DoesNotContain("<path", svg);
        // Yalnız çerçevenin kendisi: ikinci bir dikdörtgen (dolu im) olmamalı.
        Assert.Equal(1, svg.Split("<rect").Length - 1);
    }

    [Fact]
    public void Isaretli_kutuda_im_cizilir()
    {
        Assert.Contains("<path", OnayKutusu.Svg(true, OnayBicimi.Onay));
        Assert.Contains("<path", OnayKutusu.Svg(true, OnayBicimi.Carpi));
        Assert.Equal(2, OnayKutusu.Svg(true, OnayBicimi.Dolu).Split("<rect").Length - 1);
    }

    [Fact]
    public void Renk_ime_ve_cerceveye_uygulanir()
    {
        var svg = OnayKutusu.Svg(true, OnayBicimi.Onay, "#c00000");

        Assert.Contains("stroke=\"#c00000\"", svg);
        Assert.DoesNotContain("#000", svg);
    }

    [Fact]
    public void Renk_verilmezse_siyah()
        => Assert.Contains("stroke=\"#000\"", OnayKutusu.Svg(false));

    [Fact]
    public void Kare_viewBox_ile_cizilir()
    {
        // Dikdörtgen bir alana konsa bile kutu kare kalmalı: ezilmiş bir onay
        // kutusu kâğıtta baskı hatası gibi görünür.
        Assert.Contains("viewBox=\"0 0 100 100\"", OnayKutusu.Svg(false));
    }

    [Fact]
    public void Svg_kulturden_bagimsiz_bicimlenir()
    {
        // ⚠ Ondalık ayracı yerelden gelirse koordinat virgüllü çıkar ve
        // öznitelik değeri ikiye bölünür — SVG bozulur.
        var eski = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            var svg = OnayKutusu.Svg(true, OnayBicimi.Onay);

            Assert.Contains("x=\"4\"", svg);
            Assert.DoesNotContain(",\"", svg);
        }
        finally { CultureInfo.CurrentCulture = eski; }
    }
}
