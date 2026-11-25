# Pixata.Pdf.Documents [![Pixata.Pdf.Documents Nuget package](https://img.shields.io/nuget/v/Pixata.Pdf.Documents)](https://www.nuget.org/packages/Pixata.Pdf.Documents/)

![Pixata](https://raw.githubusercontent.com/MrYossu/Pixata.Utilities/master/Pixata.Pdf.Documents/pdf.png "Pixata")

Turns a document into a PDF: Word (`.docx`), RTF, HTML, a text file, or a picture (PNG or JPEG). A typical use is a CV uploaded in whatever format the person had, which your app stores as a PDF so that everyone can read it in the browser. It keeps paragraphs, headings, bold, italic and underlined text, colours and sizes, bulleted and numbered lists, tables and pictures, and lays them out on A4 pages (or whatever size you like).

It's written in .NET alone, with no Chromium, Office, LibreOffice or other native dependency, so it runs on ordinary shared hosting, and it needs no licence. It reads Word documents with Microsoft's [Open XML SDK](https://github.com/dotnet/Open-XML-SDK) (MIT), HTML with [AngleSharp](https://github.com/AngleSharp/AngleSharp) (MIT), and writes the PDF with [PdfPig](https://github.com/UglyToad/PdfPig) (Apache 2.0). It's a package of its own, rather than part of [Pixata.Pdf](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Pdf), so that apps that only combine or read PDFs don't take those libraries and the fonts.

A [Nuget package](https://www.nuget.org/packages/Pixata.Pdf.Documents/) is available for this project.

>**Version 1.0.0** is the first version, with `DocumentToPdf`, which turns Word, RTF, HTML, text and pictures into PDFs. It replaces Telerik Document Processing's Flow format providers for this (#194)

## Converting a document

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

### Word 97-2003 documents

Word 97-2003 documents (`.doc`) are in a binary format that no free .NET library reads well, so they throw a `DocumentToPdfException` saying that they need saving as a Word document (`.docx`) first. (Telerik's `DocxFormatProvider` can't read them either.) A `.doc` that's really an RTF document or a web page, as Word saves if asked, is converted as what it is.

### HTML

HTML is read for its structure, not laid out as a browser would, so CSS stylesheets, positioning, floats, flexbox, grids, background colours and web fonts are left out. Pictures are only included if they're in the page, as `data:` URLs, as `Convert()` doesn't fetch anything from the web or the disk. That's what's wanted for a document someone saved as a web page, but not for one that relies on its CSS. For those, print the page through Chromium with [Pixata.AspNetCore.Pdf.Playwright](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.AspNetCore.Pdf.Playwright), which needs Chromium on the server, but lays the page out exactly as Chrome would...

```csharp
// Registered with builder.Services.AddPixataPlaywrightPdf()
public class PageToPdf(PdfConverterInterface converter) {
  public Task<byte[]> Convert(string html) =>
    converter.Convert(html, new());
}
```

## Options

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

### Fonts

All the text is drawn in one font family, whatever fonts the document names, as a server rarely has the document's fonts, and embedding them would need their licences to allow it. The package includes [Liberation Sans](https://github.com/liberationfonts/liberation-fonts), which is the same size as Arial, so lines and pages break in much the same places as they do in Word, and has the Latin, Greek, Cyrillic and Hebrew alphabets. Hebrew, and other text that reads from the right, is laid out from the right. Only the characters a document uses are embedded in its PDF.

Characters that the font doesn't have, such as Chinese, Japanese or Arabic, are drawn as `?`. For documents in those languages, pass a font that has them in the options, such as [Noto Sans](https://fonts.google.com/noto).

Liberation Sans is under the [SIL Open Font License](https://openfontlicense.org/), which lets you use and embed it in anything, commercial or not. The licence is in `OFL.txt` in the package.

## Moving from Telerik Document Processing

| Telerik | Pixata.Pdf.Documents |
| --- | --- |
| `new DocxFormatProvider().Import(stream)`, then `new PdfFormatProvider().Export(document, output)` | `await DocumentToPdf.Convert(stream, ".docx")` |
| The same with `RtfFormatProvider` or `HtmlFormatProvider` | `await DocumentToPdf.Convert(stream, ".rtf")`, or `".html"` |
| A PNG or JPEG put on a `RadFixedPage` with `FixedContentEditor.DrawImage()` | `DocumentToPdf.Convert(picture, ".png")`, or `".jpg"` |

To put a cover page in front, or join the result to other PDFs, use `PdfCombiner` in [Pixata.Pdf](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Pdf).

## Testing

`PdfText` in [Pixata.Pdf](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Pdf#reading-the-text-of-a-pdf) reads the PDF's text back, so you can check that a conversion kept what it should...

```csharp
byte[] pdf = DocumentToPdf.Convert(File.ReadAllBytes("CV.docx"), "CV.docx");
List<string> pages = PdfText.Read(pdf);

Assert.AreEqual(2, pages.Count);
StringAssert.Contains(pages[0], "Name: Zoë Brontë");
```
