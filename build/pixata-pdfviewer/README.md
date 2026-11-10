# pixata-pdfviewer

The source of `Pixata.Blazor.PdfViewer/wwwroot/pdfjs`, the browser half of `PixataPdfViewer`. It is
[PDF.js](https://mozilla.github.io/pdf.js/) (Apache 2.0 licensed) and a little code of the package's own, bundled with
[esbuild](https://esbuild.github.io) into one minified ES module, next to the files PDF.js loads as it needs them. The built
files are committed, so you only need this folder when you change the viewer's JavaScript or update PDF.js. The `.csproj`
files don't build it, and nothing here is packed into the NuGet package except the built files.

| File | What's in it |
| --- | --- |
| `src/global.js` | Puts PDF.js where its viewer components look for it (`globalThis.pdfjsLib`). It's imported first, so that happens before they load |
| `src/index.js` | The functions the .NET component calls: creating the viewer, opening a PDF from a URL or a stream, zoom, pages, rotation, search, download and print, and the events it sends back |
| `src/markup.js` | Draws the mark-up over a page, as SVG. Pixata.Pdf's `PdfMarkupFlattener` draws the same mark-up into a PDF, so change the two together, and check that a flattened PDF still looks like the viewer |
| `src/markup-editor.js` | The mark-up tools: adding notes, highlights, strike-through, drawing and text, the pop-up that shows them, keeping the mark-up on the pages as PDF.js draws them again, and printing it |
| `build.mjs` | Builds the bundle, with PDF.js's licence notice in its header, and copies the worker, the viewer's stylesheet, the character maps, the standard fonts and the image decoders next to it |

## Rebuilding

You need Node.js 18 or later.

```bash
cd build/pixata-pdfviewer
npm ci
npm run build
```

`npm run build` empties `Pixata.Blazor.PdfViewer/wwwroot/pdfjs` and writes it again. Commit it along with your changes to
`src/`.

The package versions in `package.json` are pinned exactly, and `package-lock.json` is committed, so a rebuild gives the same
files. To update PDF.js, install the new version with `npm install --save-exact pdfjs-dist@<version>`, rebuild, and check the
viewer on the sample site's `/PdfViewerSample` page: pages, zoom (including Ctrl and the mouse wheel), rotation, search,
download, print, the error messages, and each mark-up tool, at another zoom and rotation, and printed. PDF.js's viewer components aren't a stable API, so read its release notes for
changes to the events and options `src/index.js` and `src/markup-editor.js` use. The mark-up relies on PDF.js's
`pagerender` and `pagerendered` events, and on its page views' `viewport`, `pdfPage` and `div`.

## What's left out

ASP.NET Core only serves static files whose type it knows. It doesn't know `.bcmap` (the character maps) or `.icc` (the CMYK
colour profile), so the build renames the character maps to `.bcmap.bin`, and `src/index.js` asks for them by those names
through its own `BinaryDataFactory`. PDF.js can't be told another name for the colour profile, so it's left out, and PDF.js
uses its own CMYK conversion instead.

QuickJS, which PDF.js only uses to run the scripts in a PDF's forms, is left out too, as is the rest of PDF.js's own viewer
(its sidebar, toolbar, dialogs and their images), which `PixataPdfViewer` replaces.
