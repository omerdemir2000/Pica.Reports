using System.Globalization;
using System.Text;
using Pica.Reports.Duzen;

namespace Pica.Reports;

/// <summary>
/// Çubuk barkod üretimi — kodlama ve çizim.
/// </summary>
/// <remarks>
/// <para>
/// Kütüphaneye bir barkod paketi eklenmedi: hepsi ya lisanslı ya da yüzlerce
/// simgelemi taşıyan büyük bağımlılıklar, oysa burada üç simgelem yetiyor.
/// Kod da zaten esas olarak tablodan ibaret.
/// </para>
/// <para>
/// Çıktı <b>SVG</b>: tuval onu doğrudan gösteriyor, çizici de aynı dizeyi
/// QuestPDF'e veriyor. İki ayrı çizim kodu yazılsaydı tasarımcıda görünen
/// barkod ile kâğıda basılan er geç ayrışırdı — ve barkodda ayrışma, okunmayan
/// bir etiket demektir.
/// </para>
/// <para>
/// <b>Ölçü modüldür</b>, punto değil: en dar çubuk 1 modüldür ve SVG'nin
/// <c>viewBox</c>'ı modül cinsindendir. Kutu ne kadar genişse barkod o kadar
/// gerilir; okuyucular oranı korunan her boyutu okur.
/// </para>
/// </remarks>
public static class Barkod
{
    /// <summary>Yazının barkod yüksekliğinden aldığı pay.</summary>
    private const double YaziPayi = 0.24;

    /// <summary>Sessiz alan: barkodun iki yanında bırakılması gereken boşluk.</summary>
    /// <remarks>
    /// Okuyucu barkodun nerede başladığını bu boşluktan anlar. Bırakılmazsa
    /// kutunun kenarına dayanan barkod okunmaz — en sık yapılan hata budur.
    /// </remarks>
    private const int SessizAlan = 10;

    /// <summary>
    /// Metni barkoda çevirip SVG olarak döndürür; kodlanamıyorsa <c>null</c>.
    /// </summary>
    /// <param name="metin">Kodlanacak değer.</param>
    /// <param name="tur">Simgelem.</param>
    /// <param name="yazi">Çubukların altına okunabilir metin basılsın mı?</param>
    /// <param name="renk">Çubuk rengi; boşsa siyah.</param>
    /// <param name="enBoyOrani">
    /// Barkodun konacağı kutunun en/boy oranı. Verilirse <c>viewBox</c> o orana
    /// kurulur.
    /// </param>
    /// <remarks>
    /// Oranın verilmesi PDF için gerekli: QuestPDF SVG'yi <b>kendi oranını
    /// koruyarak</b> yerleştiriyor (uzatan bir seçeneği yok), dolayısıyla
    /// viewBox kutuyla aynı oranda değilse barkod kutunun içinde küçülüp bir
    /// köşeye yaslanır. Tarayıcı esnetebiliyor ama aynı oran ona da veriliyor:
    /// iki taraf aynı dizeyi bastığında tasarımdaki barkod ile kâğıttaki
    /// birebir aynı olur.
    /// </remarks>
    public static string? Svg(string? metin, BarkodTuru tur, bool yazi = true, string? renk = null,
                              double? enBoyOrani = null)
    {
        if (Kodla(metin, tur) is not { } kod) return null;

        var moduller = kod.Moduller;
        var en = moduller.Length + 2 * SessizAlan;

        // Oran verilmediyse basılı barkodların alışıldık oranı: eninin üçte
        // biri kadar boy.
        var oran = enBoyOrani is { } o && double.IsFinite(o) && o > 0 ? o : 3;
        var boy = en / oran;

        var yaziBoyu = yazi && kod.Yazi.Length > 0 ? boy * YaziPayi : 0;
        var cubukBoyu = boy - yaziBoyu;

        var murekkep = string.IsNullOrEmpty(renk) ? "#000" : renk;

        var svg = new StringBuilder();

        svg.Append(Kultursuz(
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {Yuvarla(en)} {Yuvarla(boy)}\" preserveAspectRatio=\"none\" shape-rendering=\"crispEdges\">"));

        // Zemin beyaz: saydam bırakılırsa altındaki zemin rengi çubukların
        // arasından görünür ve okuyucu için karşıtlık kalmaz.
        svg.Append(Kultursuz($"<rect width=\"{Yuvarla(en)}\" height=\"{Yuvarla(boy)}\" fill=\"#fff\"/>"));

        // Bitişik koyu modüller tek dikdörtgende birleşiyor: 100 çubuk yerine
        // 30 düğüm, hem dosya küçülüyor hem de yan yana duran dikdörtgenlerin
        // arasında kalan saç teli kadar boşluk (kenar yumuşatma) kalkıyor.
        var i = 0;
        while (i < moduller.Length)
        {
            if (!moduller[i]) { i++; continue; }

            var bas = i;
            while (i < moduller.Length && moduller[i]) i++;

            svg.Append(Kultursuz(
                $"<rect x=\"{bas + SessizAlan}\" y=\"0\" width=\"{i - bas}\" height=\"{Yuvarla(cubukBoyu)}\" fill=\"{murekkep}\"/>"));
        }

        if (yaziBoyu > 0)
        {
            // Yazı tipi verilmiyor: SVG'yi basan taraf (tarayıcı ya da PDF
            // motoru) kendi varsayılanını kullanır. Barkodu okuyan makine
            // yazıya bakmaz; yazı insan içindir.
            svg.Append(Kultursuz(
                $"<text x=\"{Yuvarla(en / 2.0)}\" y=\"{Yuvarla(boy - yaziBoyu * 0.15)}\" font-size=\"{Yuvarla(yaziBoyu * 0.9)}\" text-anchor=\"middle\" fill=\"{murekkep}\">{Kacir(kod.Yazi)}</text>"));
        }

        svg.Append("</svg>");
        return svg.ToString();
    }

    /// <summary>Kodlanmış barkod: modül dizisi ve altına yazılacak metin.</summary>
    /// <param name="Moduller"><c>true</c> = koyu çubuk, <c>false</c> = boşluk.</param>
    /// <param name="Yazi">İnsan tarafından okunacak metin.</param>
    public readonly record struct BarkodDeseni(bool[] Moduller, string Yazi);

    /// <summary>
    /// Metni modül dizisine çevirir; simgelemin kabul etmediği bir değer için
    /// <c>null</c>.
    /// </summary>
    /// <remarks>
    /// Kodlanamayan değerde <b>uydurma bir barkod üretilmez</b>. Üretilseydi
    /// etiket basılır, okutulduğunda başka bir ürün çıkar ve hatanın nereden
    /// geldiği aylar sonra anlaşılırdı; boş kutu ise tasarımcıda hemen görünür.
    /// </remarks>
    public static BarkodDeseni? Kodla(string? metin, BarkodTuru tur)
    {
        var deger = metin?.Trim();
        if (string.IsNullOrEmpty(deger)) return null;

        return tur switch
        {
            BarkodTuru.Ean13 => Ean13(deger),
            _ => Code128(deger),
        };
    }

    // ------------------------------------------------------------- Code 128

    /// <summary>
    /// Code 128 — B kümesi (yazdırılabilir ASCII).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Yalnızca B kümesi kullanılıyor. C kümesi rakam çiftlerini tek simgeye
    /// sıkıştırıp barkodu kısaltır, ama küme değiştirme kurallarını da
    /// beraberinde getirir; rapor etiketlerinde kazanç birkaç milimetre,
    /// bedeli okunması güç bir kodlayıcı.
    /// </para>
    /// <para>
    /// Her simge 11 modüldür ve desen "çubuk-boşluk-çubuk…" genişlikleri olarak
    /// yazılır; bitiş simgesi 13 modüldür (fazladan iki modüllük çubuk).
    /// </para>
    /// </remarks>
    private static BarkodDeseni? Code128(string metin)
    {
        // B kümesi 32..126 arasını taşır; dışındaki karakter (Türkçe harfler,
        // sekme) kodlanamaz.
        if (metin.Any(c => c < 32 || c > 126)) return null;

        List<int> simgeler = [104];   // START B

        foreach (var c in metin)
            simgeler.Add(c - 32);

        // Sağlama: başlangıç değeri + her simgenin konumuyla çarpımı, 103'e göre.
        var toplam = 104L;
        for (var i = 1; i < simgeler.Count; i++)
            toplam += (long)simgeler[i] * i;

        simgeler.Add((int)(toplam % 103));
        simgeler.Add(106);            // STOP

        List<bool> moduller = [];

        foreach (var simge in simgeler)
        {
            var desen = Code128Desenleri[simge];
            var koyu = true;

            foreach (var genislik in desen)
            {
                for (var i = 0; i < genislik - '0'; i++) moduller.Add(koyu);
                koyu = !koyu;
            }
        }

        return new BarkodDeseni([.. moduller], metin);
    }

    /// <summary>
    /// Code 128 simge desenleri — her biri çubuk/boşluk genişlikleri.
    /// </summary>
    /// <remarks>
    /// Sıra ISO/IEC 15417'deki değer sırasıdır: 0..102 veri simgeleri, 103-105
    /// başlangıçlar, 106 bitiş. Tablo doğrudan standarttan alınmıştır; tek tek
    /// doğrulanacak bir şey değil, bozulursa üretilen barkod hiç okunmaz.
    /// </remarks>
    private static readonly string[] Code128Desenleri =
    [
        "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312",
        "132212", "221213", "221312", "231212", "112232", "122132", "122231", "113222",
        "123122", "123221", "223211", "221132", "221231", "213212", "223112", "312131",
        "311222", "321122", "321221", "312212", "322112", "322211", "212123", "212321",
        "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313",
        "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121",
        "313121", "211331", "231131", "213113", "213311", "213131", "311123", "311321",
        "331121", "312113", "312311", "332111", "314111", "221411", "431111", "111224",
        "111422", "121124", "121421", "141122", "141221", "112214", "112412", "122114",
        "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111",
        "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112",
        "421211", "212141", "214121", "412121", "111143", "111341", "131141", "114113",
        "114311", "411113", "411311", "113141", "114131", "311141", "411131", "211412",
        "211214", "211232", "2331112",
    ];

    // --------------------------------------------------------------- EAN-13

    /// <summary>
    /// EAN-13 — 13 haneli perakende kodu.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 12 hane verilirse 13'üncüsü (sağlama) hesaplanır; 13 hane verilirse
    /// sağlaması <b>doğrulanır</b> ve tutmuyorsa kodlanmaz. Yanlış sağlamalı
    /// bir etiket, kasada okunmayan bir üründür.
    /// </para>
    /// <para>
    /// İlk hane çubuklarda yoktur: soldaki altı hanenin hangi kodlamayla
    /// (tek/çift eşlikli) yazıldığından okunur. Bu yüzden barkodun solunda
    /// duran rakamdır — çubukların dışında.
    /// </para>
    /// </remarks>
    private static BarkodDeseni? Ean13(string metin)
    {
        var rakam = new string([.. metin.Where(char.IsAsciiDigit)]);

        if (rakam.Length == 12) rakam += EanSaglamasi(rakam);
        if (rakam.Length != 13 || EanSaglamasi(rakam[..12]) != rakam[12]) return null;

        List<bool> moduller = [];

        Ekle(moduller, "101");                     // sol koruma

        var desen = EanEslikleri[rakam[0] - '0'];

        for (var i = 1; i <= 6; i++)
            Ekle(moduller, desen[i - 1] == 'L' ? EanSol[rakam[i] - '0'] : EanSolTek[rakam[i] - '0']);

        Ekle(moduller, "01010");                   // orta koruma

        for (var i = 7; i <= 12; i++)
            Ekle(moduller, EanSag[rakam[i] - '0']);

        Ekle(moduller, "101");                     // sağ koruma

        return new BarkodDeseni([.. moduller], rakam);

        static void Ekle(List<bool> l, string desen)
        {
            foreach (var c in desen) l.Add(c == '1');
        }
    }

    /// <summary>EAN sağlama hanesi: tek konumlar 1, çift konumlar 3 ile çarpılır.</summary>
    private static char EanSaglamasi(string on2)
    {
        var toplam = 0;
        for (var i = 0; i < on2.Length; i++)
            toplam += (on2[i] - '0') * (i % 2 == 0 ? 1 : 3);

        return (char)('0' + (10 - toplam % 10) % 10);
    }

    /// <summary>İlk hanenin belirlediği sol yarı kodlama düzeni.</summary>
    private static readonly string[] EanEslikleri =
    [
        "LLLLLL", "LLGLGG", "LLGGLG", "LLGGGL", "LGLLGG",
        "LGGLLG", "LGGGLL", "LGLGLG", "LGLGGL", "LGGLGL",
    ];

    private static readonly string[] EanSol =
    [
        "0001101", "0011001", "0010011", "0111101", "0100011",
        "0110001", "0101111", "0111011", "0110111", "0001011",
    ];

    /// <summary>Sol yarının "G" kodlaması — sağ yarının ters çevrilmişi.</summary>
    private static readonly string[] EanSolTek =
    [
        "0100111", "0110011", "0011011", "0100001", "0011101",
        "0111001", "0000101", "0010001", "0001001", "0010111",
    ];

    private static readonly string[] EanSag =
    [
        "1110010", "1100110", "1101100", "1000010", "1011100",
        "1001110", "1010000", "1000100", "1001000", "1110100",
    ];

    // -------------------------------------------------------------- yardım

    /// <remarks>
    /// SVG sayıları noktayla yazar. Türkçe kültürde virgülle biçimlenen bir
    /// koordinat SVG'yi bozar ve barkod hiç çizilmez.
    /// </remarks>
    private static string Kultursuz(FormattableString s)
        => s.ToString(CultureInfo.InvariantCulture);

    /// <summary>SVG'yi gereksiz ondalıkla şişirmemek için.</summary>
    private static double Yuvarla(double d) => Math.Round(d, 3);

    private static string Kacir(string s)
        => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
