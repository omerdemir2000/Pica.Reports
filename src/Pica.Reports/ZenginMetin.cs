using System.Text;

namespace Pica.Reports;

/// <summary>
/// Zengin metin kutusunun içeriğini basılabilir hâle getiren süzgeç.
/// </summary>
/// <remarks>
/// <para>
/// <b>Kütüphane RTF okumaz.</b> Delphi tarafı zengin metni RTF olarak saklıyor
/// (<c>TfrxRichView.PropData</c>, veri sütunları) ama RTF'i çözmek barındıran
/// uygulamanın işi: orada zaten bir çevirici var ve bu kütüphane bağımsız
/// kalmalı — kendi RTF ayrıştırıcısını taşımak, bir kod sayfası tablosunu ve
/// onun bakımını da taşımak demek. Buraya gelen değer <b>im</b>dir.
/// </para>
/// <para>
/// <b>Neden süzgeç var?</b> İçerik veritabanından geliyor ve tarayıcıya im
/// olarak basılıyor. Süzülmezse bir hasta raporunun içindeki
/// <c>&lt;script&gt;</c> ya da <c>&lt;img onerror&gt;</c> raporu açan herkeste
/// çalışırdı. Süzgeç <b>izin listesiyle</b> çalışır: listede olmayan her
/// etiket düşer, <b>öznitelikler bütünüyle atılır</b> (yalnız <c>class</c>
/// kalır) — <c>style</c>, <c>href</c>, <c>src</c> ve <c>on…</c> hiç geçmez.
/// </para>
/// <para>
/// Etiketin kendisi düşse de <b>metni kalır</b>: bilinmeyen bir sarmalayıcı
/// yüzünden kâğıtta içerik kaybolmaz. <c>script</c> ve <c>style</c> bunun
/// istisnasıdır — onların gövdesi metin değil koddur, kâğıda basılırsa
/// okunmaz bir gürültü çıkar.
/// </para>
/// </remarks>
public static class ZenginMetin
{
    /// <summary>
    /// Basılmasına izin verilen etiketler.
    /// </summary>
    /// <remarks>
    /// Küme, bir RTF çeviricisinin üretebileceğiyle sınırlı: vurgu, paragraf,
    /// liste ve tablo. Bağlantı (<c>a</c>) ve resim (<c>img</c>) bilerek YOK —
    /// kâğıda basılan bir raporda tıklanacak bağlantı olmaz, resim ise kendi
    /// kutu türüdür.
    /// </remarks>
    private static readonly HashSet<string> Izinli = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "br", "div", "span",
        "b", "strong", "i", "em", "u", "s", "sub", "sup",
        "ul", "ol", "li",
        "table", "thead", "tbody", "tfoot", "tr", "td", "th",
        "h1", "h2", "h3", "h4", "h5", "h6",
    };

    /// <summary>Gövdesi de atılan etiketler — içerikleri metin değil.</summary>
    private static readonly HashSet<string> GovdesiAtilan = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "iframe", "object", "embed", "svg", "math",
    };

    /// <summary>İçeriği tek satırda özetleyen düz metin.</summary>
    /// <remarks>
    /// Tuvalde kutu küçük ve biçimli metni orada göstermek yanıltıcı olurdu:
    /// tuval yerleşim yüzeyidir, baskı önizlemesi değil. Etiketler atılır,
    /// boşluklar tek boşluğa iner.
    /// </remarks>
    public static string DuzMetin(string? im)
    {
        if (string.IsNullOrWhiteSpace(im)) return "";

        var yazi = new StringBuilder(im.Length);
        var etiketIcinde = false;

        foreach (var harf in im)
        {
            if (harf == '<') { etiketIcinde = true; continue; }
            if (harf == '>') { etiketIcinde = false; yazi.Append(' '); continue; }
            if (!etiketIcinde) yazi.Append(harf);
        }

        return string.Join(' ', Kacislari(yazi.ToString())
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// İm'i izin listesinden geçirir; sonuç doğrudan basılabilir.
    /// </summary>
    /// <remarks>
    /// Değer im gibi görünmüyorsa (hiç <c>&lt;</c> yok) düz metin sayılır ve
    /// kaçırılarak döner — böylece veri alanından gelen sıradan bir metin de
    /// bu kutuda doğru basılır, kullanıcı türü değiştirmek zorunda kalmaz.
    /// </remarks>
    public static string Temizle(string? im)
    {
        if (string.IsNullOrEmpty(im)) return "";
        if (!im.Contains('<')) return Kacir(im);

        var cikti = new StringBuilder(im.Length);

        // Kapatılmamış etiketler kâğıdın geri kalanını yutabilir: açılan her
        // etiket burada birikir ve sonda ters sırayla kapatılır.
        var yigin = new List<string>();

        var i = 0;
        while (i < im.Length)
        {
            var bas = im.IndexOf('<', i);
            if (bas < 0)
            {
                cikti.Append(Kacir(im[i..]));
                break;
            }

            if (bas > i) cikti.Append(Kacir(im[i..bas]));

            var son = im.IndexOf('>', bas);
            if (son < 0)
            {
                // Kapanmayan bir '<': geri kalanı metin say. Atılsaydı içerik
                // sessizce kaybolurdu.
                cikti.Append(Kacir(im[bas..]));
                break;
            }

            var ic = im[(bas + 1)..son];
            i = son + 1;

            // Yorum ve bildirimler (<!-- -->, <!DOCTYPE>) atılır.
            if (ic.StartsWith('!')) continue;

            var kapanis = ic.StartsWith('/');
            var ad = EtiketAdi(kapanis ? ic[1..] : ic);
            if (ad.Length == 0) continue;

            if (GovdesiAtilan.Contains(ad))
            {
                if (!kapanis) i = GovdeyiAtla(im, ad, i);
                continue;
            }

            if (!Izinli.Contains(ad)) continue;   // etiket düşer, metni kalır

            if (kapanis)
            {
                // Yalnızca gerçekten açık olan etiket kapatılır; eşleşmeyen bir
                // kapanış (</p> hiç açılmadan) çıktıya girmemeli.
                var yer = yigin.LastIndexOf(ad);
                if (yer < 0) continue;

                for (var k = yigin.Count - 1; k >= yer; k--)
                {
                    cikti.Append("</").Append(yigin[k]).Append('>');
                    yigin.RemoveAt(k);
                }
                continue;
            }

            var sinif = SinifOku(ic);
            cikti.Append('<').Append(ad);
            if (sinif is { Length: > 0 }) cikti.Append(" class=\"").Append(Kacir(sinif)).Append('"');

            // Kendi kendini kapatanlar yığına girmez.
            if (ad is "br" || ic.TrimEnd().EndsWith('/'))
            {
                cikti.Append(ad is "br" ? " />" : " />");
                continue;
            }

            cikti.Append('>');
            yigin.Add(ad);
        }

        for (var k = yigin.Count - 1; k >= 0; k--)
            cikti.Append("</").Append(yigin[k]).Append('>');

        return cikti.ToString();
    }

    // ─────────────────────────────────────────────────────────── yardımcılar

    private static string EtiketAdi(string ic)
    {
        var son = 0;
        while (son < ic.Length && (char.IsAsciiLetterOrDigit(ic[son]) || ic[son] is '-')) son++;
        return ic[..son].ToLowerInvariant();
    }

    /// <remarks>
    /// Tek tanınan öznitelik. Barındıran uygulamanın biçemi ona bağlanabilsin
    /// diye duruyor (bir RTF çeviricisi tabloya <c>class</c> veriyor); değeri
    /// yine de kaçırılıyor.
    /// </remarks>
    private static string? SinifOku(string ic)
    {
        var yer = ic.IndexOf("class", StringComparison.OrdinalIgnoreCase);
        if (yer < 0) return null;

        var esit = ic.IndexOf('=', yer);
        if (esit < 0) return null;

        var i = esit + 1;
        while (i < ic.Length && char.IsWhiteSpace(ic[i])) i++;
        if (i >= ic.Length) return null;

        var tirnak = ic[i];
        if (tirnak is '"' or '\'')
        {
            var son = ic.IndexOf(tirnak, i + 1);
            return son < 0 ? null : ic[(i + 1)..son];
        }

        var bitis = i;
        while (bitis < ic.Length && !char.IsWhiteSpace(ic[bitis])) bitis++;
        return ic[i..bitis];
    }

    /// <summary>Gövdesi atılan etiketin kapanışına kadar atlar.</summary>
    private static int GovdeyiAtla(string im, string ad, int i)
    {
        var kapanis = im.IndexOf("</" + ad, i, StringComparison.OrdinalIgnoreCase);
        if (kapanis < 0) return im.Length;

        var son = im.IndexOf('>', kapanis);
        return son < 0 ? im.Length : son + 1;
    }

    /// <remarks>
    /// Metin parçaları kaçırılır ama <b>varlık kaçışları korunur</b>:
    /// <c>&amp;nbsp;</c> zaten kaçırılmış bir metindir, yeniden kaçırılırsa
    /// kâğıda "&amp;amp;nbsp;" diye basardı.
    /// </remarks>
    private static string Kacir(string metin)
    {
        var yazi = new StringBuilder(metin.Length);

        for (var i = 0; i < metin.Length; i++)
        {
            var harf = metin[i];
            switch (harf)
            {
                case '<': yazi.Append("&lt;"); break;
                case '>': yazi.Append("&gt;"); break;
                case '"': yazi.Append("&quot;"); break;
                case '&':
                    if (VarlikMi(metin, i)) yazi.Append('&');
                    else yazi.Append("&amp;");
                    break;
                default: yazi.Append(harf); break;
            }
        }

        return yazi.ToString();
    }

    /// <summary><c>&amp;</c> bir varlık kaçışı başlatıyor mu?</summary>
    private static bool VarlikMi(string metin, int i)
    {
        var son = metin.IndexOf(';', i + 1);
        if (son < 0 || son - i > 10) return false;

        for (var k = i + 1; k < son; k++)
            if (!char.IsAsciiLetterOrDigit(metin[k]) && metin[k] != '#') return false;

        return son > i + 1;
    }

    /// <summary>Düz metinde varlık kaçışlarını okunur hâle getirir.</summary>
    private static string Kacislari(string metin)
        => metin.Replace("&nbsp;", " ")
                .Replace("&lt;", "<").Replace("&gt;", ">")
                .Replace("&quot;", "\"").Replace("&#39;", "'")
                .Replace("&amp;", "&");
}
