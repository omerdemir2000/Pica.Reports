# Katkı / Contributing

Bir **Papirus Yazılım Ltd. Şti.** ürünü — <https://www.papirusbilisim.com>

*Türkçe okuyorsanız aşağıdaki bölüm sizin; English below.*

## Geliştirme

```
dotnet build Pica.Reports.slnx
dotnet test src/Pica.Reports.Testleri
dotnet run --project ornek/Pica.Reports.Ornek     # http://localhost:5099
```

Sınamaların bir bölümü **gerçek düzen dosyaları** üzerinde çalışır. O dosyalar
bu depoda değil; yolu ortam değişkeniyle verirsiniz:

```
PICA_DUZENLER=C:\...\Cikti\Duzenler dotnet test
```

Değişken verilmezse o sınamalar sessizce atlanır — kütüphane tek başına da
sınanabilmeli.

## Dil

**Kod, yorumlar ve API adları Türkçedir** ve öyle kalacak: kütüphane Türkçe
muhasebe cetvelleri için yazıldı, alan sözcükleri (*cetvel*, *bant*,
*düzeltme*) çeviride anlamını yitiriyor. Belgelerin İngilizcesi `README.md`,
Türkçesi `README.tr.md`.

İngilizce bir API yüzeyi tartışmaya açık — sürüm 0.x iken mümkün. İsterseniz
bir konu (issue) açın.

## Yorum yazma

Bu depoda yorumlar **ne** yaptığını değil **neden** öyle yaptığını anlatır.
Bir kuralın gerekçesi (neden 4 punto alt sınır, neden ham düzen ile düzeltme
ayrı, neden bant sırası dosya sırası değil) koda gömülüdür; kaldırmayın,
değiştiriyorsanız gerekçeyi de güncelleyin.

## Sürüm

[SemVer](https://semver.org/lang/tr/). 0.x sürümlerinde API değişebilir.
Değişiklikler `CHANGELOG.md`'ye yazılır.

`PackageId` **Pica.Reports** kalmalı: statik varlıklar `_content/{PackageId}/`
altından servis ediliyor ve JS modülü o yolla yükleniyor. Değişirse sürükleme
sessizce çalışmaz.

---

## Contributing (English)

Build, test and run as shown above. Note that **the code, comments and public
API are in Turkish** — this is deliberate; see the language section above.
English documentation lives in `README.md`.

Comments here explain **why**, not what. If you change a rule, update the
reasoning next to it.

Issues and pull requests are welcome. By contributing you agree that your
contribution is licensed under the MIT licence of this project.
