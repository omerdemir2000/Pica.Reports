# Changelog

Bu dosya sürümler arasındaki değişiklikleri anlatır. Biçim
[Keep a Changelog](https://keepachangelog.com/tr/1.1.0/) esaslı, sürümleme
[SemVer](https://semver.org/lang/tr/) — **0.x sürümlerinde API değişebilir**.

## [Yayımlanmadı]

## [0.10.2] — 2026-09-22

Dördü de gerçek bir bordro külliyatında kâğıda yanlış basan hatalar; üçü
sessizdi, yani ekranda her şey doğru görünüyordu.

### Düzeltilen

- **`%g` deseni tanınıyor.** Delphi'nin "genel" biçimi: anlamsız ondalık
  sıfırlar atılır, binlik ayraç konmaz — tam gün `30`, yarım gün `29,5`.
  Taşınan külliyatta 60 kutu bu deseni kullanıyor ve hepsi bugüne kadar iki
  ondalığa düşürülüp `30,00` basıyordu.
- **Tanınmayan desende varsayılan biçim uydurulmuyor.** `Bicimleme.SayiDeseni`
  artık `null` dönüyor ve `Bicimle` varsayılan yazıma düşüyor. Eskiden
  desteklenmeyen her desen sessizce `#,##0.00`'a çevriliyordu: hata kâğıtta
  makul görünen ama yanlış bir sayı olarak çıkıyor, fark edilmiyordu.
- **`Sigdir` çalışıyor.** Alan modelde vardı, hiçbir yerde okunmuyordu: metin
  sığmayınca punto düşmüyor, kutu alt satıra kırıyordu. Dar para kutusuna
  sığmayan `98.765,43` iki satır basınca tablonun bütün satır hizası bozuluyor.
  - **Sığdırma kelime kaydırmayı yener**: ikisi birlikte işaretliyse kutu
    kırmaz, küçültür. Şablonların bir kısmı ikisini birden işaretli getiriyor
    ve kırma kazandığı için sığdırma hiçbir şey yapmıyordu.
  - Ölçü kestirmedir (bkz. `Sigdirma`), gerçek yazı tipi ölçümü değil:
    kütüphane tarayıcıda da PDF motorunda da çalışmak zorunda. Önemli olan
    tasarımcı, önizleme ve çizicinin **aynı** sayıyı bulması.
- **Düzeltmedeki bilinmeyen alan sessizce yutulmuyor.** `PuntoPt` yerine
  `Punto` yazan bir düzeltme yok sayılıyor, ortada bir hata da görünmüyordu.
  Tanınmayan alanlar artık `Uygula`'nın döndürdüğü listeye giriyor ve
  tasarımcının uyarı panelinde görünüyor. Dosya reddedilmiyor: bir yazım
  hatası yüzünden doğru yazılmış öteki yüz satırı da atmak, elindeki tek
  çıktıyı kaybetmek olurdu.
- **Elle yazılmış ölçü her kayıtta kaymıyor.** Düzeltme üç ondalığa
  yuvarlanırken `15,0821` → `15,082` oluyor, sapma tam eşitlik eşiği kadar
  (0,0005 pt) çıkan değerlerde fark "gerçek" sayılıyordu; betikle üretilmiş
  oransal ölçekleme düzeltmeleri tam bu aralıkta değer taşıyor. Yuvarlama dört
  basamağa indi (`DuzenDuzeltmesi.Basamak`), hata artık eşiğin onda biri.

## [0.10.1] — 2026-08-30

> 0.10.0 yayımlanmadı; alt rapor desteği bu sürümle birlikte çıkıyor.

### Eklenen

- **Zengin metin nesnesi** (`NesneTuru.ZenginMetin`): kutunun değeri **im
  olarak** basılır — kalın, eğik, altı çizili, paragraf, liste ve tablo
  korunur. Delphi'nin `TfrxRichView` karşılığı.
  - **Kütüphane RTF okumaz.** Delphi tarafı zengin metni RTF olarak saklıyor
    ama RTF'i çözmek barındıran uygulamanın işi; kendi ayrıştırıcısını taşımak
    bir kod sayfası tablosunu ve bakımını da taşımak demekti. Buraya gelen
    değer im'dir.
  - İçerik veritabanından geldiği için `ZenginMetin.Temizle` **izin
    listesinden** geçiriyor: biçimleme etiketleri kalır, `script`/`style`/
    `iframe` gövdesiyle atılır, `class` dışındaki bütün öznitelikler düşer
    (`style`, `href`, `on…` hiç geçmez). Süzülmeseydi bir rapor metnindeki
    `<script>` raporu açan herkeste çalışırdı.
  - İzin listesinde olmayan bir etiket düşer ama **metni kalır**; kapatılmamış
    etiketler sonda kapatılır.
  - Tuvalde biçimli metin basılmaz, başvurusu görünür: tuval yerleşim
    yüzeyidir ve zengin metnin içeriği neredeyse her zaman veriden gelir.
- **Onay kutusu nesnesi** (`NesneTuru.OnayKutusu`): matbu formların
  işaretlenen kareleri. Delphi'nin `TfrxCheckBoxView` karşılığı.
  - Üç im: onay, çarpı, dolu kare (`OnayBicimi`). Kare **her zaman** çizilir —
    boş bir kare "işaretlenmemiş" bilgisidir ve kâğıtta görünmelidir.
  - **Veriye bağlanabilir**: `Metin` bir başvuru taşıyorsa çözülen değer
    `OnayKutusu.Isaretli` ile yorumlanır — `1`, `true`, `evet`, `E`, `X` ve
    sıfırdan farklı her sayı işaretli sayılır. Karşılaştırma kültürden
    bağımsızdır (Türkçe `I/ı` tuzağı).
  - Çizim barkodda olduğu gibi **SVG**: tuval de çizici de aynı dizeyi basar.
    `viewBox` kare, dolayısıyla dikdörtgen bir alanda kutu ezilmez, ortalanır.
- Örnek uygulamaya `onam-formu` düzeni: biçimli paragraf, işaretli/işaretsiz
  kutular ve veriye bağlı onay bir arada.
- **Alt rapor**: bir bandın içindeki kutu başka bir sayfayı gösterebiliyor
  (`DuzenNesnesi.AltRaporSayfasi` → `DuzenSayfasi.Ad`) ve o sayfanın gövde
  bantları kutunun durduğu noktada akışa giriyor. Gömülen bantlar sayfa
  kırılımına katılır; hedefin kendi sayfa başlığı/sayfa sonu bantları ile yer
  tutucu kutu basılmaz. İç içe gömme çalışır, `A → B → A` döngüsü kesilir.
  Taşınan 2.109 düzenin 109'unda alt rapor var — cari hesap ekstresi, mutabakat
  yazısı, gün sonu ve vardiya raporu bunlarla basılıyor.
- Örnek uygulamaya `alt-raporlu-ekstre` düzeni ve onu **gerçek veriyle** açan
  `/ekstre` sayfası: iki küme (`Cari`, `Hareket`) ve iki değişken bağlanıyor,
  hareket sayısı seçilerek gömülen dökümün kâğıdı kırması görülebiliyor.

### Değişen

- `SayfaDizici.Diz` artık tek sayfayı değil **düzenin tamamını** alıyor:
  `Diz(duzen, sayfaIndeksi, veri, ornek)`. Alt rapor hedefi düzenin başka bir
  yerinde duruyor, tek sayfayla bulunamazdı.
- `Onizleme` bileşeninin `Sayfa` parametresi yerine `Duzen` ve `SayfaIndeksi`
  geldi — aynı sebeple.
- Önizlemenin cetvel seçicisi alt rapor hedefi olan sayfaları listelemiyor; o
  sayfalar ayrı cetvel değil, başka bir sayfanın parçası. Tasarımcıda
  listelenmeye devam ediyorlar.

## [0.9.0] — 2026-08-11

İlk genel sürüm. Kütüphane PBM2027 uygulamasının içinde geliştirildi ve bu
sürümle kendi deposuna çıktı.

### Tasarımcı

- Bantlar ve kutular kâğıt üzerinde: sürükleme, boyutlandırma, hizalama,
  ızgaraya yaslama, geri al/yinele.
- **Bant kanalı**: bantları kâğıttaki basım sırasıyla, görevleriyle ve
  bağımlılıklarıyla listeler — düzen dosyasına bakarak görülemeyen tek şey.
- Bant yüksekliği fareyle alt kenardan çekilir; alt sınır 4 punto ve en
  alttaki kutunun altı.
- Sol **araç paleti**: nesne türleri, bant türleri ve veri alanı ağacı.
  Paletten kâğıda sürükle-bırak.
- **Sayfa düzeni**: kâğıt boyu (A3…A6, Letter, Legal, 100×50 etiket, özel),
  dikey/yatay, kenar boşlukları, sütun.

### Nesneler

- Yazı, çizgi, şekil.
- **Resim**: düzenin içinde base64 olarak saklanır, dosya yolu değil.
- **Barkod**: Code 128 ve EAN-13. Çubuklar SVG olarak üretilir; tasarımcı ile
  çıktı aynı dizeyi kullanır.

### Veri

- `RaporVerisi` ile birden çok veri kümesi; satırlar Dapper'ın döndürdüğü
  sözlükler, kendi sınıflarınız ya da sözlük dizisi olabilir.
- Alan ağacı düzenden, uygulamanın bildirdiği katalogdan ve bağlanmış veriden
  birleşir.
- **Örnek veri** kipi: kutular biçimden geçmiş uydurma değerlerle görünür.

### Önizleme ve yazdırma

- Düzen veriyle sayfalara döşenir: sayfa kırılımı, yinelenen başlıklar, sayfa
  altı, `[Page#]`/`[TotalPages#]`, `SUM(…, 2)` sayfa toplamı ve nakli yekûn.
- Yazdırma tarayıcıya verilir; kâğıt ölçüsü `@page` ile bildirilir.

### Barındırma

- `AddPicaReports()` ile depo ve rapor aracı kaydolur.
- `IRaporAraci.TasarimAc/Onizle/Yazdir` — Delphi'deki `DesignReport`
  karşılığı.
- Hazır ekranlar: `/pica/duzenler`, `/pica/tasarim/{anahtar}`,
  `/pica/onizleme/{anahtar}`.
- `DosyaDuzenDeposu`: klasördeki JSON dosyalarıyla çalışan hazır depo.

### Model

- Ham düzen ile **düzeltme** ayrı: tasarımcı ham dosyaya dokunmaz, farkı
  `{anahtar}.duzeltme.json` olarak yazar. Dönüşüm yinelendiğinde emek
  kaybolmaz.
- Düzeltme kutu alanlarını, bant alanlarını, sayfa ayarlarını, eklenen ve
  silinen kutu/bantları taşır.

### Sınamalar

- 164 sınama kütüphanenin sözleşmesini denetler.
- Gerçek düzen külliyatı üzerinde çalışan 400'ü aşkın sınama daha var; yol
  `PICA_DUZENLER` ortam değişkeniyle verilir (bkz. README).
