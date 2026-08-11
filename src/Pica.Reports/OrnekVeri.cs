using Pica.Reports.Duzen;

namespace Pica.Reports;

/// <summary>
/// Önizleme için örnek (test) değerler.
/// </summary>
/// <remarks>
/// <para>
/// Tasarımcı gerçek veriyi görmez: düzen veriyi tanımaz, ekranın servisi
/// tanır. Bu yüzden tuvalde kutular <c>[borc]</c> gibi ham başvurularla
/// duruyordu ve kutuya <c>%2.2n</c> yazan kullanıcı <b>biçimin ne yaptığını
/// hiç göremiyordu</b> — desenin doğru yazılıp yazılmadığı ancak gerçek
/// veriyle basılan bir çıktıda anlaşılıyordu.
/// </para>
/// <para>
/// Buradaki değerler o boşluğu dolduruyor: uydurma ama <b>biçimden geçmiş</b>
/// değerler. Sayı kutusu "1.234,56", tarih kutusu "15.03.2027" gösterir;
/// kutunun genişliği yetiyor mu, ondalık ayracı doğru mu, hizalama tuttu mu —
/// hepsi tasarımda görünür.
/// </para>
/// <para>
/// Değerler <b>değişmezdir</b> (rastgele değil, saate bağlı değil): aynı düzen
/// her açılışta aynı önizlemeyi verir, iki çıktı karşılaştırılabilir.
/// </para>
/// </remarks>
public static class OrnekVeri
{
    /// <summary>Örneklerde kullanılan sabit an.</summary>
    /// <remarks>
    /// Bugünün tarihi kullanılmıyor: gün değiştiğinde önizleme de değişir ve
    /// "dün başka görünüyordu" diye bir soru doğar. Ay ve gün de birbirinden
    /// farklı seçildi — <c>dd.mm</c> ile <c>mm.dd</c> karışıklığı ancak öyle
    /// fark edilir.
    /// </remarks>
    public static readonly DateTime Zaman = new(2027, 3, 15, 14, 5, 0);

    /// <summary>Örnek belgenin sayfa sayısı.</summary>
    private const int ToplamSayfa = 3;

    /// <summary>
    /// Kutunun örnek çıktısı — kutunun biçimi uygulanmış hâlde.
    /// </summary>
    /// <param name="nesne">Yazı kutusu.</param>
    /// <param name="satir">Kaçıncı veri satırı; örnek değerler satırdan satıra değişir.</param>
    /// <remarks>
    /// Biçim kuralı çizicinin kuralıyla aynı: kutunun tamamı tek bir ifadeyse
    /// biçim ona uygulanır, metnin içine gömülü ifadede uygulanmaz (bkz.
    /// <c>IfadeCozucu.Coz</c>). Ayrı davransaydı önizleme yanıltırdı.
    /// </remarks>
    public static string Yaz(DuzenNesnesi nesne, int satir = 1)
    {
        if (string.IsNullOrEmpty(nesne.Metin))
            return string.IsNullOrEmpty(nesne.VeriAlani)
                ? ""
                : Bicimleme.Bicimle(Deger(nesne.VeriAlani, nesne.Bicim, satir), nesne);

        var parcalar = KutuMetni.Parcala(nesne.Metin);

        if (parcalar is [{ Ifade: true } tek])
            return Bicimleme.Bicimle(IfadeDegeri(tek.Metin, nesne.Bicim, satir), nesne);

        return string.Concat(parcalar.Select(p => p.Ifade
            ? Bicimleme.Metinle(IfadeDegeri(p.Metin, BicimTuru.Yok, satir))
            : p.Metin));
    }

    /// <summary>Tek bir başvurunun örnek değeri.</summary>
    private static object IfadeDegeri(string ifade, BicimTuru bicim, int satir)
    {
        var ic = ifade.Trim().Trim('[', ']').Trim();

        switch (ic)
        {
            case "Page#": return 1;
            case "TotalPages#": return ToplamSayfa;
            case "Line#": return satir;
            case "Date": return Zaman.ToString("dd.MM.yyyy");
            case "Time": return Zaman.ToString("HH:mm");
        }

        // Hesaplanan ifadeler (SUM, FormatFloat) alan değil; sonuçları her
        // zaman sayıdır ve toplam oldukları için satır değerlerinden büyüktür.
        if (ic.Contains('(')) return 98765.43m;

        return AlanKatalogu.Ad(ifade) is { } ad ? Deger(ad, bicim, satir) : ic;
    }

    /// <summary>
    /// Bir alanın örnek değeri.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Tür önce <b>kutunun biçiminden</b>, o yoksa <b>alan adından</b> tahmin
    /// edilir. Tahmin: gerçek bir alan listesi yok, olamaz da — düzen veriyi
    /// tanımıyor. Yanlış tahminin bedeli, önizlemede bir kutunun sayı yerine
    /// yazı göstermesi; bu, hiçbir şey göstermemekten iyi.
    /// </para>
    /// <para>
    /// Değer satırdan satıra değişiyor: bütün satırlar aynı olsaydı veri
    /// bandının yinelendiği görülmez, sütun genişliği de tek bir değere göre
    /// ayarlanırdı.
    /// </para>
    /// </remarks>
    public static object Deger(string alan, BicimTuru bicim = BicimTuru.Yok, int satir = 1)
    {
        var s = Math.Max(1, satir);

        if (bicim is BicimTuru.Sayi) return Sayi(s);
        if (bicim is BicimTuru.Tarih or BicimTuru.Saat) return Zaman.AddDays(s - 1);

        // Kutunun biçimi yoksa ad üzerinden tahmin — alan ağacından sürüklenen
        // kutunun biçimini seçen sezgiyle aynı; ikisi ayrışsaydı sürüklenen
        // alan bir türlü, önizlemesi başka türlü görünürdü.
        if (AlanSezgisi.Tarihsel(alan)) return Zaman.AddDays(s - 1);
        if (AlanSezgisi.Parasal(alan)) return Sayi(s);
        if (AlanSezgisi.Sirali(alan)) return 1000 + s;

        // Kalanı yazı. Alan adı içeride bırakılıyor: örnek çıktıda bile hangi
        // kutunun neye bağlı olduğu görünsün.
        return $"{alan} {s}";
    }

    private static decimal Sayi(int satir) => 1234.56m * satir;
}
