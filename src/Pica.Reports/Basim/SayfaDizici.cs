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
/// <para>
/// <b>Alt rapor</b> gömülü akıştır, ayrı bir belge değil: bir bandın içindeki
/// alt rapor kutusu başka bir sayfayı gösterir ve o sayfanın gövde bantları
/// akışın o noktasına girer, sonra ana akış kaldığı yerden sürer. Gömülen
/// bantlar aynı kâğıdı paylaştığı için sayfa kırılımına da katılırlar — ekstre
/// dökümü uzundur, sığmayınca kırılmak zorunda.
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

    /// <summary>Düzenin bir cetvelini sayfalara döşer.</summary>
    /// <param name="duzen">
    /// Düzenin <b>tamamı</b>. Tek sayfa yetmiyor: alt rapor kutusu hedefini
    /// sayfa adıyla gösteriyor ve o sayfa aynı düzenin başka bir yerinde duruyor
    /// (bkz. <see cref="DuzenNesnesi.AltRaporSayfasi"/>).
    /// </param>
    /// <param name="sayfaIndeksi">
    /// Basılacak cetvelin sırası. Bir şablondaki sayfalar aynı belgenin sayfaları
    /// değil, ayrı cetvellerdir; hangisinin basılacağını çağıran seçer.
    /// </param>
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
    public static List<BasilanSayfa> Diz(CetvelDuzeni duzen, int sayfaIndeksi = 0,
                                         RaporVerisi? veri = null, bool ornek = true)
    {
        if (sayfaIndeksi < 0 || sayfaIndeksi >= duzen.Sayfalar.Count) return [];

        var sayfa = duzen.Sayfalar[sayfaIndeksi];

        var ilk = Dagit(duzen, sayfa, veri, ornek, toplamSayfa: 1);

        if (ilk.Count <= 1 || !ToplamSayfaSoruluyor(duzen, sayfa)) return ilk;

        return Dagit(duzen, sayfa, veri, ornek, toplamSayfa: ilk.Count);
    }

    private static bool ToplamSayfaSoruluyor(CetvelDuzeni duzen, DuzenSayfasi sayfa)
        => Erisilen(duzen, sayfa)
            .SelectMany(s => s.Bantlar).SelectMany(b => b.Nesneler)
            .Any(n => n.Metin?.Contains("TotalPages#", StringComparison.OrdinalIgnoreCase) == true);

    /// <summary>Sayfanın kendisi ve içine gömülen alt rapor sayfaları.</summary>
    /// <remarks>
    /// Bulunanlar listesine bakarak ilerliyor: iç içe alt rapor kâğıtta bir
    /// zincir kurar (A → B → C) ve zincir kendine dönebilir (A → B → A). Aynı
    /// sayfayı ikinci kez kuyruğa almamak, o döngüyü burada da kesiyor.
    /// </remarks>
    private static List<DuzenSayfasi> Erisilen(CetvelDuzeni duzen, DuzenSayfasi kok)
    {
        List<DuzenSayfasi> bulunan = [kok];

        for (var i = 0; i < bulunan.Count; i++)
            foreach (var nesne in bulunan[i].Bantlar.SelectMany(b => b.Nesneler))
                if (duzen.SayfaBul(nesne.AltRaporSayfasi) is { } hedef && !bulunan.Contains(hedef))
                    bulunan.Add(hedef);

        return bulunan;
    }

    private static List<BasilanSayfa> Dagit(CetvelDuzeni duzen, DuzenSayfasi sayfa,
                                            RaporVerisi? veri, bool ornek, int toplamSayfa)
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

        // Gömülme zinciri — alt rapor döngüsünü kesen tek şey (bkz. AltRaporuBas).
        List<DuzenSayfasi> zincir = [sayfa];

        // Kâğıdın kenarı basılırken (sayfa başlığı, sayfa altı, örtü) gömme
        // yapılmaz: o bantlar sayfa kırılırken basılıyor ve oradan akışa girmek
        // kırılımın ortasında yeniden kırmak — yani kendini çağırmak — olurdu.
        // Kenar bantları zaten yinelenen, sabit yükseklikli bantlardır; alt rapor
        // gövdeye aittir.
        var kenar = false;

        cozucu.SayfaNo = sayfaNo;
        BasliklariBas();

        GovdeyiBas(sayfa);

        SayfayiKapat();
        return sayfalar;

        // ------------------------------------------------------------- yerel

        // Bir sayfanın akışa giren bantları. Ana sayfa için bir kez, gömülen her
        // alt rapor için yeniden çağrılıyor; sayfa kırılımı ile kâğıdın başlık ve
        // altı ANA sayfanınkine bağlı kalır, o yüzden burada yok.
        void GovdeyiBas(DuzenSayfasi s)
        {
            var grupBasligi = BantSirasi.Turden(s, BantTuru.GrupBasligi).FirstOrDefault();
            var grupSonu = BantSirasi.Turden(s, BantTuru.GrupSonu).FirstOrDefault();

            foreach (var bant in BantSirasi.Govde(s))
            {
                // Grup bantları veri döngüsünün içinde basılıyor; gövde sırasında
                // ayrıca basılsalardı kırılımdan bağımsız birer kez daha çıkarlardı.
                if (bant.Tur is BantTuru.GrupBasligi or BantTuru.GrupSonu) continue;

                if (bant.Tur is not BantTuru.Veri)
                {
                    Sigdir(BantSirasi.ZincirYuksekligi(s, bant));
                    ZinciriBas(s, bant);
                    continue;
                }

                var satirlar = Satirlar(veri, bant);
                var veriZinciri = BantSirasi.VeriZinciri(s, bant);
                var satirYuksekligi = veriZinciri.Sum(b => BantSirasi.ZincirYuksekligi(s, b));

                if (satirlar.Count == 0 || satirYuksekligi <= 0) continue;

                string? oncekiGrup = null;
                var sira = 0;

                foreach (var satir in satirlar)
                {
                    if (sayfalar.Count >= EnCokSayfa) break;

                    sira++;
                    cozucu.Satir = satir;
                    cozucu.SatirSirasi = sira;

                    var grup = grupBasligi is null ? null : GrupAnahtari(grupBasligi, bant, satir);
                    var grupDegisti = grupBasligi is not null && grup != oncekiGrup;

                    var gerekli = satirYuksekligi
                                + (grupDegisti ? BantSirasi.ZincirYuksekligi(s, grupBasligi!) : 0)
                                + (grupDegisti && oncekiGrup is not null && grupSonu is not null
                                       ? BantSirasi.ZincirYuksekligi(s, grupSonu) : 0);

                    // Sayfa kırıldıysa grup başlığı yinelensin: yoksa yeni sayfadaki
                    // satırların hangi gruba ait olduğu görünmez.
                    if (Sigdir(gerekli) && grupBasligi is not null) grupDegisti = true;

                    if (grupDegisti)
                    {
                        if (oncekiGrup is not null && grup != oncekiGrup && grupSonu is not null)
                            ZinciriBas(s, grupSonu);

                        ZinciriBas(s, grupBasligi!);
                        oncekiGrup = grup;
                    }

                    foreach (var veriBandi in veriZinciri) ZinciriBas(s, veriBandi);

                    cozucu.SatirIsle(satir);
                }

                if (oncekiGrup is not null && grupSonu is not null)
                {
                    Sigdir(BantSirasi.ZincirYuksekligi(s, grupSonu));
                    ZinciriBas(s, grupSonu);
                }

                cozucu.Satir = null;
            }
        }

        void BasliklariBas()
        {
            // Sayfa başlığı da kâğıdın kenarıdır ve sayfa kırılırken basılıyor:
            // buradan akışa girmek kırılımın ortasında yeniden kırmak olurdu.
            kenar = true;

            foreach (var b in basliklar) ZinciriBas(sayfa, b);

            kenar = false;
        }

        // Bandı ve peşine yapışan yan bantlarını basar. Yan bant sahibiyle aynı
        // sayfada aranıyor: gömülü bir bandın yan bandı da o alt rapordadır.
        void ZinciriBas(DuzenSayfasi s, DuzenBandi bant)
        {
            foreach (var b in BantSirasi.YanZinciri(s, bant)) BandiBas(b);
        }

        void BandiBas(DuzenBandi bant)
        {
            var yukseklik = BantSirasi.Yukseklik(bant);

            if (yukseklik > 0)
                foreach (var nesne in bant.Nesneler)
                {
                    // Alt rapor kutusu bir yer tutucudur, kâğıda çıkmaz: yerine
                    // hedef sayfanın bantları geliyor.
                    if (nesne.AltRaporSayfasi is { Length: > 0 }) continue;
                    if (nesne.GenislikPt <= 0 || nesne.YukseklikPt <= 0) continue;

                    kutular.Add(new BasilanKutu(
                        nesne,
                        sayfa.SolBoslukPt + nesne.SolPt,
                        y + nesne.UstPt,
                        nesne.Tur is NesneTuru.Yazi or NesneTuru.Barkod ? cozucu.Yaz(nesne) : ""));
                }

            y += yukseklik;

            // Gömülen akış bandın ARDINDAN başlıyor, kutunun durduğu satırdan
            // değil: FastReport da alt raporu bandı bastıktan sonra çalıştırır ve
            // bandın kendi yüksekliği kâğıtta yerini korur. Kutunun içinde
            // durduğu noktaya akıtmak, bandın geri kalanını gömülü içeriğin
            // altında bırakırdı.
            if (kenar) return;

            foreach (var nesne in bant.Nesneler)
                if (nesne.AltRaporSayfasi is { Length: > 0 } hedef) AltRaporuBas(hedef);
        }

        // Hedef sayfanın bantlarını akışın bu noktasına gömer.
        void AltRaporuBas(string hedefAdi)
        {
            var hedef = duzen.SayfaBul(hedefAdi);

            // Hedef bulunamadı (düzen tek sayfa gelmiş ya da ad değişmiş) ya da
            // zaten bu zincirde: sessizce geçilir. A → B → A döngüsü şablonlarda
            // olabilir ve gömme onu kâğıtta sonsuza kadar açardı. Zincirden
            // ÇIKINCA yeniden girilebiliyor: aynı alt raporun iki ayrı bantta
            // olması olağan, döngü değil.
            if (hedef is null || zincir.Contains(hedef)) return;

            // Satır bağlamı korunuyor: alt rapor bir veri satırının ortasında
            // akıyor olabilir ve gömülen gövde kendi veri bandını bitirince
            // cozucu.Satir'i boşaltıyor. Korunmasaydı ana bandın ardından gelen
            // ayrıntı bandının kutuları boş çıkardı.
            var satir = cozucu.Satir;
            var sira = cozucu.SatirSirasi;

            zincir.Add(hedef);
            // Yalnızca gövde: hedefin kendi sayfa başlığı ve sayfa sonu bantları
            // kâğıdın kenarına aittir, gömülü akışa değil — kâğıdın kenarı ana
            // sayfanındır (bkz. BantSirasi.Govde).
            GovdeyiBas(hedef);
            zincir.RemoveAt(zincir.Count - 1);

            cozucu.Satir = satir;
            cozucu.SatirSirasi = sira;
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
            kenar = true;

            // Sayfa altı kâğıdın altına yaslanır, akışın bittiği yere değil.
            y = sayfa.YukseklikPt - sayfa.AltBoslukPt - altYuksekligi;

            foreach (var b in altlar) ZinciriBas(sayfa, b);

            // Örtü bandı sayfaya mutlak konumda basılır, akışa girmez.
            foreach (var ortu in ortuler)
            {
                y = sayfa.UstBoslukPt;
                BandiBas(ortu);
            }

            kenar = false;

            sayfalar.Add(new BasilanSayfa(sayfaNo, kutular));
        }

        string? GrupAnahtari(DuzenBandi grupBasligi, DuzenBandi veriBandi, VeriSatiri satir)
        {
            var kosul = grupBasligi.GrupKosulu ?? veriBandi.GrupKosulu;
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
