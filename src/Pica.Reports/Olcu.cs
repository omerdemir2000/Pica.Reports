using System.Globalization;

namespace Pica.Reports;

/// <summary>
/// Punto ile ekran pikseli arasındaki çevrim.
/// </summary>
/// <remarks>
/// <para>
/// Düzenin bütün ölçüleri puntodur (1/72 inç) — PDF'in kendi birimi. CSS ise
/// pikselle çalışır ve tarayıcı 1 inçi 96 piksel sayar; oran bu yüzden sabittir
/// ve <b>96/72</b>'dir. Yakınlaştırma bu çevrime karışmaz: tuval CSS
/// <c>transform: scale()</c> ile büyütülür, böylece yakınlaştırma değiştiğinde
/// tek bir biçem değişir, yüzlerce kutunun konumu yeniden hesaplanmaz.
/// </para>
/// <para>
/// <b>Sayılar her zaman değişmez kültürle biçimlenir.</b> Türkçe kültürde
/// ondalık ayracı virgüldür ve <c>left:3,78px</c> CSS'te geçersizdir: kutu
/// sessizce sol üst köşeye kaçar. Biçimlemenin tek yerden geçmesinin sebebi bu.
/// </para>
/// </remarks>
public static class Olcu
{
    /// <summary>Bir puntonun CSS piksel karşılığı.</summary>
    public const double PikselPunto = 96.0 / 72.0;

    /// <summary>Bir milimetrenin punto karşılığı.</summary>
    /// <remarks>
    /// Düzen milimetre saklamaz (bkz. sınıf açıklaması) ama <b>insan</b>
    /// milimetre düşünür: kâğıt boyu ve kenar boşluğu ekranında ölçüler mm
    /// girilir, punto olarak yazılır.
    /// </remarks>
    public const double MmPunto = 72.0 / 25.4;

    /// <summary>Milimetreyi puntoya çevirir.</summary>
    public static double Puntoya_Mm(double mm) => mm * MmPunto;

    /// <summary>Puntoyu milimetreye çevirir.</summary>
    public static double Mm(double punto) => punto / MmPunto;

    /// <summary>Puntoyu CSS piksel sayısına çevirir.</summary>
    public static double Piksel(double punto) => punto * PikselPunto;

    /// <summary>Puntoyu <c>"12.34px"</c> biçiminde CSS değerine çevirir.</summary>
    public static string Px(double punto) => Sayi(Piksel(punto)) + "px";

    /// <summary>CSS pikselini puntoya çevirir — tarayıcıdan gelen ölçüler için.</summary>
    /// <remarks>
    /// Fare olaylarının <c>offsetX/Y</c>'si öğenin <b>kendi</b> koordinat
    /// düzlemindedir, yani yakınlaştırma (<c>transform: scale</c>) hesaba
    /// katılmış gelir; burada yalnızca birim çevrilir.
    /// </remarks>
    public static double Puntoya(double piksel) => piksel / PikselPunto;

    /// <summary>Sayıyı CSS'in kabul edeceği biçimde yazar.</summary>
    public static string Sayi(double deger)
        => deger.ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>Ölçüyü kullanıcıya gösterilecek biçimde yazar — <c>98,4 pt</c>.</summary>
    /// <remarks>
    /// Burada kültür değişmez değil, kullanıcınınkidir: bu sayı CSS'e değil
    /// insana gidiyor ve Türkçe okuyan biri "98.4" değil "98,4" bekler.
    /// </remarks>
    public static string Punto(double deger) => deger.ToString("0.#") + " pt";
}
