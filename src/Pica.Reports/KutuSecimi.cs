using Pica.Reports.Duzen;

namespace Pica.Reports;

/// <summary>
/// Tuvalde seçili kutu.
/// </summary>
/// <remarks>
/// Bandı da taşır: kutu adı düzen içinde benzersizdir ama düzeltme dosyası
/// kutuyu bandıyla birlikte yazar ve özellik paneli hangi bandın içinde
/// olduğunu göstermek zorundadır — bir kutunun nerede durduğu, ne yaptığının
/// yarısıdır.
/// </remarks>
public sealed record KutuSecimi(DuzenBandi Bant, DuzenNesnesi Nesne);

/// <param name="Tus">Tarayıcının <c>KeyboardEvent.key</c> değeri — <c>ArrowLeft</c>, <c>z</c>.</param>
/// <param name="Shift">Shift basılı mıydı?</param>
/// <param name="Ctrl">Ctrl (macOS'ta Cmd) basılı mıydı?</param>
public sealed record TusVurusu(string Tus, bool Shift, bool Ctrl);

/// <summary>
/// Sürükleyerek ya da boyutlandırarak varılan yeni geometri, punto.
/// </summary>
/// <remarks>
/// Tuval kutuyu <b>kendisi değiştirmez</b>, yalnız nereye bırakıldığını
/// bildirir. Değişikliği uygulayan taraf, geri alma yığınına önceki hâli
/// yazmak zorunda olan taraftır; tuval o yığını görmez.
/// </remarks>
public sealed record KutuTasimasi(
    DuzenBandi Bant,
    DuzenNesnesi Nesne,
    double SolPt,
    double UstPt,
    double GenislikPt,
    double YukseklikPt);

/// <summary>
/// Tuvalde bir kutuya tıklandı.
/// </summary>
/// <param name="Bant">Kutunun bandı; boşluğa tıklandıysa <c>null</c>.</param>
/// <param name="Nesne">Tıklanan kutu; boşluğa tıklandıysa <c>null</c>.</param>
/// <param name="Ekle">
/// Ctrl basılıydı: seçim değiştirilmiyor, kutu seçime ekleniyor (zaten
/// seçiliyse çıkarılıyor).
/// </param>
public sealed record SecimIstegi(DuzenBandi? Bant, DuzenNesnesi? Nesne, bool Ekle);

/// <summary>
/// Paletten sürüklenen bir şey bandın üstüne bırakıldı.
/// </summary>
/// <param name="Bant">Bırakıldığı bant.</param>
/// <param name="SolPt">Bırakma noktası — bandın sol üst köşesine göre, punto.</param>
/// <param name="UstPt">Bırakma noktası — bandın sol üst köşesine göre, punto.</param>
/// <remarks>
/// <b>Ne sürüklendiği burada yazmıyor</b> ve tuval onu bilmiyor: nesne türü mü,
/// veri alanı mı — sürüklemeyi başlatan taraf biliyor, bırakınca da o karar
/// veriyor. Tuvalin işi "nereye bırakıldı" sorusunu cevaplamak; adın
/// benzersizliği, geri alma yığını ve ızgaraya yaslama düzeni tutan tarafın
/// işi.
/// </remarks>
public sealed record NesneBirakma(DuzenBandi Bant, double SolPt, double UstPt);

/// <summary>Bandın fareyle varılan yeni yüksekliği, punto.</summary>
/// <param name="Bant">Boyu değişen bant.</param>
/// <param name="YukseklikPt">Yeni yükseklik; alt sınır uygulanmış hâlde.</param>
public sealed record BantBoyutlamasi(DuzenBandi Bant, double YukseklikPt);

/// <summary>Alan ağacından sürüklenen veri alanı.</summary>
/// <param name="Kume">Veri kümesinin adı; kümesiz alanda <c>null</c>.</param>
/// <param name="Alan">Alan adı.</param>
public sealed record AlanSurukleme(string? Kume, string Alan);

/// <summary>JS'ten gelen taşıma sonucu — ölçüler punto.</summary>
/// <remarks>
/// Kutu doğrudan değil adıyla gelir: JS düzenin nesnelerini görmez, yalnız
/// DOM'daki <c>data-bant</c> ve <c>data-nesne</c> özniteliklerini okur.
/// </remarks>
public sealed record TasimaGirdisi(
    string Bant,
    string Nesne,
    double SolPt,
    double UstPt,
    double GenislikPt,
    double YukseklikPt);
