# Changelog

Bu dosya sürümler arasındaki değişiklikleri anlatır. Biçim
[Keep a Changelog](https://keepachangelog.com/tr/1.1.0/) esaslı, sürümleme
[SemVer](https://semver.org/lang/tr/) — **0.x sürümlerinde API değişebilir**.

## [Yayımlanmadı]

### Eklenen

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
