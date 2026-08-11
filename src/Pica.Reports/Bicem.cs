using System.Text;
using Pica.Reports.Duzen;

namespace Pica.Reports;

/// <summary>
/// Bir kutunun ekrandaki CSS karşılığı.
/// </summary>
/// <remarks>
/// <para>
/// Tuval ile önizleme <b>aynı</b> kodu kullanıyor: ikisi ayrı yazılsaydı
/// tasarımda gördüğün kutu ile önizlemedeki er geç ayrışırdı ve hangisinin
/// doğru olduğu belirsiz kalırdı.
/// </para>
/// <para>
/// Çizicinin yaptığının aynısı: konum ve boy bandın (önizlemede sayfanın) sol
/// üst köşesine göre, çerçeve kenar kenar, zemin boşsa saydam. Ayrılan tek yer
/// yazı tipi ölçüsüdür — tarayıcı ile PDF motoru aynı satır kırmasını yapmaz.
/// </para>
/// </remarks>
internal static class Bicem
{
    /// <summary>Kutunun konumu, boyu, zemini ve çerçevesi.</summary>
    public static string Kutu(DuzenNesnesi n, double solPt, double ustPt)
    {
        var b = new StringBuilder()
            .Append("left:").Append(Olcu.Px(solPt))
            .Append(";top:").Append(Olcu.Px(ustPt))
            .Append(";width:").Append(Olcu.Px(n.GenislikPt))
            .Append(";height:").Append(Olcu.Px(n.YukseklikPt));

        if (!string.IsNullOrEmpty(n.ZeminRengi))
            b.Append(";background:").Append(n.ZeminRengi);

        if (n.Cerceve is not CerceveKenari.Yok)
        {
            var kalinlik = Olcu.Px(n.CerceveKalinligiPt);
            var renk = string.IsNullOrEmpty(n.CerceveRengi) ? "#000000" : n.CerceveRengi;

            if (n.Cerceve.HasFlag(CerceveKenari.Sol)) Kenar(b, "left", kalinlik, renk);
            if (n.Cerceve.HasFlag(CerceveKenari.Sag)) Kenar(b, "right", kalinlik, renk);
            if (n.Cerceve.HasFlag(CerceveKenari.Ust)) Kenar(b, "top", kalinlik, renk);
            if (n.Cerceve.HasFlag(CerceveKenari.Alt)) Kenar(b, "bottom", kalinlik, renk);
        }

        b.Append(";align-items:").Append(n.Dikey switch
        {
            DikeyHiza.Orta => "center",
            DikeyHiza.Alt => "flex-end",
            _ => "flex-start",
        });

        return b.ToString();

        static void Kenar(StringBuilder b, string yon, string kalinlik, string renk)
            => b.Append(";border-").Append(yon).Append(':').Append(kalinlik).Append(" solid ").Append(renk);
    }

    /// <summary>Yazı kutusunun yazı biçemi.</summary>
    public static string Yazi(DuzenNesnesi n)
    {
        var b = new StringBuilder()
            // Yazı tipi adı düzenden geldiği gibi verilir; sunucuda ya da
            // tarayıcıda yoksa yedekler devreye girer.
            .Append("font-family:'").Append(n.YaziTipi.Replace("'", "")).Append("',Arial,sans-serif")
            .Append(";font-size:").Append(Olcu.Px(n.PuntoPt))
            .Append(";padding:0 ").Append(Olcu.Px(1.5))
            .Append(";text-align:").Append(n.Yatay switch
            {
                YatayHiza.Orta => "center",
                YatayHiza.Sag => "right",
                YatayHiza.Yasli => "justify",
                _ => "left",
            })
            .Append(";white-space:").Append(n.KelimeKaydir ? "pre-wrap" : "pre");

        if (n.Kalin) b.Append(";font-weight:700");
        if (n.Egik) b.Append(";font-style:italic");
        if (n.AltiCizili) b.Append(";text-decoration:underline");
        if (!string.IsNullOrEmpty(n.Renk)) b.Append(";color:").Append(n.Renk);

        return b.ToString();
    }
}
