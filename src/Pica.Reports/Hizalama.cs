using Pica.Reports.Duzen;

namespace Pica.Reports;

/// <summary>Birden çok kutuya birlikte uygulanan düzeltmeler.</summary>
public enum HizalamaTuru
{
    Sol,
    YatayOrta,
    Sag,
    Ust,
    DikeyOrta,
    Alt,

    /// <summary>Enleri en geniş kutununkine eşitler.</summary>
    AyniEn,

    /// <summary>Boyları en yüksek kutununkine eşitler.</summary>
    AyniBoy,

    /// <summary>Aradaki yatay boşlukları eşitler.</summary>
    YatayDagit,

    /// <summary>Aradaki dikey boşlukları eşitler.</summary>
    DikeyDagit,
}

/// <summary>
/// Seçili kutuları birbirine göre hizalar.
/// </summary>
/// <remarks>
/// <para>
/// Çapa <b>her zaman kutuların kendisidir</b>, kâğıt ya da bant değil: bir
/// sütunu sola dayamak, o sütunun en soldaki kutusuna hizalamak demektir.
/// Bandın soluna hizalansaydı üç kutuluk bir düzeltme bütün sütunu kâğıdın
/// kenarına yapıştırırdı.
/// </para>
/// <para>
/// Hesap saf: değiştirilecek kutuların yeni geometrisini döndürür, kendisi
/// yazmaz. Yazan taraf geri alma yığınına önceki hâli koymak zorunda olan
/// taraftır.
/// </para>
/// </remarks>
public static class Hizalama
{
    /// <param name="Nesne">Değişecek kutu.</param>
    /// <param name="SolPt">Yeni sol.</param>
    /// <param name="UstPt">Yeni üst.</param>
    /// <param name="GenislikPt">Yeni en.</param>
    /// <param name="YukseklikPt">Yeni boy.</param>
    public readonly record struct Sonuc(
        DuzenNesnesi Nesne, double SolPt, double UstPt, double GenislikPt, double YukseklikPt);

    /// <summary>Bu işlem için gereken en az kutu sayısı.</summary>
    /// <remarks>
    /// Dağıtma üç kutudan başlar: ikisinin arasında eşitlenecek bir boşluk yok.
    /// </remarks>
    public static int EnAzKutu(HizalamaTuru tur)
        => tur is HizalamaTuru.YatayDagit or HizalamaTuru.DikeyDagit ? 3 : 2;

    /// <summary>Okunur ad.</summary>
    public static string Ad(HizalamaTuru tur) => Metin.Al("Hizalama_" + tur);

    /// <summary>
    /// Hizalamanın sonucunu hesaplar.
    /// </summary>
    /// <remarks>
    /// Yeri değişmeyen kutu sonuca girmez: geri alma yığınına hiçbir şeyi
    /// değişmemiş bir adım yazmanın anlamı yok, kullanıcı Ctrl+Z'ye basıp
    /// hiçbir şey olmadığını görürdü.
    /// </remarks>
    public static List<Sonuc> Hesapla(HizalamaTuru tur, IReadOnlyList<DuzenNesnesi> kutular)
    {
        if (kutular.Count < EnAzKutu(tur)) return [];

        var yeni = tur switch
        {
            HizalamaTuru.Sol => Tek(kutular, kutular.Min(k => k.SolPt), yatay: true),
            HizalamaTuru.Sag => Tek(kutular, kutular.Max(k => k.SolPt + k.GenislikPt), yatay: true, sagaGore: true),
            HizalamaTuru.Ust => Tek(kutular, kutular.Min(k => k.UstPt), yatay: false),
            HizalamaTuru.Alt => Tek(kutular, kutular.Max(k => k.UstPt + k.YukseklikPt), yatay: false, sagaGore: true),

            HizalamaTuru.YatayOrta => Ortala(kutular, yatay: true),
            HizalamaTuru.DikeyOrta => Ortala(kutular, yatay: false),

            HizalamaTuru.AyniEn => Esitle(kutular, en: true),
            HizalamaTuru.AyniBoy => Esitle(kutular, en: false),

            HizalamaTuru.YatayDagit => Dagit(kutular, yatay: true),
            HizalamaTuru.DikeyDagit => Dagit(kutular, yatay: false),

            _ => [],
        };

        return [.. yeni.Where(s => Farkli(s))];
    }

    private static bool Farkli(Sonuc s)
        => Math.Abs(s.Nesne.SolPt - s.SolPt) > 0.0005
        || Math.Abs(s.Nesne.UstPt - s.UstPt) > 0.0005
        || Math.Abs(s.Nesne.GenislikPt - s.GenislikPt) > 0.0005
        || Math.Abs(s.Nesne.YukseklikPt - s.YukseklikPt) > 0.0005;

    private static Sonuc Ayni(DuzenNesnesi k) => new(k, k.SolPt, k.UstPt, k.GenislikPt, k.YukseklikPt);

    private static List<Sonuc> Tek(IReadOnlyList<DuzenNesnesi> kutular, double hedef, bool yatay, bool sagaGore = false)
        => [.. kutular.Select(k =>
        {
            var s = Ayni(k);
            return yatay
                ? s with { SolPt = sagaGore ? hedef - k.GenislikPt : hedef }
                : s with { UstPt = sagaGore ? hedef - k.YukseklikPt : hedef };
        })];

    /// <remarks>
    /// Ortalama çapası seçimin kapsayan dikdörtgeninin ortasıdır. Kutuların
    /// ortalarının ortalaması alınsaydı büyük bir kutu sonucu kendine çeker,
    /// üç eşit kutuluk bir sütun ortalanmış görünmezdi.
    /// </remarks>
    private static List<Sonuc> Ortala(IReadOnlyList<DuzenNesnesi> kutular, bool yatay)
    {
        var bas = yatay ? kutular.Min(k => k.SolPt) : kutular.Min(k => k.UstPt);
        var son = yatay
            ? kutular.Max(k => k.SolPt + k.GenislikPt)
            : kutular.Max(k => k.UstPt + k.YukseklikPt);

        var orta = (bas + son) / 2;

        return [.. kutular.Select(k =>
        {
            var s = Ayni(k);
            return yatay
                ? s with { SolPt = orta - k.GenislikPt / 2 }
                : s with { UstPt = orta - k.YukseklikPt / 2 };
        })];
    }

    /// <remarks>
    /// En büyüğe eşitlenir, ortalamaya değil: eşitlemenin amacı genellikle bir
    /// sütunun hücrelerini aynı yapmak ve içerik en genişine göre yazılmış
    /// oluyor. Ortalamaya çekilseydi en geniş hücrenin metni kırpılırdı.
    /// </remarks>
    private static List<Sonuc> Esitle(IReadOnlyList<DuzenNesnesi> kutular, bool en)
    {
        var hedef = en ? kutular.Max(k => k.GenislikPt) : kutular.Max(k => k.YukseklikPt);

        return [.. kutular.Select(k =>
        {
            var s = Ayni(k);
            return en ? s with { GenislikPt = hedef } : s with { YukseklikPt = hedef };
        })];
    }

    /// <remarks>
    /// İki uç kutu yerinde kalır, aradakiler eşit boşluklarla yeniden dizilir.
    /// Boşluk, uçlar arasındaki toplam yerden kutuların topladığı yer
    /// çıkarılarak bulunur; kutular farklı genişlikte olsa da aralar eşit olur.
    /// </remarks>
    private static List<Sonuc> Dagit(IReadOnlyList<DuzenNesnesi> kutular, bool yatay)
    {
        var sirali = yatay
            ? kutular.OrderBy(k => k.SolPt).ToList()
            : kutular.OrderBy(k => k.UstPt).ToList();

        double Bas(DuzenNesnesi k) => yatay ? k.SolPt : k.UstPt;
        double Boy(DuzenNesnesi k) => yatay ? k.GenislikPt : k.YukseklikPt;

        var ilk = sirali[0];
        var son = sirali[^1];

        var acik = Bas(son) + Boy(son) - Bas(ilk);
        var dolu = sirali.Sum(Boy);
        var bosluk = (acik - dolu) / (sirali.Count - 1);

        List<Sonuc> sonuc = [];
        var imlec = Bas(ilk);

        foreach (var k in sirali)
        {
            var s = Ayni(k);
            sonuc.Add(yatay ? s with { SolPt = imlec } : s with { UstPt = imlec });
            imlec += Boy(k) + bosluk;
        }

        return sonuc;
    }
}
