// Kutu sürükleme ve boyutlandırma.
//
// Neden JS: Blazor Server'da her pointermove bir ağ gidiş-dönüşü demek.
// Sürükleme sırasında kutuları DOM'da doğrudan bu modül oynatır, sunucuya
// yalnızca BIRAKILDIĞINDA tek bir çağrı gider. Aradaki her kare sunucuya
// sorulsaydı sürükleme, ağ gecikmesi kadar geriden gelirdi.
//
// Ölçü birimi: düzenin her yeri puntodur. DOM'daki piksel değerleri
// punto × (96/72) × yakınlaştırma ile üretilir; buradaki hesap da tersini
// yapar ve sunucuya HER ZAMAN punto gönderir.

const PIKSEL_PUNTO = 96 / 72;

/** Sürükleme sayılması için gereken en küçük hareket (piksel). */
const ESIK = 3;

/** Punto; sıfır boyutlu kutu basılmaz, o yüzden altına inilmez. */
const EN_AZ = 1;

export function bagla(kok, katman) {
    if (!kok) return null;

    /** Yürüyen sürükleme; yoksa null. */
    let is = null;

    const sayi = (ad, varsayilan) => parseFloat(kok.dataset[ad] || varsayilan) || parseFloat(varsayilan);

    const olcek = () => sayi('olcek', '1');

    /** Izgara adımı, punto. 0 ise yaslama yok. */
    const izgara = () => parseFloat(kok.dataset.izgara || '0') || 0;

    function yasla(punto) {
        const a = izgara();
        return a > 0 ? Math.round(punto / a) * a : punto;
    }

    /** Ekran pikselini puntoya çevirir (yakınlaştırma dahil). */
    const pt = (piksel) => piksel / PIKSEL_PUNTO / olcek();

    /** Öğenin biçemindeki ölçüsünü punto olarak okur. */
    const oku = (el, ad) => (parseFloat(el.style[ad]) || 0) / PIKSEL_PUNTO;

    function basla(e) {
        if (e.button !== 0) return;

        // Bandın alt kenarı: kutulardan ÖNCE bakılıyor, çünkü tutamak bandın
        // içinde duruyor ve en alttaki kutunun üstüne binebiliyor.
        const bantTutamak = e.target.closest('.rt-bant-tutamak');
        if (bantTutamak) return bantBasla(e, bantTutamak);

        const tutamak = e.target.closest('.rt-tutamak');
        const kutu = e.target.closest('.rt-kutu');
        if (!kutu) return;

        // Sürükleme seçili kutuda başlar. Seçili olmayan bir kutuya basmak
        // önce onu seçer (tıklama olayı Blazor tarafında); ikinci basış
        // sürükler. Yoksa seçmek için tıklayan her el kutuyu kaydırırdı.
        if (!tutamak && kutu.dataset.secili !== 'true') return;

        // Boyutlandırma tek kutuya uygulanır — tutamaklar zaten yalnız tek
        // seçimde çiziliyor. Taşımada seçili olan HER kutu birlikte gider.
        const hedefler = tutamak ? [kutu] : [...kok.querySelectorAll('.rt-kutu[data-secili]')];

        is = {
            yon: tutamak ? tutamak.dataset.yon : 'tasi',
            x: e.clientX,
            y: e.clientY,
            basladi: false,
            ogeler: hedefler.map(el => ({
                el,
                bant: el.dataset.bant,
                nesne: el.dataset.nesne,
                sol: oku(el, 'left'),
                ust: oku(el, 'top'),
                en: oku(el, 'width'),
                boy: oku(el, 'height'),
            })),
        };

        kutu.setPointerCapture(e.pointerId);
        e.preventDefault();
        e.stopPropagation();
    }

    /**
     * Bant yüksekliğini çekme.
     *
     * Kutu sürüklemesinden ayrı tutuluyor: burada tek bir ölçü değişiyor
     * (yükseklik), yaslama dikeyde ve alt sınır bandın kendi içeriğinden
     * geliyor. Aynı işin içine sıkıştırılsaydı iki kural birbirine karışırdı.
     */
    function bantBasla(e, tutamak) {
        const bant = tutamak.closest('.rt-bant');
        if (!bant) return;

        is = {
            yon: 'bant',
            x: e.clientX,
            y: e.clientY,
            basladi: false,
            bant: {
                el: bant,
                ad: bant.dataset.bant,
                // Bildirilen yükseklik: DOM'daki değil. Basılmayan bantta DOM
                // yüksekliği yer tutucudur, bildirilen ise 0'dır.
                boy: parseFloat(bant.dataset.boy || '0') || 0,
                enAz: parseFloat(bant.dataset.enaz || '4') || 4,
            },
        };

        bant.setPointerCapture(e.pointerId);
        e.preventDefault();
        e.stopPropagation();
    }

    function bantKaydir(dy) {
        const b = is.bant;
        const yeni = Math.max(b.enAz, yasla(b.boy + pt(dy)));

        b.son = yeni;
        b.el.style.height = (yeni * PIKSEL_PUNTO) + 'px';
    }

    function kaydir(e) {
        if (!is) return;

        const dx = e.clientX - is.x;
        const dy = e.clientY - is.y;

        if (!is.basladi) {
            // Bant yüksekliğinde yalnızca dikey hareket sayılır: yatay
            // titreme sürükleme başlatmamalı.
            const yeter = is.yon === 'bant'
                ? Math.abs(dy) >= ESIK
                : Math.abs(dx) >= ESIK || Math.abs(dy) >= ESIK;

            if (!yeter) return;

            is.basladi = true;
            kok.dataset.surukleniyor = 'true';
        }

        if (is.yon === 'bant') {
            bantKaydir(dy);
            e.preventDefault();
            return;
        }

        // Yaslama BİRİNCİ kutuya göre yapılır ve aynı kayma hepsine uygulanır.
        // Her kutu ayrı ayrı yaslansaydı aralarındaki mesafe bozulur, hizalı
        // duran bir sütun sürüklendikten sonra dağılırdı.
        const ilk = is.ogeler[0];
        const p0 = hesapla(is.yon, ilk, pt(dx), pt(dy), yasla);
        const kayS = p0.sol - ilk.sol;
        const kayU = p0.ust - ilk.ust;

        for (const o of is.ogeler) {
            const p = is.yon === 'tasi'
                ? { sol: o.sol + kayS, ust: o.ust + kayU, en: o.en, boy: o.boy }
                : p0;

            o.son = p;
            o.el.style.left = (p.sol * PIKSEL_PUNTO) + 'px';
            o.el.style.top = (p.ust * PIKSEL_PUNTO) + 'px';
            o.el.style.width = (p.en * PIKSEL_PUNTO) + 'px';
            o.el.style.height = (p.boy * PIKSEL_PUNTO) + 'px';
        }

        e.preventDefault();
    }

    async function birak(e) {
        if (!is) return;

        const bitti = is;
        is = null;
        delete kok.dataset.surukleniyor;

        if (bitti.yon === 'bant') {
            try { bitti.bant.el.releasePointerCapture(e.pointerId); } catch { /* zaten bırakılmış */ }

            if (bitti.basladi && bitti.bant.son !== undefined)
                await katman.invokeMethodAsync('BantBoyutlandi', bitti.bant.ad, bitti.bant.son);

            return;
        }

        for (const o of bitti.ogeler) {
            try { o.el.releasePointerCapture(e.pointerId); } catch { /* zaten bırakılmış */ }
        }

        // Eşiği geçmeyen hareket sürükleme değil, tıklamadır: seçimi bozmadan
        // geçilir ve sunucuya hiçbir şey gönderilmez.
        if (!bitti.basladi) return;

        await katman.invokeMethodAsync('KutularTasindi', bitti.ogeler
            .filter(o => o.son)
            .map(o => ({
                bant: o.bant,
                nesne: o.nesne,
                solPt: o.son.sol,
                ustPt: o.son.ust,
                genislikPt: o.son.en,
                yukseklikPt: o.son.boy,
            })));
    }

    // Klavye Blazor'a değil buraya bağlı. İki sebebi var: (1) olayın hangi
    // öğede doğduğunu görebiliyoruz, yani özellik panelindeki bir kutuya
    // yazarken ok tuşu seçili kutuyu oynatmıyor; (2) Blazor'a bağlansaydı
    // metin alanına yazılan HER harf sunucuya bir tur atardı.
    function tus(e) {
        if (!kok.isConnected) return;
        if (e.target.closest('input, textarea, select, [contenteditable="true"]')) return;

        const ok = e.key.startsWith('Arrow');
        const kisayol = (e.ctrlKey || e.metaKey) && 'zZyY'.includes(e.key);
        if (!ok && !kisayol) return;

        // Seçili kutu yokken ok tuşu kendi işini görsün — sayfayı kaydırsın.
        if (ok && !kok.querySelector('.rt-kutu[data-secili]')) return;

        e.preventDefault();
        katman.invokeMethodAsync('TusBasildi', e.key, e.shiftKey, e.ctrlKey || e.metaKey);
    }

    kok.addEventListener('pointerdown', basla);
    kok.addEventListener('pointermove', kaydir);
    kok.addEventListener('pointerup', birak);
    kok.addEventListener('pointercancel', birak);
    document.addEventListener('keydown', tus);

    return {
        coz() {
            kok.removeEventListener('pointerdown', basla);
            kok.removeEventListener('pointermove', kaydir);
            kok.removeEventListener('pointerup', birak);
            kok.removeEventListener('pointercancel', birak);
            document.removeEventListener('keydown', tus);
        },
    };
}

/**
 * Bir kutunun sürükleme sonrası geometrisi, punto.
 *
 * Boyutlandırmada karşı kenar sabit kalır: sol tutamağı çekmek kutunun sağ
 * kenarını yerinde bırakmalı, yoksa kutu büyürken kayar.
 */
function hesapla(yon, o, dSol, dUst, yasla) {
    if (yon === 'tasi') {
        return { sol: yasla(o.sol + dSol), ust: yasla(o.ust + dUst), en: o.en, boy: o.boy };
    }

    const sag = o.sol + o.en;
    const alt = o.ust + o.boy;

    let sol = o.sol, ust = o.ust, en = o.en, boy = o.boy;

    // b(atı) ve k(uzey) karşı kenarı sabit tutar: konum değişir, boy ondan
    // türetilir. d(oğu) ve g(üney) doğrudan boyu değiştirir.
    if (yon.includes('b')) { sol = Math.min(yasla(o.sol + dSol), sag - EN_AZ); en = sag - sol; }
    if (yon.includes('d')) { en = Math.max(EN_AZ, yasla(sag + dSol) - sol); }
    if (yon.includes('k')) { ust = Math.min(yasla(o.ust + dUst), alt - EN_AZ); boy = alt - ust; }
    if (yon.includes('g')) { boy = Math.max(EN_AZ, yasla(alt + dUst) - ust); }

    return { sol, ust, en: Math.max(EN_AZ, en), boy: Math.max(EN_AZ, boy) };
}
