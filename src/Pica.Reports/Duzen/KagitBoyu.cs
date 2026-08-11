namespace Pica.Reports.Duzen;

/// <summary>
/// Adlandırılmış kâğıt boyu — ölçüler <b>dikey</b> hâlde, punto.
/// </summary>
/// <remarks>
/// Yatay kâğıt ayrı bir boy değil, aynı boyun döndürülmüşüdür: en ile boy yer
/// değiştirir (bkz. <see cref="DuzenSayfasi.Yatay"/>). Listede her boy bir kez
/// geçsin diye ölçüler hep dikey yazılıyor.
/// </remarks>
/// <param name="Ad">Ekranda görünen ad.</param>
/// <param name="GenislikPt">Dikey hâldeki en.</param>
/// <param name="YukseklikPt">Dikey hâldeki boy.</param>
public sealed record KagitBoyu(string Ad, double GenislikPt, double YukseklikPt)
{
    /// <summary>Kâğıdın mm cinsinden ölçüsü — "210 × 297 mm".</summary>
    public string Olcu => $"{Math.Round(Pica.Reports.Olcu.Mm(GenislikPt))} × " +
                          $"{Math.Round(Pica.Reports.Olcu.Mm(YukseklikPt))} mm";

    public static readonly KagitBoyu A3 = new("A3", 841.89, 1190.55);
    public static readonly KagitBoyu A4 = new("A4", 595.28, 841.89);
    public static readonly KagitBoyu A5 = new("A5", 419.53, 595.28);
    public static readonly KagitBoyu A6 = new("A6", 297.64, 419.53);
    public static readonly KagitBoyu B5 = new("B5", 498.90, 708.66);
    public static readonly KagitBoyu Letter = new("Letter", 612, 792);
    public static readonly KagitBoyu Legal = new("Legal", 612, 1008);

    /// <summary>100 × 50 mm etiket — barkod etiketlerinin yaygın boyu.</summary>
    public static readonly KagitBoyu Etiket = new("Etiket 100×50", 283.46, 141.73);

    /// <summary>Sunulan boylar, büyükten küçüğe.</summary>
    public static IReadOnlyList<KagitBoyu> Hepsi { get; } =
        [A3, B5, A4, Letter, Legal, A5, A6, Etiket];

    /// <summary>
    /// Sayfanın ölçüsüne uyan boy; hiçbiri tutmuyorsa <c>null</c> (özel boy).
    /// </summary>
    /// <remarks>
    /// Yön hesaba katılıyor: yatay bir A4'ün eni 841,89'dur ve yine A4'tür.
    /// Yarım puntoya kadar sapma hoş görülüyor — dönüştürülmüş düzenlerin
    /// ölçüleri pikselden çevrildiği için tam tutmuyor.
    /// </remarks>
    public static KagitBoyu? Bul(double genislikPt, double yukseklikPt)
        => Hepsi.FirstOrDefault(k =>
               (Yakin(k.GenislikPt, genislikPt) && Yakin(k.YukseklikPt, yukseklikPt)) ||
               (Yakin(k.YukseklikPt, genislikPt) && Yakin(k.GenislikPt, yukseklikPt)));

    private static bool Yakin(double a, double b) => Math.Abs(a - b) <= 0.5;

    /// <summary>Boyu sayfaya uygular; yön korunur.</summary>
    public void Uygula(DuzenSayfasi sayfa)
    {
        sayfa.GenislikPt = sayfa.Yatay ? YukseklikPt : GenislikPt;
        sayfa.YukseklikPt = sayfa.Yatay ? GenislikPt : YukseklikPt;
    }

    /// <summary>
    /// Sayfanın yönünü değiştirir: en ile boy yer değiştirir.
    /// </summary>
    /// <remarks>
    /// <b>Kutular yerinde kalır.</b> Yön değişince yazı alanının eni değişiyor
    /// ve sağdaki sütunlar kâğıdın dışına taşabiliyor; taşanları kendiliğinden
    /// taşımak, tasarımcının koyduğu hizayı bozmak olurdu. Taşma tuvalde
    /// görünür.
    /// </remarks>
    public static void YonDegistir(DuzenSayfasi sayfa, bool yatay)
    {
        if (sayfa.Yatay == yatay) return;

        (sayfa.GenislikPt, sayfa.YukseklikPt) = (sayfa.YukseklikPt, sayfa.GenislikPt);
        sayfa.Yatay = yatay;
    }
}
