namespace Pica.Reports.Duzen;

/// <summary>
/// Dönüştürülmüş bir düzenin üzerine uygulanan düzeltme.
/// </summary>
/// <remarks>
/// <para>
/// Delphi şablonlarının bir kısmında kutu <b>boştur</b> ve içeriğini
/// PascalScript doldurur. Betik taşınmadığı için o kutular kâğıtta boş kalır —
/// yevmiye defterinin nakli yekûn ve genel toplam hücreleri böyledir.
/// (Kaynaktaki betik zaten bozuktu: alacak nakli yekûnuna <c>'ahmet'</c>
/// yazıyordu.)
/// </para>
/// <para>
/// Düzeltme, üretilen JSON'un <b>yanına</b> konur:
/// <c>yevmiyedefteri-frxreport1.duzeltme.json</c>. Dönüştürücü ona hiç
/// dokunmaz, dolayısıyla düzen yeniden çevrildiğinde düzeltme kaybolmaz.
/// Üretilen dosyayı elle düzenlemek bu yüzden yanlıştır — ve tasarımcının
/// kaydettiği şey de düzenin kendisi değil, bu dosyadır.
/// </para>
/// <para>
/// <b>Verilmeyen alan değiştirilmez.</b> Bütün alanlar bu yüzden boş
/// geçilebilir: düzeltme kutunun tamamını değil, yalnızca farkı taşır.
/// </para>
/// </remarks>
public sealed class DuzenDuzeltmesi
{
    /// <summary>Neden gerektiği. Zorunlu değil ama boş bırakılmamalı.</summary>
    public string? Aciklama { get; set; }

    /// <summary>Değiştirilen sayfa ayarları — kâğıt boyu, yön, kenar boşlukları.</summary>
    public List<SayfaDuzeltmesi> Sayfalar { get; set; } = [];

    /// <summary>Değiştirilen bantlar.</summary>
    public List<BantDuzeltmesi> Bantlar { get; set; } = [];

    /// <summary>Düzene sonradan eklenen bantlar.</summary>
    /// <remarks>
    /// Eklenen bant kutularıyla birlikte, bütünüyle saklanır — ham düzende
    /// karşılığı yok, neye göre fark alınacağı da yok.
    /// </remarks>
    public List<EklenenBant> EklenenBantlar { get; set; } = [];

    /// <summary>Düzenden çıkarılan bantların adları.</summary>
    public List<string> SilinenBantlar { get; set; } = [];

    /// <summary>Değiştirilen kutular.</summary>
    public List<NesneDuzeltmesi> Nesneler { get; set; } = [];

    /// <summary>Düzenden çıkarılan kutular.</summary>
    public List<SilinenNesne> Silinenler { get; set; } = [];

    /// <summary>Düzene sonradan eklenen kutular.</summary>
    /// <remarks>
    /// Eklenen kutu düzeltmede <b>bütünüyle</b> durur, farkı değil: ham düzende
    /// karşılığı yok, neye göre fark alınacağı da yok.
    /// </remarks>
    public List<EklenenNesne> Eklenenler { get; set; } = [];

    /// <summary>Düzeltmeyi düzene uygular.</summary>
    /// <returns>Karşılığı bulunamayan düzeltmeler — çağıran günlüğe yazar.</returns>
    /// <remarks>
    /// Sıra önemli: önce silinenler çıkarılır, sonra eklenenler konur, en son
    /// alan değişiklikleri yazılır. Tersi olsaydı silinmiş bir kutuya yazılan
    /// değişiklik "bulunamadı" diye uyarı üretirdi.
    /// </remarks>
    public List<string> Uygula(CetvelDuzeni duzen)
    {
        List<string> bulunamayan = [];

        // Bantlar kutulardan ÖNCE: silinen bandın kutularına ayrıca dokunmaya
        // gerek kalmaz, eklenen bandın kutuları da kendisiyle birlikte gelir.
        foreach (var ad in SilinenBantlar)
        {
            var sayfa = duzen.Sayfalar.FirstOrDefault(s => s.Bantlar.Any(b => Es(b.Ad, ad)));

            if (sayfa is null) bulunamayan.Add($"{ad} (silinen bant)");
            else sayfa.Bantlar.RemoveAll(b => Es(b.Ad, ad));
        }

        foreach (var e in EklenenBantlar)
        {
            if (e.Sayfa < 0 || e.Sayfa >= duzen.Sayfalar.Count)
            {
                bulunamayan.Add($"{e.Bant.Ad} (eklenen bant, {e.Sayfa}. sayfa yok)");
                continue;
            }

            var sayfa = duzen.Sayfalar[e.Sayfa];

            // Kopya: aynı düzeltme birden çok düzene uygulanırsa hepsi aynı
            // bant örneğini paylaşır ve birindeki değişiklik diğerine sızardı.
            if (!sayfa.Bantlar.Any(b => Es(b.Ad, e.Bant.Ad))) sayfa.Bantlar.Add(e.Bant.Kopya());
        }

        foreach (var s in Silinenler)
        {
            var bant = BantBul(duzen, s.Bant);
            var nesne = bant?.Nesneler.FirstOrDefault(n => Es(n.Ad, s.Nesne));

            if (bant is null || nesne is null) bulunamayan.Add($"{s.Bant}/{s.Nesne} (silinen)");
            else bant.Nesneler.Remove(nesne);
        }

        foreach (var e in Eklenenler)
        {
            var bant = BantBul(duzen, e.Bant);

            if (bant is null) bulunamayan.Add($"{e.Bant}/{e.Nesne.Ad} (eklenen)");
            // Kopya: aynı düzeltme nesnesi birden çok düzene uygulanırsa
            // hepsi aynı kutu örneğini paylaşır ve birinde yapılan değişiklik
            // diğerine sızardı.
            else if (!bant.Nesneler.Any(n => Es(n.Ad, e.Nesne.Ad))) bant.Nesneler.Add(e.Nesne.Kopya());
        }

        foreach (var d in Sayfalar)
        {
            if (d.Sayfa < 0 || d.Sayfa >= duzen.Sayfalar.Count)
            {
                bulunamayan.Add($"{d.Sayfa}. sayfa (sayfa ayarı)");
                continue;
            }

            d.Uygula(duzen.Sayfalar[d.Sayfa]);
        }

        foreach (var d in Bantlar)
        {
            var bant = BantBul(duzen, d.Ad);

            if (bant is null) bulunamayan.Add($"{d.Ad} (bant)");
            else d.Uygula(bant);
        }

        foreach (var d in Nesneler)
        {
            var nesne = Bul(duzen, d.Bant, d.Nesne);

            if (nesne is null)
            {
                bulunamayan.Add($"{d.Bant}/{d.Nesne}");
                continue;
            }

            d.Uygula(nesne);
        }

        return bulunamayan;
    }

    private static bool Es(string? a, string? b)
        => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static DuzenBandi? BantBul(CetvelDuzeni duzen, string ad)
        => duzen.Sayfalar.SelectMany(s => s.Bantlar).FirstOrDefault(b => Es(b.Ad, ad));

    /// <summary>
    /// İki düzen arasındaki farkı düzeltme olarak çıkarır.
    /// </summary>
    /// <param name="ham">Dönüştürücünün ürettiği düzen; hiç değiştirilmemiş.</param>
    /// <param name="calisma">Tasarımcıda düzenlenmiş hâli.</param>
    /// <param name="aciklama">Düzeltmenin gerekçesi; varsa korunur.</param>
    /// <remarks>
    /// <para>
    /// Düzeltme <b>her kayıtta baştan hesaplanır</b>, üzerine eklenmez. Böylece
    /// bir alanı elle özgün değerine geri döndürmek onu düzeltmeden de düşürür;
    /// biriktirilseydi dosya, aslında hiçbir şeyi değiştirmeyen satırlarla
    /// dolardı ve hangi değişikliğin gerçek olduğu okunmazdı.
    /// </para>
    /// <para>
    /// Bantlar ve kutular <b>ada göre</b> eşleşir: ham düzende karşılığı
    /// olmayan eklenmiş, çalışma kopyasında karşılığı olmayan silinmiştir.
    /// Adın benzersizliği bu yüzden şart — tasarımcı yeni ad üretirken düzendeki
    /// bütün adlara bakar.
    /// </para>
    /// </remarks>
    public static DuzenDuzeltmesi Cikar(CetvelDuzeni ham, CetvelDuzeni calisma, string? aciklama = null)
    {
        var duzeltme = new DuzenDuzeltmesi { Aciklama = aciklama };

        for (var s = 0; s < calisma.Sayfalar.Count; s++)
        {
            if (s < ham.Sayfalar.Count
                && SayfaDuzeltmesi.Cikar(s, ham.Sayfalar[s], calisma.Sayfalar[s]) is { } sayfaFarki)
                duzeltme.Sayfalar.Add(sayfaFarki);
        }

        for (var s = 0; s < calisma.Sayfalar.Count; s++)
        foreach (var bant in calisma.Sayfalar[s].Bantlar)
        {
            var hamBant = BantBul(ham, bant.Ad);

            if (hamBant is null)
            {
                // Sonradan eklenmiş bant: kutularıyla birlikte bütünüyle
                // taşınır. Kutuları ayrıca "eklenen kutu" olarak yazılmaz,
                // yoksa düzeltme uygulanırken iki kez konurdu.
                duzeltme.EklenenBantlar.Add(new EklenenBant { Sayfa = s, Bant = bant.Kopya() });
                continue;
            }

            if (BantDuzeltmesi.Cikar(hamBant, bant) is { } bantFarki)
                duzeltme.Bantlar.Add(bantFarki);

            foreach (var nesne in bant.Nesneler)
            {
                var oz = hamBant.Nesneler.FirstOrDefault(n => Es(n.Ad, nesne.Ad));

                if (oz is null)
                {
                    duzeltme.Eklenenler.Add(new EklenenNesne { Bant = bant.Ad, Nesne = nesne.Kopya() });
                    continue;
                }

                if (NesneDuzeltmesi.Cikar(bant.Ad, oz, nesne) is { } fark)
                    duzeltme.Nesneler.Add(fark);
            }

            foreach (var oz in hamBant.Nesneler)
                if (!bant.Nesneler.Any(n => Es(n.Ad, oz.Ad)))
                    duzeltme.Silinenler.Add(new SilinenNesne { Bant = bant.Ad, Nesne = oz.Ad });
        }

        // Ham düzende olup çalışma kopyasında olmayan bantlar silinmiştir.
        foreach (var hamBant in ham.Sayfalar.SelectMany(s => s.Bantlar))
            if (BantBul(calisma, hamBant.Ad) is null)
                duzeltme.SilinenBantlar.Add(hamBant.Ad);

        return duzeltme;
    }

    /// <summary>Düzeltilecek bir şey var mı?</summary>
    public bool Bos => Sayi == 0;

    /// <summary>Düzeltmenin kaç ayrı değişiklik taşıdığı.</summary>
    public int Sayi
        => Nesneler.Count + Bantlar.Count + Silinenler.Count + Eklenenler.Count
         + EklenenBantlar.Count + SilinenBantlar.Count + Sayfalar.Count;

    private static DuzenNesnesi? Bul(CetvelDuzeni duzen, string? bant, string nesne)
        => duzen.Sayfalar
            .SelectMany(s => s.Bantlar)
            .Where(b => bant is null || string.Equals(b.Ad, bant, StringComparison.OrdinalIgnoreCase))
            .SelectMany(b => b.Nesneler)
            .FirstOrDefault(n => string.Equals(n.Ad, nesne, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Bir sayfanın kâğıt ayarlarına uygulanan değişiklik. Verilmeyen alan
/// değiştirilmez.
/// </summary>
/// <remarks>
/// Kâğıt boyu, yön ve kenar boşlukları düzeltme dosyasında taşınır: tasarımcıda
/// A4'ten A5'e geçen biri bunu kaydedebilmeli ve dönüşüm yinelendiğinde
/// kaybetmemeli. <b>Bantlar taşınmaz</b> — kâğıt küçülünce sığmayan bantı
/// kendiliğinden kırpmak, tasarımcının koyduğu yerleşimi bozmak olurdu; taşma
/// tuvalde görünür.
/// </remarks>
public sealed class SayfaDuzeltmesi
{
    /// <summary>Sayfanın sırası (0'dan). Bir şablonda birden çok cetvel olabilir.</summary>
    public int Sayfa { get; set; }

    public double? GenislikPt { get; set; }
    public double? YukseklikPt { get; set; }
    public bool? Yatay { get; set; }
    public double? SolBoslukPt { get; set; }
    public double? SagBoslukPt { get; set; }
    public double? UstBoslukPt { get; set; }
    public double? AltBoslukPt { get; set; }
    public int? SutunSayisi { get; set; }
    public double? SutunAraligiPt { get; set; }

    public void Uygula(DuzenSayfasi s)
    {
        if (GenislikPt is { } a) s.GenislikPt = a;
        if (YukseklikPt is { } b) s.YukseklikPt = b;
        if (Yatay is { } c) s.Yatay = c;
        if (SolBoslukPt is { } d) s.SolBoslukPt = d;
        if (SagBoslukPt is { } e) s.SagBoslukPt = e;
        if (UstBoslukPt is { } f) s.UstBoslukPt = f;
        if (AltBoslukPt is { } g) s.AltBoslukPt = g;
        if (SutunSayisi is { } h) s.SutunSayisi = h;
        if (SutunAraligiPt is { } i) s.SutunAraligiPt = i;
    }

    /// <summary>İki sayfa arasındaki farkı çıkarır; fark yoksa <c>null</c>.</summary>
    public static SayfaDuzeltmesi? Cikar(int sira, DuzenSayfasi ham, DuzenSayfasi yeni)
    {
        var d = new SayfaDuzeltmesi { Sayfa = sira };
        var fark = false;

        Olcu(ham.GenislikPt, yeni.GenislikPt, v => d.GenislikPt = v);
        Olcu(ham.YukseklikPt, yeni.YukseklikPt, v => d.YukseklikPt = v);
        Olcu(ham.SolBoslukPt, yeni.SolBoslukPt, v => d.SolBoslukPt = v);
        Olcu(ham.SagBoslukPt, yeni.SagBoslukPt, v => d.SagBoslukPt = v);
        Olcu(ham.UstBoslukPt, yeni.UstBoslukPt, v => d.UstBoslukPt = v);
        Olcu(ham.AltBoslukPt, yeni.AltBoslukPt, v => d.AltBoslukPt = v);
        Olcu(ham.SutunAraligiPt, yeni.SutunAraligiPt, v => d.SutunAraligiPt = v);

        if (ham.Yatay != yeni.Yatay) { d.Yatay = yeni.Yatay; fark = true; }
        if (ham.SutunSayisi != yeni.SutunSayisi) { d.SutunSayisi = yeni.SutunSayisi; fark = true; }

        return fark ? d : null;

        void Olcu(double a, double b, Action<double> yaz)
        {
            if (Math.Abs(a - b) < 0.0005) return;
            yaz(Math.Round(b, 3));
            fark = true;
        }
    }
}

/// <summary>Düzene sonradan eklenen bant.</summary>
public sealed class EklenenBant
{
    /// <summary>Bandın konacağı sayfanın sırası (0'dan).</summary>
    /// <remarks>
    /// Bir şablondaki sayfalar aynı belgenin sayfaları değil, ayrı cetvellerdir;
    /// bandın hangisine ait olduğu bu yüzden saklanmak zorunda.
    /// </remarks>
    public int Sayfa { get; set; }

    /// <summary>Bandın tamamı, kutularıyla birlikte.</summary>
    public required DuzenBandi Bant { get; set; }
}

/// <summary>Düzenden çıkarılan kutu.</summary>
public sealed class SilinenNesne
{
    /// <summary>Kutunun bulunduğu bant.</summary>
    public required string Bant { get; set; }

    /// <summary>Kutunun adı.</summary>
    public required string Nesne { get; set; }
}

/// <summary>Düzene sonradan eklenen kutu.</summary>
public sealed class EklenenNesne
{
    /// <summary>Kutunun konacağı bant.</summary>
    public required string Bant { get; set; }

    /// <summary>Kutunun tamamı.</summary>
    public required DuzenNesnesi Nesne { get; set; }
}

/// <summary>
/// Tek bir banda uygulanan değişiklik. Verilmeyen alan değiştirilmez.
/// </summary>
/// <remarks>
/// Bandın <b>türü ve tasarımdaki dikey konumu düzeltilemez</b>: ikisi de
/// basım sırasını belirliyor (bkz. <see cref="BantSirasi"/>) ve onları
/// değiştirmek raporu yeniden tasarlamak demektir. Öyle bir ihtiyaç varsa
/// doğru yer düzeltme dosyası değil, Delphi şablonu ya da dönüştürücüdür.
/// </remarks>
public sealed class BantDuzeltmesi
{
    /// <summary>Bandın adı — <c>MasterData1</c>.</summary>
    public required string Ad { get; set; }

    public double? YukseklikPt { get; set; }
    public bool? Uzayabilir { get; set; }
    public bool? SonrasindaYeniSayfa { get; set; }
    public string? VeriKumesi { get; set; }
    public string? GrupKosulu { get; set; }
    public string? YanBant { get; set; }

    /// <summary>Verilen alanları banda yazar.</summary>
    public void Uygula(DuzenBandi b)
    {
        if (YukseklikPt is { } a) b.YukseklikPt = a;
        if (Uzayabilir is { } c) b.Uzayabilir = c;
        if (SonrasindaYeniSayfa is { } d) b.SonrasindaYeniSayfa = d;
        if (VeriKumesi is not null) b.VeriKumesi = VeriKumesi;
        if (GrupKosulu is not null) b.GrupKosulu = GrupKosulu;
        if (YanBant is not null) b.YanBant = YanBant;
    }

    /// <summary>İki bant arasındaki farkı çıkarır; fark yoksa <c>null</c>.</summary>
    public static BantDuzeltmesi? Cikar(DuzenBandi ham, DuzenBandi yeni)
    {
        var d = new BantDuzeltmesi { Ad = yeni.Ad };
        var fark = false;

        if (Math.Abs(ham.YukseklikPt - yeni.YukseklikPt) > 0.0005)
        {
            d.YukseklikPt = Math.Round(yeni.YukseklikPt, 3);
            fark = true;
        }

        if (ham.Uzayabilir != yeni.Uzayabilir) { d.Uzayabilir = yeni.Uzayabilir; fark = true; }
        if (ham.SonrasindaYeniSayfa != yeni.SonrasindaYeniSayfa) { d.SonrasindaYeniSayfa = yeni.SonrasindaYeniSayfa; fark = true; }

        fark |= Yaz(ham.VeriKumesi, yeni.VeriKumesi, v => d.VeriKumesi = v);
        fark |= Yaz(ham.GrupKosulu, yeni.GrupKosulu, v => d.GrupKosulu = v);
        fark |= Yaz(ham.YanBant, yeni.YanBant, v => d.YanBant = v);

        return fark ? d : null;

        static bool Yaz(string? a, string? b, Action<string> yaz)
        {
            if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) return false;
            if (string.Equals(a, b, StringComparison.Ordinal)) return false;

            yaz(b ?? "");
            return true;
        }
    }
}

/// <summary>Tek bir kutuya uygulanan değişiklik. Verilmeyen alan değiştirilmez.</summary>
public sealed class NesneDuzeltmesi
{
    /// <summary>Kutunun bulunduğu bant. Boş bırakılırsa bütün bantlarda aranır.</summary>
    public string? Bant { get; set; }

    /// <summary>Kutunun adı — düzendeki <c>Memo33</c> gibi.</summary>
    public required string Nesne { get; set; }

    // ----------------------------------------------------------- yerleşim

    public double? SolPt { get; set; }
    public double? UstPt { get; set; }
    public double? GenislikPt { get; set; }
    public double? YukseklikPt { get; set; }

    // -------------------------------------------------------------- içerik

    public string? Metin { get; set; }
    public string? VeriAlani { get; set; }

    /// <summary>Resim kutusunun yeni içeriği (veri URI'si).</summary>
    /// <remarks>
    /// Düzeltme dosyasını şişirir — bir logo birkaç KB base64 demektir — ama
    /// alternatifi resmi üretilmiş düzene yazmaktı; orası bir sonraki dönüşümde
    /// üzerine yazılıyor ve resim sessizce kaybolurdu.
    /// </remarks>
    public string? ResimVerisi { get; set; }

    public ResimUyumu? ResimUyumu { get; set; }

    public BarkodTuru? BarkodTuru { get; set; }
    public bool? BarkodYazisi { get; set; }

    // ------------------------------------------------------------- görünüm

    public string? YaziTipi { get; set; }
    public double? PuntoPt { get; set; }
    public bool? Kalin { get; set; }
    public bool? Egik { get; set; }
    public bool? AltiCizili { get; set; }
    public string? Renk { get; set; }
    public string? ZeminRengi { get; set; }
    public YatayHiza? Yatay { get; set; }
    public DikeyHiza? Dikey { get; set; }
    public CerceveKenari? Cerceve { get; set; }
    public double? CerceveKalinligiPt { get; set; }
    public string? CerceveRengi { get; set; }

    // --------------------------------------------------------------- biçim

    public BicimTuru? Bicim { get; set; }
    public string? BicimDeseni { get; set; }
    public string? OndalikAyraci { get; set; }
    public string? BinlikAyraci { get; set; }

    // ---------------------------------------------------------------- akış

    public bool? KelimeKaydir { get; set; }
    public bool? Uzayabilir { get; set; }
    public bool? Sigdir { get; set; }

    /// <summary>Verilen alanları kutuya yazar.</summary>
    public void Uygula(DuzenNesnesi n)
    {
        if (SolPt is { } a) n.SolPt = a;
        if (UstPt is { } b) n.UstPt = b;
        if (GenislikPt is { } c) n.GenislikPt = c;
        if (YukseklikPt is { } d) n.YukseklikPt = d;

        if (Metin is not null) n.Metin = Metin;
        if (VeriAlani is not null) n.VeriAlani = VeriAlani;

        if (ResimVerisi is not null) n.ResimVerisi = ResimVerisi;
        if (ResimUyumu is { } s) n.ResimUyumu = s;
        if (BarkodTuru is { } t) n.BarkodTuru = t;
        if (BarkodYazisi is { } u) n.BarkodYazisi = u;

        if (YaziTipi is not null) n.YaziTipi = YaziTipi;
        if (PuntoPt is { } e) n.PuntoPt = e;
        if (Kalin is { } f) n.Kalin = f;
        if (Egik is { } g) n.Egik = g;
        if (AltiCizili is { } h) n.AltiCizili = h;
        if (Renk is not null) n.Renk = Renk;
        if (ZeminRengi is not null) n.ZeminRengi = ZeminRengi;
        if (Yatay is { } i) n.Yatay = i;
        if (Dikey is { } j) n.Dikey = j;
        if (Cerceve is { } k) n.Cerceve = k;
        if (CerceveKalinligiPt is { } l) n.CerceveKalinligiPt = l;
        if (CerceveRengi is not null) n.CerceveRengi = CerceveRengi;

        if (Bicim is { } m) n.Bicim = m;
        if (BicimDeseni is not null) n.BicimDeseni = BicimDeseni;
        if (OndalikAyraci is not null) n.OndalikAyraci = OndalikAyraci;
        if (BinlikAyraci is not null) n.BinlikAyraci = BinlikAyraci;

        if (KelimeKaydir is { } o) n.KelimeKaydir = o;
        if (Uzayabilir is { } p) n.Uzayabilir = p;
        if (Sigdir is { } r) n.Sigdir = r;
    }

    /// <summary>
    /// İki kutu arasındaki farkı çıkarır; fark yoksa <c>null</c>.
    /// </summary>
    /// <remarks>
    /// Konum ve boy karşılaştırması yuvarlanır: dönüşüm punto değerlerini
    /// <c>piksel × 0,75</c> ile üretiyor ve ondalık kuyruğu uzun oluyor
    /// (<c>2,8346475</c>). Ham eşitlik arasaydık kayan nokta gürültüsü her
    /// kayıtta düzeltmeye sahte satır eklerdi.
    /// </remarks>
    public static NesneDuzeltmesi? Cikar(string bant, DuzenNesnesi ham, DuzenNesnesi yeni)
    {
        var d = new NesneDuzeltmesi { Bant = bant, Nesne = yeni.Ad };
        var fark = false;

        Olcu(ham.SolPt, yeni.SolPt, v => d.SolPt = v);
        Olcu(ham.UstPt, yeni.UstPt, v => d.UstPt = v);
        Olcu(ham.GenislikPt, yeni.GenislikPt, v => d.GenislikPt = v);
        Olcu(ham.YukseklikPt, yeni.YukseklikPt, v => d.YukseklikPt = v);

        Yazi(ham.Metin, yeni.Metin, v => d.Metin = v);
        Yazi(ham.VeriAlani, yeni.VeriAlani, v => d.VeriAlani = v);

        Yazi(ham.ResimVerisi, yeni.ResimVerisi, v => d.ResimVerisi = v);
        Deger(ham.ResimUyumu, yeni.ResimUyumu, v => d.ResimUyumu = v);
        Deger(ham.BarkodTuru, yeni.BarkodTuru, v => d.BarkodTuru = v);
        Deger(ham.BarkodYazisi, yeni.BarkodYazisi, v => d.BarkodYazisi = v);

        Yazi(ham.YaziTipi, yeni.YaziTipi, v => d.YaziTipi = v);
        Olcu(ham.PuntoPt, yeni.PuntoPt, v => d.PuntoPt = v);
        Deger(ham.Kalin, yeni.Kalin, v => d.Kalin = v);
        Deger(ham.Egik, yeni.Egik, v => d.Egik = v);
        Deger(ham.AltiCizili, yeni.AltiCizili, v => d.AltiCizili = v);
        Yazi(ham.Renk, yeni.Renk, v => d.Renk = v);
        Yazi(ham.ZeminRengi, yeni.ZeminRengi, v => d.ZeminRengi = v);
        Deger(ham.Yatay, yeni.Yatay, v => d.Yatay = v);
        Deger(ham.Dikey, yeni.Dikey, v => d.Dikey = v);
        Deger(ham.Cerceve, yeni.Cerceve, v => d.Cerceve = v);
        Olcu(ham.CerceveKalinligiPt, yeni.CerceveKalinligiPt, v => d.CerceveKalinligiPt = v);
        Yazi(ham.CerceveRengi, yeni.CerceveRengi, v => d.CerceveRengi = v);

        Deger(ham.Bicim, yeni.Bicim, v => d.Bicim = v);
        Yazi(ham.BicimDeseni, yeni.BicimDeseni, v => d.BicimDeseni = v);
        Yazi(ham.OndalikAyraci, yeni.OndalikAyraci, v => d.OndalikAyraci = v);
        Yazi(ham.BinlikAyraci, yeni.BinlikAyraci, v => d.BinlikAyraci = v);

        Deger(ham.KelimeKaydir, yeni.KelimeKaydir, v => d.KelimeKaydir = v);
        Deger(ham.Uzayabilir, yeni.Uzayabilir, v => d.Uzayabilir = v);
        Deger(ham.Sigdir, yeni.Sigdir, v => d.Sigdir = v);

        return fark ? d : null;

        void Olcu(double a, double b, Action<double> yaz)
        {
            if (Math.Abs(a - b) < 0.0005) return;
            yaz(Math.Round(b, 3));
            fark = true;
        }

        void Deger<T>(T a, T b, Action<T> yaz) where T : struct
        {
            if (EqualityComparer<T>.Default.Equals(a, b)) return;
            yaz(b);
            fark = true;
        }

        // Boş dize ile null aynı sayılır: ikisi de "değer yok" demek ve
        // arayüzdeki boş bir kutu sırf türü farklı diye düzeltmeye girmemeli.
        void Yazi(string? a, string? b, Action<string> yaz)
        {
            if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) return;
            if (string.Equals(a, b, StringComparison.Ordinal)) return;
            yaz(b ?? "");
            fark = true;
        }
    }
}
