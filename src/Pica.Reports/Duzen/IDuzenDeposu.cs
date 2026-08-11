namespace Pica.Reports.Duzen;

/// <summary>
/// Tasarımcının düzenlere eriştiği kapı.
/// </summary>
/// <remarks>
/// <para>
/// Kütüphane <b>dosya sistemine dokunmaz</b>. Düzenlerin diskte mi, veritabanında
/// mı, yoksa bir HTTP servisinin ardında mı durduğu barındıran uygulamanın
/// bileceği iştir; tasarımcı yalnızca bu beş işlemi ister.
/// </para>
/// <para>
/// <b>Ham düzen ile düzeltme ayrı ayrı istenir.</b> Çalışma anında ikisi
/// birleştirilip tek bir düzen olarak basılır, ama tasarımcının ikisini de
/// görmesi gerekir: hangi kutunun elle değiştirildiğini işaretleyebilmesi ve —
/// asıl önemlisi — kaydederken ham düzeni değil düzeltmeyi yazabilmesi için.
/// Ham düzen üretilmiş bir dosyadır, üzerine yazmak sonraki dönüşümde kaybolan
/// bir değişiklik demektir (bkz. <see cref="DuzenDuzeltmesi"/>).
/// </para>
/// </remarks>
public interface IDuzenDeposu
{
    /// <summary>Seçim listesine girecek bütün düzenler.</summary>
    Task<IReadOnlyList<DuzenKaydi>> ListeleAsync(CancellationToken iptal = default);

    /// <summary>
    /// Düzenin <b>düzeltme uygulanmamış</b> hâli; yoksa <c>null</c>.
    /// </summary>
    Task<CetvelDuzeni?> HamGetirAsync(string anahtar, CancellationToken iptal = default);

    /// <summary>Düzenin elle yazılmış düzeltmesi; yoksa <c>null</c>.</summary>
    Task<DuzenDuzeltmesi?> DuzeltmeGetirAsync(string anahtar, CancellationToken iptal = default);

    /// <summary>
    /// Düzeltmeyi yazar. <paramref name="duzeltme"/> <c>null</c> ise düzeltme
    /// kaldırılır ve düzen üretilmiş hâline döner.
    /// </summary>
    /// <remarks>
    /// Uygulama, düzeni önbelleğe alıyorsa burada o anahtarı düşürmelidir;
    /// yoksa tasarımcıda kaydedilen değişiklik çıktıya yansımaz.
    /// </remarks>
    Task DuzeltmeKaydetAsync(string anahtar, DuzenDuzeltmesi? duzeltme, CancellationToken iptal = default);

    /// <summary>
    /// <b>Ham</b> düzen olarak yeni bir kayıt açar — listedeki "yeni düzen".
    /// </summary>
    /// <remarks>
    /// <para>
    /// Diğer yazma işleminden (<see cref="DuzeltmeKaydetAsync"/>) ayrı durmasının
    /// sebebi şu: düzeltme her zaman var olan bir ham düzenin üstüne yazılır ve o
    /// düzen dönüştürücünün ürettiği dosyadır. Sıfırdan açılan düzenin altında
    /// öyle bir dosya yoktur; ilk hâlini <b>ham</b> olarak yazacak biri gerekir,
    /// yoksa yeni düzen kaydedilemez.
    /// </para>
    /// <para>
    /// Anahtar zaten kullanılıyorsa uygulama hata atmalıdır — üzerine yazmak,
    /// dönüştürülmüş bir düzeni sessizce silmek olurdu.
    /// </para>
    /// </remarks>
    Task OlusturAsync(string anahtar, CetvelDuzeni duzen, CancellationToken iptal = default);
}

/// <summary>Düzen listesindeki tek satır.</summary>
/// <param name="Anahtar">Dosya ve adres anahtarı — <c>yevmiyedefteri-frxreport1</c>.</param>
/// <param name="Ad">Ekranda görünen ad.</param>
/// <param name="Kaynak">Hangi Delphi dosyasından geldiği; bilinmiyorsa <c>null</c>.</param>
/// <param name="Duzeltmeli">Bu düzenin elle yazılmış bir düzeltmesi var mı?</param>
public sealed record DuzenKaydi(string Anahtar, string Ad, string? Kaynak, bool Duzeltmeli);
