namespace Pica.Reports.Duzen;

/// <summary>
/// Delphi tarafındaki bir FastReport şablonunun (<c>.fr3</c> dosyası ya da bir
/// ekranın içine gömülü <c>TfrxReport</c> nesnesi) taşınmış hâli.
/// </summary>
/// <remarks>
/// <para>
/// <b>Ölçü birimi puntodur (1/72 inç)</b> — PDF'in kendi birimi. FastReport
/// düzeni 96 DPI pikselinde saklar; dönüştürücü <c>punto = piksel × 0,75</c>
/// ile çevirir. Bu çarpım tam sayıdan tam sayıya gider, yuvarlama hatası
/// birikmez. Milimetre saklanmadı: okurken kolaydır ama her dönüşümde küsurat
/// üretir ve iki sütunun kenarı kâğıtta üst üste binmez.
/// </para>
/// <para>
/// Düzen <b>veriyi tanımaz</b>. Nesneler alan adlarına bağlanır
/// (<see cref="DuzenNesnesi.VeriAlani"/>); satırları veren taraf ekranın
/// servisidir. Delphi'de sorgu şablonun içindeydi (<c>SQL.Strings</c>) ve
/// MySQL'e yazılmıştı; o sorgular taşınmaz.
/// </para>
/// </remarks>
public sealed class CetvelDuzeni
{
    /// <summary>Adres ve dosya adında kullanılan anahtar — <c>yevmiye-listesi</c>.</summary>
    public required string Anahtar { get; set; }

    /// <summary>Ekranda ve PDF üst verisinde görünen ad.</summary>
    public required string Ad { get; set; }

    /// <summary>Hangi Delphi dosyasından geldiği — <c>YvmLst.dfm</c>, <c>Mizan.fr3</c>.</summary>
    public required string Kaynak { get; set; }

    public List<DuzenSayfasi> Sayfalar { get; set; } = [];

    /// <summary>
    /// Şablonun içinde tanımlı veri kümeleri ve tasarım anındaki sorguları.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bir rapor şablonunda birden çok sorgu olabilir ve <b>hepsi dışarıdan
    /// beslenmez</b>: barındıran program genelde yalnız birini ezer (VB6'da
    /// <c>FindObject("SQL")</c>), ötekiler şablonda yazdıkları sorguyla çalışır —
    /// kâğıdın başlığındaki firma bilgisi, form ayarları, iade toplamı böyle
    /// gelir. Sorgular <b>taşınmaz</b> (çalıştırmak barındıran uygulamanın işi)
    /// ama kaybolmamalı: hangi kutunun hangi alandan geldiği ancak onlarla
    /// okunur.
    /// </para>
    /// </remarks>
    public List<DuzenVeriKumesi> VeriKumeleri { get; set; } = [];

    /// <summary>
    /// Şablonun içindeki PascalScript. <b>Taşınmaz, çalıştırılmaz</b>; dönüşümde
    /// gözden kaçmasın diye saklanır. Boş olmayan her betik elle karşılığı
    /// yazılması gereken bir davranıştır (nakli yekûn, koşullu gizleme...).
    /// </summary>
    public List<TasinmayanBetik> Betikler { get; set; } = [];

    /// <summary>Dönüştürücünün anlamadığı ya da atladığı şeyler.</summary>
    public List<string> Uyarilar { get; set; } = [];

    /// <summary>
    /// Yalnızca verilen sayfayı içeren bir kopya.
    /// </summary>
    /// <remarks>
    /// Bir FastReport şablonunda birden çok <c>TfrxReportPage</c> olabilir ve
    /// bunlar aynı belgenin sayfaları değil, <b>ayrı cetvellerdir</b>: mizan
    /// şablonunun birinci sayfası "Aylık Mizan Cetveli", ikincisi "Aylık Mizan
    /// Cetveli (Ayrıntılı)"dır ve ikisi aynı veriyi basar. Hepsini birden
    /// basmak cetveli kopyalamak olurdu; ekran hangisini istediğini seçer.
    /// </remarks>
    public CetvelDuzeni TekSayfa(int indeks)
    {
        if (indeks < 0 || indeks >= Sayfalar.Count)
            throw new ArgumentOutOfRangeException(nameof(indeks),
                $"{Anahtar}: {Sayfalar.Count} sayfası var, {indeks}. sayfa istendi.");

        return new CetvelDuzeni
        {
            Anahtar = Anahtar,
            Ad = Ad,
            Kaynak = Kaynak,
            Sayfalar = [Sayfalar[indeks]],
            VeriKumeleri = VeriKumeleri,
            Betikler = Betikler,
            Uyarilar = Uyarilar,
        };
    }

    /// <summary>Adı verilen sayfa; ad boşsa ya da eşleşme yoksa <c>null</c>.</summary>
    /// <remarks>
    /// Alt rapor hedefini <b>adıyla</b> gösteriyor (<c>Page="Page2"</c>), sırasıyla
    /// değil: şablonda sayfa eklendiğinde sıra kayar, ad kaymaz.
    /// </remarks>
    public DuzenSayfasi? SayfaBul(string? ad)
        => string.IsNullOrEmpty(ad)
            ? null
            : Sayfalar.FirstOrDefault(s => string.Equals(s.Ad, ad, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Sayfa başka bir sayfanın alt raporu mu — yani kendi başına bir cetvel
    /// değil, onun bir parçası mı?
    /// </summary>
    /// <remarks>
    /// Önizlemenin cetvel seçicisi bunu soruyor: alt rapor hedefi ayrı bir
    /// cetvel diye listelenirse kullanıcı aynı içeriği iki yerde görür ve
    /// hangisinin basılacağını anlamaz. <b>Tasarımcıda listelenmeye devam
    /// eder</b>; gömülü de olsa düzenlenebilmesi gereken bir sayfadır.
    /// </remarks>
    public bool AltRaporMu(DuzenSayfasi sayfa)
        => sayfa.Ad is { Length: > 0 } ad
        && Sayfalar.Any(s => !ReferenceEquals(s, sayfa)
                             && s.Bantlar.SelectMany(b => b.Nesneler)
                                 .Any(n => string.Equals(n.AltRaporSayfasi, ad,
                                                         StringComparison.OrdinalIgnoreCase)));
}

/// <summary>Şablonun içinde tanımlı bir veri kümesi.</summary>
/// <param name="Ad">
/// Kümenin <b>görünen</b> adı. Bantlar ve kutular bunu yazar
/// (<c>[Barkod."cari_kodu"]</c>); veriyi bu adla vermek gerekir.
/// </param>
/// <param name="NesneAdi">
/// Kümenin şablondaki nesne adı.
/// </param>
/// <remarks>
/// <para>
/// <b>İkisi neden ayrı?</b> FastReport'ta bir sorgunun nesne adı
/// (<c>Name</c>) ile görünen adı (<c>UserName</c>) farklı olabilir ve sık sık
/// farklıdır: taşınan 2.110 şablonun 750'sinde ayrışıyor
/// (<c>sql</c> → <c>Barkod</c>, <c>sqlSatislar</c> → <c>Satislar</c>).
/// İfadeler görünen adı, barındıran programın ezdiği sorgu ise nesne adını
/// kullanır — ikisi birden saklanmazsa "ekranın sorgusu hangi kümeye gidecek"
/// sorusu cevapsız kalır.
/// </para>
/// </remarks>
/// <param name="Sql">Tasarım anındaki sorgu metni; boş olabilir.</param>
public sealed record DuzenVeriKumesi(string Ad, string? NesneAdi, string Sql);

/// <param name="Nerede">Nesne ya da rapor adı.</param>
/// <param name="Olay">Olay adı — <c>OnBeforePrint</c>.</param>
/// <param name="Kod">Pascal kaynağı.</param>
public sealed record TasinmayanBetik(string Nerede, string Olay, string Kod);

public sealed class DuzenSayfasi
{
    /// <summary>Sayfanın şablondaki adı — <c>Page1</c>.</summary>
    /// <remarks>
    /// Sıra numarası yetmiyor: alt rapor hedefini adıyla gösteriyor
    /// (<see cref="DuzenNesnesi.AltRaporSayfasi"/>) ve şablonun ortasına bir
    /// sayfa eklendiğinde sıra kayar, ad kaymaz. Eski düzen dosyalarında bu
    /// alan yok — o yüzden boş geçilebilir.
    /// </remarks>
    public string? Ad { get; set; }

    public double GenislikPt { get; set; }
    public double YukseklikPt { get; set; }

    public double SolBoslukPt { get; set; }
    public double SagBoslukPt { get; set; }
    public double UstBoslukPt { get; set; }
    public double AltBoslukPt { get; set; }

    /// <summary>Kâğıt yatay mı? FastReport'ta <c>Orientation = poLandscape</c>.</summary>
    public bool Yatay { get; set; }

    /// <summary>Sütun sayısı (<c>Columns</c>); 0 ve 1 aynı anlama gelir.</summary>
    public int SutunSayisi { get; set; }

    public double SutunAraligiPt { get; set; }

    public List<DuzenBandi> Bantlar { get; set; } = [];
}

/// <summary>
/// FastReport bant türleri. Sıra, bandın kâğıttaki basılma sırasıdır ve
/// <c>TfrxReportPage</c> içindeki nesne sırasından bağımsızdır.
/// </summary>
public enum BantTuru
{
    Bilinmeyen = 0,
    RaporBasligi,      // TfrxReportTitle    — ilk sayfada bir kez
    SayfaBasligi,      // TfrxPageHeader     — her sayfanın üstünde
    Baslik,            // TfrxHeader         — veriden önce, sütun başlıkları
    SutunBasligi,      // TfrxColumnHeader
    GrupBasligi,       // TfrxGroupHeader
    Veri,              // TfrxMasterData     — her satır için
    AltVeri,           // TfrxDetailData
    AltAltVeri,        // TfrxSubdetailData
    GrupSonu,          // TfrxGroupFooter
    SutunSonu,         // TfrxColumnFooter
    Alt,               // TfrxFooter         — veriden sonra, toplamlar
    RaporSonu,         // TfrxReportSummary  — son sayfada bir kez
    SayfaSonu,         // TfrxPageFooter     — her sayfanın altında
    Ust,               // TfrxOverlay        — sayfaya mutlak konumda basılan
    Yan,               // TfrxChild
}

public sealed class DuzenBandi
{
    public required string Ad { get; set; }
    public BantTuru Tur { get; set; }

    public double YukseklikPt { get; set; }

    /// <summary>
    /// Bandın tasarım sayfasındaki dikey konumu.
    /// </summary>
    /// <remarks>
    /// <b>Basım sırasını bu belirler.</b> FastReport tasarımcısında bantlar
    /// alt alta dizilir ve kâğıda o sırayla çıkar; dosyadaki nesne sırası ise
    /// bantların eklenme sırasıdır ve bambaşka olabilir. Muhasebe fişleri
    /// listesinin şablonunda dosya sırası "veri, sayfa altı, sayfa başlığı,
    /// rapor sonu, başlık, toplam…" iken kâğıttaki sıra "başlık, veri,
    /// toplam, özet başlığı, özet, özet toplamı"dır.
    /// </remarks>
    public double UstPt { get; set; }

    /// <summary>Bandın beslendiği veri kümesinin adı (<c>frxDBDataset1</c>).</summary>
    public string? VeriKumesi { get; set; }

    /// <summary>Grup bandının kırılım ifadesi (<c>GroupHeader.Condition</c>).</summary>
    public string? GrupKosulu { get; set; }

    /// <summary>Bant boyunca içeriği kadar uzayabilir mi? (<c>StretchMode</c>)</summary>
    public bool Uzayabilir { get; set; }

    /// <summary>Bandın altından yeni sayfa başlar mı?</summary>
    public bool SonrasindaYeniSayfa { get; set; }

    /// <summary>
    /// Bu bandın hemen ardından basılacak yan bandın adı
    /// (FastReport <c>Child</c> özelliği, <see cref="BantTuru.Yan"/>).
    /// </summary>
    /// <remarks>
    /// Yan bant kâğıtta kendi başına bir yeri olmayan, sahibine yapışık basılan
    /// banttır: ödeme emrinin altındaki "uygundur" bloğu, gönderme emri
    /// satırları arasındaki boşluk gibi. Bağ olmadan bu bantlar ya hiç
    /// basılmaz ya da yanlış yerde çıkar.
    /// </remarks>
    public string? YanBant { get; set; }

    public List<DuzenNesnesi> Nesneler { get; set; } = [];

    /// <summary>
    /// Bandın ve içindeki kutuların kopyası.
    /// </summary>
    /// <remarks>
    /// Kutu listesi de kopyalanır, paylaşılmaz: aynı düzeltme iki düzene
    /// uygulandığında birine eklenen kutu diğerinde de belirirdi.
    /// </remarks>
    public DuzenBandi Kopya()
    {
        var k = (DuzenBandi)MemberwiseClone();
        k.Nesneler = [.. Nesneler.Select(n => n.Kopya())];
        return k;
    }
}

public enum NesneTuru
{
    Bilinmeyen = 0,
    Yazi,     // TfrxMemoView
    Cizgi,    // TfrxLineView
    Sekil,    // TfrxShapeView
    Resim,    // TfrxPictureView
    Barkod,   // TfrxBarCodeView
}

/// <summary>
/// Barkod simgelemi.
/// </summary>
/// <remarks>
/// <para>
/// Taşınan 137 düzende barkod yok; bu tür tasarımcıda sıfırdan kurulan
/// belgeler için var (etiket, makbuz, ambar fişi).
/// </para>
/// <para>
/// İkisi de <b>çubuk</b> barkoddur ve ikisi birlikte iki ayrı işi karşılar:
/// Code 128 serbest metni, EAN-13 perakende ürün kodunu. Kare kodlar (QR,
/// DataMatrix) hata düzeltme kodlaması gerektirdiği, Code 39 ise desen tablosu
/// doğrulanamadığı için girmedi — yanlış bir tablo, okunmayan etiket demektir.
/// </para>
/// </remarks>
public enum BarkodTuru
{
    /// <summary>Code 128 — harf ve rakam, en yaygın olanı.</summary>
    Code128 = 0,

    /// <summary>EAN-13 — perakende ürün kodu; 12 ya da 13 rakam.</summary>
    Ean13 = 2,
}

/// <summary>Resmin kutuya nasıl sığdırılacağı.</summary>
public enum ResimUyumu
{
    /// <summary>Oranı korunarak kutuya sığdırılır; kenarlarda boşluk kalabilir.</summary>
    Sigdir = 0,

    /// <summary>Kutuyu tamamen doldurur; oran bozulur.</summary>
    Uzat,
}

public enum YatayHiza { Sol = 0, Orta, Sag, Yasli }

public enum DikeyHiza { Ust = 0, Orta, Alt }

/// <summary>Sayı/tarih biçimi — FastReport <c>DisplayFormat.Kind</c>.</summary>
public enum BicimTuru { Yok = 0, Sayi, Tarih, Saat, Metin, Boolean }

[Flags]
public enum CerceveKenari
{
    Yok = 0,
    Sol = 1,
    Sag = 2,
    Ust = 4,
    Alt = 8,
    Tumu = Sol | Sag | Ust | Alt,
}

public sealed class DuzenNesnesi
{
    public required string Ad { get; set; }
    public NesneTuru Tur { get; set; }

    // ----------------------------------------------------------- yerleşim
    // Bandın sol üst köşesine göre.

    public double SolPt { get; set; }
    public double UstPt { get; set; }
    public double GenislikPt { get; set; }
    public double YukseklikPt { get; set; }

    // -------------------------------------------------------------- içerik

    /// <summary>
    /// Kutunun metni. İçinde <c>[frxDBDataset1."Fisno"]</c> biçiminde alan
    /// başvuruları ve <c>[Page#]</c> gibi değişkenler olabilir; çözümü
    /// çizicinin işidir.
    /// </summary>
    public string? Metin { get; set; }

    /// <summary>
    /// Kutu tek bir alana bağlıysa o alanın adı (<c>DataField</c>). Metnin
    /// içindeki başvurudan farklıdır: FastReport ikisini birden taşır.
    /// </summary>
    public string? VeriAlani { get; set; }

    // -------------------------------------------------------------- resim

    /// <summary>
    /// Resim kutusunun içeriği — <c>data:image/png;base64,…</c> biçiminde.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Resim düzenin <b>içinde</b> durur, bir dosya yolu olarak değil. Yol
    /// verilseydi düzen tek başına taşınamazdı: düzeltme dosyası kaynak ağacında
    /// gezerken işaret ettiği logo başka bir kurulumda bulunamaz ve kâğıda boş
    /// bir kutu basılırdı. Bedeli dosya boyutu; rapor logoları birkaç KB'dır.
    /// </para>
    /// <para>
    /// Veri URI'si olarak saklanır çünkü tarayıcı onu doğrudan
    /// <c>&lt;img src&gt;</c>'e koyabiliyor; çizici baştaki başlığı atıp
    /// base64'ü çözer.
    /// </para>
    /// </remarks>
    public string? ResimVerisi { get; set; }

    public ResimUyumu ResimUyumu { get; set; }

    // --------------------------------------------------------- alt rapor

    /// <summary>
    /// Bu kutunun yerine akışa girecek sayfanın adı — FastReport
    /// <c>TfrxSubreport.Page</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Alt rapor ayrı bir belge değil, <b>başka bir sayfanın bantlarının bu
    /// noktada akışa girmesi</b>dir: cari hesap ekstresinin şablonunda kâğıdın
    /// üst yarısı ana sayfada, hareket dökümü ikinci sayfada durur ve ikincisi
    /// birincinin bandına gömülür.
    /// </para>
    /// <para>
    /// Dolu olan kutu <b>basılmaz</b>; yalnızca gömmenin nerede olacağını
    /// söyler. Dönüştürücü yer tutucu bir yazı kutusu bırakıyor — kutunun boyu
    /// bandın ne kadar yer tutacağını belirlediği için o kutu duruyor, metni
    /// kâğıda çıkmıyor.
    /// </para>
    /// </remarks>
    public string? AltRaporSayfasi { get; set; }

    // ------------------------------------------------------------- barkod

    public BarkodTuru BarkodTuru { get; set; }

    /// <summary>Çubukların altına okunabilir metin basılsın mı?</summary>
    public bool BarkodYazisi { get; set; } = true;

    // ------------------------------------------------------------- görünüm

    public string YaziTipi { get; set; } = "Arial";
    public double PuntoPt { get; set; } = 10;
    public bool Kalin { get; set; }
    public bool Egik { get; set; }
    public bool AltiCizili { get; set; }

    /// <summary>Yazı rengi, <c>#RRGGBB</c>. Boşsa siyah.</summary>
    public string? Renk { get; set; }

    /// <summary>Zemin rengi, <c>#RRGGBB</c>. Boşsa saydam.</summary>
    public string? ZeminRengi { get; set; }

    public YatayHiza Yatay { get; set; }
    public DikeyHiza Dikey { get; set; }

    public CerceveKenari Cerceve { get; set; }
    public double CerceveKalinligiPt { get; set; } = 1;
    public string? CerceveRengi { get; set; }

    // ------------------------------------------------------------- biçim

    public BicimTuru Bicim { get; set; }

    /// <summary>FastReport deseni — <c>%2.2n</c>, <c>dd.mm.yyyy</c>.</summary>
    public string? BicimDeseni { get; set; }

    public string? OndalikAyraci { get; set; }
    public string? BinlikAyraci { get; set; }

    // -------------------------------------------------------------- akış

    public bool KelimeKaydir { get; set; }

    /// <summary>İçerik sığmazsa kutu uzasın mı? (<c>CanGrow</c>/<c>StretchMode</c>)</summary>
    public bool Uzayabilir { get; set; }

    /// <summary>Metni kutuya sığdırmak için puntoyu küçült. (<c>AutoWidth</c>/<c>WordWrap</c> ile birlikte)</summary>
    public bool Sigdir { get; set; }

    /// <summary>
    /// Kutunun o anki hâlinin kopyası.
    /// </summary>
    /// <remarks>
    /// Geri alma yığını bunu kullanır. Bütün alanlar ya değer türü ya da
    /// değişmez dize olduğu için sığ kopya yeterli — alan listesi yazmaya
    /// gerek yok, dolayısıyla modele yeni bir alan eklendiğinde burası
    /// kendiliğinden doğru kalır.
    /// </remarks>
    public DuzenNesnesi Kopya() => (DuzenNesnesi)MemberwiseClone();

    /// <summary>
    /// Verilen kopyanın bütün alanlarını bu kutuya yazar.
    /// </summary>
    /// <remarks>
    /// Nesnenin kendisi değiştirilir, yerine yenisi konmaz: tuvaldeki seçim ve
    /// "değişmiş kutular" kümesi bu örneğe başvuruyla tutunuyor, değiştirilse
    /// ikisi de kopan bir başvuruyu göstermeye devam ederdi.
    /// <para>
    /// <b>Yeni bir alan eklendiğinde buraya da eklenmeli.</b> Eksik kalan alan
    /// geri alınamayan bir değişiklik demektir.
    /// </para>
    /// </remarks>
    public void YazUzerine(DuzenNesnesi k)
    {
        Tur = k.Tur;

        SolPt = k.SolPt;
        UstPt = k.UstPt;
        GenislikPt = k.GenislikPt;
        YukseklikPt = k.YukseklikPt;

        Metin = k.Metin;
        VeriAlani = k.VeriAlani;

        ResimVerisi = k.ResimVerisi;
        ResimUyumu = k.ResimUyumu;

        AltRaporSayfasi = k.AltRaporSayfasi;

        BarkodTuru = k.BarkodTuru;
        BarkodYazisi = k.BarkodYazisi;

        YaziTipi = k.YaziTipi;
        PuntoPt = k.PuntoPt;
        Kalin = k.Kalin;
        Egik = k.Egik;
        AltiCizili = k.AltiCizili;
        Renk = k.Renk;
        ZeminRengi = k.ZeminRengi;
        Yatay = k.Yatay;
        Dikey = k.Dikey;
        Cerceve = k.Cerceve;
        CerceveKalinligiPt = k.CerceveKalinligiPt;
        CerceveRengi = k.CerceveRengi;

        Bicim = k.Bicim;
        BicimDeseni = k.BicimDeseni;
        OndalikAyraci = k.OndalikAyraci;
        BinlikAyraci = k.BinlikAyraci;

        KelimeKaydir = k.KelimeKaydir;
        Uzayabilir = k.Uzayabilir;
        Sigdir = k.Sigdir;
    }
}
