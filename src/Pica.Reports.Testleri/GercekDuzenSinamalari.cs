using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pica.Reports.Testleri;

/// <summary>
/// Gerçek düzen dosyaları üzerinde kayıpsızlık.
/// </summary>
/// <remarks>
/// <para>
/// Uydurma örnekler her şeyi yakalamaz: gerçek şablonlarda punto değerlerinin
/// ondalık kuyruğu uzun (<c>2,8346475</c>), bazı bantların yüksekliği sıfır,
/// bazı kutuların eni sıfır, elle yazılmış düzeltmeler var. Bu sınamalar o
/// külliyat üzerinde çalışır.
/// </para>
/// <para>
/// <b>Külliyat yoksa sınamalar atlanır.</b> Kütüphane ayrı bir depoya çıktığında
/// bu dosyalar orada olmayacak; sınamanın kırmızı yanması yerine sessizce
/// geçmesi doğru — sınadığı şey kütüphanenin kendisi değil, kütüphanenin
/// belirli bir veri kümesiyle uyumu.
/// </para>
/// </remarks>
public class GercekDuzenSinamalari
{
    private static readonly JsonSerializerOptions Secenekler = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    /// <summary>
    /// Düzen dosyalarının bulunduğu dizin; yoksa <c>null</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Önce <c>PICA_DUZENLER</c> ortam değişkenine bakılır, sonra çıktı
    /// dizininden yukarı doğru aranır: sınama <c>bin/Debug/net10.0</c> içinden
    /// çalışıyor ve depo kökünün nerede olduğu sabit değil.
    /// </para>
    /// <para>
    /// Ortam değişkeni kütüphane <b>kendi deposuna ayrıldıktan sonra</b> gerekli
    /// oldu: 137 gerçek düzen barındıran uygulamanın deposunda duruyor ve
    /// buradan yukarı çıkarak bulunamıyor. Değişken verilmezse o sınamalar
    /// sessizce atlanır — kütüphane tek başına da sınanabilmeli.
    /// </para>
    /// <code>
    /// PICA_DUZENLER=D:\...\PBM2027\Cikti\Duzenler dotnet test
    /// </code>
    /// </remarks>
    private static string? Dizin()
    {
        if (Environment.GetEnvironmentVariable("PICA_DUZENLER") is { Length: > 0 } verilen)
            return Directory.Exists(verilen) ? verilen : null;

        var yol = AppContext.BaseDirectory;

        for (var i = 0; i < 8 && yol is not null; i++)
        {
            var aday = Path.Combine(yol, "Cikti", "Duzenler");
            if (Directory.Exists(aday)) return aday;

            yol = Path.GetDirectoryName(yol.TrimEnd(Path.DirectorySeparatorChar));
        }

        return null;
    }

    public static TheoryData<string> Duzenler()
    {
        var veri = new TheoryData<string>();
        var dizin = Dizin();

        if (dizin is null)
        {
            // xUnit boş TheoryData'yı hata sayar; külliyat yokken tek bir
            // yer tutucu ile geçiliyor ve sınama gövdesi hemen dönüyor.
            veri.Add("");
            return veri;
        }

        foreach (var dosya in Directory.EnumerateFiles(dizin, "*.json").Order(StringComparer.Ordinal))
        {
            var ad = Path.GetFileNameWithoutExtension(dosya);
            if (!ad.EndsWith(".duzeltme", StringComparison.OrdinalIgnoreCase)) veri.Add(ad);
        }

        return veri;
    }

    [Theory]
    [MemberData(nameof(Duzenler))]
    public void Duzeltme_cikarimi_kayipsizdir(string anahtar)
    {
        if (anahtar.Length == 0) return;   // külliyat yok

        var dizin = Dizin()!;

        var ham = Oku(dizin, anahtar);
        var beklenen = Oku(dizin, anahtar);
        var geri = Oku(dizin, anahtar);

        // Elle yazılmış düzeltme varsa uygulanır: "beklenen", tasarımcının
        // ekranda gösterdiği düzendir.
        var ozgun = Duzeltme(dizin, anahtar);
        ozgun?.Uygula(beklenen);

        // Tasarımcının kaydedeceği şey bu fark. Diskten de geçirilir, çünkü
        // gerçekte dosyaya yazılıp geri okunuyor.
        var turetilen = DuzenDuzeltmesi.Cikar(ham, beklenen);
        var yazi = JsonSerializer.Serialize(turetilen, Secenekler);
        JsonSerializer.Deserialize<DuzenDuzeltmesi>(yazi, Secenekler)!.Uygula(geri);

        var sapma = Sapma(beklenen, geri);

        Assert.True(sapma.Count == 0,
            $"{anahtar}: {sapma.Count} kutu farklı — {string.Join(", ", sapma.Take(5))}");
    }

    [Theory]
    [MemberData(nameof(Duzenler))]
    public void Dokunulmamis_duzen_bos_duzeltme_uretir(string anahtar)
    {
        if (anahtar.Length == 0) return;

        var dizin = Dizin()!;

        // Bir düzeni açıp hiçbir şey yapmadan kaydeden biri sahte bir düzeltme
        // dosyası oluşturmamalı. Kayan nokta gürültüsü buradan sızardı.
        var duzeltme = DuzenDuzeltmesi.Cikar(Oku(dizin, anahtar), Oku(dizin, anahtar));

        Assert.True(duzeltme.Bos, $"{anahtar}: {duzeltme.Sayi} sahte değişiklik üretildi");
    }

    [Theory]
    [MemberData(nameof(Duzenler))]
    public void Her_duzenin_basim_sirasi_hesaplanabilir(string anahtar)
    {
        if (anahtar.Length == 0) return;

        var duzen = Oku(Dizin()!, anahtar);

        foreach (var sayfa in duzen.Sayfalar)
        {
            var sira = BantSirasi.BasimSirasi(sayfa);

            // Her bant en çok bir kez basılmalı — zincir kurulumundaki bir
            // hata bandı iki kere listeye sokabilir ve tuval onu iki kez çizer.
            var yinelenen = sira.GroupBy(s => s.Bant).Where(g => g.Count() > 1).Select(g => g.Key.Ad);
            Assert.True(!yinelenen.Any(), $"{anahtar}: yinelenen bant — {string.Join(", ", yinelenen)}");
        }
    }

    private static CetvelDuzeni Oku(string dizin, string anahtar)
        => JsonSerializer.Deserialize<CetvelDuzeni>(
               File.ReadAllText(Path.Combine(dizin, anahtar + ".json")), Secenekler)
           ?? throw new InvalidOperationException($"Düzen okunamadı: {anahtar}");

    private static DuzenDuzeltmesi? Duzeltme(string dizin, string anahtar)
    {
        var yol = Path.Combine(dizin, anahtar + ".duzeltme.json");

        return File.Exists(yol)
            ? JsonSerializer.Deserialize<DuzenDuzeltmesi>(File.ReadAllText(yol), Secenekler)
            : null;
    }

    /// <summary>İki düzen arasındaki kutu farkları.</summary>
    private static List<string> Sapma(CetvelDuzeni a, CetvelDuzeni b)
    {
        List<string> sapma = [];

        foreach (var (b1, b2) in a.Sayfalar.SelectMany(s => s.Bantlar)
                     .Zip(b.Sayfalar.SelectMany(s => s.Bantlar)))
        {
            if (b1.Nesneler.Count != b2.Nesneler.Count)
            {
                sapma.Add($"{b1.Ad}: {b1.Nesneler.Count} ≠ {b2.Nesneler.Count} kutu");
                continue;
            }

            if (BantDuzeltmesi.Cikar(b1, b2) is not null) sapma.Add($"{b1.Ad} (bant)");

            foreach (var (n1, n2) in b1.Nesneler.Zip(b2.Nesneler))
                if (NesneDuzeltmesi.Cikar(b1.Ad, n1, n2) is not null)
                    sapma.Add($"{b1.Ad}/{n1.Ad}");
        }

        return sapma;
    }
}
