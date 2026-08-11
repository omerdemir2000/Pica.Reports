using System.Text.Json;
using System.Text.Json.Serialization;


namespace Pica.Reports.Duzen;

/// <summary>
/// Düzenleri bir klasördeki JSON dosyalarından okuyan hazır depo.
/// </summary>
/// <remarks>
/// <para>
/// Kütüphane düzenlerin nerede durduğunu bilmez; tek istediği
/// <see cref="IDuzenDeposu"/>'dur. Bu sınıf en yaygın hâli hazır veriyor:
/// düzenler bir klasörde JSON dosyası olarak durur. Veritabanı, HTTP servisi
/// ya da gömülü kaynak isteyen kendi uygulamasını yazar — tasarımcı farkı
/// görmez.
/// </para>
/// <para>
/// İki dosya türü var ve <b>ayrı durmaları esastır</b>:
/// </para>
/// <list type="bullet">
///   <item><c>{anahtar}.json</c> — ham düzen. Bir dönüştürücü üretiyorsa
///         (FastReport şablonlarından) her dönüşümde üzerine yazılır.</item>
///   <item><c>{anahtar}.duzeltme.json</c> — tasarımcının kaydettiği fark.
///         Dönüştürücü ona dokunmaz, dolayısıyla emek kaybolmaz.</item>
/// </list>
/// </remarks>
public sealed class DosyaDuzenDeposu(string dizin) : IDuzenDeposu
{
    private static readonly JsonSerializerOptions Secenekler = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    /// <remarks>
    /// Düzeltme dosyaları elle de okunur ve düzenlenir: Türkçe harfler
    /// <c>ç</c> diye kaçırılırsa dosya insan için okunmaz hâle gelir.
    /// </remarks>
    private static readonly JsonSerializerOptions YazmaSecenekleri = new(Secenekler)
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private string DuzenYolu(string anahtar) => Path.Combine(dizin, anahtar + ".json");

    private string DuzeltmeYolu(string anahtar) => Path.Combine(dizin, anahtar + ".duzeltme.json");

    public Task<IReadOnlyList<DuzenKaydi>> ListeleAsync(CancellationToken iptal = default)
    {
        if (!Directory.Exists(dizin)) return Task.FromResult<IReadOnlyList<DuzenKaydi>>([]);

        IReadOnlyList<DuzenKaydi> liste =
        [
            .. Directory.EnumerateFiles(dizin, "*.json")
                // "*.duzeltme.json" bir düzen değil, düzenin yamasıdır.
                .Where(y => !y.EndsWith(".duzeltme.json", StringComparison.OrdinalIgnoreCase))
                .Select(y => Path.GetFileNameWithoutExtension(y)!)
                .Order(StringComparer.CurrentCulture)
                .Select(anahtar =>
                {
                    var duzen = Oku(anahtar);
                    return new DuzenKaydi(
                        anahtar,
                        duzen?.Ad ?? anahtar,
                        duzen?.Kaynak,
                        File.Exists(DuzeltmeYolu(anahtar)));
                })
        ];

        return Task.FromResult(liste);
    }

    /// <remarks>
    /// Düzeltme <b>uygulanmadan</b> döner. Tasarımcı ikisini ayrı ayrı ister:
    /// hangi kutunun elle değiştirildiğini işaretleyebilmesi ve kaydederken
    /// ham düzeni değil farkı yazabilmesi için.
    /// </remarks>
    public Task<CetvelDuzeni?> HamGetirAsync(string anahtar, CancellationToken iptal = default)
        => Task.FromResult(Oku(anahtar));

    public Task<DuzenDuzeltmesi?> DuzeltmeGetirAsync(string anahtar, CancellationToken iptal = default)
    {
        var yol = DuzeltmeYolu(anahtar);
        if (!GecerliAnahtar(anahtar) || !File.Exists(yol)) return Task.FromResult<DuzenDuzeltmesi?>(null);

        return Task.FromResult(
            JsonSerializer.Deserialize<DuzenDuzeltmesi>(File.ReadAllText(yol), Secenekler));
    }

    public async Task DuzeltmeKaydetAsync(string anahtar, DuzenDuzeltmesi? duzeltme,
                                          CancellationToken iptal = default)
    {
        Denetle(anahtar);

        if (!File.Exists(DuzenYolu(anahtar)))
            throw new FileNotFoundException($"Düzen yok, düzeltmesi yazılamaz: {anahtar}");

        var yol = DuzeltmeYolu(anahtar);

        // Boş düzeltme dosyası bırakmanın anlamı yok: düzen ilk hâline dönsün,
        // dosya da ortadan kalksın.
        if (duzeltme is null || duzeltme.Bos)
        {
            if (File.Exists(yol)) File.Delete(yol);
            return;
        }

        await File.WriteAllTextAsync(yol, JsonSerializer.Serialize(duzeltme, YazmaSecenekleri), iptal);
    }

    /// <remarks>
    /// <c>FileMode.CreateNew</c>: var olan bir düzenin üstüne yazmak, emeği
    /// sessizce silmek olurdu. Denetim yarışa da kapalı — iki kullanıcı aynı
    /// anda aynı anahtarı açsa <c>File.Exists</c> ikisinde de geçerdi.
    /// </remarks>
    public async Task OlusturAsync(string anahtar, CetvelDuzeni duzen, CancellationToken iptal = default)
    {
        Denetle(anahtar);
        Directory.CreateDirectory(dizin);

        var json = JsonSerializer.Serialize(duzen, YazmaSecenekleri);

        try
        {
            await using var akis = new FileStream(
                DuzenYolu(anahtar), FileMode.CreateNew, FileAccess.Write, FileShare.None);

            await akis.WriteAsync(System.Text.Encoding.UTF8.GetBytes(json), iptal);
        }
        catch (IOException) when (File.Exists(DuzenYolu(anahtar)))
        {
            throw new IOException($"Bu anahtar zaten kullanılıyor: {anahtar}");
        }
    }

    private CetvelDuzeni? Oku(string anahtar)
    {
        var yol = DuzenYolu(anahtar);
        if (!GecerliAnahtar(anahtar) || !File.Exists(yol)) return null;

        try
        {
            return JsonSerializer.Deserialize<CetvelDuzeni>(File.ReadAllText(yol), Secenekler);
        }
        catch (JsonException)
        {
            // Bozuk dosya listeyi düşürmemeli: diğer düzenler açılmaya devam
            // etsin, bozuk olan adıyla görünsün.
            return null;
        }
    }

    /// <remarks>
    /// Anahtar dosya adına giriyor: <c>"../../appsettings.json"</c> gibi bir
    /// değer klasörün dışına çıkardı.
    /// </remarks>
    private static bool GecerliAnahtar(string anahtar)
        => anahtar.Length > 0 && anahtar.All(c => char.IsAsciiLetterOrDigit(c) || c == '-');

    private static void Denetle(string anahtar)
    {
        if (!GecerliAnahtar(anahtar))
            throw new ArgumentException($"Geçersiz düzen anahtarı: {anahtar}", nameof(anahtar));
    }
}
