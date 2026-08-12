# Yayın adımları

Bu dosya **Papirus Yazılım** içindir: depoyu GitHub'a açmak, NuGet'e paket
göndermek ve arama sonuçlarında görünmek için yapılacaklar. Bir kez yapılır,
sonra sürüm etiketi atmak yeter.

## 1. GitHub deposu — YAPILDI

<https://github.com/omerdemir2000/Pica.Reports> · **Public** · varsayılan dal
`master`.

Depo adı `pica-reports` değil **`Pica.Reports`** oldu (NuGet paket adıyla
birebir), dal da `main`e çevrilmedi. İkisini birden değiştirmek isterseniz
GitHub → Settings → *Repository name* / *Default branch*; ad değişince eski
adres yeni adrese yönlenir ama `git remote set-url` yapmak yine de gerekir.

> **Dal adı ile iş akışı bağlı.** `.github/workflows/ci.yml` `master` ve `main`
> dallarını birden dinliyor; dalı yeniden adlandırırsanız CI yine çalışır.

## 2. Deponun görünen yüzü (About) — YAPILDI

Description, website ve topics API'den yazıldı. Değiştirmek için depo
sayfasında sağ üstteki ⚙ **About** düğmesi.

**Description** — arama sonuçlarında görünen satır, iki dili birden taşıyor:

```
Blazor rapor tasarım aracı — banded report designer for Blazor. Papirus Yazılım Ltd. Şti.
```

**Website**

```
https://www.papirusbilisim.com
```

**Topics** (etiketler — GitHub aramasında bulunmayı bunlar sağlıyor):

```
blazor  report-designer  reporting  dotnet  csharp  aspnetcore
razor-components  fastreport  report-generator  banded-report
blazor-server  blazor-webassembly  pdf-alternative  turkish
```

"Releases" ve "Packages" kutularını açık bırakın; NuGet paketi bağlanınca depo
sayfasında görünür.

### Google'da "Blazor rapor tasarım aracı" ile bulunmak

Arama motorları depo sayfasını ve README'yi okuyor. Şunlar zaten yapıldı:

- `README.md` başlığı **"Pica.Reports — Blazor Report Designer"**, hemen altında
  Türkçe karşılığı ve `README.tr.md` bağlantısı.
- Türkçe README'nin başlığı **"Blazor Rapor Tasarım Aracı"**.
- İki dosyanın sonunda anahtar sözcük satırı.
- Paket açıklaması (`Description`) iki dili birden taşıyor; NuGet sayfası da
  aranıyor.

Geri kalanı zamanla oluyor: bağlantı almak (kendi sitenizden depoya link,
LinkedIn/blog duyurusu) sıralamayı en çok değiştiren şey.

## 3. NuGet'e gönderme

**Bir kez:** <https://www.nuget.org> → hesap → **Trusted Publishing** → yeni ilke.
Depoda saklanan uzun ömürlü bir anahtar **yok**: GitHub koşuya imzalı bir OIDC
belirteci veriyor, nuget.org onu ilkeyle karşılaştırıp bir saatlik geçici anahtar
üretiyor. Sızacak, dolacak ya da döndürülecek bir sır kalmıyor.

| Alan | Değer |
| --- | --- |
| Package Owner | `omerdemir2000` |
| CI/CD Provider | GitHub Actions |
| Repository Owner | `omerdemir2000` |
| Repository | `Pica.Reports` |
| Workflow File | `release.yml` — yalnız dosya adı, `.github/workflows/` öneki **yok** |
| Environment | **boş** — iş akışı GitHub environment kullanmıyor |

Dördü de birebir eşleşmezse belirteç reddedilir. `release.yml`'ın adı ilkeye
yazılı: dosyayı yeniden adlandırırsanız ilkeyi de güncelleyin.

> **İlke ilk 7 gün "geçici" olabilir.** nuget.org, depoyu kimliğine kilitlemek
> için GitHub'ın depo ve sahip kimliklerini ilk başarılı yayında öğreniyor
> (silinip aynı adla yeniden açılan bir depo, ilkeyi devralamasın diye). O süre
> içinde yayın yapılmazsa ilke pasifleşir; pencere istendiği zaman yeniden
> başlatılabiliyor. Genelde özel depolarda görülür.

Eski yöntem (nuget.org → **API Keys** → `NUGET_API_KEY` gizli değeri) hâlâ
çalışıyor ama artık kullanılmıyor: anahtarın en fazla 365 günlük ömrü var ve
dolduğunda yayın düşer.

**Her sürümde:** `src/Pica.Reports/Pica.Reports.csproj` içindeki `<Version>`'ı
artırın, `CHANGELOG.md`'ye yazın, sonra:

```bash
git commit -am "0.9.1"
git tag v0.9.1
git push && git push origin v0.9.1
```

Etiket `release` iş akışını tetikler: derler, sınar, paketler ve gönderir.

**Elle göndermek isterseniz:**

```bash
dotnet pack src/Pica.Reports -c Release -o artifacts
dotnet nuget push artifacts/Pica.Reports.0.9.0.nupkg \
  --api-key <ANAHTAR> --source https://api.nuget.org/v3/index.json
```

Paket yayımlandıktan sonra **silinemez** (yalnızca listeden gizlenir) ve aynı
sürüm numarası bir daha kullanılamaz. Emin değilseniz önce
`--source https://apiint.nuget.org/v3/index.json` (NuGet'in deneme ortamı)
kullanın.

## 4. Sürüm numarası

[SemVer](https://semver.org/lang/tr/): `0.9.0` yayına hazır ama API'nin
donduğu sözü verilmiş değil. `1.0.0` o sözdür — API'nin Türkçe mi İngilizce mi
olacağı kararı verildikten sonra.

## 5. Yayımdan önce bakılacaklar

- [ ] `dotnet test` yeşil.
- [ ] `CHANGELOG.md` güncel.
- [ ] `PackageId` **Pica.Reports** (değişirse statik varlık yolları kırılır).
- [ ] Marka sicilleri: TÜRKPATENT, EUIPO ve USPTO'da 9. ve 42. sınıfta "PICA".
      *Pica* tipografide jenerik bir terim ve yazılım alanında canlı çakışan
      bir marka bulunamadı, ama siciller denetlenmedi.
- [x] Depo **Public** ve LICENSE görünüyor.
- [ ] README'deki NuGet rozeti ilk sürüm etiketine kadar kırmızı: paket henüz
      nuget.org'da yok. `git tag v0.9.0 && git push origin v0.9.0` ile yayımlanır
      ve rozet kendiliğinden yeşile döner.
