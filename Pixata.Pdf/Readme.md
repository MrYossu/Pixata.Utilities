# Pixata.Pdf [![Pixata.Pdf Nuget package](https://img.shields.io/nuget/v/Pixata.Pdf)](https://www.nuget.org/packages/Pixata.Pdf/)

![Pixata](https://raw.githubusercontent.com/MrYossu/Pixata.Utilities/master/Pixata.Pdf/pdf.png "Pixata")

Helpers for working with PDFs that you already have, such as joining a cover page your app made to a document the user uploaded, drawing the notes and highlights users added in `PixataPdfViewer` into a copy of a PDF, or reading a PDF's text. To make PDFs from Blazor components, see the `DocumentTemplateHelper` in [Pixata.AspNetCore](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.AspNetCore/Readme.Documents.md), with the [Playwright converter](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.AspNetCore.Pdf.Playwright).

It uses [PdfPig](https://github.com/UglyToad/PdfPig), which is Apache 2.0 licensed, so it's free to use in any app. (PDFsharp, the better known library, moved to the AGPL licence in version 6.5, which would oblige most apps using it to publish their source code.) It doesn't depend on ASP.NET Core, so it can be used in any .NET 8 or 10 app.

A [Nuget package](https://www.nuget.org/packages/Pixata.Pdf/) is available for this project.

>**Version 1.2.0** adds `PdfText`, which reads the text of each page of a PDF, with the words in reading order, and counts its pages, such as to check in a test that a document your app made says what it should. It replaces Telerik's `PdfFormatProvider` in document tests. See [reading the text of a PDF](#reading-the-text-of-a-pdf) (#184)

>**Version 1.1.0** adds `PdfMarkupFlattener`, which draws mark-up added with Pixata.Blazor.PdfViewer (notes, highlights, strike-through, drawing and text) into a copy of a PDF, with the notes listed on a page at the end. See [drawing mark-up into a PDF](#drawing-mark-up-into-a-pdf) (#140)

>**Version 1.0.0** adds `PdfCombiner`, which combines several PDFs into one (#141).

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
