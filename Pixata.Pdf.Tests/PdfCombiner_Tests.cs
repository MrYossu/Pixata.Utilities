using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Extensions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Pixata.Pdf.Tests;

[TestClass]
public class PdfCombiner_Tests {
  // A PDF with a page of each size, with its text on it
  private static byte[] Pdf(params (string Text, double Width, double Height)[] pages) {
    using PdfDocumentBuilder builder = new();
    PdfDocumentBuilder.AddedFont font = builder.AddStandard14Font(Standard14Font.Helvetica);
    foreach ((string text, double width, double height) in pages) {
      PdfPageBuilder page = builder.AddPage(width, height);
      page.AddText(text, 12, new PdfPoint(50, height - 50), font);
    }
    return builder.Build();
  }

  // A4 and A5 in points
  private const double A4Width = 595, A4Height = 842, A5Width = 420, A5Height = 595;

  // A page that is turned a quarter, as a scanner or Word can make, with a sticky note on it. PdfPig's builder can't set /Rotate or add
  // annotations, so this is written by hand
  private static byte[] RotatedPdf() {
    string[] objects = [
      "<< /Type /Catalog /Pages 2 0 R >>",
      "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
      "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Rotate 90 /Contents 4 0 R /Resources << >> /Annots [5 0 R] >>",
      "<< /Length 0 >>\nstream\n\nendstream",
      "<< /Type /Annot /Subtype /Text /Rect [100 700 120 720] /Contents (Approved by Jim) >>"
    ];
    StringBuilder pdf = new("%PDF-1.4\n");
    List<int> offsets = [];
    for (int i = 0; i < objects.Length; i++) {
      offsets.Add(pdf.Length);
      pdf.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
    }
    int xref = pdf.Length;
    pdf.Append($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
    foreach (int offset in offsets) {
      pdf.Append($"{offset:D10} 00000 n \n");
    }
    pdf.Append($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
    return Encoding.ASCII.GetBytes(pdf.ToString());
  }

  private static List<Page> Pages(ApiResponse<byte[]> response) {
    Assert.AreEqual(ApiResponseStates.Success, response.State, response.Message);
    using PdfDocument document = PdfDocument.Open(response.Data!);
    return [.. document.GetPages()];
  }

  [TestMethod]
  public void Combine_PutsThePagesInOrder_KeepingTheirSizes() {
    byte[] cover = Pdf(("Cover", A4Width, A4Height));
    byte[] invoice = Pdf(("Invoice page 1", A5Height, A5Width), ("Invoice page 2", A4Width, A4Height));

    List<Page> pages = Pages(PdfCombiner.Combine(cover, invoice));

    Assert.AreEqual(3, pages.Count);
    CollectionAssert.AreEqual(new[] { "Cover", "Invoice page 1", "Invoice page 2" }, pages.Select(p => p.Text).ToArray());
    Assert.AreEqual((A4Width, A4Height), (pages[0].Width, pages[0].Height));
    // A landscape A5 page stays landscape
    Assert.AreEqual((A5Height, A5Width), (pages[1].Width, pages[1].Height));
    Assert.AreEqual((A4Width, A4Height), (pages[2].Width, pages[2].Height));
  }

  [TestMethod]
  public void Combine_KeepsAPagesRotationAndAnnotations() {
    ApiResponse<byte[]> response = PdfCombiner.Combine(Pdf(("Cover", A4Width, A4Height)), RotatedPdf());

    Assert.AreEqual(ApiResponseStates.Success, response.State, response.Message);
    using PdfDocument document = PdfDocument.Open(response.Data!);
    Page cover = document.GetPage(1);
    Page rotated = document.GetPage(2);
    Assert.AreEqual(0, cover.Rotation.Value);
    Assert.AreEqual(90, rotated.Rotation.Value);
    Assert.AreEqual("Approved by Jim", rotated.ExperimentalAccess.GetAnnotations().Single().Content);
  }

  [TestMethod]
  public void Combine_APdfMadeByChromium() {
    // Made by Pixata.AspNetCore.Pdf.Playwright, with embedded font subsets, as a cover page usually is
    byte[] cover = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestFiles", "cover.pdf"));

    List<Page> pages = Pages(PdfCombiner.Combine(cover, Pdf(("Supplier's invoice", A4Width, A4Height))));

    Assert.AreEqual(2, pages.Count);
    StringAssert.Contains(pages[0].Text, "Invoice 1234");
    Assert.AreEqual("Supplier's invoice", pages[1].Text);
  }

  [TestMethod]
  public void Combine_OnePdf_GivesItsPages() {
    List<Page> pages = Pages(PdfCombiner.Combine(Pdf(("Only", A4Width, A4Height))));

    Assert.AreEqual("Only", pages.Single().Text);
  }

  [TestMethod]
  public void Combine_SamePdfTwice() {
    byte[] pdf = Pdf(("Again", A4Width, A4Height));

    List<Page> pages = Pages(PdfCombiner.Combine([pdf, pdf]));

    CollectionAssert.AreEqual(new[] { "Again", "Again" }, pages.Select(p => p.Text).ToArray());
  }

  [TestMethod]
  public void Combine_ADamagedPdf_SaysWhichOne() {
    byte[] notAPdf = Encoding.ASCII.GetBytes("This is a Word document, honest");

    ApiResponse<byte[]> response = PdfCombiner.Combine(Pdf(("Cover", A4Width, A4Height)), notAPdf, Pdf(("Last", A4Width, A4Height)));

    Assert.AreEqual(ApiResponseStates.Failure, response.State);
    StringAssert.StartsWith(response.Message, "PDF 2 of 3 couldn't be read: ");
    Assert.IsNull(response.Data);
  }

  [TestMethod]
  public void Combine_Nothing_Fails() {
    ApiResponse<byte[]> response = PdfCombiner.Combine(new List<byte[]>());

    Assert.AreEqual(ApiResponseStates.Failure, response.State);
    Assert.AreEqual("There are no PDFs to combine", response.Message);
  }

  [TestMethod]
  public async Task Combine_Streams_ReadsEachFromItsPosition() {
    using MemoryStream first = new([.. "junk"u8, .. Pdf(("First", A4Width, A4Height))]);
    first.Position = 4;
    using MemoryStream second = new(Pdf(("Second", A5Width, A5Height)));

    ApiResponse<byte[]> response = await PdfCombiner.Combine([first, second]);

    List<Page> pages = Pages(response);
    CollectionAssert.AreEqual(new[] { "First", "Second" }, pages.Select(p => p.Text).ToArray());
    // The streams are left open, as they belong to the caller
    Assert.IsTrue(first.CanRead);
    Assert.IsTrue(second.CanRead);
  }
}
