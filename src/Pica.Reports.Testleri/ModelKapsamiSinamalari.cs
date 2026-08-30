using System.Reflection;

namespace Pica.Reports.Testleri;

/// <summary>
/// Kutu modelinin alanlarını <b>elle yazılmış</b> üç yerin de taşıdığını
/// doğrular: <see cref="DuzenNesnesi.YazUzerine"/>,
/// <see cref="NesneDuzeltmesi.Cikar"/> ve <see cref="NesneDuzeltmesi.Uygula"/>.
/// </summary>
/// <remarks>
/// <para>
/// Üçü de alan alan yazılıyor ve modele yeni bir özellik eklendiğinde
/// güncellenmeleri gerekiyor. Unutulduğunda <b>hiçbir hata çıkmaz</b>: alan
/// geri alınamaz olur (YazUzerine), ya da tasarımcıda yapılan değişiklik
/// kaydedilmez ve kullanıcı kaydettiğini sanarak ekranı kapatır
/// (Cikar/Uygula). İkisi de sessiz.
/// </para>
/// <para>
/// Bu yüzden alan listesi burada <b>yansımayla</b> çıkarılıyor: yeni bir
/// özellik eklendiğinde sınama kendiliğinden onu da dener ve eksik kalan yer
/// derlemede değil ama ilk <c>dotnet test</c>'te ortaya çıkar.
/// </para>
/// <para>
/// Tanınmayan bir özellik türü <b>hata verir</b>, sessizce atlanmaz —
/// atlansaydı sınama yeni türlerde kendi kapsamını sessizce daraltırdı.
/// </para>
/// </remarks>
public class ModelKapsamiSinamalari
{
    /// <summary>
    /// Karşılaştırmaya girmeyen özellikler.
    /// </summary>
    /// <remarks>
    /// <c>Ad</c> kutunun kimliğidir: <c>YazUzerine</c> onu bilerek taşımaz
    /// (geri alma kutunun adını değiştirmemeli) ve düzeltme onu anahtar olarak
    /// kullanır, alan olarak değil. <c>Tur</c> ise düzeltmede yok — bir kutunun
    /// türünü değiştirmek onu başka bir kutu yapar.
    /// </remarks>
    private static readonly string[] DisTutulan = ["Ad"];

    /// <summary>
    /// Düzeltmenin bilerek TAŞIMADIĞI alanlar.
    /// </summary>
    /// <remarks>
    /// İkisi de tasarımcıda düzenlenemiyor, dolayısıyla bir farkları da olamaz:
    /// <list type="bullet">
    ///   <item><c>Tur</c> — kutunun türünü değiştirmek onu başka bir kutu yapar;
    ///         panelde böyle bir alan yok.</item>
    ///   <item><c>AltRaporSayfasi</c> — hangi sayfanın nereye gömüleceği düzenin
    ///         yapısıdır, yerleşim ayarı değil; bant türü ve dikey konumla aynı
    ///         sebeple düzeltme dışında (bkz. README, "Ham düzen ve düzeltme").</item>
    /// </list>
    /// <b>Geri alma (YazUzerine) ikisini de taşır</b> ve o sınama onları
    /// kapsıyor — buradaki liste yalnızca düzeltme turu içindir.
    /// </remarks>
    private static readonly string[] DuzeltmeDisi = ["Tur", "AltRaporSayfasi"];

    private static IEnumerable<PropertyInfo> Alanlar()
        => typeof(DuzenNesnesi).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && !DisTutulan.Contains(p.Name));

    /// <summary>Verilen değerden KESİNLİKLE farklı bir değer üretir.</summary>
    private static object? Degistir(PropertyInfo p, object? simdiki)
    {
        var tur = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;

        if (tur == typeof(string)) return (simdiki as string ?? "") + "z";
        if (tur == typeof(bool)) return !(bool)(simdiki ?? false);
        if (tur == typeof(double)) return (double)(simdiki ?? 0d) + 7;
        if (tur == typeof(int)) return (int)(simdiki ?? 0) + 7;

        if (tur.IsEnum)
        {
            foreach (var d in Enum.GetValues(tur))
                if (!d.Equals(simdiki)) return d;

            throw new InvalidOperationException($"{tur.Name} tek değerli; değiştirilemiyor.");
        }

        throw new InvalidOperationException(
            $"Bilinmeyen özellik türü: {p.Name} ({tur.Name}). Sınama güncellenmeli.");
    }

    [Fact]
    public void YazUzerine_butun_alanlari_tasir()
    {
        var kutu = new DuzenNesnesi { Ad = "K1" };

        // Her alanı varsayılanından farklı bir değere çek.
        var beklenen = new Dictionary<string, object?>();
        foreach (var p in Alanlar())
        {
            var yeni = Degistir(p, p.GetValue(kutu));
            p.SetValue(kutu, yeni);
            beklenen[p.Name] = yeni;
        }

        var kopya = kutu.Kopya();

        // Özgün kutuyu yeniden değiştir; kopya etkilenmemeli.
        foreach (var p in Alanlar())
            p.SetValue(kutu, Degistir(p, p.GetValue(kutu)));

        kutu.YazUzerine(kopya);

        var eksik = Alanlar()
            .Where(p => !Equals(p.GetValue(kutu), beklenen[p.Name]))
            .Select(p => p.Name)
            .ToList();

        Assert.True(eksik.Count == 0,
            $"DuzenNesnesi.YazUzerine bu alanları taşımıyor: {string.Join(", ", eksik)}");
    }

    [Fact]
    public void Kopya_ozgun_kutudan_bagimsizdir()
    {
        // Sığ kopya yeterli çünkü bütün alanlar değer türü ya da değişmez dize;
        // yine de bağın koptuğu doğrulanmalı.
        var kutu = new DuzenNesnesi { Ad = "K1", Metin = "a", SolPt = 10 };
        var kopya = kutu.Kopya();

        kutu.Metin = "b";
        kutu.SolPt = 20;

        Assert.Equal("a", kopya.Metin);
        Assert.Equal(10, kopya.SolPt);
    }

    [Fact]
    public void Duzeltme_butun_alanlari_tasir()
    {
        // Ham kutu varsayılan, yeni kutu her alanı değişmiş hâlde: aradaki fark
        // düzeltmeye girmeli ve düzeltme ham kutuya uygulandığında yeni kutu
        // geri gelmeli. Bir alan Cikar'da ya da Uygula'da eksikse tur kapanmaz.
        var kapsam = Alanlar().Where(p => !DuzeltmeDisi.Contains(p.Name)).ToList();

        var ham = new DuzenNesnesi { Ad = "K1" };
        var yeni = new DuzenNesnesi { Ad = "K1" };

        foreach (var p in kapsam)
            p.SetValue(yeni, Degistir(p, p.GetValue(ham)));

        var duzeltme = NesneDuzeltmesi.Cikar("Bant1", ham, yeni);
        Assert.NotNull(duzeltme);

        var uygulanan = ham.Kopya();
        duzeltme.Uygula(uygulanan);

        var eksik = kapsam
            .Where(p => !Equals(p.GetValue(uygulanan), p.GetValue(yeni)))
            .Select(p => p.Name)
            .ToList();

        Assert.True(eksik.Count == 0,
            $"NesneDuzeltmesi bu alanları taşımıyor (Cikar ya da Uygula eksik): {string.Join(", ", eksik)}");
    }

    [Fact]
    public void Dokunulmamis_kutu_duzeltme_uretmez()
    {
        // Sahte düzeltme, dosyayı her kayıtta büyütür ve "burada bir değişiklik
        // var" diyerek yanıltır.
        var ham = new DuzenNesnesi { Ad = "K1", Metin = "a", SolPt = 12.5 };

        Assert.Null(NesneDuzeltmesi.Cikar("Bant1", ham, ham.Kopya()));
    }

    [Fact]
    public void Her_nesne_turunun_bir_adi_var()
    {
        // Etiketler elle yazılıyor; yeni bir tür eklenip karşılığı unutulursa
        // arayüzde "Bilinmeyen" görünür.
        foreach (var tur in Enum.GetValues<NesneTuru>())
        {
            if (tur is NesneTuru.Bilinmeyen) continue;

            var ad = Etiketler.Nesne(tur);

            Assert.False(string.IsNullOrWhiteSpace(ad), $"{tur} için ad yok.");
            Assert.NotEqual(Etiketler.Nesne(NesneTuru.Bilinmeyen), ad);
        }
    }

    [Fact]
    public void Her_onay_biciminin_bir_adi_var()
    {
        foreach (var bicim in Enum.GetValues<OnayBicimi>())
            Assert.False(string.IsNullOrWhiteSpace(Etiketler.Onay(bicim)), $"{bicim} için ad yok.");
    }
}
