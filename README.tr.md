<img src="appicon.svg" alt="Papirus Yazılım" width="72" align="right" />

# Pica.Reports — Blazor Rapor Tasarım Aracı

*Türkçe · [English](README.md)*

Bantlı rapor düzenleri için Blazor tasarım bileşeni: tarayıcıda çalışan bir
rapor tasarımcısı, veri alanı ağacı, önizleme ve yazdırma.

Bir **[Papirus Yazılım Ltd. Şti.](https://www.papirusbilisim.com)** ürünüdür.
MIT lisanslıdır.

Delphi/FastReport (`.fr3`, `.dfm`) raporlarını .NET'e taşırken şablonlar
genellikle bir kez JSON'a çevrilir ve orada kalır. Sonrasında bir kutuyu iki
punto sola almak için kaynak koda inmek gerekir. Bu bileşen o adımı ekrana
taşır: düzeni kâğıt üzerinde gösterir, kutuları sürükleterek düzenletir ve
sonucu **üretilmiş dosyanın yanına bir düzeltme dosyası olarak** yazar.

Bağımlılığı yok: CSS çatısı, ikon paketi, PDF motoru istemez. Kendi CSS'ini ve
satır içi SVG ikonlarını taşır.

> **Ad hakkında.** *Pica* tipografide bir ölçü birimidir — 1/6 inç, 12 punto —
> ve bu kütüphanenin bütün ölçüleri puntodur. Ad, aynı sözcüğü taşıyan başka
> ürünlerle bir bağ ya da ortaklık iddia etmez.

## Önce çalıştırıp görün

Depoda çalışan bir örnek uygulama var — on örnek düzenle birlikte:

```
dotnet run --project ornek/Pica.Reports.Ornek
```

<http://localhost:5099> · ayrıntı: [ornek/Pica.Reports.Ornek/README.md](ornek/Pica.Reports.Ornek/README.md)

## Kurulum

```xml
<PackageReference Include="Pica.Reports" Version="0.9.0" />
```

Biçem dosyasını sayfaya ekleyin:

```razor
<link rel="stylesheet" href="_content/Pica.Reports/rapor-tasarim.css" />
```

> **Paket adı değiştirilmemeli.** Statik varlıklar `_content/{PaketAdı}/`
> altından servis edilir ve bileşen JS modülünü
> `./_content/Pica.Reports/tasarimci.js` adresinden yükler. `PackageId` başka
> bir şey yapılırsa sürükleme sessizce çalışmaz — hata da vermez, çünkü modül
> yüklenemediğinde yalnızca olay dinleyicileri kurulmamış olur.

## En kısa kullanım

```csharp
// Program.cs
builder.Services.AddPicaReports(o => o.Duzenler = "Raporlar");

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode()
   .AddAdditionalAssemblies(typeof(IRaporAraci).Assembly);   // hazır ekranlar
```

```razor
@* Routes.razor *@
<Router AppAssembly="typeof(Program).Assembly"
        AdditionalAssemblies="new[] { typeof(RaporAraci).Assembly }">
```

Sonra kendi ekranınızdan:

```razor
@inject IRaporAraci Rapor

var veri = new RaporVerisi()
    .Ekle("Fisler", await baglanti.QueryAsync("select * from fis"))   // Dapper
    .Ekle("Ozet", ozetSatirlari)                                      // kendi sınıfınız
    .Degisken("KurumAdi", kurum.Ad);

await Rapor.TasarimAc("fatura", veri);   // tasarım ekranı  (DesignReport karşılığı)
await Rapor.Onizle("fatura", veri);      // önizleme
```

Hazır ekranlar: `/pica/duzenler`, `/pica/tasarim/{anahtar}`,
`/pica/onizleme/{anahtar}`. Kendi adres düzeninizi istiyorsanız bileşenleri
doğrudan sayfanıza koyun (aşağıda).

> **İki yerde birden tanıtmak gerekir.** Hazır ekranlar ayrı bir derlemede:
> `Routes.razor`'daki `AdditionalAssemblies` yönlendirici içindir,
> `AddAdditionalAssemblies` ise sunucu içindir. Biri eksikse ilk istek 404
> döner.

## Veri kaynakları

Bir raporda **birden çok veri kümesi** olabilir; eşleşme **ada göredir**.
Düzendeki bant `VeriKumesi = "Fisler"` diyorsa, kümeyi o adla vermelisiniz:

```csharp
var veri = new RaporVerisi()
    .Ekle("Fisler", fisSatirlari)      // IEnumerable<dynamic> (Dapper), IEnumerable<T> ya da sözlük dizisi
    .Ekle("Ozet", ozetSatirlari)
    .Tanit("Personel", "Ad", "Soyad")  // verisi yok, yalnızca alan adları — tasarım için
    .Degisken("Yil", 2027);            // satır dışı kutular buradan okur
```

Kütüphane satırın **türüne değil şekline** bakar: sözlükse anahtarlar alan
adıdır (Dapper böyle döndürür), nesneyse genel özellikler alan olur. Boş bir
listede alanlar `T`'den çıkarılır — sorgu hiç satır döndürmese bile alan ağacı
dolu kalır.

Veri iki yere birden gider: **alan ağacına** (sürükleyip kutu oluşturursunuz)
ve **önizlemeye** (gerçek satırlarla basılır).

## Önizleme

Araç çubuğundaki **Önizle**, düzeni veriyle sayfalara dizip gösterir: veri
bandı satır sayısı kadar yinelenir, sayfa dolunca kırılır, sayfa altı kâğıdın
altına yaslanır, `[Page#]`/`[TotalPages#]` yerine oturur, `SUM(…, 2)` her
sayfada sıfırlanır.

**Bu bir PDF değildir.** Kütüphane PDF üretmez; tarayıcı ile PDF motoru aynı
satır kırmasını yapmaz. Yerleşim doğrudur, harflerin tam pikseli değil. Kendi
PDF çıktınız varsa adres kalıbını verin (`OnizlemeAdresi`), araç çubuğunda
ayrıca bir **PDF** düğmesi çıkar.

### Yazdırma

Önizlemenin **Yazdır** düğmesi tarayıcının yazdırma kutusunu açar; basılan şey
önizlemenin kendisidir. Kâğıdın ölçüsü yazıcıya `@page` ile bildirilir, yani
etiket boyunda ya da yatay bir düzen doğru kâğıda basılır. Ekrana ait ne varsa
(araç çubukları, palet, gölge, yakınlaştırma, kılavuz çizgileri) kâğıda
çıkmaz; her sayfa kendi yaprağına gider ve çıktı 1/1 basılır.

`Rapor.Yazdir("fatura", veri)` önizlemeyi `?yazdir=1` ile açar: ekran gelir
gelmez yazdırma kutusu çıkar.

Listedeki satırlarda da **Önizle** ve **Yazdır** düğmeleri var — yalnızca ilgili
geri çağrıyı bağladıysanız görünürler:

```razor
<DuzenListesi Depo="Depo"
              Duzenle="a => Rapor.TasarimAc(a)"
              Onizle="a => Rapor.Onizle(a)"
              Yazdir="a => Rapor.Yazdir(a)" />
```

## Sayfa düzeni

Araç çubuğundaki **Sayfa** düğmesi kâğıdı düzenletir: hazır boylar (A3, B5, A4,
Letter, Legal, A5, A6, 100×50 etiket), **dikey/yatay**, kenar boşlukları ve
sütun sayısı. Ölçüler ekranda milimetre girilir, düzende punto saklanır.

Yön kâğıdın enini ve boyunu yer değiştirir — ekranda değil **kâğıtta** karşılığı
olan bir ayardır. Kutular yerinde kalır: taşanları kendiliğinden taşımak,
koyduğunuz hizayı bozardı; taşma tuvalde görünür.

Sayfa ayarları da düzeltme dosyasına yazılır, "Orijinale dön" ile geri alınır.

## Bileşenleri kendi sayfanıza koymak

Bileşen düzenlerin nerede durduğunu bilmez; bir depo verirsiniz:

```csharp
public interface IDuzenDeposu
{
    Task<IReadOnlyList<DuzenKaydi>> ListeleAsync(CancellationToken iptal = default);
    Task<CetvelDuzeni?> HamGetirAsync(string anahtar, CancellationToken iptal = default);
    Task<DuzenDuzeltmesi?> DuzeltmeGetirAsync(string anahtar, CancellationToken iptal = default);
    Task DuzeltmeKaydetAsync(string anahtar, DuzenDuzeltmesi? duzeltme, CancellationToken iptal = default);
    Task OlusturAsync(string anahtar, CetvelDuzeni duzen, CancellationToken iptal = default);
}
```

Klasördeki JSON dosyalarıyla çalışan hazır bir uygulaması var
(`DosyaDuzenDeposu`); `AddPicaReports()` onu kendiliğinden kaydeder. Kendi
deponuzu vermek için `AddPicaReports<KendiDepom>()`.

İki bileşen var — **liste** ve **tasarımcı** — ve ikisi ayrı sayfaya konur.
Hangi adresin hangisine gittiğine uygulama karar verir; bileşenler gezinmez,
haber verir:

```razor
@* Liste — /duzen-tasarim *@
@inject IDuzenDeposu Depo

<DuzenListesi Depo="Depo"
              Duzenle="a => Gezinme.NavigateTo($&quot;/duzen-tasarim/{a}&quot;)" />
```

```razor
@* Tasarımcı — /duzen-tasarim/{Anahtar} *@
@page "/duzen-tasarim/{Anahtar}"
@inject IDuzenDeposu Depo

<RaporTasarimci Depo="Depo"
                SeciliAnahtar="@Anahtar"
                Kapat="() => Gezinme.NavigateTo(&quot;/duzen-tasarim&quot;)"
                OnizlemeAdresi="/duzen/{0}.pdf" />
```

Listedeki **yeni düzen** düğmesi ad ve anahtar sorar, `YeniDuzen.Bos` ile A4
boyunda iki bantlı boş bir düzen üretir, `OlusturAsync` ile yazdırır ve
ardından `Duzenle` ile onu açtırır. Depo `OlusturAsync` içinde var olan bir
anahtarın üstüne **yazmamalı**, hata atmalıdır.

`OnizlemeAdresi` boş bırakılırsa PDF önizleme düğmesi görünmez. Bileşen PDF
üretmez — hangi motorla basıldığı barındıran uygulamanın işidir. `Kapat`
bağlanmazsa tasarımcının araç çubuğundaki "Kapat" düğmesi hiç görünmez; tek
ekran olarak da gömülebilir.

## Nesne türleri

| Tür | Ne yapar |
| --- | --- |
| Yazı | Metin ve veri alanı; biçim (sayı/tarih deseni) buna uygulanır |
| Çizgi | Tek kenarlı kutu — çizginin çerçevesi yoktur, kendisi çizgidir |
| Şekil | Çerçeve ve zemin |
| Resim | Düzenin **içinde** saklanan resim (veri URI'si) |
| Barkod | Code 128 ya da EAN-13 |

Nesneler soldaki araç paletinden eklenir. İki yol var:

- **Sürükle bırak** — düğmeyi kâğıdın üstüne sürükleyin; nesne, bıraktığınız
  bandın bıraktığınız noktasına girer (ızgara açıksa ona yaslanır, kutu bandın
  ve kâğıdın dışına taşmaz). Hedef bant bırakmadan önce kesikli çizgiyle
  işaretlenir.
- **Tıkla** — nesne seçili banda (ya da seçili kutunun bandına) eklenir.

Resim dosya yolu değil, base64 olarak düzenin içinde durur: düzeltme dosyası
tek başına taşınabilsin diye. Yol verilseydi işaret ettiği logo başka bir
kurulumda bulunamaz ve kâğıda boş kutu basılırdı.

Barkod çubukları kütüphanede üretilir ve **SVG** olarak verilir; tuval onu
doğrudan gösterir, çizici de aynı dizeyi PDF motoruna verir. Kodlanamayan bir
değer (Code 128'e Türkçe harf, EAN-13'e yanlış sağlama) **boş kutu** basar —
uydurma bir barkod, okutulduğunda başka bir şey söyleyen bir etiket demektir.

## Alan ağacı

Paletin "Veri" bölümü, düzenin veri kümelerini ve alanlarını ağaç olarak
gösterir. Alanı kâğıda **sürükleyip bırakınca** o alana bağlı bir yazı kutusu
oluşur: metni `[MuhFis."Aciklama"]`, `VeriAlani` alanı `Aciklama`, biçimi de
addan tahmin edilir (tutar → sağa yaslı `%2.2n`, tarih → `dd.mm.yyyy`).

Ağaç düzenin **kendisinden** çıkarılır — bantların bağlı olduğu kümeler ve
kutulardaki başvurular. Bu liste eksiksiz olamaz: düzende hiç geçmeyen bir alan
görünmez. Uygulama gerçek listeyi biliyorsa `VeriKumeleri` ile verebilir; iki
liste birleşir ve düzendeki alanlar hiçbir zaman düşürülmez.

```razor
<RaporTasarimci Depo="Depo" SeciliAnahtar="@Anahtar"
                VeriKumeleri="@([new VeriKumesiTanimi("MuhFis", ["FisNo", "Tarih", "Tutar"])])" />
```

## Örnek veri

Araç çubuğundaki **Örnek veri** düğmesi kutuları başvurularıyla değil,
biçimden geçmiş örnek değerlerle gösterir: `[borc]` yerine `1.234,56`,
`[tarih]` yerine `15.03.2027`. Böylece `%2.2n` deseninin ne yaptığı, sayının
kutuya sığıp sığmadığı ve ondalık ayracının doğruluğu gerçek veri olmadan
görünür.

Değerler `OrnekVeri` sınıfından gelir, **değişmezdir** (rastgele değil, saate
bağlı değil) ve biçimleri `Bicimleme` uygular — çizicinin kullandığı sınıfın
aynısı. Barındıran uygulama aynı sınıfı kendi önizleme çıktısını beslemek için
de kullanabilir.

## Ham düzen ve düzeltme

Bu ayrım kütüphanenin merkezinde:

- **Ham düzen** (`{anahtar}.json`) dönüştürücünün ürettiği dosyadır. Dönüşüm
  tekrarlandığında üzerine yazılır.
- **Düzeltme** (`{anahtar}.duzeltme.json`) elle yapılan değişikliklerin
  farkıdır. Dönüştürücü ona dokunmaz.

Tasarımcı ham düzeni **hiç değiştirmez**; kaydettiğinde ham hâlle çalışma
kopyası arasındaki farkı çıkarıp düzeltme dosyasına yazar. Bunun iki sonucu var:
düzen yeniden dönüştürüldüğünde emeğiniz kaybolmaz, ve bir alanı özgün değerine
geri döndürmek onu dosyadan da düşürür.

Düzeltme şunları taşır: kutu alanları, bant alanları, eklenen ve silinen
kutular, eklenen ve silinen bantlar. Yani ham düzeni terk etmeyi gerektiren bir
değişiklik yok — üretilmiş dosya her zaman olduğu gibi kalabilir.

Bantların ve kutuların eşleşmesi **ada göredir**; tasarımcı yeni ad üretirken
düzendeki bütün adlara bakar. Bandın **türü ve dikey konumu** düzeltilemez:
ikisi de basım sırasını belirler (aşağıya bakın) ve değiştirmek raporu yeniden
tasarlamak demektir. Yeni bir bant eklerken konumu seçilebilir — bant, o anda
seçili olan bandın hemen altına girer.

## Bant sırası

Bir FastReport şablonunda **dosyadaki bant sırası kâğıttaki sıra değildir**;
bantlar türlerine göre basılır ve gövde bantları tasarımdaki dikey konumlarına
göre sıralanır. Tuvalin solundaki kanal bantları kâğıttaki sırayla, görevleriyle
ve bağımlılıklarıyla listeler — düzenin JSON'una bakarak görülemeyecek tek şey
budur.

Sıra `BantSirasi.BasimSirasi(sayfa)` ile hesaplanır. Kendi çiziciniz de bunu
kullanmalı; iki taraf ayrı ayrı hesaplarsa tasarımcı er geç kâğıtta olmayan bir
sıra gösterir.

## Kısayollar

| Tuş | İş |
| --- | --- |
| Tıkla | Kutuyu seç |
| Ctrl + tıkla | Seçime ekle / çıkar |
| Sürükle | Seçili kutuları taşı (önce seçmek gerekir) |
| Tutamak sürükle | Boyutlandır (tek seçimde) |
| Ok tuşları | Bir ızgara adımı kaydır |
| Shift + ok | On adım kaydır |
| Ctrl + Z / Ctrl + Y | Geri al / yinele |
| Bandın alt kenarını sürükle | Bant yüksekliği |

Kanaldaki bir banda tıklamak onu seçer; bant seçiliyken soldaki paletin
**nesne** düğmeleri o banda kutu ekler — ya da düğmeyi doğrudan kâğıda
sürükleyip bırakırsınız. **Bant** düğmeleri yeni bandı seçili bandın hemen
altına koyar.

**Bandın yüksekliği fareyle çekilir**: alt kenarına gelin (imleç değişir) ve
aşağı yukarı sürükleyin. Izgara açıksa ona yaslanır. İki alt sınır var:

- **4 punto** — daha aşağı inilse bandın tutulacak kenarı kalmaz ve bir daha
  büyütülemezdi. Bandı hiç bastırmamak isteyen yüksekliği panelden `0` yazar.
- **En alttaki kutunun altı** — bandın basılacak yüksekliği zaten ona göre
  hesaplanıyor; daha aşağı çekmek kâğıtta hiçbir şeyi değiştirmez, yalnızca
  değiştirdiğinizi sanırsınız.

Kesin bir değer isterseniz bant panelindeki yükseklik kutusuna yazın; ikisi de
aynı geri alma adımından geçer.

## Ölçü birimi

Her yer **puntodur** (1/72 inç) — PDF'in kendi birimi. Ekranda `punto × 96/72`
ile piksele çevrilir, yakınlaştırma CSS `transform: scale()` ile yapılır.
Milimetre saklanmaz: okurken kolaydır ama her dönüşümde küsurat üretir ve iki
sütunun kenarı kâğıtta üst üste binmez.

## Tuval baskı önizlemesi değildir

Tarayıcı ile PDF motoru aynı satır kırmasını yapmaz. Tuval **yerleşim
yüzeyidir**: kutunun nereye konduğunu gösterir, ne bastığını değil. Doğruluğun
ölçüsü yanındaki PDF önizlemesi olmalı.

## Temalar

Bileşen `<html data-theme="dark">` özniteliğini ve işletim sistemi tercihini
izler. Renkleri `--rt-*` CSS değişkenleriyle kendi tasarım sisteminize
bağlayabilirsiniz:

```css
/* .rt-kok iki ekranın da köküdür — liste ve tasarımcı aynı değişkenleri
   kullanır. */
.rt-kok {
    --rt-panel: var(--benim-panel-rengim);
    --rt-vurgu: var(--benim-vurgum);
}
```

Kâğıdın rengi bilerek tema dışıdır: çıktı beyaz kâğıda basılıyor ve tasarımcı
onu koyu temada gri gösterirse yerleşim kararları yanlış zeminde verilir.

## Sınamalar

```
dotnet test
```

173 sınama kütüphanenin kendi sözleşmesini denetler ve her yerde çalışır.

Bunlara ek olarak **gerçek düzen dosyaları** üzerinde çalışan bir küme daha var:
düzeltme çıkarımının kayıpsız olduğunu ve dokunulmamış bir düzenin sahte
düzeltme üretmediğini doğruluyor. Dosyalar bu depoda değil — barındıran
uygulamanın `Cikti/Duzenler` klasöründeler — ve bulunamazlarsa o sınamalar
**sessizce atlanır**. Yol ortam değişkeniyle verilir:

```
PICA_DUZENLER=D:\...\Uygulama\Cikti\Duzenler dotnet test
```

Bu ayrım bilinçli: kütüphane tek başına da sınanabilmeli, ama gerçek külliyat
elde varken onun üzerinden geçmemek de saçma olurdu.

## Diller

Arayüz **İngilizce (varsayılan) ve Türkçe** konuşuyor. Yerelleştirme .NET'in
kendi düzeniyle: `.resx` kaynakları ve uydu derlemeler. Dil
`CultureInfo.CurrentUICulture`'dan geliyor — yani uygulamanızın zaten verdiği
karardan:

```csharp
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("tr")
    .AddSupportedCultures("en", "tr")
    .AddSupportedUICultures("en", "tr"));
```

Kütüphanenin kendi dil ayarı **yok**: olsaydı iki ayar birbiriyle çelişirdi.
Blazor Server'da devrenin kültürü ilk istekten miras kaldığı için dil
değiştirmek tam sayfa yenilemesi ister; örnek uygulama bunun bir yolunu
gösteriyor (çerez + küçük bir uç).

Kültür **biçimi de** belirliyor: `1.234,56` mı `1,234.56` mı, tarih `gg.AA.yyyy`
mı `MM/dd/yyyy` mi — hepsi aynı ayardan.

**Yeni dil eklemek** tek dosya demek: `Kaynak.resx`in yanına
`Kaynak.<dil>.resx`. Kod değişmiyor. Çevirisi olmayan metin İngilizce görünür,
ekran bozulmaz.

## Katkı ve lisans

MIT lisanslı. Telif **Papirus Yazılım Ltd. Şti.** —
<https://www.papirusbilisim.com>

Katkı için [CONTRIBUTING.md](CONTRIBUTING.md), sürüm geçmişi için
[CHANGELOG.md](CHANGELOG.md).

Kod ve yorumlar Türkçedir; API adları da öyle (`CetvelDuzeni`,
`IDuzenDeposu`). Bu bilinçli bir tercih, eksik değil.
