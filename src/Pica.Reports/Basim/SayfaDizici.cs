using Pica.Reports.Duzen;
using Pica.Reports.Veri;

namespace Pica.Reports.Basim;

/// <summary>Kâğıda basılmış tek bir kutu — konumu sayfanın sol üst köşesine göre.</summary>
public sealed record BasilanKutu(DuzenNesnesi Nesne, double SolPt, double UstPt, string Metin);

/// <summary>Dizilmiş bir sayfa.</summary>
public sealed record BasilanSayfa(int No, IReadOnlyList<BasilanKutu> Kutular);

/// <summary>
/// Düzeni veriyle sayfalara döşer — önizlemenin motoru.
/// </summary>
/// <remarks>
/// <para>
/// Tuval "hangi kutu nerede duruyor" sorusunu cevaplar; bu sınıf "kâğıtta ne
/// görünecek" sorusunu: veri bandı satır sayısı kadar yinelenir, sayfa dolunca
/// kırılır, her sayfanın üstüne başlık ve altına sayfa altı bantları konur.
/// </para>
/// <para>
/// <b>Sayfa kırılımı elle yapılıyor</b>, tarayıcıya bırakılmıyor: sayfa altına
/// basılan "SAYFA TOPLAMI" ve "Nakli Yekün" o sayfaya hangi satırların
/// düştüğünü bilmeden hesaplanamaz. Aynı sebeple bir PDF çizicisi de kendi
/// sayfalamasını yapmak zorundadır — iki taraf da <see cref="BantSirasi"/>'nı
/// kullandığı sürece sıralar ayrışmaz.
/// </para>
/// <para>
/// Veri verilmezse düzen <b>örnek satırlarla</b> dizilir: yerleşimi görmek için
/// veriye ihtiyaç yok ve tasarım anında zaten veri yok.
/// </para>
/// </remarks>
public static class SayfaDizici
{
    /// <summary>Veri yokken üretilen örnek satır sayısı.</summary>
    /// <remarks>
    /// Bir sayfayı dolduracak kadar değil bilerek: örnek önizlemenin işi sayfa
    /// kırılımını denemek değil, yerleşimi göstermek.
    /// </remarks>
    private const int OrnekSatir = 8;

    /// <summary>Sonsuz döngüye karşı üst sınır.</summary>
    /// <remarks>
    /// Yüksekliği kâğıttan büyük bir veri bandı, sığmadığı için sayfayı
    /// sonsuza kadar kırardı. Sınır aşılınca dizim durur; önizleme eksik
    /// görünür ama tarayıcı kilitlenmez.
    /// </remarks>
    private const int EnCokSayfa = 50;

    /// <summary>Düzeni sayfalara döşer.</summary>
    /// <param name="sayfa">Dizilecek cetvel sayfası.</param>
    /// <param name="veri">Rapor verisi; <c>null</c> ise örnek satırlar kullanılır.</param>
    /// <param name="ornek">
    /// Çözülemeyen başvurular uydurma değerle mi dolsun? Kapalıyken ifade
    /// olduğu gibi görünür — eksik veri fark edilsin diye.
    /// </param>
    /// <remarks>
    /// Düzen <b>iki kez</b> dizilebilir: <c>[TotalPages#]</c> yazan bir kutu
    /// varsa toplam sayfa sayısı ilk geçişte bilinmiyor. QuestPDF de belgeyi
    /// aynı sebeple iki kez dolaşır.
    /// </remarks>
    public static List<BasilanSayfa> Diz(DuzenSayfasi sayfa, RaporVerisi? veri = null, bool ornek = true)
    {
        var ilk = Dagit(sayfa, veri, ornek, toplamSayfa: 1);

        if (ilk.Count <= 1 || !ToplamSayfaSoruluyor(sayfa)) return ilk;

        return Dagit(sayfa, veri, ornek, toplamSayfa: ilk.Count);
    }

    private static bool ToplamSayfaSoruluyor(DuzenSayfasi sayfa)
        => sayfa.Bantlar.SelectMany(b => b.Nesneler)
            .Any(n => n.Metin?.Contains("TotalPages#", StringComparison.OrdinalIgnoreCase) == true);

    private static List<BasilanSayfa> Dagit(DuzenSayfasi sayfa, RaporVerisi? veri, bool ornek, int toplamSayfa)
    {
        var cozucu = new DegerCozucu(veri, ornek) { ToplamSayfa = toplamSayfa };

        var basliklar = BantSirasi.SayfaBaslari(sayfa).ToList();
        var altlar = BantSirasi.SayfaAltlari(sayfa).ToList();
        var ortuler = BantSirasi.Turden(sayfa, BantTuru.Ust).ToList();

        var altYuksekligi = altlar.Sum(b => BantSirasi.ZincirYuksekligi(sayfa, b));

        var ustSinir = sayfa.UstBoslukPt;
        var altSinir = sayfa.YukseklikPt - sayfa.AltBoslukPt - altYuksekligi;

        List<BasilanSayfa> sayfalar = [];
        List<BasilanKutu> kutular = [];
        var sayfaNo = 1;
        var y = ustSinir;

        cozucu.SayfaNo = sayfaNo;
        BasliklariBas();

        var grupBasligi = BantSirasi.Turden(sayfa, BantTuru.GrupBasligi).FirstOrDefault();
        var grupSonu = BantSirasi.Turden(sayfa, BantTuru.GrupSonu).FirstOrDefault();

        foreach (var bant in BantSirasi.Govde(sayfa))
        {
            // Grup bantları veri döngüsünün içinde basılıyor; gövde sırasında
            // ayrıca basılsalardı kırılımdan bağımsız birer kez daha çıkarlardı.
            if (bant.Tur is BantTuru.GrupBasligi or BantTuru.GrupSonu) continue;

            if (bant.Tur is not BantTuru.Veri)
            {
                Sigdir(BantSirasi.ZincirYuksekligi(sayfa, bant));
                ZinciriBas(bant);
                continue;
            }

            var satirlar = Satirlar(veri, bant);
            var zincir = BantSirasi.VeriZinciri(sayfa, bant);
            var satirYuksekligi = zincir.Sum(b => BantSirasi.ZincirYuksekligi(sayfa, b));

            if (satirlar.Count == 0 || satirYuksekligi <= 0) continue;

            string? oncekiGrup = null;
            var sira = 0;

            foreach (var satir in satirlar)
            {
                if (sayfalar.Count >= EnCokSayfa) break;

                sira++;
                cozucu.Satir = satir;
                cozucu.SatirSirasi = sira;

                var grup = grupBasligi is null ? null : GrupAnahtari(bant, satir);
                var grupDegisti = grupBasligi is not null && grup != oncekiGrup;

                var gerekli = satirYuksekligi
                            + (grupDegisti ? BantSirasi.ZincirYuksekligi(sayfa, grupBasligi!) : 0)
                            + (grupDegisti && oncekiGrup is not null && grupSonu is not null
                                   ? BantSirasi.ZincirYuksekligi(sayfa, grupSonu) : 0);

                // Sayfa kırıldıysa grup başlığı yinelensin: yoksa yeni sayfadaki
                // satırların hangi gruba ait olduğu görünmez.
                if (Sigdir(gerekli) && grupBasligi is not null) grupDegisti = true;

                if (grupDegisti)
                {
                    if (oncekiGrup is not null && grup != oncekiGrup && grupSonu is not null)
                        ZinciriBas(grupSonu);

                    ZinciriBas(grupBasligi!);
                    oncekiGrup = grup;
                }

                foreach (var veriBandi in zincir) ZinciriBas(veriBandi);

                cozucu.SatirIsle(satir);
            }

            if (oncekiGrup is not null && grupSonu is not null)
            {
                Sigdir(BantSirasi.ZincirYuksekligi(sayfa, grupSonu));
                ZinciriBas(grupSonu);
            }

            cozucu.Satir = null;
        }

        SayfayiKapat();
        return sayfalar;

        // ------------------------------------------------------------- yerel

        void BasliklariBas()
        {
            foreach (var b in basliklar) ZinciriBas(b);
        }

        // Bandı ve peşine yapışan yan bantlarını basar.
        void ZinciriBas(DuzenBandi bant)
        {
            foreach (var b in BantSirasi.YanZinciri(sayfa, bant)) BandiBas(b);
        }

        void BandiBas(DuzenBandi bant)
        {
            var yukseklik = BantSirasi.Yukseklik(bant);
            if (yukseklik <= 0) return;

            foreach (var nesne in bant.Nesneler)
            {
                if (nesne.GenislikPt <= 0 || nesne.YukseklikPt <= 0) continue;

                kutular.Add(new BasilanKutu(
                    nesne,
                    sayfa.SolBoslukPt + nesne.SolPt,
                    y + nesne.UstPt,
                    nesne.Tur is NesneTuru.Yazi or NesneTuru.Barkod ? cozucu.Yaz(nesne) : ""));
            }

            y += yukseklik;
        }

        // Gerekli yükseklik sığmıyorsa sayfayı kırar; kırdıysa true döner.
        bool Sigdir(double gerekli)
        {
            if (y + gerekli <= altSinir || sayfalar.Count >= EnCokSayfa) return false;

            SayfayiKapat();

            sayfaNo++;
            cozucu.SayfaNo = sayfaNo;

            // Nakli yekûn: kapanan sayfanın toplamı devredene eklenir.
            cozucu.SayfayiKapat();

            kutular = [];
            y = ustSinir;
            BasliklariBas();

            return true;
        }

        void SayfayiKapat()
        {
            // Sayfa altı kâğıdın altına yaslanır, akışın bittiği yere değil.
            y = sayfa.YukseklikPt - sayfa.AltBoslukPt - altYuksekligi;

            foreach (var b in altlar) ZinciriBas(b);

            // Örtü bandı sayfaya mutlak konumda basılır, akışa girmez.
            foreach (var ortu in ortuler)
            {
                y = sayfa.UstBoslukPt;
                BandiBas(ortu);
            }

            sayfalar.Add(new BasilanSayfa(sayfaNo, kutular));
        }

        string? GrupAnahtari(DuzenBandi veriBandi, VeriSatiri satir)
        {
            var kosul = grupBasligi?.GrupKosulu ?? veriBandi.GrupKosulu;
            if (string.IsNullOrEmpty(kosul)) return null;

            var alan = AlanKatalogu.Coz(kosul).Alan ?? kosul;
            return satir[alan]?.ToString() ?? "";
        }
    }

    /// <summary>
    /// Veri bandını besleyecek satırlar; veri yoksa örnek satırlar.
    /// </summary>
    /// <remarks>
    /// Örnek satırlar <b>boş</b>: alanları yok, dolayısıyla çözücü her alanı
    /// örnek değerle dolduruyor. Uydurma alan adları koymak gerçek alanları
    /// gölgelerdi.
    /// <para>
    /// Kümesi yazılmış ama kayıtlı olmayan bant <b>boş kalır</b>: yanlış kümeyi
    /// basmaktansa hiç basmamak doğru — "Fisler" bekleyen bir bandın "Ozet"
    /// satırlarını basması, kâğıtta fark edilmesi güç bir hatadır.
    /// </para>
    /// </remarks>
    private static IReadOnlyList<VeriSatiri> Satirlar(RaporVerisi? veri, DuzenBandi bant)
    {
        if (veri?.Kume(bant.VeriKumesi) is { } kume) return kume.Satirlar;

        if (veri is not null && !string.IsNullOrEmpty(bant.VeriKumesi)) return [];

        return [.. Enumerable.Range(0, OrnekSatir)
            .Select(_ => new VeriSatiri(new Dictionary<string, object?>()))];
    }
}
