using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Pixata.Pdf.Tests;

[TestClass]
public class PdfText_Tests {
  // A two-page quote made by Pixata.AspNetCore.Pdf.Playwright, so with Chromium's subset TrueType fonts, ToUnicode maps and compressed
  // object streams. The encrypted copies were made from it, one needing the password "secret" to open, and one with only an owner password
  private static byte[] Quote => File.ReadAllBytes(Path.Combine("TestFiles", "quote.pdf"));
  private static byte[] EncryptedQuote => File.ReadAllBytes(Path.Combine("TestFiles", "quote-encrypted.pdf"));
  private static byte[] OwnerPasswordQuote => File.ReadAllBytes(Path.Combine("TestFiles", "quote-owner-only.pdf"));

  [TestMethod]
  public void Read_GivesTheTextOfEachPage() {
    List<string> pages = PdfText.Read(Quote);

    Assert.AreEqual(2, pages.Count);
    Assert.AreEqual("Terms and conditions\nPayment is due within 30 days.", pages[1]);
  }

  [TestMethod]
  public void Read_ReadsNonAsciiText() {
    string page = PdfText.Read(Quote)[0];

    StringAssert.Contains(page, "Customer: Café Rouge Ltd, 12 Rue de l'Église");
    StringAssert.Contains(page, "Total: £15.60");
  }

  [TestMethod]
  public void Read_PutsEachLineInReadingOrder() {
    // Chromium draws the table a cell at a time, but each row comes out as a line, with its cells in order
    string[] lines = PdfText.Read(Quote)[0].Split('\n');

    CollectionAssert.AreEqual(new[] {
      "Quote Q-1042",
      "Customer: Café Rouge Ltd, 12 Rue de l'Église",
      "Item Qty Price",
      "Crème brûlée 2 £4.50",
      "Espresso 3 £2.20",
      "Total: £15.60"
    }, lines);
  }

  [TestMethod]
  public async Task Read_FromAStream_LeavesItOpen() {
    await using MemoryStream stream = new(Quote);

    List<string> pages = await PdfText.Read(stream);

    Assert.AreEqual(2, pages.Count);
    Assert.IsTrue(stream.CanRead);
  }

  [TestMethod]
  public async Task PageCount_CountsThePages() {
    Assert.AreEqual(2, PdfText.PageCount(Quote));
    await using MemoryStream stream = new(Quote);
    Assert.AreEqual(2, await PdfText.PageCount(stream));
  }

  [TestMethod]
  public void Read_WordsDrawnOutOfOrder_AreInReadingOrder() {
    // The second line is drawn first, and its words right to left
    using PdfDocumentBuilder builder = new();
    PdfDocumentBuilder.AddedFont font = builder.AddStandard14Font(Standard14Font.Helvetica);
    PdfPageBuilder page = builder.AddPage(595, 842);
    page.AddText("world", 12, new PdfPoint(100, 700), font);
    page.AddText("second", 12, new PdfPoint(50, 700), font);
    page.AddText("Hello", 12, new PdfPoint(50, 750), font);
    page.AddText("there", 12, new PdfPoint(100, 750), font);
    builder.AddPage(595, 842);

    List<string> pages = PdfText.Read(builder.Build());

    CollectionAssert.AreEqual(new[] { "Hello there\nsecond world", "" }, pages);
  }

  [TestMethod]
  public void Read_OwnerPasswordOnly_IsRead() =>
    // Anyone can open a PDF that only has an owner password, as it's only there to stop it being changed
    Assert.AreEqual("Terms and conditions\nPayment is due within 30 days.", PdfText.Read(OwnerPasswordQuote)[1]);

  [TestMethod]
  public void Read_NeedsAPassword_Throws() {
    PdfTextException ex = Assert.ThrowsException<PdfTextException>(() => PdfText.Read(EncryptedQuote));
    Assert.AreEqual("The PDF is encrypted, and needs a password to open", ex.Message);
    Assert.ThrowsException<PdfTextException>(() => PdfText.PageCount(EncryptedQuote));
  }

  [TestMethod]
  public void Read_NotAPdf_Throws() {
    PdfTextException ex = Assert.ThrowsException<PdfTextException>(() => PdfText.Read(Encoding.UTF8.GetBytes("<html>Not a PDF</html>")));
    Assert.AreEqual("The data isn't a PDF, as it doesn't start with %PDF-", ex.Message);
    ex = Assert.ThrowsException<PdfTextException>(() => PdfText.PageCount([]));
    Assert.AreEqual("The PDF is empty", ex.Message);
  }

  [TestMethod]
  public void Read_DamagedPdf_Throws() {
    // The first half of the quote, as an upload that was cut short would be
    byte[] cut = Quote[..(Quote.Length / 2)];

    PdfTextException ex = Assert.ThrowsException<PdfTextException>(() => PdfText.Read(cut));
    StringAssert.StartsWith(ex.Message, "The PDF couldn't be read: ");
  }
}
