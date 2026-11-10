using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Extensions.Pdf;

namespace Pixata.Extensions.Tests;

[TestClass]
public class PdfMarkup_Tests {
  [TestMethod]
  public void Markup_GoesToJsonAndBack() {
    List<PdfMarkupItem> markup = [
      new() { Type = PdfMarkupType.Highlight, Page = 2, Author = "Accounts", AuthorId = "7", Created = new(2026, 11, 9, 10, 0, 0, DateTimeKind.Utc), Rects = [new(1, 2, 3, 4)], Text = "Total" },
      new() { Type = PdfMarkupType.Ink, Author = "Approver", Colour = "#0000ff", Strokes = [[new(1, 1), new(2, 2)]], LineWidth = 3 },
      new() { Type = PdfMarkupType.Note, Author = "Approver", Position = new(10, 20), Text = "Check", Modified = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc) }
    ];

    string json = PdfMarkup.ToJson(markup);
    List<PdfMarkupItem> read = PdfMarkup.FromJson(json);

    Assert.IsTrue(json.Contains("\"type\":\"Highlight\""), json);
    Assert.IsFalse(json.Contains("\"width\""), "Width and height are worked out, not stored");
    Assert.IsFalse(json.Contains("\"position\":null"), "Nulls are left out");
    Assert.AreEqual(3, read.Count);
    Assert.AreEqual(markup[0].Id, read[0].Id);
    Assert.AreEqual(PdfMarkupType.Highlight, read[0].Type);
    Assert.AreEqual(2, read[0].Page);
    Assert.AreEqual("7", read[0].AuthorId);
    Assert.AreEqual(markup[0].Created, read[0].Created);
    Assert.AreEqual(new PdfMarkupRect(1, 2, 3, 4), read[0].Rects[0]);
    Assert.AreEqual(new PdfMarkupPoint(2, 2), read[1].Strokes[0][1]);
    Assert.AreEqual(3, read[1].LineWidth);
    Assert.AreEqual(new PdfMarkupPoint(10, 20), read[2].Position);
    Assert.AreEqual(markup[2].Modified, read[2].Modified);
    Assert.IsNull(read[0].Position);
  }

  [TestMethod]
  [DataRow(null)]
  [DataRow("")]
  [DataRow("  ")]
  [DataRow("null")]
  public void NoJson_GivesNoMarkup(string? json) =>
    Assert.AreEqual(0, PdfMarkup.FromJson(json).Count);

  [TestMethod]
  public void Clone_CopiesTheLists() {
    PdfMarkupItem item = new() { Type = PdfMarkupType.Ink, Rects = [new(1, 2, 3, 4)], Strokes = [[new(1, 1)]] };

    PdfMarkupItem copy = item.Clone();
    copy.Rects.Add(new(5, 6, 7, 8));
    copy.Strokes[0].Add(new(2, 2));
    copy.Strokes.Add([]);
    copy.Text = "Changed";

    Assert.AreEqual(item.Id, copy.Id);
    Assert.AreEqual(1, item.Rects.Count);
    Assert.AreEqual(1, item.Strokes.Count);
    Assert.AreEqual(1, item.Strokes[0].Count);
    Assert.AreEqual("", item.Text);
  }

  [TestMethod]
  public void ARect_KnowsItsSize() {
    PdfMarkupRect rect = new(10, 20, 110, 70);

    Assert.AreEqual(100, rect.Width);
    Assert.AreEqual(50, rect.Height);
  }
}
