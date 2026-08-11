namespace Pica.Reports.Duzen;

/// <summary>Bandın kâğıttaki görevi.</summary>
public enum BantRolu
{
    /// <summary>Her sayfanın üstünde yinelenir.</summary>
    SayfaBasi,

    /// <summary>Akışın içinde, bir kez ya da satır başına basılır.</summary>
    Icerik,

    /// <summary>Her sayfanın altında yinelenir.</summary>
    SayfaAlti,

    /// <summary>Sayfaya mutlak konumda basılan örtü; akışa girmez.</summary>
    Ortu,
}

/// <param name="Bant">Bandın kendisi.</param>
/// <param name="Rol">Kâğıttaki görevi.</param>
/// <param name="Girinti">
/// Bağımlılık derinliği: ana veri bandı 0, ona bağlı ayrıntı bandı 1, bir
/// bandın peşine yapışan yan bant sahibinin bir altı. Sıralı listede kimin
/// kime bağlı olduğu başka türlü görünmez.
/// </param>
public sealed record SiraliBant(DuzenBandi Bant, BantRolu Rol, int Girinti);

/// <summary>
/// Bantların kâğıda basılma sırası.
/// </summary>
/// <remarks>
/// <para>
/// <b>Dosyadaki sıra basım sırası değildir.</b> FastReport bantları türüne göre
/// basar ve tasarımcıda bantlar herhangi bir sırada durabilir; muhasebe fişleri
/// listesinin şablonunda dosya sırası "veri, sayfa altı, sayfa başlığı, rapor
/// sonu, başlık, toplam…" iken kâğıttaki sıra "başlık, veri, toplam, özet
/// başlığı, özet, özet toplamı"dır. Gövde bantlarının kendi aralarındaki sırayı
/// <see cref="DuzenBandi.UstPt"/> verir.
/// </para>
/// <para>
/// Bu sınıf hem çizicinin hem tasarımcının doğruluk kaynağıdır. İkisi ayrı ayrı
/// hesaplasaydı tasarımcı er geç kâğıtta olmayan bir sıra gösterirdi — ve bunun
/// fark edilmesi zor olurdu, çünkü yanlış olan çıktı değil, çıktıya dair
/// söylenen şey olurdu.
/// </para>
/// </remarks>
public static class BantSirasi
{
    /// <summary>Yan bant zincirinde döngü olursa kesen sayaç.</summary>
    /// <remarks>
    /// Bir yan bandın kendini ya da sahibini göstermesi sayfayı sonsuza kadar
    /// uzatırdı. Şablonların hiçbirinde ikiden derin zincir yok; 20 fazlasıyla
    /// geniş bir üst sınır.
    /// </remarks>
    private const int ZincirSiniri = 20;

    /// <summary>
    /// Bandın basılacak yüksekliği: bildirilen değer ile içindeki kutuların
    /// eriştiği en alt nokta, hangisi büyükse.
    /// </summary>
    /// <remarks>
    /// Şablonların bir kısmında bandın <c>Height</c>'ı 0'dır ama içinde
    /// yüksekliği olan kutular vardır (FastReport tasarımcısında bant elle
    /// küçültülünce kutular yerinde kalır). Bildirilen değere güvenilseydi o
    /// bantlar hem kâğıtta hem tasarımcıda görünmezdi.
    /// </remarks>
    public static double Yukseklik(DuzenBandi bant)
        => Math.Max(bant.YukseklikPt, IcerikYuksekligi(bant));

    /// <summary>İçindeki kutuların eriştiği en alt nokta; kutu yoksa 0.</summary>
    public static double IcerikYuksekligi(DuzenBandi bant)
        => bant.Nesneler.Count == 0 ? 0 : bant.Nesneler.Max(n => n.UstPt + n.YukseklikPt);

    /// <summary>Kutusuz bir bandın inebileceği en küçük yükseklik, punto.</summary>
    /// <remarks>
    /// Sıfır değil: sıfır yükseklikli bant kâğıda hiç basılmaz ve tuvalde de
    /// tutulacak bir kenarı kalmaz — fareyle küçülten biri onu bir daha
    /// büyütemezdi. Bandı hiç bastırmamak isteyen, yüksekliği panelden 0
    /// yazar; orada geri dönüş var.
    /// </remarks>
    public const double EnAzYukseklik = 4;

    /// <summary>
    /// Bandın fareyle inebileceği en küçük yükseklik.
    /// </summary>
    /// <remarks>
    /// İçindeki kutuların altına inilmiyor: basılacak yükseklik zaten
    /// <see cref="Yukseklik"/> ile en alttaki kutuya göre hesaplanıyor, yani
    /// daha aşağı çekmenin kâğıtta hiçbir karşılığı olmaz. Kullanıcı bandı
    /// küçülttüğünü sanır, çıktı değişmez.
    /// </remarks>
    public static double EnAz(DuzenBandi bant)
        => Math.Max(EnAzYukseklik, IcerikYuksekligi(bant));

    /// <summary>Sayfanın verilen türlerdeki bantları, tür listesinin sırasıyla.</summary>
    public static IEnumerable<DuzenBandi> Turden(DuzenSayfasi s, params BantTuru[] turler)
        => turler.SelectMany(t => s.Bantlar.Where(b => b.Tur == t));

    /// <summary>Her sayfanın üstünde yinelenen bantlar.</summary>
    public static IEnumerable<DuzenBandi> SayfaBaslari(DuzenSayfasi s)
        => Turden(s, BantTuru.SayfaBasligi, BantTuru.SutunBasligi);

    /// <summary>Her sayfanın altında yinelenen bantlar.</summary>
    public static IEnumerable<DuzenBandi> SayfaAltlari(DuzenSayfasi s)
        => Turden(s, BantTuru.SayfaSonu);

    /// <summary>
    /// Akışa giren bantlar, tasarımdaki dikey sırayla.
    /// </summary>
    /// <remarks>
    /// Ayrıntı bantları listeye girmez; bağlı oldukları ana veri bandıyla
    /// birlikte basılırlar (bkz. <see cref="VeriZinciri"/>).
    /// </remarks>
    public static List<DuzenBandi> Govde(DuzenSayfasi s)
        => [.. s.Bantlar
              .Where(b => b.Tur is BantTuru.RaporBasligi or BantTuru.Baslik
                                or BantTuru.Veri or BantTuru.Alt or BantTuru.RaporSonu)
              .OrderBy(b => b.UstPt)];

    /// <summary>
    /// Ana veri bandı ve peşinden gelen ayrıntı bantları.
    /// </summary>
    /// <remarks>
    /// FastReport'ta <c>DetailData</c> ana bandın her satırı için kendi veri
    /// kümesinden yeniden döner. Taşınan raporlarda bu 1:1 kullanılmış —
    /// gönderme emrinde ana bant emri açar, ayrıntı bandı gövdesini basar —
    /// o yüzden satırın devamı gibi ele alınıyor.
    /// </remarks>
    public static List<DuzenBandi> VeriZinciri(DuzenSayfasi s, DuzenBandi ana)
    {
        var sonraki = s.Bantlar
            .Where(b => b.Tur is BantTuru.Veri && b.UstPt > ana.UstPt)
            .MinBy(b => b.UstPt);

        return [ana, .. s.Bantlar
            .Where(b => b.Tur is BantTuru.AltVeri or BantTuru.AltAltVeri
                        && b.UstPt > ana.UstPt
                        && (sonraki is null || b.UstPt < sonraki.UstPt))
            .OrderBy(b => b.UstPt)];
    }

    /// <summary>
    /// Bant ve peşine yapışan yan bantları, basılma sırasıyla.
    /// </summary>
    /// <remarks>
    /// Yan bant kâğıtta kendi başına bir yeri olmayan, sahibine yapışık basılan
    /// banttır: ödeme emrinin altındaki "uygundur" bloğu, gönderme emri
    /// satırları arasındaki boşluk gibi.
    /// </remarks>
    public static IEnumerable<DuzenBandi> YanZinciri(DuzenSayfasi s, DuzenBandi bant)
    {
        var sirada = bant;
        var koruma = 0;

        while (sirada is not null && koruma++ < ZincirSiniri)
        {
            yield return sirada;

            sirada = sirada.YanBant is { Length: > 0 } ad
                ? s.Bantlar.FirstOrDefault(x => x.Tur == BantTuru.Yan && x.Ad == ad)
                : null;
        }
    }

    /// <summary>Bandın ve peşine yapışan yan bantlarının toplam yüksekliği.</summary>
    public static double ZincirYuksekligi(DuzenSayfasi s, DuzenBandi bant)
        => YanZinciri(s, bant).Sum(Yukseklik);

    /// <summary>
    /// Sayfanın bütün bantları, kâğıda çıkacakları sırayla ve rolleriyle.
    /// </summary>
    /// <remarks>
    /// Veri bantları kâğıtta satır sayısı kadar yinelenir; burada <b>bir kez</b>
    /// görünürler. Tasarımcının gösterdiği şey basılmış belge değil, o belgeyi
    /// üreten sıradır.
    /// </remarks>
    public static List<SiraliBant> BasimSirasi(DuzenSayfasi s)
    {
        List<SiraliBant> sira = [];

        foreach (var bant in SayfaBaslari(s))
            Zincirle(bant, BantRolu.SayfaBasi, 0);

        // Grup bantları veri satırlarını sarar: kırılım değiştiğinde başlık,
        // grup bittiğinde sonu basılır. Çizici de ilkini alır (bkz. Icerik).
        var grupBasligi = Turden(s, BantTuru.GrupBasligi).FirstOrDefault();
        var grupSonu = Turden(s, BantTuru.GrupSonu).FirstOrDefault();

        foreach (var bant in Govde(s))
        {
            if (bant.Tur is not BantTuru.Veri)
            {
                Zincirle(bant, BantRolu.Icerik, 0);
                continue;
            }

            if (grupBasligi is not null) Zincirle(grupBasligi, BantRolu.Icerik, 0);

            var zincir = VeriZinciri(s, bant);
            for (var i = 0; i < zincir.Count; i++)
                Zincirle(zincir[i], BantRolu.Icerik, i == 0 ? 1 : 2);

            if (grupSonu is not null) Zincirle(grupSonu, BantRolu.Icerik, 0);
        }

        foreach (var bant in SayfaAltlari(s))
            Zincirle(bant, BantRolu.SayfaAlti, 0);

        // Örtü en sonda: akışa hiç girmez, sayfanın üstüne mutlak konumda
        // basılır. Sıralı listede yeri yoktur ama tasarımcıda görünmezse
        // kâğıtta beliren kutuların nereden geldiği anlaşılmaz.
        foreach (var bant in Turden(s, BantTuru.Ust))
            Zincirle(bant, BantRolu.Ortu, 0);

        return sira;

        // Zincirin her halkası bir öncekinin peşine yapışır; girinti de o
        // yüzden halka başına bir artar.
        void Zincirle(DuzenBandi bant, BantRolu rol, int girinti)
        {
            var derinlik = 0;

            foreach (var halka in YanZinciri(s, bant))
                sira.Add(new SiraliBant(halka, rol, girinti + derinlik++));
        }
    }
}
