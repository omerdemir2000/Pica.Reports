using Pica.Reports.Duzen;

namespace Pica.Reports.Veri;

/// <summary>
/// Kutu metnindeki başvuruları çözer ve sonucu kutunun biçimine sokar.
/// </summary>
/// <remarks>
/// <para>
/// Tanınan başvurular: <c>[Kume."Alan"]</c>, <c>[Alan]</c>, <c>[Page#]</c>,
/// <c>[TotalPages#]</c>, <c>[Line#]</c>, <c>[Date]</c>, <c>[Time]</c>,
/// <c>[SUM(&lt;Kume."Alan"&gt;)]</c> ve sayfa toplamı biçimi
/// <c>[SUM(…, Bant, 2)]</c>, ayrıca nakli yekûn için <c>[Nakil_Alan]</c> ve
/// yürüyen toplam için <c>[Genel_Alan]</c>.
/// </para>
/// <para>
/// <b>Çözülemeyen başvuru boş bırakılmaz</b>, olduğu gibi kalır: kâğıtta
/// görülür ve eksik olduğu fark edilir. Örnek kipinde ise uydurma bir değer
/// konur — amaç yerleşimi görmek.
/// </para>
/// <para>
/// Bu sınıf <b>önizleme içindir</b>. Kendi çizicinizi yazarken aynı kuralları
/// uygulamanız gerekir; ayrışırlarsa tasarımda görünen ile kâğıda basılan
/// ayrışır. Ortak parçalar zaten paylaşılıyor: metin ayrıştırma
/// (<see cref="KutuMetni"/>) ve biçimleme (<see cref="Bicimleme"/>).
/// </para>
/// </remarks>
public sealed class DegerCozucu(RaporVerisi? veri = null, bool ornek = true)
{
    /// <summary>Basılmakta olan satır; satır bandı dışında <c>null</c>.</summary>
    public VeriSatiri? Satir { get; set; }

    /// <summary>Satırın rapor içindeki sırası (1'den) — <c>[Line#]</c>.</summary>
    public int SatirSirasi { get; set; }

    public int SayfaNo { get; set; } = 1;

    public int ToplamSayfa { get; set; } = 1;

    /// <summary>O sayfadaki satırların alan toplamları — <c>SUM(…, 2)</c>.</summary>
    public Dictionary<string, decimal> SayfaToplami { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Önceki sayfalardan devreden toplam — <c>[Nakil_Alan]</c>.</summary>
    public Dictionary<string, decimal> DevredenToplam { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Kutunun basılacak metni.</summary>
    /// <remarks>
    /// Biçim kuralı: kutunun tamamı tek bir ifadeyse biçim ona uygulanır,
    /// metnin içine gömülü ifadede uygulanmaz. FastReport da böyle davranır.
    /// </remarks>
    public string Yaz(DuzenNesnesi nesne)
    {
        if (string.IsNullOrEmpty(nesne.Metin))
            return string.IsNullOrEmpty(nesne.VeriAlani)
                ? ""
                : Bicimleme.Bicimle(Alan(nesne.VeriAlani, nesne.Bicim), nesne);

        var parcalar = KutuMetni.Parcala(nesne.Metin);

        if (parcalar is [{ Ifade: true } tek])
            return Bicimleme.Bicimle(Degerlendir(tek.Metin, nesne.Bicim), nesne);

        return string.Concat(parcalar.Select(p => p.Ifade
            ? Bicimleme.Metinle(Degerlendir(p.Metin, BicimTuru.Yok))
            : p.Metin));
    }

    /// <summary>Satırın sayısal alanlarını sayfa toplamına ekler.</summary>
    /// <remarks>
    /// Çizici her satırı bastıktan sonra çağırır; sayfa kapanınca
    /// <see cref="SayfayiKapat"/> devreye alır. Defterlerde sayfa altındaki
    /// "SAYFA TOPLAMI" ile "Nakli Yekün" bu ikisinden çıkar.
    /// </remarks>
    public void SatirIsle(VeriSatiri satir)
    {
        foreach (var (alan, deger) in satir.Alanlar)
            if (deger is not null && Bicimleme.SayiyaCevir(deger) is { } sayi)
                SayfaToplami[alan] = SayfaToplami.GetValueOrDefault(alan) + sayi;
    }

    /// <summary>Sayfa toplamını devredene ekler ve sıfırlar.</summary>
    public void SayfayiKapat()
    {
        foreach (var (alan, tutar) in SayfaToplami)
            DevredenToplam[alan] = DevredenToplam.GetValueOrDefault(alan) + tutar;

        SayfaToplami.Clear();
    }

    // ------------------------------------------------------------ çözümleme

    private object? Degerlendir(string ifade, BicimTuru bicim)
    {
        var ic = ifade.Trim().Trim('[', ']').Trim();

        switch (ic)
        {
            case "Page#": return SayfaNo;
            case "TotalPages#": return ToplamSayfa;
            case "Line#": return SatirSirasi;
            case "Date": return Zaman().ToString("dd.MM.yyyy");
            case "Time": return Zaman().ToString("HH:mm");
        }

        if (Toplam(ic) is { } toplam) return toplam;
        if (Devreden(ic) is { } devreden) return devreden;

        // Hesaplanan başka ifadeler (FormatFloat, Pascal parçaları) taşınmıyor:
        // örnek kipinde bir sayı, veri kipinde ifadenin kendisi görünür.
        if (ic.Contains('(')) return ornek ? 98765.43m : "[" + ifade.Trim().Trim('[', ']') + "]";

        var (kume, alan) = AlanKatalogu.Coz(ifade);
        if (alan is null) return "[" + ic + "]";

        return Alan(alan, bicim, kume);
    }

    /// <remarks>
    /// Arama sırası: <b>satır → o kümenin ilk satırı → rapor değişkenleri</b>.
    /// Satır bandı dışındaki kutular (başlık, toplam) değişkenlerden okur;
    /// veri kümesinin adı verilmişse ve satır yoksa kümenin ilk satırına
    /// bakılıyor — bir başlık kutusu <c>[Kurum."Ad"]</c> yazdığında boş kalmasın
    /// diye.
    /// </remarks>
    private object? Alan(string alan, BicimTuru bicim, string? kume = null)
    {
        if (Satir?.Var(alan) == true) return Satir[alan];

        if (veri is not null)
        {
            if (kume is not null && veri.Kume(kume) is { Satirlar.Count: > 0 } k)
                return k.Satirlar[0][alan];

            if (veri.Degiskenler.TryGetValue(alan, out var degisken)) return degisken;

            // Kümesi yazılmamış başvuru: tek küme varsa oradan okunur.
            if (veri.Kume(null) is { Satirlar.Count: > 0 } tek && tek.Satirlar[0].Var(alan))
                return tek.Satirlar[0][alan];
        }

        return ornek ? OrnekVeri.Deger(alan, bicim, Math.Max(1, SatirSirasi)) : "[" + alan + "]";
    }

    /// <summary>
    /// <c>SUM(&lt;Kume."Alan"&gt;, Bant, 2)</c> — üçüncü parametre sıfırlama
    /// düzeyi: <c>2</c> sayfa toplamı, diğerleri rapor toplamı.
    /// </summary>
    private decimal? Toplam(string ifade)
    {
        if (!ifade.StartsWith("SUM(", StringComparison.OrdinalIgnoreCase) || !ifade.EndsWith(')'))
            return null;

        var ic = ifade[4..^1];

        var ac = ic.IndexOf('"');
        var kapa = ac < 0 ? -1 : ic.IndexOf('"', ac + 1);
        if (kapa < 0) return null;

        var alan = ic[(ac + 1)..kapa];
        var parcalar = ic.Split(',');
        var sayfaBazli = parcalar.Length >= 3 && parcalar[^1].Trim() == "2";

        if (sayfaBazli) return SayfaToplami.GetValueOrDefault(alan);

        // Rapor toplamı: kümenin tamamı gezilir.
        var kumeAdi = ic[..Math.Max(0, ac)].Trim(' ', '<', '"', '.');
        var kaynak = veri?.Kume(kumeAdi.Length > 0 ? kumeAdi : null);

        if (kaynak is null) return ornek ? 98765.43m : 0m;

        return kaynak.Satirlar.Sum(s => s[alan] is { } d ? Bicimleme.SayiyaCevir(d) ?? 0m : 0m);
    }

    private decimal? Devreden(string ad)
    {
        if (ad.StartsWith("Nakil_", StringComparison.OrdinalIgnoreCase))
            return DevredenToplam.GetValueOrDefault(ad[6..]);

        if (ad.StartsWith("Genel_", StringComparison.OrdinalIgnoreCase))
        {
            var alan = ad[6..];
            return DevredenToplam.GetValueOrDefault(alan) + SayfaToplami.GetValueOrDefault(alan);
        }

        return null;
    }

    /// <remarks>
    /// Örnek kipinde sabit an kullanılıyor: aynı düzen her açılışta aynı
    /// önizlemeyi versin, iki çıktı karşılaştırılabilsin.
    /// </remarks>
    private DateTime Zaman() => ornek ? OrnekVeri.Zaman : DateTime.Now;
}
