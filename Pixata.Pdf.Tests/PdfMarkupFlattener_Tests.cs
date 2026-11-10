using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Extensions;
using Pixata.Extensions.Pdf;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Pixata.Pdf.Tests;

[TestClass]
public class PdfMarkupFlattener_Tests {
  // A PDF of A4 pages, each with its number on it
  private static byte[] Pdf(int pages = 2) {
    using PdfDocumentBuilder builder = new();
    PdfDocumentBuilder.AddedFont font = builder.AddStandard14Font(Standard14Font.Helvetica);
    for (int number = 1; number <= pages; number++) {
      PdfPageBuilder page = builder.AddPage(595, 842);
      page.AddText($"Page {number} of the invoice", 12, new PdfPoint(50, 792), font);
    }
    return builder.Build();
  }

  private static readonly DateTime Created = new(2026, 11, 9, 10, 30, 0, DateTimeKind.Utc);

  private static List<PdfMarkupItem> Markup() => [
    new() { Type = PdfMarkupType.Highlight, Page = 1, Author = "Accounts", Rects = [new(50, 788, 200, 804)], Created = Created },
    new() { Type = PdfMarkupType.StrikeThrough, Page = 1, Author = "Accounts", Colour = PdfMarkup.DefaultColour, Rects = [new(50, 700, 200, 712)], Created = Created },
    new() { Type = PdfMarkupType.Ink, Page = 2, Author = "Approver", Colour = "#0000ff", Strokes = [[new(100, 100), new(150, 150), new(200, 100)]], Created = Created },
    new() { Type = PdfMarkupType.Text, Page = 2, Author = "Approver", Colour = PdfMarkup.DefaultColour, Position = new(300, 500), Text = "£1,234.50\nNot £1,243.50", Created = Created },
    new() { Type = PdfMarkupType.Note, Page = 2, Author = "Approver", Colour = PdfMarkup.DefaultColour, Position = new(400, 400), Text = "Please check the VAT", Created = Created }
  ];

  private static List<Page> Pages(ApiResponse<byte[]> response) {
    Assert.AreEqual(ApiResponseStates.Success, response.State, response.Message);
    using PdfDocument document = PdfDocument.Open(response.Data!);
    return [.. document.GetPages()];
  }

  private static PdfMarkupFlattenOptions Utc => new() { TimeZone = TimeZoneInfo.Utc };

  [TestMethod]
  public void TheMarkup_IsDrawnIn_AndTheNotesAreListedAtTheEnd() {
    List<Page> pages = Pages(PdfMarkupFlattener.Flatten(Pdf(), Markup(), Utc));

    Assert.AreEqual(3, pages.Count);
    Assert.IsTrue(pages[0].Text.Contains("Page 1 of the invoice"));
    Assert.AreEqual(1, pages[0].NumberOfImages, "The highlight is a translucent image");
    Assert.IsTrue(pages[1].Text.Contains("£1,234.50"));
    Assert.IsTrue(pages[1].Text.Contains("Not £1,243.50"));
    string notes = pages[2].Text;
    Assert.IsTrue(notes.Contains("Notes"));
    Assert.IsTrue(notes.Contains("Approver"));
    Assert.IsTrue(notes.Contains("9 Nov 2026 10:30"));
    Assert.IsTrue(notes.Contains("Please check the VAT"));
    Assert.AreEqual(595, pages[2].Width, 1);
    Assert.AreEqual(842, pages[2].Height, 1);
  }

  [TestMethod]
  public void WithoutTheNotesPage_NotesAreLeftOut() {
    List<Page> pages = Pages(PdfMarkupFlattener.Flatten(Pdf(), Markup(), new() { NotesPage = false }));
    List<Page> withNotes = Pages(PdfMarkupFlattener.Flatten(Pdf(), Markup()));

    Assert.AreEqual(2, pages.Count);
    // The note's marker, numbered 1, is left out too
    Assert.AreEqual(withNotes[1].Text.Count(c => c == '1') - 1, pages[1].Text.Count(c => c == '1'));
  }

  [TestMethod]
  public void NoMarkup_GivesACopyOfThePdf() {
    List<Page> pages = Pages(PdfMarkupFlattener.Flatten(Pdf(3), []));

    Assert.AreEqual(3, pages.Count);
    Assert.IsTrue(pages[2].Text.Contains("Page 3 of the invoice"));
  }

  [TestMethod]
  public void MarkupOnPagesThePdfDoesntHave_IsLeftOut() {
    List<PdfMarkupItem> markup = [new() { Type = PdfMarkupType.Note, Page = 5, Position = new(10, 10), Text = "Lost" }];

    List<Page> pages = Pages(PdfMarkupFlattener.Flatten(Pdf(), markup));

    Assert.AreEqual(2, pages.Count);
  }

  [TestMethod]
  public void ManyNotes_GoOnMoreThanOneNotesPage() {
    List<PdfMarkupItem> markup = [.. Enumerable.Range(1, 60).Select(n => new PdfMarkupItem {
      Type = PdfMarkupType.Note, Page = 1, Author = "Approver", Position = new(50, 10 * n), Text = $"Note number {n}, which is long enough to need more than one line on the notes page, as it goes on and on"
    })];

    List<Page> pages = Pages(PdfMarkupFlattener.Flatten(Pdf(1), markup));

    Assert.IsTrue(pages.Count > 2);
    Assert.IsTrue(pages.Last().Text.Contains("Note number 60"));
    Assert.IsFalse(pages[1].Text.Contains("0001"), "Notes without a time haven't a date");
  }

  [TestMethod]
  public async Task AStream_CanBeFlattened() {
    using MemoryStream pdf = new(Pdf());

    List<Page> pages = Pages(await PdfMarkupFlattener.Flatten(pdf, Markup()));

    Assert.AreEqual(3, pages.Count);
  }

  [TestMethod]
  public void SomethingThatIsntAPdf_Fails() {
    ApiResponse<byte[]> response = PdfMarkupFlattener.Flatten("Not a PDF"u8.ToArray(), Markup());

    Assert.AreEqual(ApiResponseStates.Failure, response.State);
    Assert.IsTrue(response.Message.StartsWith("The mark-up couldn't be drawn into the PDF"), response.Message);
  }

  [TestMethod]
  public void TheBuiltInFont_DropsAccents_AndShowsWhatItCantDrawAsAQuestionMark() {
    List<PdfMarkupItem> markup = [new() { Type = PdfMarkupType.Text, Page = 1, Position = new(50, 500), Text = "Café, naïve, €5, £5 and שלום" }];

    List<Page> pages = Pages(PdfMarkupFlattener.Flatten(Pdf(1), markup));

    Assert.IsTrue(pages[0].Text.Contains("Cafe, naive, ?5, £5 and ????"), pages[0].Text);
  }

  [TestMethod]
  [DataRow(100.0, 12.0, 0, true)]
  [DataRow(12.0, 100.0, 0, false)]
  [DataRow(100.0, 12.0, 90, true)]
  [DataRow(12.0, 100.0, 90, false)]
  [DataRow(10.0, 11.0, 0, true)]
  [DataRow(10.0, 11.0, 90, false)]
  [DataRow(11.0, 10.0, 270, false)]
  public void AStrikeThrough_GoesAlongTheLongerSide_OrTheWayTextRunsIfItsNearlySquare(double width, double height, int rotation, bool across) =>
    Assert.AreEqual(across, PdfMarkupFlattener.StrikesAcross(new(0, 0, width, height), rotation));

  [TestMethod]
  [DataRow("#ffd400", 255, 212, 0)]
  [DataRow("0000FF", 0, 0, 255)]
  [DataRow("#0f0", 0, 255, 0)]
  [DataRow("red", 211, 47, 47)]
  [DataRow(null, 211, 47, 47)]
  public void Colours_AreRead_AndAnythingElseIsRed(string? colour, int red, int green, int blue) =>
    Assert.AreEqual(((byte)red, (byte)green, (byte)blue), PdfMarkupFlattener.Colour(colour));

  [TestMethod]
  [DataRow("Hello", "Hello")]
  [DataRow("שלום", "םולש")]
  [DataRow("שלום world", "world םולש")]
  [DataRow("Total: שלום 5", "Total: םולש 5")]
  [DataRow("סה״כ 1,234.50 (כולל VAT)", "(VAT ללוכ) 1,234.50 כ״הס")]
  public void RightToLeftText_IsPutInTheOrderItsSeen(string line, string visual) =>
    Assert.AreEqual(visual, Bidi.Visual(line));
}
