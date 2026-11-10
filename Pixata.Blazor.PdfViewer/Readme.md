# Pixata.Blazor.PdfViewer [![Pixata.Blazor.PdfViewer Nuget package](https://img.shields.io/nuget/v/Pixata.Blazor.PdfViewer)](https://www.nuget.org/packages/Pixata.Blazor.PdfViewer/)

`PixataPdfViewer` shows a PDF in a Blazor page, with page navigation, zoom (fit width, fit page and percentages), rotation, text that can be selected and searched, and download and print buttons. It fills its container, so it can sit beside a form in a split layout, which is what it was written for: checking invoices against the details typed in from them.

The PDF is drawn by [PDF.js](https://mozilla.github.io/pdf.js/), Mozilla's PDF engine, which is part of this package, so nothing is loaded from a CDN. It works in Blazor Server and WebAssembly, and is styled by the same `--pixata-blazor-*` variables as the components in [Pixata.Blazor](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor), which it uses for its toolbar.

It's a separate package because PDF.js and the files it needs come to about 5MB. They're only downloaded by a browser that shows a PDF, but they'd be in every app that uses Pixata.Blazor.

See the [live demo](https://test.pixata.co.uk/PdfViewerSample) for how it looks, and the [source for that page](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/PdfViewerSample.razor) for the code.

>**Version 1.0.0** is the first release (#139).

## Contents

- [Setup](#setup)
- [Basic use](#basic-use)
- [Where the PDF comes from](#where-the-pdf-comes-from)
- [Size and layout](#size-and-layout)
- [The toolbar](#the-toolbar)
- [Pages and zoom](#pages-and-zoom)
- [Events](#events)
- [Parameters](#parameters)
- [Methods and properties](#methods-and-properties)
- [Translating](#translating)
- [Styling](#styling)
- [Content Security Policy](#content-security-policy)
- [How it works](#how-it-works)

## Setup

Install the package, add its stylesheet to your host page after Pixata.Blazor's...

```xml
<link rel="stylesheet" href="_content/Pixata.Blazor/pixata.css" />
<link rel="stylesheet" href="_content/Pixata.Blazor.PdfViewer/pixata-pdfviewer.css" />
```

...and add `@using Pixata.Blazor.PdfViewer` to your `_Imports.razor`. There's nothing to register. PDF.js's own styles, which lay out the pages and their text, are added to the page by the viewer's script when it's first used.

## Basic use

```xml
<div style="height: 80vh;">
  <PixataPdfViewer Url="api/invoices/1042/pdf" FileName="INV-1042.pdf" />
</div>
```

## Where the PDF comes from

Give the viewer one of these. Giving more than one throws an `InvalidOperationException`, and giving none shows "No document".

| Parameter | Type | Description |
| --- | --- | --- |
| `Url` | `string?` | A URL the browser fetches the PDF from. It can be an endpoint of your app that the user's cookies give access to. A URL on another site needs that site to allow it with CORS. PDF.js asks for the parts of the file it needs as it needs them, if the server supports range requests, so the first page of a large PDF shows quickly |
| `Data` | `byte[]?` | The PDF itself, say from a database. It's streamed to the browser, so a large file doesn't have to fit in one SignalR message |
| `Stream` | `Stream?` | A stream to read the PDF from. It's read from its current position, and isn't disposed, so dispose of it yourself afterwards |

Setting a different URL, array or stream shows the new PDF. While it's loading, the viewer says "Loading...", and if it can't be shown, the viewer says why (see [events](#events)).

## Size and layout

By default the viewer fills the width and height of its container, and is at least `--pixata-blazor-pdfviewer-min-height` (20rem) tall, so give the container a height. `Height` and `Width` set the size instead, as CSS sizes.

To show the PDF beside a form, put both in a grid...

```xml
<div style="display: grid; grid-template-columns: 3fr 2fr; gap: 1rem; height: calc(100vh - 8rem);">
  <PixataPdfViewer Data="_invoice.Pdf" FileName="@($"{_invoice.Number}.pdf")" Zoom="PixataPdfViewerZoom.FitPage" />
  <EditForm Model="_invoice" OnValidSubmit="Approve">
    ...
  </EditForm>
</div>
```

When the viewer changes size, a fitted zoom is worked out again, so the pages still fit.

## The toolbar

From left to right, the toolbar has...

- the previous and next page buttons, with a box showing the page number, which you can type a page number into
- the zoom out and zoom in buttons, with a drop-down of fit width, fit page and percentages from 50% to 400%
- the rotate anticlockwise and clockwise buttons, which only change how the pages are shown, not the PDF
- the search box. Matches are highlighted on every page, and the box says how many there are. Enter goes to the next match and Shift+Enter to the previous one, as do the up and down buttons
- the download and print buttons

`ShowSearch`, `ShowRotate`, `ShowDownload` and `ShowPrint` turn parts off, and `ShowToolbar="false"` turns it all off. When the toolbar is too narrow for everything, it wraps onto a second line.

As well as the toolbar, the pages can be scrolled with the mouse, the keyboard (once the pages have the focus) or a finger. Holding Ctrl and turning the mouse wheel, or a pinch on a touchpad, zooms around the pointer. The text can be selected and copied, and links in the PDF work: one to another page goes to it, and one to a web site opens in a new tab.

Printing draws each page as an image at 150 dots per inch, and prints them from a hidden frame. That works the same in every browser, which handing the PDF to the browser's own viewer doesn't. The print button is busy while the pages are drawn.

## Pages and zoom

`Page` is the page shown, counting from 1. Bind it to know which page the user is on, and set it to show a page...

```xml
<PixataPdfViewer Data="_pdf" @bind-Page="_page" />
<p>Page @_page</p>
<button @onclick="() => _page = 3">Show the delivery note</button>
```

`Zoom` is how big the pages are shown...

| `Zoom` | Pages shown |
| --- | --- |
| `PixataPdfViewerZoom.FitWidth` | As wide as the viewer (the default) |
| `PixataPdfViewerZoom.FitPage` | Each page fits inside the viewer, so a whole page can be seen at once |
| A number, such as `1.5m` or `PixataPdfViewerZoom.Scale(1.5m)` | At that scale, where 1 is 100%. It's kept between 0.1 and 25 |

Each PDF is opened at `Page`, or at its last page if it hasn't that many, in which case `PageChanged` says so. So when you show another PDF, set `Page` back to 1 as well if you bind it. Changing either parameter changes what's shown. A parent that sets them without binding them, such as `Zoom="1m"`, doesn't undo what the user has done whenever it renders, as only a change to the parameter's value counts.

## Events

| Event | Arguments | Fires |
| --- | --- | --- |
| `OnLoaded` | `PixataPdfViewerLoadedEventArgs`, with `PageCount` | When a PDF has been opened |
| `OnError` | `PixataPdfViewerErrorEventArgs`, with `Kind` and `Detail` | When a PDF can't be shown. `Kind` is `PasswordProtected`, `NotAPdf`, `NotFound` or `Other` (a network failure, say), and `Detail` is PDF.js's description, for a log. The viewer shows a message as well |
| `OnDownload` | `PixataPdfViewerDownloadEventArgs`, with `FileName` and `IsCancelled` | When the download button is clicked, before the PDF is downloaded. Change `FileName`, or set `IsCancelled` to do something else instead |
| `PageChanged` | `int` | When the page shown changes. Use `@bind-Page` |

```csharp
private void Downloading(PixataPdfViewerDownloadEventArgs args) =>
  args.FileName = $"{_invoice.Supplier} {_invoice.Number}.pdf";
```

PDFs protected by a password can't be shown yet.

## Parameters

| Parameter | Type | Description |
| --- | --- | --- |
| `Url`, `Data`, `Stream` | | The PDF. See [where the PDF comes from](#where-the-pdf-comes-from) |
| `FileName` | `string` | The name the PDF is saved as when it's downloaded. Defaults to `document.pdf` |
| `Zoom` | `PixataPdfViewerZoom` | How big the pages are shown. Defaults to `FitWidth` |
| `Page` | `int` | The page shown, counting from 1. Use `@bind-Page` |
| `ShowToolbar` | `bool` | Shows the toolbar. Defaults to `true` |
| `ShowSearch`, `ShowRotate`, `ShowDownload`, `ShowPrint` | `bool` | Show those parts of the toolbar. Default to `true` |
| `Height`, `Width` | `string?` | The viewer's size, as CSS sizes. By default it fills its container |
| `Class` | `string?` | Extra classes for the outer element |
| `Id` | `string?` | The outer element's id |
| `Messages` | `PixataPdfViewerMessages?` | The text the viewer shows, for translating |

## Methods and properties

| Member | Description |
| --- | --- |
| `GoToPage(int page)` | Shows a page, counting from 1 |
| `SetZoom(PixataPdfViewerZoom zoom)` | Sets the zoom |
| `Rotate(bool clockwise = true)` | Turns the pages a quarter turn |
| `Search(string text)` | Searches for some text, highlighting every match and showing the first. An empty string clears the search |
| `SearchNext()`, `SearchPrevious()` | Show the next or previous match |
| `Download()` | Downloads the PDF, raising `OnDownload` first |
| `Print()` | Prints the PDF |
| `IsLoaded` | Whether a PDF is being shown |
| `PageCount` | The number of pages, or 0 if there's no PDF |
| `CurrentPage` | The page shown |

```xml
<PixataPdfViewer @ref="_viewer" Url="@_url" />
<button @onclick="() => _viewer.Search(_invoice.Number)">Find the invoice number</button>

@code {
  private PixataPdfViewer _viewer = null!;
}
```

## Translating

Give the viewer a `PixataPdfViewerMessages` with your own text. Its properties are the buttons' tooltips and accessible names, the drop-down's options, and the messages. `PageCountFormat` ("of {0}") and `MatchesFormat` ("{0} of {1}") are format strings...

```xml
<PixataPdfViewer Url="@_url" Messages="_french" />

@code {
  private static readonly PixataPdfViewerMessages _french = new() {
    PreviousPage = "Page précédente",
    NextPage = "Page suivante",
    PageCountFormat = "sur {0}",
    Search = "Rechercher",
    NoMatches = "Aucun résultat"
  };
}
```

## Styling

The viewer is styled by these custom properties, which you can set on `:root` or, with `Class`, on a particular viewer, as well as the shared `--pixata-blazor-*` ones (text colour, font, border radius and so on)...

| Property | Default |
| --- | --- |
| `--pixata-blazor-pdfviewer-min-height` | `20rem` |
| `--pixata-blazor-pdfviewer-border-color` | `--pixata-blazor-input-border-color` |
| `--pixata-blazor-pdfviewer-toolbar-bg` | `--pixata-blazor-surface-alt-bg` |
| `--pixata-blazor-pdfviewer-bg` | `#e9ecef`, behind the pages |
| `--pixata-blazor-pdfviewer-page-shadow` | A small shadow under each page |
| `--pixata-blazor-pdfviewer-page-gap` | `.75rem`, between the pages |
| `--pixata-blazor-pdfviewer-match-bg` | A pale yellow, behind each search match |
| `--pixata-blazor-pdfviewer-current-match-bg` | An orange, behind the match being shown |

The elements have these classes...

| Class | Element |
| --- | --- |
| `pixata-blazor-pdfviewer` | The outer element |
| `pixata-blazor-pdfviewer-toolbar` | The toolbar, made of groups of tools (`pixata-blazor-pdfviewer-tools`) |
| `pixata-blazor-pdfviewer-body` | Everything under the toolbar |
| `pixata-blazor-pdfviewer-container` | The scrolling area the pages are in. Inside it, the pages are PDF.js's `.pdfViewer .page` |
| `pixata-blazor-pdfviewer-status` | The message shown while there's no PDF to show, with `pixata-blazor-pdfviewer-error` for an error |

## Content Security Policy

If your app has a Content Security Policy, the viewer needs...

- `worker-src 'self'`, for PDF.js's worker, which reads the PDF away from the page
- `script-src 'wasm-unsafe-eval'`, for the image decoders that some PDFs need (scanned documents often use JBIG2 or JPEG 2000 images). Without it, PDF.js falls back to slower JavaScript decoders
- `img-src blob:`, for printing
- `connect-src` to include wherever a `Url` points

## How it works

The component renders the toolbar and an empty scrolling area, and the script (`wwwroot/pdfjs/pixata-pdfviewer.js`) has PDF.js's viewer draw the pages in that area, telling the component the page, zoom and search matches as they change. Pages are drawn as they're scrolled to, so a long PDF opens quickly.

The script is PDF.js 6.5 and a little code of this package's, bundled into one file. It's built from [build/pixata-pdfviewer](https://github.com/MrYossu/Pixata.Utilities/tree/master/build/pixata-pdfviewer), which explains how to rebuild it. Next to it are PDF.js's worker, its styles, and the files it loads only when a PDF needs them: character maps for Chinese, Japanese and Korean text, the standard fonts for PDFs that don't include their own, and the image decoders. Each is used under its own licence, which is next to it: PDF.js under the Apache License 2.0, and the rest under BSD, MIT and SIL Open Font licences.
