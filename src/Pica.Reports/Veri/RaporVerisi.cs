using System.Collections;
using System.Reflection;

namespace Pica.Reports.Veri;

/// <summary>
/// Bir rapora beslenen veri: adlandırılmış veri kümeleri ve rapor değişkenleri.
/// </summary>
/// <remarks>
/// <para>
/// <b>Kümenin adı rapordaki adla aynı olmalı.</b> Düzendeki bantlar
/// <c>VeriKumesi = "Fisler"</c> der ve kutular <c>[Fisler."Tutar"]</c> yazar;
/// eşleşme ada göredir. Ad tutmazsa bant besleneceği kümeyi bulamaz ve boş
/// basar — hata vermez, çünkü "veri gelmedi" ile "veri boş" aynı görünür.
/// </para>
/// <para>
/// Veri nereden geldiğini söylemez: Dapper'ın döndürdüğü satırlar, kendi
/// sınıflarınız ya da elle kurulmuş sözlükler olabilir. Kütüphane veritabanı
/// tanımaz.
/// </para>
/// <example>
/// <code>
/// var veri = new RaporVerisi()
///     .Ekle("Fisler", await baglanti.QueryAsync("select * from fis"))     // Dapper
///     .Ekle("Ozet", ozetSatirlari)                                        // POCO listesi
///     .Degisken("KurumAdi", kurum.Ad);
/// </code>
/// </example>
/// </remarks>
public sealed class RaporVerisi
{
    private readonly Dictionary<string, VeriKumesi> kumeler = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Rapor düzeyi değerler — başlıktaki kurum adı, tarih aralığı.</summary>
    /// <remarks>
    /// Veri bandının dışındaki kutular (başlık, rapor sonu) satır göremez;
    /// okudukları yer burasıdır.
    /// </remarks>
    public Dictionary<string, object?> Degiskenler { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Kayıtlı bütün kümeler.</summary>
    public IReadOnlyCollection<VeriKumesi> Kumeler => kumeler.Values;

    /// <summary>Hazır kurulmuş bir kümeyi ekler; aynı adlı varsa değiştirir.</summary>
    public RaporVerisi Ekle(VeriKumesi kume)
    {
        kumeler[kume.Ad] = kume;
        return this;
    }

    /// <summary>
    /// Satırları veri kümesi olarak ekler.
    /// </summary>
    /// <param name="ad">Rapordaki veri kümesi adı.</param>
    /// <param name="satirlar">
    /// Dapper satırları (<c>IDictionary&lt;string, object&gt;</c>), kendi
    /// sınıflarınızın listesi ya da sözlük dizisi.
    /// </param>
    public RaporVerisi Ekle<T>(string ad, IEnumerable<T> satirlar)
        => Ekle(VeriKumesi.Olustur(ad, satirlar));

    /// <summary>
    /// Verisi henüz olmayan bir kümeyi yalnızca <b>alan adlarıyla</b> tanıtır.
    /// </summary>
    /// <remarks>
    /// Tasarım anında işe yarar: rapor daha yazılmadan alan ağacında kümenin ve
    /// alanlarının görünmesi gerekir. Sorguyu çalıştırmadan tasarım yapılabilsin
    /// diye.
    /// </remarks>
    public RaporVerisi Tanit(string ad, params string[] alanlar)
        => Ekle(new VeriKumesi(ad, alanlar, []));

    public RaporVerisi Degisken(string ad, object? deger)
    {
        Degiskenler[ad] = deger;
        return this;
    }

    /// <summary>
    /// Adı verilen küme; ad boşsa <b>tek küme varsa o</b>, yoksa <c>null</c>.
    /// </summary>
    /// <remarks>
    /// Bandın veri kümesi yazılmamış olabilir — taşınan düzenlerin çoğu tek
    /// kümeyle çalışıyor ve adı hiç yazmıyor. Tek küme varken onu seçmek doğru
    /// tahmindir; iki küme varken tahmin etmek yanlış bandı beslemek olurdu.
    /// </remarks>
    public VeriKumesi? Kume(string? ad)
    {
        if (!string.IsNullOrEmpty(ad))
            return kumeler.GetValueOrDefault(ad);

        return kumeler.Count == 1 ? kumeler.Values.First() : null;
    }

    /// <summary>Tasarımcının alan ağacına verilecek katalog.</summary>
    public List<VeriKumesiTanimi> Katalog()
        => [.. kumeler.Values.Select(k => new VeriKumesiTanimi(k.Ad, k.Alanlar))];
}

/// <summary>Adlandırılmış bir veri kümesi: alan listesi ve satırlar.</summary>
public sealed class VeriKumesi(string ad, IReadOnlyList<string> alanlar, IReadOnlyList<VeriSatiri> satirlar)
{
    public string Ad { get; } = ad;

    /// <summary>Kümenin alan adları — alan ağacı buradan doluyor.</summary>
    public IReadOnlyList<string> Alanlar { get; } = alanlar;

    public IReadOnlyList<VeriSatiri> Satirlar { get; } = satirlar;

    /// <summary>
    /// Herhangi bir satır dizisinden küme kurar.
    /// </summary>
    /// <remarks>
    /// <para>
    /// İki biçim tanınır ve <b>ayırt etme satırın kendisine bakılarak</b>
    /// yapılır, türe değil: Dapper <c>dynamic</c> döndürüyor ve derleme anında
    /// <c>T</c> yalnızca <c>object</c> görünüyor.
    /// </para>
    /// <list type="bullet">
    ///   <item><b>Sözlük satırı</b> (Dapper): anahtarlar alan adıdır.</item>
    ///   <item><b>Nesne satırı</b>: genel okunabilir özellikler alan olur.</item>
    /// </list>
    /// <para>
    /// Alan listesi bütün satırların birleşimidir: Dapper'ın satırları aynı
    /// sorgudan gelir ve aynı anahtarları taşır, ama elle kurulmuş sözlüklerde
    /// bir satırda olmayan alan diğerinde olabilir. Boş kümede tür bilgisinden
    /// (<c>T</c>) çıkarılır — sorgu hiç satır döndürmese bile alan ağacı dolu
    /// kalsın diye.
    /// </para>
    /// </remarks>
    public static VeriKumesi Olustur<T>(string ad, IEnumerable<T> satirlar)
    {
        List<VeriSatiri> liste = [];
        List<string> alanlar = [];
        HashSet<string> gorulen = new(StringComparer.OrdinalIgnoreCase);

        foreach (var satir in satirlar)
        {
            if (satir is null) continue;

            var deger = Cevir(satir);
            liste.Add(deger);

            foreach (var alan in deger.Alanlar.Keys)
                if (gorulen.Add(alan)) alanlar.Add(alan);
        }

        if (alanlar.Count == 0)
            foreach (var ozellik in Ozellikler(typeof(T)))
                if (gorulen.Add(ozellik.Name)) alanlar.Add(ozellik.Name);

        return new VeriKumesi(ad, alanlar, liste);
    }

    private static VeriSatiri Cevir(object satir)
    {
        // Dapper'ın satırı IDictionary<string, object>; kendi kurduğunuz
        // sözlükler genelde IDictionary<string, object?>. İkisi de gelir.
        if (satir is IDictionary<string, object?> sozluk)
            return new VeriSatiri(new Dictionary<string, object?>(sozluk, StringComparer.OrdinalIgnoreCase));

        if (satir is IDictionary ham)
        {
            Dictionary<string, object?> cevrilen = new(StringComparer.OrdinalIgnoreCase);

            foreach (DictionaryEntry giris in ham)
                if (giris.Key is string anahtar) cevrilen[anahtar] = giris.Value;

            return new VeriSatiri(cevrilen);
        }

        Dictionary<string, object?> nesne = new(StringComparer.OrdinalIgnoreCase);

        foreach (var ozellik in Ozellikler(satir.GetType()))
            nesne[ozellik.Name] = ozellik.GetValue(satir);

        return new VeriSatiri(nesne);
    }

    /// <remarks>
    /// Dizinli özellikler (<c>this[int]</c>) atlanıyor: adları alan değil ve
    /// okunmaları için değer gerekiyor.
    /// </remarks>
    private static IEnumerable<PropertyInfo> Ozellikler(Type tur)
        => tur == typeof(object) || tur.IsPrimitive || tur == typeof(string)
            ? []
            : tur.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                 .Where(o => o.CanRead && o.GetIndexParameters().Length == 0);
}

/// <summary>Tek bir veri satırı.</summary>
public sealed class VeriSatiri(IReadOnlyDictionary<string, object?> alanlar)
{
    public IReadOnlyDictionary<string, object?> Alanlar { get; } = alanlar;

    /// <summary>Alanın değeri; alan yoksa <c>null</c>.</summary>
    public object? this[string alan] => Alanlar.GetValueOrDefault(alan);

    public bool Var(string alan) => Alanlar.ContainsKey(alan);
}
