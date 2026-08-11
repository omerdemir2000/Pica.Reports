using Pica.Reports.Duzen;

namespace Pica.Reports;

/// <summary>
/// Düzen değişikliklerinin geri alma / yineleme yığını.
/// </summary>
/// <remarks>
/// <para>
/// Adım, bir çift işlemdir: geri al ve yinele. İki tür değişiklik var ve ikisi
/// de aynı çifte indirgeniyor — bir kutunun alanları değişti (önceki ve sonraki
/// hâlin kopyası saklanır) ya da düzenin yapısı değişti, yani kutu eklendi veya
/// silindi (listeye koyan ve çıkaran işlemler saklanır).
/// </para>
/// <para>
/// Alan değişikliğinde hangi alanın değiştiği <b>tutulmaz</b>, kutunun tamamının
/// kopyası alınır. Alan bazlı tutulsaydı her yeni özellik yığına da eklenmek
/// zorunda kalır, unutulan alan sessizce geri alınamaz olurdu.
/// </para>
/// <para>
/// Yığın <b>düzen başına</b>dır; başka bir düzene geçildiğinde temizlenmeli,
/// yoksa geri alma başka bir belgenin kutusuna yazmaya çalışır.
/// </para>
/// </remarks>
public sealed class GeriAlYigini
{
    private sealed record Adim(Action Geri, Action Ileri);

    private readonly List<Adim> adimlar = [];

    /// <summary>Bir sonraki geri almanın hedefi. Yığının sonundan sayılır.</summary>
    private int imlec;

    /// <summary>
    /// Yığında tutulan en fazla adım.
    /// </summary>
    /// <remarks>
    /// Sınırsız bırakmanın anlamı yok: sekiz saatlik bir oturumda binlerce adım
    /// birikir ve hiçbiri kullanılmaz. İki yüz adım, "yanlışlıkla sürükledim"
    /// için fazlasıyla yeterli.
    /// </remarks>
    private const int Sinir = 200;

    public bool GeriAlinabilir => imlec > 0;

    public bool Yinelenebilir => imlec < adimlar.Count;

    /// <summary>Yığını boşaltır — başka bir düzene geçilirken.</summary>
    public void Temizle()
    {
        adimlar.Clear();
        imlec = 0;
    }

    /// <summary>
    /// Bir kutunun alan değişikliğini yığına yazar.
    /// </summary>
    /// <param name="hedef">Değişen kutu — kopya değil, düzenin içindeki örnek.</param>
    /// <param name="once">
    /// Değişiklikten <b>önce</b> alınmış kopyası (<see cref="DuzenNesnesi.Kopya"/>).
    /// </param>
    public void Alan(DuzenNesnesi hedef, DuzenNesnesi once)
    {
        var sonra = hedef.Kopya();
        Yapisal(() => hedef.YazUzerine(once), () => hedef.YazUzerine(sonra));
    }

    /// <summary>
    /// Kutu ekleme ya da silme gibi yapısal bir değişikliği yığına yazar.
    /// </summary>
    /// <param name="geri">Değişikliği geri alan işlem.</param>
    /// <param name="ileri">Değişikliği yeniden yapan işlem.</param>
    public void Yapisal(Action geri, Action ileri)
    {
        // Geri alınmış adımların üstüne yeni bir değişiklik gelirse ileri yol
        // kapanır: metin düzenleyicilerin hepsi böyle davranır ve kullanıcı
        // bunu bilir.
        if (imlec < adimlar.Count) adimlar.RemoveRange(imlec, adimlar.Count - imlec);

        adimlar.Add(new Adim(geri, ileri));

        if (adimlar.Count > Sinir) adimlar.RemoveAt(0);

        imlec = adimlar.Count;
    }

    /// <summary>Son değişikliği geri alır; alınacak bir şey yoksa <c>false</c>.</summary>
    public bool GeriAl()
    {
        if (!GeriAlinabilir) return false;

        adimlar[--imlec].Geri();
        return true;
    }

    /// <summary>Geri alınan değişikliği yineler; yineleyecek bir şey yoksa <c>false</c>.</summary>
    public bool Yinele()
    {
        if (!Yinelenebilir) return false;

        adimlar[imlec++].Ileri();
        return true;
    }
}
