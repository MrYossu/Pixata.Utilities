using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Pdf;

namespace Pixata.AspNetCore.Pdf.Playwright.Tests;

// Creates real PDFs with the Chromium that the package's targets copy into the test output, so these show that the converter works on the
// operating system the tests run on. CI runs them on Windows and on Linux (issue #145)
[TestClass]
public partial class PdfConversion_Tests {
  private static PlaywrightPdfConverter _converter = null!;

  [ClassInitialize]
  public static void Initialise(TestContext _) =>
    _converter = new(new() { TimeoutSeconds = 60 });

  [ClassCleanup]
  public static async Task Cleanup() =>
    await _converter.DisposeAsync();

  private const string Html = """
    <html><body style="font-family: sans-serif">
      <h1>Invoice 1234</h1>
      <p>Café, €12.50</p>
      <div style="break-before: page">Second page</div>
    </body></html>
    """;

  [TestMethod]
  public async Task Convert_MakesAPdf() {
    byte[] pdf = await _converter.Convert(Html, new());

    string text = Encoding.Latin1.GetString(pdf);
    StringAssert.StartsWith(text, "%PDF-");
    StringAssert.Contains(text, "%%EOF");
    Assert.AreEqual(2, PdfText.PageCount(pdf));
  }

  [TestMethod]
  public async Task Convert_TheTextCanBeReadBack() {
    byte[] pdf = await _converter.Convert(Html, new());

    List<string> pages = PdfText.Read(pdf);
    Assert.AreEqual("Invoice 1234\nCafé, €12.50", pages[0]);
    Assert.AreEqual("Second page", pages[1]);
  }

  [TestMethod]
  public async Task Convert_ALongDocument_RunsOntoMorePages() {
    string rows = string.Concat(Enumerable.Range(1, 80).Select(n => $"<tr><td>Line {n}</td><td>£{n}.00</td></tr>"));
    byte[] pdf = await _converter.Convert($"<html><body style=\"font-family: sans-serif\"><table>{rows}</table></body></html>", new());

    List<string> pages = PdfText.Read(pdf);
    Assert.IsTrue(pages.Count > 1);
    StringAssert.StartsWith(pages[0], "Line 1 £1.00\nLine 2 £2.00");
    StringAssert.EndsWith(pages[^1], "Line 80 £80.00");
  }

  [TestMethod]
  public async Task Convert_UsesThePaperSizeAndOrientation() {
    byte[] portrait = await _converter.Convert(Html, new() { PaperSize = PdfPaperSize.A4 });
    byte[] landscape = await _converter.Convert(Html, new() { PaperSize = PdfPaperSize.A4, Orientation = PdfOrientation.Landscape });

    // A4 is 210mm by 297mm, which is 595 by 842 points
    (double width, double height) = MediaBox(portrait);
    Assert.AreEqual(595.3, width, 1);
    Assert.AreEqual(841.9, height, 1);
    (width, height) = MediaBox(landscape);
    Assert.AreEqual(841.9, width, 1);
    Assert.AreEqual(595.3, height, 1);
  }

  [TestMethod]
  public async Task Convert_SeveralAtOnce() {
    byte[][] pdfs = await Task.WhenAll(Enumerable.Range(1, 6).Select(n => _converter.Convert(Html.Replace("1234", $"{n}"), new())));

    Assert.IsTrue(pdfs.All(pdf => Encoding.Latin1.GetString(pdf, 0, 5) == "%PDF-"));
  }

  private static (double Width, double Height) MediaBox(byte[] pdf) {
    Match match = MediaBoxRegex().Match(Encoding.Latin1.GetString(pdf));
    Assert.IsTrue(match.Success, "The PDF has no MediaBox");
    return (double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
  }

  [GeneratedRegex(@"/MediaBox\s*\[\s*0\s+0\s+([\d.]+)\s+([\d.]+)\s*\]")]
  private static partial Regex MediaBoxRegex();
}
