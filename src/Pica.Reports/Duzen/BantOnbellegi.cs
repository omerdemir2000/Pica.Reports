namespace Pica.Reports.Duzen;

/// <summary>
/// Bantların basım sırasını önbellekler ve <b>düzen değiştiğinde tazeler</b>.
/// </summary>
/// <remarks>
/// <para>
/// Sıra her çizimde yeniden hesaplanabilirdi ama tuval onu birçok yerde
/// okuyor (kâğıdın boyu, her bandın konumu, kanal satırları) ve kalabalık
/// düzenlerde bant sayısı üç haneli.
/// </para>
/// <para>
/// <b>Yalnızca sayfa nesnesine bakmak yetmiyor</b> — ve bu bir hataydı: bant
/// eklemek ya da silmek <c>Sayfalar[i].Bantlar</c> listesini <i>yerinde</i>
/// değiştiriyor, sayfa nesnesi aynı kalıyor. Sıra tazelenmediği için eklenen
/// bant ekranda hiç görünmüyor, silinen bant ise durmaya devam ediyordu;
/// dışarıdan bakınca "düğme çalışmıyor" gibi görünüyor, oysa düzen doğru
/// değişmişti. Bu yüzden bant <b>listesi</b> de karşılaştırılıyor.
/// </para>
/// <para>
/// Karşılaştırma başvuruya göre ve sırayla: ekleme, silme, yer değiştirme ve
/// geri alma (silinen bandın yerine konması) hepsi yakalanır. Maliyeti bant
/// sayısı kadar başvuru karşılaştırması — sıralamanın yanında hiç kalır.
/// </para>
/// </remarks>
internal sealed class BantOnbellegi
{
    private DuzenSayfasi? sayfa;
    private DuzenBandi[] bantlar = [];
    private List<SiraliBant> sira = [];

    /// <summary>Sayfanın basım sırası; gerekiyorsa yeniden hesaplanır.</summary>
    public List<SiraliBant> Sira(DuzenSayfasi? s)
    {
        if (s is null)
        {
            sayfa = null;
            bantlar = [];
            return sira = [];
        }

        if (!Degisti(s)) return sira;

        sayfa = s;
        bantlar = [.. s.Bantlar];

        return sira = BantSirasi.BasimSirasi(s);
    }

    private bool Degisti(DuzenSayfasi s)
    {
        if (!ReferenceEquals(sayfa, s)) return true;
        if (bantlar.Length != s.Bantlar.Count) return true;

        for (var i = 0; i < bantlar.Length; i++)
            if (!ReferenceEquals(bantlar[i], s.Bantlar[i]))
                return true;

        return false;
    }
}
