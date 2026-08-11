# Pica.Reports örneği

Kütüphanenin nasıl gömüldüğünü gösteren en küçük Blazor uygulaması. Çalıştırın:

```
dotnet run --project ornek/Pica.Reports.Ornek
```

Sonra <http://localhost:5099>. Visual Studio'da başlangıç projesi seçip F5 de
yeterli.

## Uygulamanın tamamı

Örnek bilerek küçük tutuldu:

| Dosya | Ne yapar |
| --- | --- |
| `Program.cs` | `AddPicaReports()` — depo ve rapor aracı; hazır ekranları sunucuya tanıtır |
| `Components/Routes.razor` | Hazır ekranları yönlendiriciye tanıtır (`AdditionalAssemblies`) |
| `Components/Pages/VeriBagla.razor` | **Veriyi bağlayıp raporu açma** — Dapper satırlarının aynısı |
| `Components/Pages/Duzenler.razor` | `<DuzenListesi>` — kendi sayfanıza gömme örneği |
| `Components/Pages/Tasarimci.razor` | `<RaporTasarimci>` — kendi sayfanıza gömme örneği |

İki yol da gösteriliyor: kütüphanenin **hazır ekranları** (`/pica/…`) ve
bileşenleri **kendi sayfanıza gömmek**.

Dikkat edilecek üç şey:

- **Yükseklik.** Tasarımcı kendisine verilen yüksekliği doldurur, sayfa akışına
  göre uzamaz. Kabı yükseklik vermezse kâğıt görünmez (`ornek.css`).
- **Biçem `<head>` içinde.** Sayfanın kendi `HeadContent`'ine konursa gezinmeyle
  gelen ilk çizimde uygulanmaz ve tasarımcı bir an çıplak görünür.
- **`MapStaticAssets`, `UseStaticFiles` değil.** Kütüphanenin CSS ve JS'i pakete
  gömülüdür (`_content/Pica.Reports/…`); `UseStaticFiles` yalnızca `wwwroot`'a
  bakar ve o yolda 404 alırsınız. Ayrıca geliştirmede ortam **Development**
  olmalı (bkz. `Properties/launchSettings.json`).
- **Hazır ekranlar iki yerde birden tanıtılır.** `Routes.razor`'daki
  `AdditionalAssemblies` yönlendirici içindir, `Program.cs`'teki
  `AddAdditionalAssemblies` sunucu içindir. Yalnız birini yazarsanız
  `/pica/tasarim/…` adresi **404** döner — biri istemciyi, diğeri ilk isteği
  karşılar.

## Veriyle rapor açma

`/veri` sayfası "DesignRep" akışını gösteriyor: iki veri kümesi (`Fisler`,
`Ozet`) kuruluyor, rapor aracına bağlanıyor ve tasarım/önizleme ekranı
açılıyor.

```csharp
var veri = new RaporVerisi()
    .Ekle("Fisler", fisler)     // Dapper'ın döndürdüğü sözlük satırları
    .Ekle("Ozet", ozet)         // kendi sınıfınızın listesi
    .Degisken("KurumAdi", "…");

await Rapor.Onizle("iki-veri-kumesi", veri);
```

Küme adları düzendeki adlarla **birebir aynı** olmalı: bantlar
`VeriKumesi = "Fisler"` diyor, kutular `[Fisler."Tutar"]` yazıyor.

## Örnek düzenler

`Duzenler/` altında on düzen var; her biri kütüphanenin bir yanını gösteriyor.
Hepsi uydurma verilerle çalışır — araç çubuğundaki **Örnek veri** düğmesine
basınca kutular biçimlerinden geçmiş değerlerle dolar.

| Düzen | Gösterdiği |
| --- | --- |
| **Basit Liste** | En yalın hâl: sayfa başlığı, sütun başlıkları, veri bandı, sayfa altı, sayfa numarası |
| **Gruplu Liste** | Grup başlığı ve grup sonu; kırılım değişince başlık yinelenir, toplam basılır |
| **Sayfa Toplamlı Defter** | `SUM(…, 2)` her sayfada sıfırlanan toplam, `Nakil_`/`Genel_` ile nakli yekûn |
| **Fatura** | Gömülü resim, zeminli başlık hücreleri, rapor sonu toplamları, çerçeveli tutar |
| **Barkod Etiketi** | Etiket boyunda sayfa (100×50 mm), Code 128 ve EAN-13 |
| **Logolu Antet** | Her sayfada yinelenen antet ve alt bilgi |
| **Yatay Cetvel** | Yatay A4; sütunu çok olan cetveller |
| **İki Veri Kümesi** | İki bağımsız liste alt alta; alan ağacında iki küme ayrı görünür |
| **Biçimli Alanlar** | Sayı ve tarih desenleri yan yana — `%2.2n`, `%1.4f`, `dd.mm.yyyy` |
| **Serbest Belge** | Tek sayfalık belge: şekil, çizgi, çerçeveli kutular, uzayabilen alan, imza blokları |

Düzenler **ham** dosyalardır. Tasarımcıda kaydettiğiniz her değişiklik onların
yanına `{anahtar}.duzeltme.json` olarak yazılır; ham dosya değişmez. Denemek
için bir kutuyu oynatıp kaydedin, sonra klasöre bakın — ve "Orijinale dön" ile
geri alın.

## Önizleme var, PDF yok

Araç çubuğundaki **Önizle** düzeni veriyle sayfalara dizip gösterir — sayfa
kırılımı, yinelenen başlıklar, sayfa numarası, sayfa toplamı. Ama bu bir PDF
değildir: kütüphane **PDF üretmez**, hangi motorla basılacağı barındıran
uygulamanın işidir. Kendi uygulamanızda bir basım ucu varsa adres kalıbını
verin, tasarımcıya bir **PDF** düğmesi daha eklenir:

```razor
<RaporTasarimci Depo="Depo" SeciliAnahtar="@Anahtar"
                OnizlemeAdresi="/cikti/duzen/{0}.pdf" />
```

Çizici tarafını nasıl yazacağınız size kalmış; düzen modeli (`CetvelDuzeni`,
bantlar, kutular) ve basım sırası (`BantSirasi.BasimSirasi`) kütüphanede hazır.
