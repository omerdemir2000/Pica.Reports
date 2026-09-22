namespace Pica.Reports.Duzen;

/// <summary>
/// <see cref="DuzenNesnesi.Sigdir"/> açık bir kutunun metni sığsın diye
/// düşürülmüş puntosu.
/// </summary>
/// <remarks>
/// <para>
/// FastReport'ta <c>Sigdir</c>, "metin kutuya sığmıyorsa <b>puntoyu küçült</b>"
/// demektir — alt satıra kırmak değil. Alan bu kütüphanede modelde duruyordu
/// ama hiçbir yerde okunmuyordu: 24 pt genişliğindeki para kutusuna sığmayan
/// <c>98.765,43</c>, kâğıda <c>98.765,4</c> + <c>3</c> diye iki satır çıkıyordu.
/// Yanındaki 30 pt'lik kardeşi tek satır basarken bir hücrenin iki satıra
/// kayması, tablonun bütün satır hizasını bozar.
/// </para>
/// <para>
/// <b>Sığdırma, kelime kaydırmayı yener.</b> İkisi birlikte açıksa kutu kırmaz,
/// küçültür: sığdırmanın varlık sebebi kırılmayı önlemektir, ikisi aynı anda
/// uygulanırsa alan hiçbir şey yapmamış olur (şablonların bir kısmında ikisi
/// birlikte işaretli geliyor).
/// </para>
/// <para>
/// <b>Ölçü kestirmedir, gerçek yazı tipi ölçümü değil.</b> Kütüphane tarayıcıda
/// da PDF motorunda da çalışmak zorunda ve ikisinin ölçüm arayüzü yok; buradaki
/// tablo Arial'in genişlik ölçülerinden (1000 birimlik em) türetildi ve dar/kalın
/// kesimler katsayıyla düzeltiliyor. Kestirme birkaç yüzde şaşabilir — o yüzden
/// kırpma da açık kalır (CSS <c>overflow:hidden</c>): sığmayan metin kutudan
/// taşıp komşusunun üstüne yazmaz. Önemli olan tasarımcı, önizleme ve çizicinin
/// <b>aynı</b> sayıyı bulması; ayrı ayrı kestirselerdi ekranda sığan bir kutu
/// kâğıtta sığmazdı.
/// </para>
/// </remarks>
public static class Sigdirma
{
    /// <summary>Sığdırmanın inebileceği en küçük punto.</summary>
    /// <remarks>
    /// Bunun altında yazı okunmaz; kâğıda mürekkep lekesi basmaktansa metnin
    /// kırpılması yeğdir — kırpılan kutu gözle görülür, 1 punto basılan kutu
    /// görülmez.
    /// </remarks>
    public const double EnAzPuntoPt = 3;

    /// <summary>
    /// Kutunun iki yanındaki iç boşluk, punto.
    /// </summary>
    /// <remarks>
    /// <see cref="Bicem"/> kutuya <c>padding:0 1.5pt</c> veriyor ve metin o
    /// boşluğun içine yazılmıyor; sığdırma bunu saymazsa kutu hep bir tık dar
    /// basar.
    /// </remarks>
    public const double YanDolguPt = 1.5;

    /// <summary>Metnin sığması için gereken punto; sığdırma kapalıysa kutununki.</summary>
    /// <param name="nesne">Kutu — genişliği, puntosu ve yazı tipi buradan.</param>
    /// <param name="metin">
    /// Kâğıda basılacak metin. Tasarımcıda örnek değer, önizlemede çözülmüş
    /// değer verilir; <c>null</c> ya da boşsa küçültülecek bir şey yoktur.
    /// </param>
    public static double Punto(DuzenNesnesi nesne, string? metin)
    {
        if (!nesne.Sigdir || string.IsNullOrEmpty(metin)) return nesne.PuntoPt;
        if (nesne.PuntoPt <= EnAzPuntoPt) return nesne.PuntoPt;

        var alan = nesne.GenislikPt - 2 * YanDolguPt;
        if (alan <= 0) return nesne.PuntoPt;

        // Çok satırlı metinde en uzun satır belirler: kırılma zaten
        // istenmiyor, satırlar metnin kendi satır sonlarından geliyor.
        var enUzun = 0.0;

        foreach (var satir in metin.Split('\n'))
            enUzun = Math.Max(enUzun, Em(satir.AsSpan().TrimEnd('\r'), nesne));

        if (enUzun <= 0) return nesne.PuntoPt;

        // Metin zaten sığıyorsa punto BÜYÜTÜLMEZ: sığdırma daraltma ilkesidir,
        // kutuyu doldurma ilkesi değil. Büyütseydi tasarımcının verdiği punto
        // anlamını yitirir, komşu kutular farklı boyda basardı.
        var gereken = Math.Min(nesne.PuntoPt, alan / enUzun);

        return Math.Max(EnAzPuntoPt, Math.Round(gereken, 2));
    }

    /// <summary>Metnin 1 puntoluk yazıdaki genişliği (em cinsinden).</summary>
    public static double Em(ReadOnlySpan<char> metin, DuzenNesnesi nesne)
    {
        var toplam = 0.0;

        foreach (var c in metin) toplam += Em(c);

        // Arial Narrow gövdesi Arial'in yaklaşık %82'si; kalın kesim birkaç
        // yüzde geniş. Yazı tipi adına bakmak kaba ama taşınan düzenlerde
        // geçen tek dar kesim Arial Narrow.
        if (nesne.YaziTipi.Contains("Narrow", StringComparison.OrdinalIgnoreCase)) toplam *= 0.82;
        if (nesne.Kalin) toplam *= 1.05;

        return toplam;
    }

    /// <summary>Tek karakterin em genişliği — Arial ölçülerinden.</summary>
    /// <remarks>
    /// Harf harf tablo yazmak yerine sınıflara bölündü: rakamlar sabit
    /// genişlikte (para sütunları bu yüzden hizalanır), noktalama dar, büyük
    /// harfler geniş. Tek tek doğru olmayabilir; <b>toplamı</b> birkaç yüzde
    /// içinde doğrudur ve sığdırmanın ihtiyacı olan da odur.
    /// </remarks>
    public static double Em(char c) => c switch
    {
        ' ' or '\t' => 0.278,

        // Rakamlar ve para ayraçları: Arial'de hepsi 556/1000, ayraçlar 278.
        >= '0' and <= '9' => 0.556,
        '.' or ',' or ':' or ';' or '\'' or '|' or '!' => 0.278,
        '-' or '/' or '(' or ')' or '[' or ']' or '`' => 0.333,
        '%' => 0.889,

        // Dar küçük harfler.
        'i' or 'j' or 'l' or 'ı' or 'f' or 't' or 'r' => 0.26,
        'm' => 0.833,
        'w' => 0.722,
        >= 'a' and <= 'z' => 0.54,

        // Dar ve geniş büyük harfler.
        'I' or 'İ' => 0.278,
        'M' => 0.833,
        'W' => 0.944,
        >= 'A' and <= 'Z' => 0.69,

        // Türkçe harfler latin kardeşleriyle aynı gövdededir; şapka ve
        // çengel genişlik eklemez.
        'ş' or 'ç' or 'ğ' or 'ü' or 'ö' => 0.54,
        'Ş' or 'Ç' or 'Ğ' or 'Ü' or 'Ö' => 0.69,

        _ => 0.54,
    };
}
