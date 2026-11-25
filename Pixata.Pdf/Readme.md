# Pixata.Pdf [![Pixata.Pdf Nuget package](https://img.shields.io/nuget/v/Pixata.Pdf)](https://www.nuget.org/packages/Pixata.Pdf/)

![Pixata](https://raw.githubusercontent.com/MrYossu/Pixata.Utilities/master/Pixata.Pdf/pdf.png "Pixata")

Helpers for making and working with PDFs: turning a Word document, RTF, HTML, a text file or a picture into a PDF, such as a CV a user uploaded, joining a cover page your app made to a document the user uploaded, drawing the notes and highlights users added in `PixataPdfViewer` into a copy of a PDF, or reading a PDF's text. To make PDFs from Blazor components, see the `DocumentTemplateHelper` in [Pixata.AspNetCore](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.AspNetCore/Readme.Documents.md), with the [Playwright converter](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.AspNetCore.Pdf.Playwright).

It's written in .NET alone, with no Chromium, Office, LibreOffice or other native dependency, so it runs on ordinary shared hosting, in any .NET 8 or 10 app, and it needs no licence. It uses [PdfPig](https://github.com/UglyToad/PdfPig), which is Apache 2.0 licensed, so it's free to use in any app. (PDFsharp, the better known library, moved to the AGPL licence in version 6.5, which would oblige most apps using it to publish their source code.) `DocumentToPdf` also reads Word documents with Microsoft's [Open XML SDK](https://github.com/dotnet/Open-XML-SDK), and HTML with [AngleSharp](https://github.com/AngleSharp/AngleSharp), which are both MIT licensed.

A [Nuget package](https://www.nuget.org/packages/Pixata.Pdf/) is available for this project.

>**Version 1.3.0** adds `DocumentToPdf`, which turns Word documents (`.docx`), RTF, HTML, text files and pictures into PDFs, keeping headings, bold and italic, lists, tables and pictures. It replaces Telerik Document Processing's Flow format providers for this. See [turning documents into PDFs](#turning-documents-into-pdfs). It brings in the Open XML SDK and AngleSharp, and the fonts it uses, which add about 10MB to an app (#194)

>**Version 1.2.0** adds `PdfText`, which reads the text of each page of a PDF, with the words in reading order, and counts its pages, such as to check in a test that a document your app made says what it should. It replaces Telerik's `PdfFormatProvider` in document tests. See [reading the text of a PDF](#reading-the-text-of-a-pdf) (#184)

>**Version 1.1.0** adds `PdfMarkupFlattener`, which draws mark-up added with Pixata.Blazor.PdfViewer (notes, highlights, strike-through, drawing and text) into a copy of a PDF, with the notes listed on a page at the end. See [drawing mark-up into a PDF](#drawing-mark-up-into-a-pdf) (#140)

>**Version 1.0.0** adds `PdfCombiner`, which combines several PDFs into one (#141).

## Turning documents into PDFs

`DocumentToPdf` turns a document into a PDF: Word (`.docx`), RTF, HTML, a text file, or a picture (PNG or JPEG). A typical use is a CV uploaded in whatever format the person had, which your app stores as a PDF so that everyone can read it in the browser. It keeps paragraphs, headings, bold, italic and underlined text, colours and sizes, bulleted and numbered lists, tables and pictures, and lays them out on A4 pages (or whatever size you like). It's in the `Pixata.Pdf.Documents` namespace.

`DocumentToPdf.Convert()` takes the file's contents and its name (or just its extension), and returns the PDF...

```csharp
byte[] pdf = DocumentToPdf.Convert(file, "CV.docx");
```

There's an async overload that takes a `Stream`, such as an uploaded file. The stream is read from its current position to the end, and is left open, as it's yours...

```csharp
// Blazor
await using Stream stream = browserFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
byte[] pdf = await DocumentToPdf.Convert(stream, browserFile.Name);

// ASP.NET Core
await using Stream stream = formFile.OpenReadStream();
byte[] pdf = await DocumentToPdf.Convert(stream, formFile.FileName);
```

What's in the file counts for more than its name, as files get renamed, and Word saves RTF and web pages with a `.doc` extension if asked. So a Word document called `CV.doc`, or an RTF document called `CV.docx`, is read as what it really is. A PDF is returned as it is, so you can pass every upload through `Convert()` without checking which ones are already PDFs.

`DocumentToPdf.CanConvert(fileName)` says whether a file can be converted, going by its extension, such as to check an upload before reading it, and `DocumentToPdf.Extensions` lists the extensions, such as for the `accept` attribute of a file input...

```razor
<InputFile OnChange="Upload" accept="@string.Join(",", DocumentToPdf.Extensions)" />
```

### When a file can't be converted

A file that can't be converted throws a `DocumentToPdfException`, rather than giving an empty PDF. Its message says why in words you can show the user, eg...

| The file | The message |
| --- | --- |
| Empty | The file is empty |
| A Word 97-2003 document (`.doc`) | Word 97-2003 documents (.doc) can't be converted. Open it in Word and save it as a Word document (.docx) |
| A Word document with a password | The Word document is protected with a password, so it can't be read |
| Not what its name says, eg a web page called `CV.docx` | The file isn't a Word document. It may be damaged, or have the wrong name |
| Damaged | The Word document couldn't be read. It may be damaged. ... |
| A kind that can't be converted, eg `.xlsx` | .xlsx files can't be converted. Only .docx, .docm, .dotx, .dotm, .rtf, .html, .htm, .txt, .png, .jpg, .jpeg and .pdf files can |

```csharp
try {
  cv.Pdf = await DocumentToPdf.Convert(stream, browserFile.Name);
} catch (DocumentToPdfException ex) {
  _error = ex.Message;
}
```

### What each format keeps

| Format | Extensions | What's kept |
| --- | --- | --- |
| Word | `.docx`, `.docm`, `.dotx`, `.dotm` | Paragraphs and headings, with the fonts' sizes and colours, bold, italic, underlining and strike-through that the document's styles and formatting give. Alignment, indents, spacing, and right-to-left paragraphs. Bulleted and numbered lists, numbered as Word numbers them (including `1.a)` style numbers, restarted lists, letters, Roman and Hebrew numbers). Tables, with their columns' widths, merged cells and borders. Pictures, at the size they are in the document. Page breaks, and the text of text boxes. Inserted text from tracked changes is kept, and deleted and hidden text left out |
| RTF | `.rtf` | The same as Word, from RTF written by Word, WordPad or LibreOffice. Text in other code pages, such as Hebrew or Greek, and the Symbol font's Greek letters and bullets, are read as the characters they are |
| HTML | `.html`, `.htm` | The page's structure: headings, paragraphs, bold, italic and so on, lists, tables (with `colspan` and borders), pictures, rules and line breaks, with the colours, sizes, alignment and direction given in `style` attributes, and `page-break-before` and `-after`. See [HTML](#html) for what isn't |
| Text | `.txt` | Each line as it is. The encoding is UTF-8 or UTF-16 if the file says so (or is valid UTF-8), else Windows-1252, as older Windows programs wrote |
| Pictures | `.png`, `.jpg`, `.jpeg` | The picture on a page of its own, centred at its size at 96 dots per inch, or made smaller to fit the page |
| PDF | `.pdf` | Returned as it is |

Left out, in every format: headers, footers, footnotes and comments; the document's own page size and margins (the [options](#options) set those); and floating pictures and text boxes, which come after the paragraph they're anchored to rather than where they float. Pictures in other formats, such as GIF, BMP, SVG or EMF, are left out, as only PNG and JPEG can go in a PDF as they are. A table is split between its rows when it doesn't fit on a page, and a row that's taller than a page is split too.

#### Word 97-2003 documents

Word 97-2003 documents (`.doc`) are in a binary format that no free .NET library reads well, so they throw a `DocumentToPdfException` saying that they need saving as a Word document (`.docx`) first. (Telerik's `DocxFormatProvider` can't read them either.) A `.doc` that's really an RTF document or a web page, as Word saves if asked, is converted as what it is.

#### HTML

HTML is read for its structure, not laid out as a browser would, so CSS stylesheets, positioning, floats, flexbox, grids, background colours and web fonts are left out. Pictures are only included if they're in the page, as `data:` URLs, as `Convert()` doesn't fetch anything from the web or the disk. That's what's wanted for a document someone saved as a web page, but not for one that relies on its CSS. For those, print the page through Chromium with [Pixata.AspNetCore.Pdf.Playwright](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.AspNetCore.Pdf.Playwright), which needs Chromium on the server, but lays the page out exactly as Chrome would...

```csharp
// Registered with builder.Services.AddPixataPlaywrightPdf()
public class PageToPdf(PdfConverterInterface converter) {
  public Task<byte[]> Convert(string html) =>
    converter.Convert(html, new());
}
```

### Options

`Convert()` takes an optional `DocumentToPdfOptions`...

```csharp
byte[] pdf = DocumentToPdf.Convert(file, fileName, new() {
  PageWidthMm = 215.9,  // US Letter
  PageHeightMm = 279.4,
  MarginMm = 25
});
```

| Option | What it does | Default |
| --- | --- | --- |
| `PageWidthMm`, `PageHeightMm` | The size of the pages, in millimetres. They must be more than 0, or `Convert()` throws an `ArgumentOutOfRangeException` | 210 by 297, which is A4 |
| `MarginMm` | The margin on each side, in millimetres | 20 |
| `FontSize` | The size of text whose document doesn't give one, in points, such as in text files, HTML and pictures' pages | 11 |
| `Font`, `BoldFont`, `ItalicFont`, `BoldItalicFont` | TrueType fonts (the contents of `.ttf` files) to use in place of Liberation Sans. Any face you don't give uses `Font` (or the bold face, for bold italic). They're embedded in the PDF, so check that their licences allow that | Liberation Sans |

#### Fonts

All the text is drawn in one font family, whatever fonts the document names, as a server rarely has the document's fonts, and embedding them would need their licences to allow it. The package includes [Liberation Sans](https://github.com/liberationfonts/liberation-fonts), which is the same size as Arial, so lines and pages break in much the same places as they do in Word, and has the Latin, Greek, Cyrillic and Hebrew alphabets. Hebrew, and other text that reads from the right, is laid out from the right. Only the characters a document uses are embedded in its PDF.

Characters that the font doesn't have, such as Chinese, Japanese or Arabic, are drawn as `?`. For documents in those languages, pass a font that has them in the options, such as [Noto Sans](https://fonts.google.com/noto).

Liberation Sans is under the [SIL Open Font License](https://openfontlicense.org/), which lets you use and embed it in anything, commercial or not. The licence is in `OFL.txt` in the package.

### Moving from Telerik Document Processing

| Telerik | Pixata.Pdf |
| --- | --- |
| `new DocxFormatProvider().Import(stream)`, then `new PdfFormatProvider().Export(document, output)` | `await DocumentToPdf.Convert(stream, ".docx")` |
| The same with `RtfFormatProvider` or `HtmlFormatProvider` | `await DocumentToPdf.Convert(stream, ".rtf")`, or `".html"` |
| A PNG or JPEG put on a `RadFixedPage` with `FixedContentEditor.DrawImage()` | `DocumentToPdf.Convert(picture, ".png")`, or `".jpg"` |

To put a cover page in front, or join the result to other PDFs, use [`PdfCombiner`](#combining-pdfs). To check in a test that a conversion kept what it should, read the PDF's text back with [`PdfText`](#reading-the-text-of-a-pdf).

## Combining PDFs

`PdfCombiner.Combine()` takes the PDFs in the order you want them, and returns one PDF with all their pages. Each page keeps its own size, orientation and rotation, so a landscape page in the middle of a portrait document stays landscape, and its annotations (such as comments and highlights) are kept.

```csharp
byte[] cover = await _documentTemplateHelper.CreatePdfFromTemplate<InvoiceCover>(("Invoice", invoice));
ApiResponse<byte[]> combined = PdfCombiner.Combine(cover, supplierInvoice);
if (combined) {
  await File.WriteAllBytesAsync(path, combined.Data!);
} else {
  _logger.LogWarning("Couldn't combine the PDFs: {Message}", combined.Message);
}
```

There are three overloads...

| Method | What it takes |
| --- | --- |
| `Combine(params byte[][] pdfs)` | The PDFs' contents, eg `Combine(cover, invoice)` |
| `Combine(IEnumerable<byte[]> pdfs)` | The same, from a list |
| `Combine(IEnumerable<Stream> pdfs, CancellationToken)` | Streams, such as uploaded files, or files opened from disk. Each one is read from its current position to the end, and is left open, as it's yours. This one is async |

Each returns an `ApiResponse<byte[]>` (from [Pixata.Extensions](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Extensions)), rather than throwing. If one of the PDFs can't be read, such as a file that isn't really a PDF, or one that's damaged, the response is a `Failure` whose message says which one, counting from 1, and why, eg `"PDF 2 of 3 couldn't be read: ..."`. Combining no PDFs at all is a `Failure` too.

A few things to know...

- **Each PDF is read whole**, as a PDF's index is at its end, so combining very large files takes as much memory as they do.
- **Only the pages are carried over**, with their content and annotations. Bookmarks, the document's properties (such as its title) and interactive form fields belong to the document rather than its pages, so they aren't. If a PDF has a form, fill it in and flatten it first.

## Reading the text of a PDF

`PdfText.Read()` gives the text of each page of a PDF, in order. It's meant for checking that a document your app made says what it should, such as in a test that a quote shows its customer, lines and totals, or that a long report runs onto a second page, as apps moving off Telerik did with its `PdfFormatProvider`...

```csharp
byte[] pdf = await _documentTemplateHelper.CreatePdfFromTemplate<QuoteTemplate>(("Quote", quote));
List<string> pages = PdfText.Read(pdf);

Assert.AreEqual(2, pages.Count);
StringAssert.Contains(pages[0], "Customer: Café Rouge Ltd");
StringAssert.Contains(pages[0], "Crème brûlée 2 £4.50");
StringAssert.Contains(pages[0], "Total: £15.60");
```

On each page, the lines are in order from the top, separated by new lines (`\n`), and the words on a line are in order from the left, separated by single spaces. A PDF can draw its text in any order (Chromium draws a table a cell at a time, for example), so the words are put back in the order they're read, and a table's row comes out as one line, with its cells in order, as above. A page with no text, such as a scanned one, gives `""`.

`PdfText.PageCount()` gives the number of pages, without reading the text. Both have overloads that take a `Stream` and are async, which read it from its current position to the end and leave it open.

| Method | What it gives |
| --- | --- |
| `Read(byte[] pdf)`, `Read(Stream pdf, CancellationToken)` | The text of each page, as a `List<string>`, the first page's first |
| `PageCount(byte[] pdf)`, `PageCount(Stream pdf, CancellationToken)` | The number of pages |

It reads what Chromium makes, through [Pixata.AspNetCore.Pdf.Playwright](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.AspNetCore.Pdf.Playwright), including its subset fonts and compressed objects, and text with accents and symbols such as £ and €. Unlike `PdfCombiner` and `PdfMarkupFlattener`, which return an `ApiResponse`, these throw a `PdfTextException` if the PDF can't be read, as a test should fail with the reason. That's when the data isn't a PDF, it's damaged, or it needs a password to open. A PDF with only an owner password (which stops it being changed, but lets anyone open it) is read as usual.

The words on a line are always put in order from the left, so a line in a right-to-left language, such as Hebrew, has its words the wrong way round.

## Drawing mark-up into a PDF

`PixataPdfViewer`, in [Pixata.Blazor.PdfViewer](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor.PdfViewer#mark-up), lets users add notes, highlights, strike-through, drawing and text to a PDF, which it keeps apart from the PDF, as a list of `PdfMarkupItem`s (from [Pixata.Extensions](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Extensions/Readme.SharedModels.md#pdf-mark-up)). `PdfMarkupFlattener.Flatten()` draws that mark-up into a copy of the PDF, so it can be archived, or sent to someone who hasn't the viewer, looking as it did in it...

```csharp
List<PdfMarkupItem> markup = PdfMarkup.FromJson(invoice.MarkupJson);
ApiResponse<byte[]> marked = PdfMarkupFlattener.Flatten(invoice.Pdf, markup, new() { TimeZone = userTimeZone });
if (marked) {
  await File.WriteAllBytesAsync(Path.Combine(archiveFolder, $"{invoice.Number}.pdf"), marked.Data!);
} else {
  _logger.LogWarning("Couldn't draw the mark-up into the PDF: {Message}", marked.Message);
}
```

- **Highlights, strike-through, drawing and text** are drawn on their pages. Highlights are see-through, so the text under them can still be read.
- **Each note** is drawn on its page as a numbered circle, and listed on pages added at the end, under that number, with its page, who wrote it and when.
- **The mark-up becomes part of the page**, so it can't be changed or deleted in a PDF reader, and the original PDF, which you still have, is the one to keep for the record. Pages keep their size, rotation and annotations, as `PdfCombiner` keeps them.

There's an overload that takes a `Stream`, and is async. As with `PdfCombiner`, it returns an `ApiResponse<byte[]>`, which is a `Failure` saying why if the PDF can't be read. Mark-up on a page the PDF hasn't got is left out.

`PdfMarkupFlattenOptions` says how it's drawn...

| Property | Description |
| --- | --- |
| `NotesPage` | Adds the notes pages, and numbers the notes on their pages. Defaults to `true`. When it's `false`, notes are left out altogether |
| `NotesTitle` | The heading of the notes pages. Defaults to "Notes" |
| `NotePageFormat` | How a note's page is shown, where `{0}` is the page number. Defaults to "Page {0}" |
| `TimeZone` | The time zone the notes' dates are shown in, as they're stored in UTC. Defaults to the server's, so pass the user's |
| `DateFormat` | How the dates are shown, in the current culture. Defaults to "d MMM yyyy HH:mm" |
| `HighlightOpacity` | How much a highlight hides the page under it, from 0 to 1. Defaults to 0.4, as the viewer shows it |
| `Font` | A TrueType font (the contents of a `.ttf` file) for the text and the notes. See below |

Without a `Font`, text is drawn in Helvetica, which is built into every PDF reader, but only has Western European letters. Accented letters lose their accents (café comes out as cafe), and other characters, such as Hebrew, Greek or the euro sign, come out as question marks. To draw them, give it a font that has them, such as one of the Noto fonts. It's embedded in the PDF, so check that its licence allows that (the Noto fonts' does). Hebrew text is put in the order it's read, with any numbers or English words in it the right way round. Arabic is too, but its letters are drawn separately, rather than joined up as they should be.
