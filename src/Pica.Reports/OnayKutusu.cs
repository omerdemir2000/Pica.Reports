using System.Globalization;
using System.Text;
using Pica.Reports.Duzen;

namespace Pica.Reports;

/// <summary>
/// Onay kutusunun çizimi ve değerinin yorumlanması.
/// </summary>
/// <remarks>
/// <para>
/// Çizim <b>SVG</b> olarak üretiliyor, barkodda olduğu gibi: tuval de çizici de
/// aynı dizeyi basar. İki ayrı çizim olsaydı tasarımcıdaki kutu ile kâğıttaki
/// er geç ayrışırdı — ve bir onay kutusunda ayrışma, formun yanlış okunması
/// demektir.
/// </para>
/// <para>
/// <b>Kare her zaman çizilir</b>, işaret yalnızca işaretliyken. Boş bir kare
/// "işaretlenmemiş" bilgisidir ve kâğıtta görünmesi gerekir; matbu formlarda
/// (Form 14, KVKK onayı) kutular zaten elle işaretlenmek üzere boş basılır.
/// </para>
/// </remarks>
public static class OnayKutusu
{
    /// <summary>Çizim kutusu — kenar payı bırakılmış birim kare.</summary>
    private const double Kenar = 100;

    /// <summary>Çerçeve kalınlığı; kutu küçüldükçe oranı korunur.</summary>
    private const double CizgiKalinligi = 8;

    /// <summary>
    /// Onay kutusunun SVG'si.
    /// </summary>
    /// <param name="isaretli">İm çizilsin mi?</param>
    /// <param name="bicim">İşaretin biçimi.</param>
    /// <param name="renk">İm ve çerçeve rengi; boşsa siyah.</param>
    /// <remarks>
    /// <c>viewBox</c> kare, <c>preserveAspectRatio</c> varsayılan: kutu
    /// dikdörtgen bir alana konsa bile <b>kare kalır</b> ve ortalanır. Ezilmiş
    /// bir onay kutusu kâğıtta baskı hatası gibi görünür.
    /// </remarks>
    public static string Svg(bool isaretli, OnayBicimi bicim = OnayBicimi.Onay, string? renk = null)
    {
        var murekkep = string.IsNullOrEmpty(renk) ? "#000" : renk;
        var p = CizgiKalinligi / 2;
        var i = Kenar - CizgiKalinligi;

        var svg = new StringBuilder();

        // ⚠ Her çağrı TEK bir interpolasyon olmalı: birleştirme (+) sonucu
        // FormattableString değil string üretir ve kültürsüz biçimleme devre
        // dışı kalır — Türkçe yerelde koordinatlar virgüllü çıkar, SVG bozulur.
        svg.Append(Kultursuz(
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {Kenar} {Kenar}\" width=\"100%\" height=\"100%\" role=\"img\">"));

        // Çerçeve: dolu biçimde de çizilir, kutunun sınırı belli olsun diye.
        svg.Append(Kultursuz(
            $"<rect x=\"{p}\" y=\"{p}\" width=\"{i}\" height=\"{i}\" fill=\"none\" stroke=\"{murekkep}\" stroke-width=\"{CizgiKalinligi}\" />"));

        if (isaretli)
        {
            svg.Append(bicim switch
            {
                OnayBicimi.Carpi => Kultursuz(
                    $"<path d=\"M26,26 L74,74 M74,26 L26,74\" fill=\"none\" stroke=\"{murekkep}\" stroke-width=\"12\" stroke-linecap=\"round\" />"),

                OnayBicimi.Dolu => Kultursuz(
                    $"<rect x=\"26\" y=\"26\" width=\"48\" height=\"48\" fill=\"{murekkep}\" />"),

                _ => Kultursuz(
                    $"<path d=\"M24,52 L44,72 L78,28\" fill=\"none\" stroke=\"{murekkep}\" stroke-width=\"12\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />"),
            });
        }

        svg.Append("</svg>");
        return svg.ToString();
    }

    /// <summary>
    /// Veriden gelen bir değeri "işaretli mi" diye yorumlar.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Onay kutusu veriye bağlanabiliyor ve gelen değerin biçimi
    /// <b>veritabanına göre değişiyor</b>: <c>bit</c> sütun <c>True</c>,
    /// <c>char(1)</c> sütun <c>E</c> ya da <c>X</c>, sayısal sütun <c>1</c>
    /// döndürüyor. Delphi tarafında bu ayrımlar ekran kodunda tek tek
    /// yazılıydı; burada tek yerde toplandı.
    /// </para>
    /// <para>
    /// <b>Boş değer işaretsizdir</b> — veri gelmediği için kutuyu işaretlemek,
    /// formda olmayan bir onayı varmış gibi göstermek olurdu.
    /// </para>
    /// <para>
    /// Karşılaştırma <b>kültürden bağımsızdır</b>: Türkçe yerelde
    /// <c>"TRUE".ToLower()</c> "true" değil "trve"ye benzer bir sonuç
    /// vermez ama <c>I/ı</c> ayrımı yüzünden <c>"EVET"</c> gibi değerlerde
    /// tuzak vardır; <see cref="StringComparison.OrdinalIgnoreCase"/> onu keser.
    /// </para>
    /// </remarks>
    public static bool Isaretli(string? deger)
    {
        var d = deger?.Trim();
        if (string.IsNullOrEmpty(d)) return false;

        foreach (var dogru in Dogrular)
            if (string.Equals(d, dogru, StringComparison.OrdinalIgnoreCase)) return true;

        // Sayısal sütunlar: sıfırdan farklı her değer işaretli sayılır.
        return double.TryParse(d, NumberStyles.Any, CultureInfo.InvariantCulture, out var sayi)
            && sayi != 0;
    }

    /// <remarks>
    /// Türkçe ve İngilizce karşılıklar birlikte: veri hangi dilde yazılmışsa
    /// kutunun doğru basması gerekir ve şablon o kararı vermiyor.
    /// </remarks>
    private static readonly string[] Dogrular =
        ["1", "true", "t", "yes", "y", "e", "evet", "var", "x", "✓", "on", "checked"];

    /// <remarks>
    /// Ondalık ayracı yerelden gelirse SVG bozulur: virgüllü bir koordinat
    /// öznitelik değerini ikiye böler. Barkodda da aynı sebeple kültürsüz
    /// biçimleniyor.
    /// </remarks>
    private static string Kultursuz(FormattableString metin)
        => metin.ToString(CultureInfo.InvariantCulture);
}
