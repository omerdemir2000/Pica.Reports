using System.Text;

namespace Pica.Reports.Duzen;

/// <summary>
/// Sıfırdan açılan düzenin ilk hâli.
/// </summary>
/// <remarks>
/// Taşınan 137 düzen dönüştürücüden geliyor; bu sınıf yalnızca listedeki "yeni
/// düzen" düğmesi için var. Ürettiği şey <b>ham</b> düzendir: üstüne yazılacak
/// düzeltmenin karşılaştırılacağı taban.
/// </remarks>
public static class YeniDuzen
{
    /// <summary>A4 kâğıdın punto cinsinden ölçüleri (210 × 297 mm).</summary>
    public const double A4GenislikPt = 595.28;
    public const double A4YukseklikPt = 841.89;

    /// <summary>10 mm kenar boşluğu.</summary>
    private const double BoslukPt = 28.35;

    /// <summary>
    /// Tek sayfalı, iki bantlı boş bir düzen.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bomboş bir sayfa açılmıyor: bant olmadan kutu eklenemiyor (kutu bir bandın
    /// içine girer) ve kullanıcı tuvalde tıklayacak yer bulamıyor. En az iki bant
    /// gerekiyor — başlık bir kez, veri her satır için basılır; bu ikisi olmadan
    /// hiçbir cetvel kurulamaz.
    /// </para>
    /// <para>
    /// Başlık bandındaki tek kutu düzenin adını taşır. Boş bırakılabilirdi ama o
    /// zaman ilk PDF önizlemesi bembeyaz çıkar ve kullanıcı düzenin mi boş
    /// olduğunu yoksa basımın mı çalışmadığını ayırt edemezdi.
    /// </para>
    /// </remarks>
    public static CetvelDuzeni Bos(string anahtar, string ad)
    {
        var genislik = A4GenislikPt - 2 * BoslukPt;

        return new CetvelDuzeni
        {
            Anahtar = anahtar,
            Ad = ad,
            // Delphi karşılığı yok: bu düzen dönüştürülmedi, tasarımcıda açıldı.
            Kaynak = "",
            Sayfalar =
            [
                new DuzenSayfasi
                {
                    GenislikPt = A4GenislikPt,
                    YukseklikPt = A4YukseklikPt,
                    SolBoslukPt = BoslukPt,
                    SagBoslukPt = BoslukPt,
                    UstBoslukPt = BoslukPt,
                    AltBoslukPt = BoslukPt,
                    Bantlar =
                    [
                        new DuzenBandi
                        {
                            Ad = "RaporBasligi1",
                            Tur = BantTuru.RaporBasligi,
                            UstPt = 0,
                            YukseklikPt = 32,
                            Nesneler =
                            [
                                new DuzenNesnesi
                                {
                                    Ad = "Baslik",
                                    Tur = NesneTuru.Yazi,
                                    SolPt = 0,
                                    UstPt = 0,
                                    GenislikPt = genislik,
                                    YukseklikPt = 18,
                                    Metin = ad,
                                    PuntoPt = 12,
                                    Kalin = true,
                                    Yatay = YatayHiza.Orta,
                                    Dikey = DikeyHiza.Orta,
                                },
                            ],
                        },
                        new DuzenBandi
                        {
                            Ad = "Veri1",
                            Tur = BantTuru.Veri,
                            UstPt = 48,
                            YukseklikPt = 18,
                        },
                    ],
                },
            ],
        };
    }

    /// <summary>
    /// Addan dosya ve adres anahtarı üretir — "Aylık Mizan" → "aylik-mizan".
    /// </summary>
    /// <remarks>
    /// <para>
    /// Anahtar hem dosya adına hem adrese giriyor: yalnızca ASCII harf, rakam ve
    /// tire kalır. Türkçe harfler karşılıklarına çevrilir; atılsalardı "Şubeler"
    /// ile "ubeler" aynı anahtara düşerdi.
    /// </para>
    /// <para>
    /// Sonuç <b>öneridir</b>, dayatma değil: kullanıcı kutuda değiştirebilir.
    /// Anahtarın benzersizliğini depo denetler, burası bilemez. Karşılığı hiç
    /// kalmayan bir addan boş dize döner; çağıran onu kendi karşılar.
    /// </para>
    /// </remarks>
    public static string Anahtarla(string ad)
    {
        var yazi = new StringBuilder(ad.Length);

        foreach (var harf in ad.Trim())
        {
            // Türkçe harfler tek tek yazılıyor; dizenin tamamına
            // ToLowerInvariant uygulanamaz, çünkü "İ" orada "i" + birleşen
            // noktaya ayrılıyor ve o nokta tireye dönüşerek "İstanbul"u
            // "i-stanbul" yapardı.
            var c = harf switch
            {
                'ç' or 'Ç' => 'c',
                'ğ' or 'Ğ' => 'g',
                'ı' or 'I' or 'İ' => 'i',
                'ö' or 'Ö' => 'o',
                'ş' or 'Ş' => 's',
                'ü' or 'Ü' => 'u',
                _ => char.ToLowerInvariant(harf),
            };

            if (char.IsAsciiLetterOrDigit(c)) yazi.Append(c);
            // Tire tireyi izlemez: "Mizan  (aylık)" tek tireyle bölünür.
            else if (yazi.Length > 0 && yazi[^1] != '-') yazi.Append('-');
        }

        return yazi.ToString().Trim('-');
    }
}
