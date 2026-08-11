using Pica.Reports.Duzen;

namespace Pica.Reports;

/// <summary>
/// Model değerlerinin ekranda görünen adları.
/// </summary>
/// <remarks>
/// <para>
/// Enum adları arayüze doğrudan çıkmaz: <c>SutunBasligi</c> yerine "Column
/// header" ya da "Sütun başlığı" yazılır. Tek yerden geldikleri için tuvalde
/// ve özellik panelinde aynı sözcük kullanılır — arayüzün kelime dağarcığı,
/// kullanıcının yolunu bulmasının yarısıdır.
/// </para>
/// <para>
/// Karşılıklar <see cref="Metin"/> üzerinden dil dosyalarından geliyor;
/// buradaki tek iş, enum değerini kaynak anahtarına çevirmek.
/// </para>
/// </remarks>
public static class Etiketler
{
    /// <summary>Bant türünün okunur adı.</summary>
    public static string Bant(BantTuru tur) => Metin.Al("Bant_" + (tur switch
    {
        BantTuru.RaporBasligi => nameof(BantTuru.RaporBasligi),
        BantTuru.SayfaBasligi => nameof(BantTuru.SayfaBasligi),
        BantTuru.Baslik => nameof(BantTuru.Baslik),
        BantTuru.SutunBasligi => nameof(BantTuru.SutunBasligi),
        BantTuru.GrupBasligi => nameof(BantTuru.GrupBasligi),
        BantTuru.Veri => nameof(BantTuru.Veri),
        BantTuru.AltVeri => nameof(BantTuru.AltVeri),
        BantTuru.AltAltVeri => nameof(BantTuru.AltAltVeri),
        BantTuru.GrupSonu => nameof(BantTuru.GrupSonu),
        BantTuru.SutunSonu => nameof(BantTuru.SutunSonu),
        BantTuru.Alt => nameof(BantTuru.Alt),
        BantTuru.RaporSonu => nameof(BantTuru.RaporSonu),
        BantTuru.SayfaSonu => nameof(BantTuru.SayfaSonu),
        BantTuru.Ust => nameof(BantTuru.Ust),
        BantTuru.Yan => nameof(BantTuru.Yan),
        _ => "Bilinmeyen",
    }));

    /// <summary>Bandın kâğıttaki görevini bir cümleyle anlatır.</summary>
    public static string Rol(BantRolu rol) => Metin.Al("Rol_" + (rol switch
    {
        BantRolu.SayfaBasi => nameof(BantRolu.SayfaBasi),
        BantRolu.SayfaAlti => nameof(BantRolu.SayfaAlti),
        BantRolu.Ortu => nameof(BantRolu.Ortu),
        _ => nameof(BantRolu.Icerik),
    }));

    /// <summary>Nesne türünün okunur adı.</summary>
    public static string Nesne(NesneTuru tur) => Metin.Al("Nesne_" + (tur switch
    {
        NesneTuru.Yazi => nameof(NesneTuru.Yazi),
        NesneTuru.Cizgi => nameof(NesneTuru.Cizgi),
        NesneTuru.Sekil => nameof(NesneTuru.Sekil),
        NesneTuru.Resim => nameof(NesneTuru.Resim),
        NesneTuru.Barkod => nameof(NesneTuru.Barkod),
        _ => "Bilinmeyen",
    }));

    /// <summary>Biçim türünün okunur adı — cümle içinde geçen küçük hâli.</summary>
    public static string Bicim(BicimTuru tur) => Metin.Al("Bicim_" + (tur switch
    {
        BicimTuru.Sayi => "SayiKucuk",
        BicimTuru.Tarih => "TarihKucuk",
        BicimTuru.Saat => "SaatKucuk",
        BicimTuru.Metin => "MetinKucuk",
        BicimTuru.Boolean => "BooleanKucuk",
        _ => "Bicimsiz",
    }));

    /// <summary>Biçim türünün liste seçeneği olarak adı.</summary>
    public static string BicimSecenegi(BicimTuru tur) => Metin.Al("Bicim_" + tur);

    /// <summary>
    /// Barkod simgeleminin adı.
    /// </summary>
    /// <remarks>
    /// Çevrilmiyor: "Code 128" ve "EAN-13" simgelemlerin kendi adları, her
    /// dilde aynı yazılır.
    /// </remarks>
    public static string Barkod(BarkodTuru tur) => tur switch
    {
        BarkodTuru.Ean13 => "EAN-13",
        _ => "Code 128",
    };
}
