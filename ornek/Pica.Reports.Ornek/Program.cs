using Microsoft.AspNetCore.Localization;
using Pica.Reports.Servisler;
using Pica.Reports.Ornek;
using Pica.Reports.Ornek.Components;

var builder = WebApplication.CreateBuilder(args);

// Diller. Kütüphanenin arayüzü İngilizce ve Türkçe konuşuyor; varsayılan
// İngilizce. Kültür hem BİÇİMİ (ondalık ayracı, tarih) hem ARAYÜZ DİLİNİ
// belirliyor ve kütüphane onu uygulamadan alıyor — kendi dil ayarı yok, olsaydı
// iki ayar çelişirdi.
string[] diller = ["en", "tr"];

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Kütüphanenin kurulumu tek satır: düzen deposu (klasördeki JSON dosyaları) ve
// rapor aracı (IRaporAraci) kaydolur. Kendi deponuz varsa
// AddPicaReports<KendiDepom>() diyebilirsiniz.
builder.Services.AddPicaReports(o => o.Duzenler = "Duzenler");

var app = builder.Build();

// İstek yerelleştirmesi: dil çerezden okunur, yoksa tarayıcının Accept-Language
// başlığından, o da tutmazsa varsayılan (İngilizce). Blazor Server'da devrenin
// kültürü ilk istekten miras alınır, bu yüzden dil değiştirmek TAM SAYFA
// yenilemesi ister — aşağıdaki /dil ucu bunu yapıyor.
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture(diller[0])
    .AddSupportedCultures(diller)
    .AddSupportedUICultures(diller));

// Dil değiştirme ucu: çerezi yazar ve geldiği sayfaya döner.
app.MapGet("/dil/{kod}", (string kod, string? donus, HttpContext baglam) =>
{
    baglam.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(kod)),
        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), Path = "/" });

    return Results.LocalRedirect(string.IsNullOrEmpty(donus) ? "/" : donus);
});

// Blazor'ın etkileşimli uç noktaları sahtecilik korumasını şart koşuyor;
// ara katman olmadan her istek 500 döner.
app.UseAntiforgery();

// MapStaticAssets, UseStaticFiles DEĞİL: kütüphanenin CSS ve JS dosyaları
// pakete gömülü (_content/Pica.Reports/...) ve derleme sırasında üretilen
// varlık listesinden servis ediliyor. UseStaticFiles yalnızca wwwroot'a bakar,
// o yolda 404 döner ve tasarımcı biçemsiz açılır, sürükleme de çalışmaz.
app.MapStaticAssets();

// AddAdditionalAssemblies: kütüphanenin hazır ekranları (/pica/…) ayrı bir
// derlemede. Yönlendiriciye tanıtmak (Routes.razor) yetmiyor — sunucu, adresi
// tanımadığı için ilk isteğe 404 döner. İki yerde birden söylenmeli.
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(IRaporAraci).Assembly);

app.Run();
