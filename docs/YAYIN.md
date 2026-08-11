# Yayın adımları

Bu dosya **Papirus Yazılım** içindir: depoyu GitHub'a açmak, NuGet'e paket
göndermek ve arama sonuçlarında görünmek için yapılacaklar. Bir kez yapılır,
sonra sürüm etiketi atmak yeter.

## 1. GitHub deposu

Depo yerel olarak hazır ve ilk commit atılmış durumda. Uzak depo açmak sizin
hesabınıza bağlı:

**Tarayıcıdan:** <https://github.com/new> → depo adı `pica-reports`, görünürlük
**Public**, "Add a README" ve ".gitignore" seçeneklerini **işaretlemeyin**
(dosyalar burada zaten var).

Sonra:

```bash
cd D:\C#\Projeler_Demir\PicaReports
git remote add origin https://github.com/<hesap>/pica-reports.git
git branch -M main
git push -u origin main
```

**GitHub CLI kuruluysa** tek satır:

```bash
gh repo create <hesap>/pica-reports --public --source=. --remote=origin --push
```

## 2. Deponun görünen yüzü (About)

Depo sayfasında sağ üstteki ⚙ **About** düğmesinden:

**Description** — arama sonuçlarında görünen satır. İki dili birden taşısın:

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
razor-class-library  fastreport  barcode  pdf  rapor  rapor-tasarim
blazor-components  turkish
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

**Bir kez:** <https://www.nuget.org> → hesap → **API Keys** → *Create*:

- Key Name: `pica-reports-release`
- Select Scopes: **Push** (yalnızca)
- Glob Pattern: `Pica.Reports*` — anahtarı bu pakete kısıtlar. Geniş yetkili
  anahtar sızarsa bütün paketleriniz etkilenir.

Anahtarı GitHub'a koyun: depo → **Settings → Secrets and variables → Actions →
New repository secret** → ad `NUGET_API_KEY`.

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
- [ ] Depo **Public** ve LICENSE görünüyor.
