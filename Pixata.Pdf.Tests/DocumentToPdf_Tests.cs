using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Pdf.Documents;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Pixata.Pdf.Tests;

[TestClass]
public class DocumentToPdf_Tests {
  // The same two-page CV in each format: made with python-docx, then saved as RTF, HTML and Word 97-2003 by LibreOffice. It has a
  // title, bold and italic text, headings, a bulleted and a numbered list, a table, a 5cm-wide picture, and a page break
  private static byte[] TestFile(string name) =>
    File.ReadAllBytes(Path.Combine("TestFiles", name));

  private static List<string> Pages(byte[] file, string fileName, DocumentToPdfOptions? options = null) =>
    PdfText.Read(DocumentToPdf.Convert(file, fileName, options));

  private static string Html(string body) =>
    $"<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>Test</title></head><body>{body}</body></html>";

  private static byte[] Bytes(string text) =>
    Encoding.UTF8.GetBytes(text);

  // ---------- The formats ----------

  [DataTestMethod]
  [DataRow("cv.docx")]
  [DataRow("cv.rtf")]
  [DataRow("cv.html")]
  public void Convert_KeepsTheTextListsAndTables(string fileName) {
    List<string> pages = Pages(TestFile(fileName), fileName);

    Assert.AreEqual(2, pages.Count);
    StringAssert.StartsWith(pages[0], "Curriculum vitae\nName: Zoë Brontë\nProfile\n");
    StringAssert.Contains(pages[0], "An experienced café manager, earning £32,000 a year, with a proven record.");
    StringAssert.Contains(pages[0], "• Rota planning\n• Stock control\n• Training new staff\nSteps\n1. Open the café\n2. Count the till");
    StringAssert.Contains(pages[0], "Years Employer Role\n2020–2024 Crème de la Crème Manager\n2016–2020 Le Petit Déjeuner Barista");
    Assert.AreEqual("References\nAvailable on request.", pages[1]);
  }

  [DataTestMethod]
  [DataRow("cv.docx")]
  [DataRow("cv.rtf")]
  [DataRow("cv.html")]
  public void Convert_KeepsBoldAndItalicText(string fileName) {
    using PdfDocument pdf = PdfDocument.Open(DocumentToPdf.Convert(TestFile(fileName), fileName));
    List<Word> words = [.. pdf.GetPage(1).GetWords()];

    StringAssert.Contains(Font(words, "Brontë"), "Bold");
    StringAssert.Contains(Font(words, "experienced"), "Italic");
    StringAssert.Contains(Font(words, "proven"), "BoldItalic");
    Assert.IsFalse(Font(words, "café").Contains("Bold") || Font(words, "café").Contains("Italic"));
  }

  private static string Font(List<Word> words, string text) =>
    words.First(word => word.Text == text).Letters[0].FontName ?? "";

  [TestMethod]
  public void Convert_Word_KeepsSizesColoursAndPictureSizes() {
    using PdfDocument pdf = PdfDocument.Open(DocumentToPdf.Convert(TestFile("cv.docx"), "cv.docx"));
    Page page = pdf.GetPage(1);
    List<Word> words = [.. page.GetWords()];

    // The title is 26pt, and the headings are blue, as their styles say
    Assert.AreEqual(26, words.First(word => word.Text == "Curriculum").Letters[0].PointSize, .1);
    (double red, double green, double blue) = words.First(word => word.Text == "Profile").Letters[0].Color.ToRGBValues();
    Assert.AreEqual(0x36 / 255.0, red, .01);
    Assert.AreEqual(0x5f / 255.0, green, .01);
    Assert.AreEqual(0x91 / 255.0, blue, .01);
    // The picture is 5cm wide
    IPdfImage image = page.GetImages().Single();
    Assert.AreEqual(5 / 2.54 * 72, image.BoundingBox.Width, .5);
    Assert.AreEqual(image.BoundingBox.Width / 2, image.BoundingBox.Height, .5);
  }

  [DataTestMethod]
  [DataRow("cv.txt")]
  [DataRow("cv-windows-1252.txt")]
  public void Convert_Text_KeepsEachLine(string fileName) {
    List<string> pages = Pages(TestFile(fileName), fileName);

    Assert.AreEqual(1, pages.Count);
    Assert.AreEqual("Curriculum vitae\nName: Zoë Brontë\nAn experienced café manager, earning £32,000 a year.", pages[0]);
  }

  [TestMethod]
  public void Convert_APicture_PutsItOnAPageAtItsOwnSize() {
    using PdfDocument pdf = PdfDocument.Open(DocumentToPdf.Convert(TestFile("logo.png"), "logo.png"));

    Assert.AreEqual(1, pdf.NumberOfPages);
    Page page = pdf.GetPage(1);
    IPdfImage image = page.GetImages().Single();
    // 400 by 200 pixels at 96 dots per inch, centred
    Assert.AreEqual(300, image.BoundingBox.Width, .5);
    Assert.AreEqual(150, image.BoundingBox.Height, .5);
    Assert.AreEqual((page.Width - 300) / 2, image.BoundingBox.Left, .5);
  }

  [TestMethod]
  public void Convert_ALargePicture_IsMadeSmallerToFitThePage() {
    using PdfDocument pdf = PdfDocument.Open(DocumentToPdf.Convert(TestFile("photo.jpg"), "photo.jpg"));

    Assert.AreEqual(1, pdf.NumberOfPages);
    Page page = pdf.GetPage(1);
    IPdfImage image = page.GetImages().Single();
    double margin = 20 * 72 / 25.4;
    // 1600 by 2400 pixels, so its height is what limits it
    Assert.IsTrue(image.BoundingBox.Height <= page.Height - 2 * margin && image.BoundingBox.Height > page.Height - 2 * margin - 10);
    Assert.IsTrue(image.BoundingBox.Bottom >= margin - .5);
    Assert.AreEqual(image.BoundingBox.Height * 2 / 3, image.BoundingBox.Width, .5);
  }

  [TestMethod]
  public void Convert_APdf_IsReturnedAsItIs() {
    byte[] pdf = DocumentToPdf.Convert(TestFile("cv.txt"), "cv.txt");

    Assert.AreSame(pdf, DocumentToPdf.Convert(pdf, "cv.pdf"));
  }

  [TestMethod]
  public async Task Convert_AStream_ConvertsIt() {
    using MemoryStream stream = new(TestFile("cv.rtf"));

    byte[] pdf = await DocumentToPdf.Convert(stream, "cv.rtf");

    Assert.AreEqual(2, PdfText.PageCount(pdf));
  }

  // ---------- What's in the file counts for more than its name ----------

  [DataTestMethod]
  [DataRow("cv.docx", "CV.DOC")]
  [DataRow("cv.rtf", "cv.doc")]
  [DataRow("cv.html", "cv.doc")]
  [DataRow("cv.rtf", "cv.docx")]
  [DataRow("cv.docx", "docx")]
  [DataRow("cv.docx", ".docm")]
  public void Convert_GoesByWhatIsInTheFile(string testFile, string fileName) {
    List<string> pages = Pages(TestFile(testFile), fileName);

    StringAssert.StartsWith(pages[0], "Curriculum vitae\nName: Zoë Brontë");
  }

  [TestMethod]
  public void Convert_APictureWithTheWrongName_IsStillAPicture() {
    using PdfDocument pdf = PdfDocument.Open(DocumentToPdf.Convert(TestFile("logo.png"), "logo.jpg"));

    Assert.AreEqual(1, pdf.GetPage(1).GetImages().Count());
  }

  // ---------- Files that can't be converted ----------

  [DataTestMethod]
  [DataRow("cv.doc", "cv.doc", "Word 97-2003 documents (.doc) can't be converted. Open it in Word and save it as a Word document (.docx)")]
  [DataRow("cv.doc", "cv.docx", "The Word document is protected with a password, so it can't be read")]
  [DataRow("cv.html", "cv.docx", "The file isn't a Word document. It may be damaged, or have the wrong name")]
  [DataRow("cv.txt", "cv.rtf", "The file isn't an RTF document, as it doesn't start with {\\rtf")]
  public void Convert_TheWrongKindOfFile_SaysWhy(string testFile, string fileName, string message) {
    DocumentToPdfException ex = Assert.ThrowsException<DocumentToPdfException>(() => DocumentToPdf.Convert(TestFile(testFile), fileName));

    Assert.AreEqual(message, ex.Message);
  }

  [TestMethod]
  public void Convert_AnEmptyFile_Throws() {
    DocumentToPdfException ex = Assert.ThrowsException<DocumentToPdfException>(() => DocumentToPdf.Convert([], "cv.docx"));

    Assert.AreEqual("The file is empty", ex.Message);
  }

  [TestMethod]
  public void Convert_AnUnknownKindOfFile_SaysWhatCanBeConverted() {
    DocumentToPdfException ex = Assert.ThrowsException<DocumentToPdfException>(() => DocumentToPdf.Convert(Bytes("a,b\n1,2"), "data.csv"));

    Assert.AreEqual(".csv files can't be converted. Only .docx, .docm, .dotx, .dotm, .rtf, .html, .htm, .txt, .png, .jpg, .jpeg and .pdf files can", ex.Message);
  }

  [TestMethod]
  public void Convert_ADamagedWordDocument_Throws() {
    byte[] file = TestFile("cv.docx");

    DocumentToPdfException ex = Assert.ThrowsException<DocumentToPdfException>(() => DocumentToPdf.Convert(file[..(file.Length / 2)], "cv.docx"));

    StringAssert.StartsWith(ex.Message, "The Word document couldn't be read. It may be damaged.");
  }

  [TestMethod]
  public void Convert_ADamagedPicture_Throws() {
    byte[] file = TestFile("logo.png")[..12];

    DocumentToPdfException ex = Assert.ThrowsException<DocumentToPdfException>(() => DocumentToPdf.Convert(file, "logo.png"));

    Assert.AreEqual("The picture couldn't be read. It may be damaged, or not really a PNG or JPEG", ex.Message);
  }

  [TestMethod]
  public void Convert_APageWithNoWidth_Throws() =>
    Assert.ThrowsException<ArgumentOutOfRangeException>(() => DocumentToPdf.Convert(TestFile("cv.txt"), "cv.txt", new() { PageWidthMm = 0 }));

  [DataTestMethod]
  [DataRow("CV.DOCX", true)]
  [DataRow("cv.dotx", true)]
  [DataRow("docx", true)]
  [DataRow(".rtf", true)]
  [DataRow("index.htm", true)]
  [DataRow("photo.JPEG", true)]
  [DataRow("quote.pdf", true)]
  [DataRow("cv.doc", false)]
  [DataRow("sheet.xlsx", false)]
  [DataRow("README", false)]
  public void CanConvert_SaysWhichFilesCanBeConverted(string fileName, bool expected) =>
    Assert.AreEqual(expected, DocumentToPdf.CanConvert(fileName));

  // ---------- Options ----------

  [TestMethod]
  public void Convert_UsesThePageSizeAndFontSizeGiven() {
    DocumentToPdfOptions options = new() { PageWidthMm = 210, PageHeightMm = 148, FontSize = 20 };

    using PdfDocument pdf = PdfDocument.Open(DocumentToPdf.Convert(TestFile("cv.txt"), "cv.txt", options));

    Page page = pdf.GetPage(1);
    Assert.AreEqual(210 * 72 / 25.4, page.Width, .5);
    Assert.AreEqual(148 * 72 / 25.4, page.Height, .5);
    Assert.AreEqual(20, page.Letters[0].PointSize, .1);
  }

  // ---------- Long documents ----------

  [TestMethod]
  public void Convert_ALongDocument_RunsOntoMorePagesWithNothingLost() {
    string body = string.Concat(Enumerable.Range(1, 150).Select(number => $"<p>Paragraph {number} of the report.</p>"));

    List<string> pages = Pages(Bytes(Html(body)), "report.html");

    Assert.IsTrue(pages.Count > 2);
    List<string> lines = [.. pages.SelectMany(page => page.Split('\n'))];
    CollectionAssert.AreEqual(Enumerable.Range(1, 150).Select(number => $"Paragraph {number} of the report.").ToList(), lines);
  }

  [TestMethod]
  public void Convert_ALongTable_IsSplitBetweenRows() {
    string rows = string.Concat(Enumerable.Range(1, 120).Select(number => $"<tr><td>Row {number}</td><td>£{number}.00</td></tr>"));

    List<string> pages = Pages(Bytes(Html($"<table border=\"1\">{rows}</table>")), "table.html");

    Assert.IsTrue(pages.Count > 1);
    List<string> lines = [.. pages.SelectMany(page => page.Split('\n'))];
    CollectionAssert.AreEqual(Enumerable.Range(1, 120).Select(number => $"Row {number} £{number}.00").ToList(), lines);
  }

  [TestMethod]
  public void Convert_ATableRowTallerThanAPage_IsSplit() {
    string lines = string.Concat(Enumerable.Range(1, 90).Select(number => $"Line {number}<br>"));

    List<string> pages = Pages(Bytes(Html($"<table border=\"1\"><tr><td>{lines}</td></tr></table>")), "table.html");

    Assert.IsTrue(pages.Count > 1);
    List<string> text = [.. pages.SelectMany(page => page.Split('\n'))];
    CollectionAssert.AreEqual(Enumerable.Range(1, 90).Select(number => $"Line {number}").ToList(), text);
  }

  // ---------- HTML ----------

  [TestMethod]
  public void Convert_Html_FollowsTheStructure() {
    string html = Html("""
      <h1>Order &amp; delivery</h1>
      <script>document.write("Not shown");</script>
      <p>First line<br>Second line</p>
      <p style="display: none">Hidden</p>
      <ol start="3"><li>Three<ul><li>Inner</li></ul></li><li>Four</li></ol>
      <ol type="a"><li>Letter</li></ol>
      <table><tr><td colspan="2">Both columns</td></tr><tr><td>Left</td><td>Right</td></tr></table>
      <p style="page-break-before: always">Next page</p>
      """);

    List<string> pages = Pages(Bytes(html), "order.html");

    Assert.AreEqual(2, pages.Count);
    Assert.AreEqual("Order & delivery\nFirst line\nSecond line\n3. Three\n◦ Inner\n4. Four\na. Letter\nBoth columns\nLeft Right", pages[0]);
    Assert.AreEqual("Next page", pages[1]);
  }

  [TestMethod]
  public void Convert_Html_KeepsHebrewReadingFromTheRight() {
    using PdfDocument pdf = PdfDocument.Open(DocumentToPdf.Convert(Bytes(Html("<p dir=\"rtl\">שלום עולם</p><p>Left</p>")), "hebrew.html"));
    List<Word> words = [.. pdf.GetPage(1).GetWords()];

    // Hebrew is drawn from the right, so the first word is the one further right, and the paragraph is against the right margin
    Word first = words.First(word => word.Text.Contains('ש') && word.Text.Contains('ל') && word.Text.Contains('ו') && word.Text.Contains('ם') && !word.Text.Contains('ע'));
    Word second = words.First(word => word.Text.Contains('ע'));
    Assert.IsTrue(first.BoundingBox.Left > second.BoundingBox.Right);
    Assert.IsTrue(first.BoundingBox.Right > words.First(word => word.Text == "Left").BoundingBox.Right + 300);
  }

  // ---------- RTF ----------

  [TestMethod]
  public void Convert_Rtf_ReadsCharactersInEachEncoding() {
    List<DocBlock> blocks = RtfReader.Read(TestFile("samples.rtf"));

    List<string> text = [.. blocks.OfType<DocParagraph>().Select(Text)];
    // Windows-1252 bytes, Unicode with a fallback that's skipped, Greek in the Symbol font, and Hebrew in a font with code page 1255
    CollectionAssert.Contains(text, "Café costs €5, αβ and α•.");
    CollectionAssert.Contains(text, "שלום");
  }

  [TestMethod]
  public void Convert_Rtf_LeavesOutWhatIsNotTheText() {
    List<string> pages = Pages(TestFile("samples.rtf"), "samples.rtf");

    Assert.AreEqual(2, pages.Count);
    string all = string.Join("\n", pages);
    foreach (string missing in new[] { "Not shown", "Someone", "Header text", "Hidden text", "Footnote text" }) {
      Assert.IsFalse(all.Contains(missing), missing);
    }
    StringAssert.StartsWith(pages[0], "Prices\nCafé costs €5, αβ and α•.\nCentred in red\nShown text\n");
    StringAssert.Contains(pages[0], "• First item\n2. Second item\nOne Two\nThree Four\nAfter the table");
    Assert.AreEqual("Second page", pages[1]);
  }

  [TestMethod]
  public void Convert_Rtf_KeepsHeadingsTablesAndAlignment() {
    List<DocBlock> blocks = RtfReader.Read(TestFile("samples.rtf"));

    DocParagraph heading = blocks.OfType<DocParagraph>().First(paragraph => Text(paragraph) == "Prices");
    Assert.AreEqual(1, heading.HeadingLevel);
    Assert.AreEqual(16, ((DocText)heading.Inlines[0]).Style.Size);
    DocParagraph centred = blocks.OfType<DocParagraph>().First(paragraph => Text(paragraph) == "Centred in red");
    Assert.AreEqual(DocAlignment.Centre, centred.Alignment);
    Assert.AreEqual(((byte)200, (byte)0, (byte)0), ((DocText)centred.Inlines[0]).Style.Colour);
    DocTable table = blocks.OfType<DocTable>().Single();
    Assert.AreEqual(2, table.Rows.Count);
    Assert.IsTrue(table.Borders);
    CollectionAssert.AreEqual(new List<double> { 100, 100 }, table.ColumnWidths);
  }

  private static string Text(DocParagraph paragraph) =>
    string.Concat(paragraph.Inlines.OfType<DocText>().Select(text => text.Text));

  // ---------- Word ----------

  private const string WordNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

  // A Word document with the body and numbering given, in Word's XML
  private static byte[] Word(string body, string? numbering = null) {
    using MemoryStream stream = new();
    using (WordprocessingDocument document = WordprocessingDocument.Create(stream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document)) {
      MainDocumentPart main = document.AddMainDocumentPart();
      main.Document = new Document($"<w:document xmlns:w=\"{WordNamespace}\" xmlns:v=\"urn:schemas-microsoft-com:vml\"><w:body>{body}</w:body></w:document>");
      if (numbering is not null) {
        main.AddNewPart<NumberingDefinitionsPart>().Numbering = new Numbering($"<w:numbering xmlns:w=\"{WordNamespace}\">{numbering}</w:numbering>");
      }
    }
    return stream.ToArray();
  }

  private static string Paragraph(string runs, string properties = "") =>
    $"<w:p><w:pPr>{properties}</w:pPr>{runs}</w:p>";

  private static string Run(string text, string properties = "") =>
    $"<w:r><w:rPr>{properties}</w:rPr><w:t xml:space=\"preserve\">{text}</w:t></w:r>";

  [TestMethod]
  public void Convert_Word_LeavesOutHiddenAndDeletedText() {
    string body = Paragraph(Run("Kept ") + Run("hidden ", "<w:vanish/>") + "<w:del w:id=\"1\" w:author=\"A\"><w:r><w:delText>deleted </w:delText></w:r></w:del>"
                            + "<w:ins w:id=\"2\" w:author=\"A\">" + Run("inserted ") + "</w:ins>"
                            + "<w:hyperlink w:anchor=\"top\">" + Run("linked ") + "</w:hyperlink>" + Run("capitals", "<w:caps/>"));

    List<string> pages = Pages(Word(body), "changes.docx");

    Assert.AreEqual("Kept inserted linked CAPITALS", pages.Single());
  }

  [TestMethod]
  public void Convert_Word_NumbersListsAsWordDoes() {
    string numbering = """
      <w:abstractNum w:abstractNumId="0">
        <w:lvl w:ilvl="0"><w:start w:val="1"/><w:numFmt w:val="decimal"/><w:lvlText w:val="%1."/><w:pPr><w:ind w:left="720" w:hanging="360"/></w:pPr></w:lvl>
        <w:lvl w:ilvl="1"><w:start w:val="1"/><w:numFmt w:val="lowerLetter"/><w:lvlText w:val="%1.%2)"/><w:pPr><w:ind w:left="1440" w:hanging="360"/></w:pPr></w:lvl>
      </w:abstractNum>
      <w:abstractNum w:abstractNumId="1">
        <w:lvl w:ilvl="0"><w:start w:val="1"/><w:numFmt w:val="bullet"/><w:lvlText w:val="·"/><w:rPr><w:rFonts w:ascii="Symbol" w:hAnsi="Symbol"/></w:rPr></w:lvl>
        <w:lvl w:ilvl="1"><w:start w:val="1"/><w:numFmt w:val="bullet"/><w:lvlText w:val="o"/><w:rPr><w:rFonts w:ascii="Courier New" w:hAnsi="Courier New"/></w:rPr></w:lvl>
      </w:abstractNum>
      <w:abstractNum w:abstractNumId="2">
        <w:lvl w:ilvl="0"><w:start w:val="1"/><w:numFmt w:val="hebrew1"/><w:lvlText w:val="%1."/></w:lvl>
      </w:abstractNum>
      <w:num w:numId="1"><w:abstractNumId w:val="0"/></w:num>
      <w:num w:numId="2"><w:abstractNumId w:val="0"/><w:lvlOverride w:ilvl="0"><w:startOverride w:val="1"/></w:lvlOverride></w:num>
      <w:num w:numId="3"><w:abstractNumId w:val="1"/></w:num>
      <w:num w:numId="4"><w:abstractNumId w:val="2"/><w:lvlOverride w:ilvl="0"><w:startOverride w:val="15"/></w:lvlOverride></w:num>
      """;
    static string Item(string text, int list, int level = 0) =>
      Paragraph(Run(text), $"<w:numPr><w:ilvl w:val=\"{level}\"/><w:numId w:val=\"{list}\"/></w:numPr>");
    string body = Item("One", 1) + Item("Inner", 1, 1) + Item("Inner again", 1, 1) + Item("Two", 1) + Item("Inner restarted", 1, 1)
                  + Paragraph(Run("Between the lists")) + Item("Restarted", 2) + Item("Bullet", 3) + Item("Second level", 3, 1) + Item("Fifteen", 4);

    List<string> pages = Pages(Word(body, numbering), "lists.docx");

    Assert.AreEqual("1. One\n1.a) Inner\n1.b) Inner again\n2. Two\n2.a) Inner restarted\nBetween the lists\n1. Restarted\n• Bullet\n◦ Second level\nטו. Fifteen", pages.Single());
  }

  [TestMethod]
  public void Convert_Word_PutsTextBoxesAfterTheirParagraph() {
    string textBox = "<w:r><w:pict><v:shape style=\"width:100pt;height:50pt\"><v:textbox><w:txbxContent>" + Paragraph(Run("In the box"))
                     + "</w:txbxContent></v:textbox></v:shape></w:pict></w:r>";
    string body = Paragraph(Run("Anchor ") + textBox + Run("paragraph")) + Paragraph(Run("Next"));

    List<string> pages = Pages(Word(body), "box.docx");

    Assert.AreEqual("Anchor paragraph\nIn the box\nNext", pages.Single());
  }

  [TestMethod]
  public void Convert_Word_KeepsPageBreaksAndMergedCells() {
    string table = """
      <w:tbl>
        <w:tblPr><w:tblBorders><w:top w:val="single" w:sz="4"/></w:tblBorders></w:tblPr>
        <w:tblGrid><w:gridCol w:w="2000"/><w:gridCol w:w="2000"/><w:gridCol w:w="4000"/></w:tblGrid>
        <w:tr><w:tc><w:tcPr><w:gridSpan w:val="2"/></w:tcPr><w:p><w:r><w:t>Wide</w:t></w:r></w:p></w:tc><w:tc><w:tcPr><w:vMerge w:val="restart"/></w:tcPr><w:p><w:r><w:t>Tall</w:t></w:r></w:p></w:tc></w:tr>
        <w:tr><w:tc><w:p><w:r><w:t>A</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>B</w:t></w:r></w:p></w:tc><w:tc><w:tcPr><w:vMerge/></w:tcPr><w:p><w:r><w:t>Tall</w:t></w:r></w:p></w:tc></w:tr>
      </w:tbl>
      """;
    string body = Paragraph(Run("Before") + "<w:r><w:br w:type=\"page\"/></w:r>" + Run("After")) + table
                  + Paragraph(Run("Own page"), "<w:pageBreakBefore/>");

    List<string> pages = Pages(Word(body), "breaks.docx");

    CollectionAssert.AreEqual(new List<string> { "Before", "After\nWide Tall\nA B", "Own page" }, pages);
  }
}
