# Pixata.Pdf [![Pixata.Pdf Nuget package](https://img.shields.io/nuget/v/Pixata.Pdf)](https://www.nuget.org/packages/Pixata.Pdf/)

Helpers for working with PDFs that you already have, such as joining a cover page your app made to a document the user uploaded. To make PDFs from Blazor components, see the `DocumentTemplateHelper` in [Pixata.AspNetCore](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.AspNetCore/Readme.Documents.md), with the [Playwright converter](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.AspNetCore.Pdf.Playwright).

It uses [PdfPig](https://github.com/UglyToad/PdfPig), which is Apache 2.0 licensed, so it's free to use in any app. (PDFsharp, the better known library, moved to the AGPL licence in version 6.5, which would oblige most apps using it to publish their source code.) It doesn't depend on ASP.NET Core, so it can be used in any .NET 8 or 10 app.

A [Nuget package](https://www.nuget.org/packages/Pixata.Pdf/) is available for this project.

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
