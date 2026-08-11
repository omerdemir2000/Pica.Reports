<img src="appicon.svg" alt="Papirus Yazılım Ltd. Şti." width="72" align="right" />

# Pica.Reports

**A free, open-source banded report designer for Blazor.** Drag boxes around on
paper, bind a dataset, watch the pages break — with no CSS framework, no icon
pack, no PDF engine and no commercial licence.

English · [Türkçe](README.tr.md) — **Blazor rapor tasarım aracı**

A product of **[Papirus Yazılım Ltd. Şti.](https://www.papirusbilisim.com)**

[![NuGet](https://img.shields.io/nuget/v/Pica.Reports.svg)](https://www.nuget.org/packages/Pica.Reports)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)
![Blazor Server &amp; WebAssembly](https://img.shields.io/badge/Blazor-Server%20%7C%20WASM-5C2D91)

<!--
  Record the screencast and put it here before announcing this anywhere:
  ![The Pica.Reports designer](docs/pica-reports.gif)
  A drag-and-drop designer sells itself in eight seconds. Prose does not.
-->

Every report designer for .NET is somebody's paid product. This one is MIT, and
it's small enough to read in an afternoon.

## The problem it solves

When Delphi/FastReport reports (`.fr3`, `.dfm`) are migrated to .NET, the
templates are usually converted to JSON once and then left there. From that
point on, nudging a single box two points to the left means going back into
source code.

Pica.Reports puts that step on screen: it shows the layout on paper, lets you
drag the boxes, and writes the result **as a patch file beside the generated
one** — so the next conversion doesn't throw your work away.

> **About the name.** *Pica* is a typographic unit — 1/6 inch, 12 points — and
> every measurement in this library is in points. The name claims no
> relationship or partnership with other products that happen to share the word.

## Try it first

A working sample app ships in the repo, with ten example layouts:

```
dotnet run --project ornek/Pica.Reports.Ornek
```

<http://localhost:5099> · details: [ornek/Pica.Reports.Ornek/README.md](ornek/Pica.Reports.Ornek/README.md)

## What's in the box

| | |
| --- | --- |
| **Design surface** | Drag, resize, multi-select, snap to grid, align, undo/redo |
| **Objects** | Text, line, shape, image, barcode (Code 128 / EAN-13) |
| **Data tree** | Datasets and fields in the palette; drag a field onto the paper to bind it |
| **Print preview** | Real pagination — data bands repeat, pages break, footers sit at the bottom, page totals reset |
| **Page setup** | Paper size, portrait/landscape, margins, columns |
| **Sample data** | See `1,234.56` instead of `[amount]` without running a query |
| **Patch model** | Hand edits are stored as a diff; the generated layout is never touched |
| **Ready-made screens** | Routable list / designer / preview pages — or embed the components yourself |
| **Themes** | Follows `data-theme` and the OS preference; recolour through `--rt-*` variables |

**No dependencies.** The only package reference is
`Microsoft.AspNetCore.Components.Web`. The library brings its own CSS and inline
SVG icons, and runs under both Blazor Server and WebAssembly.

## Install

```xml
<PackageReference Include="Pica.Reports" Version="0.9.0" />
```

Add the stylesheet to the page:

```html
<link rel="stylesheet" href="_content/Pica.Reports/rapor-tasarim.css" />
```

Serve the library's static assets with `MapStaticAssets`, **not**
`UseStaticFiles` — the CSS and JS live inside the package under
`_content/Pica.Reports/`, and `UseStaticFiles` only looks at `wwwroot`.

> **Don't rename the package.** Static assets are served from
> `_content/{PackageId}/`, and the canvas loads its JS module from
> `./_content/Pica.Reports/tasarimci.js`. Change `PackageId` and dragging stops
> working *silently* — no error either, because a module that fails to load
> simply never attaches its event listeners.

## Shortest possible usage

```csharp
// Program.cs
builder.Services.AddPicaReports(o => o.Duzenler = "Reports");   // layout folder

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode()
   .AddAdditionalAssemblies(typeof(IRaporAraci).Assembly);      // ready-made screens
```

```razor
@* Routes.razor *@
<Router AppAssembly="typeof(Program).Assembly"
        AdditionalAssemblies="new[] { typeof(RaporAraci).Assembly }">
```

Then, from your own screen:

```razor
@inject IRaporAraci Rapor

var data = new RaporVerisi()
    .Ekle("Invoices", await connection.QueryAsync("select * from invoice"))   // Dapper
    .Ekle("Summary", summaryRows)                                             // your own class
    .Degisken("CompanyName", company.Name);

await Rapor.TasarimAc("invoice", data);   // designer  (the equivalent of DesignReport)
await Rapor.Onizle("invoice", data);      // preview
```

The ready-made screens are `/pica/duzenler`, `/pica/tasarim/{key}` and
`/pica/onizleme/{key}`. If you want your own URLs, put the components on your
own pages (below).

> **It has to be registered in two places.** The ready-made screens live in a
> separate assembly: `AdditionalAssemblies` in `Routes.razor` is for the router,
> `AddAdditionalAssemblies` is for the server. Miss either one and the first
> request 404s.

The data doesn't travel through the URL — a report's rows don't fit in an
address bar, and shouldn't sit there. `IRaporAraci` is scoped, which in Blazor
means one user's circuit, so one user's data never leaks into another's.

## Data sources

A report can have **more than one dataset**, and matching is **by name**. If a
band in the layout says `VeriKumesi = "Invoices"`, you must supply the dataset
under that name:

```csharp
var data = new RaporVerisi()
    .Ekle("Invoices", invoiceRows)     // IEnumerable<dynamic> (Dapper), IEnumerable<T>, or dictionaries
    .Ekle("Summary", summaryRows)
    .Tanit("Staff", "FirstName", "LastName")   // no rows, field names only — for design time
    .Degisken("Year", 2027);                   // boxes outside a data band read from here
```

The library looks at the **shape** of a row, not its type: a dictionary's keys
are field names (that's what Dapper returns), and an object's public properties
become fields. On an empty list, the fields are derived from `T` — so the field
tree stays populated even when the query returns no rows.

Get the name wrong and the band prints empty rather than failing: from the
inside, "no data arrived" and "the data was empty" look identical.

Data goes to two places at once: the **field tree** (where you drag fields out
to create boxes) and the **preview** (where it's printed with real rows).

## Preview

**Preview** on the toolbar lays the layout out over pages: the data band repeats
once per row, pages break when they fill, the page footer sits at the bottom of
the sheet, `[Page#]`/`[TotalPages#]` are substituted, and `SUM(…, 2)` resets on
every page.

Page breaking is done by the library rather than the browser, because a page
total or a carried-forward line can't be computed without knowing which rows
fell on which page. With no data bound, the preview uses **sample rows** — at
design time the point is the layout, not the pagination.

**This is not a PDF.** The library doesn't generate PDFs; a browser and a PDF
engine don't break lines the same way. The layout is right, the exact pixels of
the glyphs are not. If you have your own PDF output, pass the URL pattern
(`OnizlemeAdresi`) and a separate **PDF** button appears on the toolbar.

### Printing

**Print** on the preview toolbar opens the browser print dialogue; what gets
printed is the preview itself. The paper size is declared to the printer with
`@page`, so a label-sized or landscape layout goes onto the right sheet. Anything
that belongs to the screen — toolbars, palette, shadows, zoom, margin guides —
stays off the paper, every page goes on its own sheet, and the output is 1:1.

`Rapor.Yazdir("invoice", data)` opens the preview with `?yazdir=1`: the print
dialogue appears as soon as the screen does. The list rows have **Preview** and
**Print** buttons too — they show up only if you bind the matching callback.

## Page setup

**Page** on the toolbar sets up the sheet: preset sizes (A3, B5, A4, Letter,
Legal, A5, A6, 100×50 label), **portrait/landscape**, margins and column count.
Measurements are entered in millimetres on screen and stored as points in the
layout.

Orientation swaps the width and height of the sheet — it's a setting with a
counterpart on **paper**, not on screen. Boxes stay where they are: moving the
overflowing ones automatically would destroy the alignment you set, and the
overflow is visible on the canvas.

Page settings are written to the patch file too, and **Reset to original** takes
them back.

## Putting the components on your own pages

The component doesn't know where layouts are stored — you hand it a store:

```csharp
public interface IDuzenDeposu
{
    Task<IReadOnlyList<DuzenKaydi>> ListeleAsync(CancellationToken iptal = default);
    Task<CetvelDuzeni?> HamGetirAsync(string anahtar, CancellationToken iptal = default);
    Task<DuzenDuzeltmesi?> DuzeltmeGetirAsync(string anahtar, CancellationToken iptal = default);
    Task DuzeltmeKaydetAsync(string anahtar, DuzenDuzeltmesi? duzeltme, CancellationToken iptal = default);
    Task OlusturAsync(string anahtar, CetvelDuzeni duzen, CancellationToken iptal = default);
}
```

There's a ready implementation over JSON files in a folder
(`DosyaDuzenDeposu`), and `AddPicaReports()` registers it for you. To supply
your own, use `AddPicaReports<MyStore>()`.

There are two components — the **list** and the **designer** — and they go on
separate pages. Which URL leads where is the application's decision; the
components never navigate, they report:

```razor
@* List — /layouts *@
@inject IDuzenDeposu Depo

<DuzenListesi Depo="Depo"
              Duzenle="key => Nav.NavigateTo($&quot;/layouts/{key}&quot;)" />
```

```razor
@* Designer — /layouts/{Key} *@
@page "/layouts/{Key}"
@inject IDuzenDeposu Depo

<RaporTasarimci Depo="Depo"
                SeciliAnahtar="@Key"
                Kapat="() => Nav.NavigateTo(&quot;/layouts&quot;)"
                OnizlemeAdresi="/reports/{0}.pdf" />
```

The **new layout** button in the list asks for a name and a key, produces an
empty A4 layout with two bands via `YeniDuzen.Bos`, writes it with
`OlusturAsync`, and then opens it through `Duzenle`. A store **must not**
overwrite an existing key inside `OlusturAsync`; it should throw.

Leave `OnizlemeAdresi` unset and the PDF button never appears. Leave `Kapat`
unbound and the designer's Close button never appears either — so it can be
embedded as a single full-screen view.

## Object types

| Type | What it does |
| --- | --- |
| Text | Text and data fields; number/date formatting applies here |
| Line | A one-sided box — a line has no frame, it *is* the line |
| Shape | Frame and fill |
| Image | An image stored **inside** the layout (as a data URI) |
| Barcode | Code 128 or EAN-13 |

Objects come from the tool palette on the left, in two ways:

- **Drag and drop** — drag the button onto the paper; the object lands in the
  band and at the point where you dropped it (snapped to the grid if it's on,
  and never outside the band or the sheet). The target band is outlined with a
  dashed border before you release.
- **Click** — the object is added to the selected band (or to the band of the
  selected box).

Images are stored as base64 inside the layout rather than as file paths, so the
patch file can travel on its own. A path would point at a logo another
installation can't find, and the page would print an empty box.

Barcode bars are generated in the library and handed over as **SVG**: the canvas
displays it directly, and your renderer passes the same string to the PDF
engine. A value that can't be encoded (Turkish letters in Code 128, a bad EAN-13
checksum) prints an **empty box** — an invented barcode is a label that says
something else when it's scanned.

## Field tree

The palette's "Data" section shows the layout's datasets and fields as a tree.
**Drag and drop** a field onto the paper and you get a text box bound to it: the
text becomes `[Invoices."Description"]`, `VeriAlani` becomes `Description`, and
the format is guessed from the name (amount → right-aligned `%2.2n`, date →
`dd.mm.yyyy`).

The tree is derived from the **layout itself** — the datasets its bands are
bound to and the references inside its boxes. That list can't be complete: a
field the layout never mentions doesn't appear. If the application knows the
real list, it can pass it through `VeriKumeleri`; the two lists are merged and
fields already in the layout are never dropped.

```razor
<RaporTasarimci Depo="Depo" SeciliAnahtar="@Key"
                VeriKumeleri="@([new VeriKumesiTanimi("Invoices", ["No", "Date", "Total"])])" />
```

## Sample data

The **Sample data** button on the toolbar shows boxes with formatted example
values instead of their references: `1,234.56` in place of `[debit]`,
`15.03.2027` in place of `[date]`. That makes it visible — without any real data
— what the `%2.2n` pattern does, whether the number fits the box, and whether
the decimal separator is right.

The values come from the `OrnekVeri` class, are **fixed** (not random, not
clock-dependent), and are formatted by `Bicimleme` — the very class your
renderer uses. The host application can use the same class to feed its own
preview output.

## Raw layout and patch

This distinction sits at the centre of the library:

- **The raw layout** (`{key}.json`) is what the converter produced. Re-running
  the conversion overwrites it.
- **The patch** (`{key}.duzeltme.json`) is the diff of the edits made by hand.
  The converter never touches it.

The designer **never modifies the raw layout**. On save it extracts the
difference between the raw version and the working copy and writes that to the
patch file. Two things follow: your work isn't lost when the layout is converted
again, and returning a property to its original value removes it from the file
as well.

A patch carries box properties, band properties, added and deleted boxes, and
added and deleted bands. In other words there's no change that forces you to
abandon the raw layout — the generated file can always stay exactly as it is.

Bands and boxes are matched **by name**, and the designer checks every name in
the layout when it generates a new one. A band's **type and vertical position**
can't be patched: both determine print order (see below), and changing them
means redesigning the report. When you add a band you can choose where it goes —
it lands directly beneath the currently selected one.

## Band order

In a FastReport template, **the order in the file is not the order on paper**:
bands print by type, and body bands are sorted by their vertical position in the
design. The channel to the left of the canvas lists them in paper order, with
their roles and their dependencies — the one thing you cannot see by reading the
layout's JSON.

The order is computed by `BantSirasi.BasimSirasi(page)`. Your own renderer
should use it too; if the two sides compute it separately, the designer will
eventually show an order that never reaches the paper.

## Shortcuts

| Key | Action |
| --- | --- |
| Click | Select a box |
| Ctrl + click | Add to / remove from the selection |
| Drag | Move the selected boxes (select them first) |
| Drag a handle | Resize (single selection) |
| Arrow keys | Nudge by one grid step |
| Shift + arrow | Nudge by ten steps |
| Ctrl + Z / Ctrl + Y | Undo / redo |
| Drag a band's bottom edge | Band height (minimum 4 pt, and never above the lowest box) |

Clicking a band in the channel selects it; while a band is selected, the
palette's **object** buttons add boxes to it — or you drag the button straight
onto the paper. The **band** buttons put the new band directly beneath the
selected one.

## Units

Everything is in **points** (1/72 inch) — PDF's own unit. On screen it's
converted with `points × 96/72`, and zoom is a CSS `transform: scale()`.
Millimetres are never stored: they're easy to read, but they produce rounding on
every conversion, and then the edges of two columns don't line up on paper.

## The canvas is not a print preview

A browser and a PDF engine don't break lines the same way. The canvas is a
**layout surface**: it shows where a box was put, not what it prints. The
measure of fidelity should be the PDF preview beside it.

## Themes

The component follows the `<html data-theme="dark">` attribute and the operating
system preference. Bind the colours to your own design system through the
`--rt-*` CSS variables:

```css
/* .rt-kok is the root of both screens — the list and the designer share
   the same variables. */
.rt-kok {
    --rt-panel: var(--my-panel-colour);
    --rt-vurgu: var(--my-accent);
}
```

The paper's colour is deliberately outside the theme: the output is printed on
white paper, and a designer that shows it grey in dark mode makes you take
layout decisions against the wrong background.

## Sizing

The designer fills the height it's given; it doesn't stretch with the page flow.
If its container gives it no height, the paper isn't visible at all.

## Languages

The **user interface** speaks English (default) and Turkish. Localisation uses
the standard .NET mechanism — `.resx` resources compiled into satellite
assemblies — and the language follows `CultureInfo.CurrentUICulture`, i.e.
whatever your application already decides:

```csharp
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures("en", "tr")
    .AddSupportedUICultures("en", "tr"));
```

The library has no language setting of its own; a second setting would only
contradict yours. The same culture also decides the **formats**: whether an
amount reads `1,234.56` or `1.234,56`, and how a date is written.

In Blazor Server the circuit inherits the culture of the first request, so
switching language needs a full page reload — the sample app shows one way
(a cookie plus a small endpoint).

**Adding a language is one file:** `Kaynak.<code>.resx` next to `Kaynak.resx`.
No code changes. A string with no translation falls back to English instead of
breaking the screen. Pull requests with new languages are welcome.

## A note on the API language

**The code, the comments and the public API names are in Turkish**
(`CetvelDuzeni`, `IDuzenDeposu`, `RaporVerisi`). That's a deliberate choice, not
an unfinished translation: this is a tool for a Turkish reporting stack, and
renaming the surface would strand the comments and the domain vocabulary in a
second language.

Here's the map. It's shorter than you'd expect:

| Turkish | English |
| --- | --- |
| `CetvelDuzeni` · `DuzenSayfasi` | report layout · layout page |
| `DuzenBandi` · `DuzenNesnesi` | band · object (box) |
| `IDuzenDeposu` · `DosyaDuzenDeposu` | layout store · file-system store |
| `DuzenDuzeltmesi` | layout patch |
| `RaporVerisi` · `VeriKumesi` · `VeriSatiri` | report data · dataset · row |
| `IRaporAraci` | report tool (opens the designer / preview) |
| `RaporTasarimci` · `DuzenListesi` | designer component · list component |
| `BantSirasi.BasimSirasi` | band order · print order |
| `Bicimleme` · `OrnekVeri` · `KagitBoyu` | formatting · sample data · paper size |
| `Ekle` · `Degisken` · `Tanit` | add · variable · declare |
| `ListeleAsync` · `HamGetirAsync` · `OlusturAsync` | list · get raw · create |
| `DuzeltmeGetirAsync` · `DuzeltmeKaydetAsync` | get patch · save patch |
| `TasarimAc` · `Onizle` · `ListeAc` · `Bagla` | open designer · preview · open list · bind |

## Tests

```
dotnet test
```

173 tests check the library's own contract, and they run everywhere.

On top of those there's another set that runs against **real layout files**,
verifying that patch extraction is lossless and that an untouched layout
produces no spurious patch. Those files aren't in this repo — they live in the
host application's `Cikti/Duzenler` folder — and when they can't be found, those
tests are **skipped silently**. The path is given through an environment
variable:

```
PICA_DUZENLER=D:\...\App\Cikti\Duzenler dotnet test
```

The split is deliberate: the library has to be testable on its own, but ignoring
a real corpus when one is at hand would have been silly.

## Contributing and licence

MIT licensed. Copyright **Papirus Yazılım Ltd. Şti.** — <https://www.papirusbilisim.com>

See [CONTRIBUTING.md](CONTRIBUTING.md) and [CHANGELOG.md](CHANGELOG.md).

Issues and pull requests are welcome. English is fine — the API names stay
Turkish, but the conversation doesn't have to.

---

<sub>Keywords: Blazor report designer · banded report designer for .NET ·
Blazor reporting component · FastReport alternative · rapor tasarım aracı ·
Blazor rapor tasarımcısı · bantlı rapor · .NET rapor aracı ·
Papirus Yazılım Ltd. Şti.</sub>
