using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests;

// These follow what Telerik's DateInput was seen to do with the same key presses
[TestClass]
public class DateInputModel_Tests {
  private static readonly CultureInfo Uk = new("en-GB", useUserOverride: false);

  private static DateInputModel Model(string format = "dd MMM yyyy", DateTime? value = null) {
    DateInputModel model = new(format, Uk);
    model.SetValue(value);
    return model;
  }

  private static string Selected(DateInputModel model) =>
    model.Text[model.Selection.Start..model.Selection.End];

  private static void Keys(DateInputModel model, params string[] keys) {
    foreach (string key in keys) {
      model.Key(key);
    }
  }

  [TestMethod]
  public void Parse_StandardFormat_UsesTheCulturesPattern() {
    DateInputModel model = Model("d", new DateTime(2026, 3, 9));

    Assert.AreEqual("09/03/2026", model.Text);
  }

  [TestMethod]
  public void Text_EmptySegments_ShowTheirFormat() {
    DateInputModel model = Model();

    Assert.AreEqual("dd MMM yyyy", model.Text);
    Assert.IsTrue(model.IsEmpty);
    Assert.IsTrue(model.IsValid);
    Assert.IsNull(model.Value);
  }

  [TestMethod]
  public void ArrowUp_OnACompleteDate_UsesDateArithmetic() {
    DateInputModel model = Model(value: new DateTime(2026, 1, 31));

    model.Key("ArrowUp");
    Assert.AreEqual(new DateTime(2026, 2, 1), model.Value);

    model.SetValue(new DateTime(2026, 1, 31));
    Keys(model, "ArrowRight", "ArrowUp");
    Assert.AreEqual(new DateTime(2026, 2, 28), model.Value);
    Assert.AreEqual("Feb", Selected(model));
  }

  [TestMethod]
  public void Digits_StayInTheSegmentWhileAnotherDigitCouldFollow() {
    DateInputModel model = Model(value: new DateTime(2026, 3, 1));

    model.Key("3");
    Assert.AreEqual("03", Selected(model));

    model.Key("1");
    Assert.AreEqual(new DateTime(2026, 3, 31), model.Value);
    Assert.AreEqual("Mar", Selected(model));
  }

  [TestMethod]
  public void Digits_ThatCanOnlyBeOneDigit_MoveOn() {
    DateInputModel model = Model(value: new DateTime(2026, 3, 1));

    model.Key("5");
    Assert.AreEqual("Mar", Selected(model));
    model.Key("5");
    Assert.AreEqual(new DateTime(2026, 5, 5), model.Value);
    Assert.AreEqual("2026", Selected(model));
  }

  [TestMethod]
  public void Digits_ThatWouldMakeAnImpossibleDate_AreIgnored() {
    DateInputModel model = Model(value: new DateTime(2026, 12, 31));
    model.Key("ArrowRight");

    // 11 would be 31st November, so the second 1 starts the month again, as January
    Keys(model, "1", "1");
    Assert.AreEqual(new DateTime(2026, 1, 31), model.Value);
    Assert.AreEqual("Jan", Selected(model));

    // 9 would be 31st September, so it's ignored
    model.Key("9");
    Assert.AreEqual(new DateTime(2026, 1, 31), model.Value);
  }

  [TestMethod]
  public void Year_TakesFourDigits() {
    DateInputModel model = Model(value: new DateTime(2026, 3, 1));
    Keys(model, "End", "1", "9", "9");

    Assert.AreEqual("0199", Selected(model));

    model.Key("7");
    Assert.AreEqual(new DateTime(1997, 3, 1), model.Value);
  }

  [TestMethod]
  public void TwoDigitYear_UsesTwoDigitYearMax() {
    DateInputModel model = Model("dd/MM/yy", new DateTime(2026, 3, 1));
    Keys(model, "End", "6", "8");
    Assert.AreEqual(2068, model.Value!.Value.Year);

    Keys(model, "6", "9");
    Assert.AreEqual(1969, model.Value!.Value.Year);
  }

  [TestMethod]
  public void Backspace_ClearsTheSegmentAndMakesTheDateIncomplete() {
    DateInputModel model = Model(value: new DateTime(2026, 1, 31));

    Keys(model, "ArrowRight", "Backspace");

    Assert.AreEqual("31 MMM 2026", model.Text);
    Assert.IsNull(model.Value);
    Assert.IsFalse(model.IsValid);
    Assert.IsFalse(model.IsEmpty);
  }

  [TestMethod]
  public void TypingIntoAnEmptyInput_BuildsTheDate() {
    DateInputModel model = Model("dd/MM/yyyy");

    Keys(model, "2", "5", "1", "2", "2", "0", "2", "6");

    Assert.AreEqual(new DateTime(2026, 12, 25), model.Value);
    Assert.AreEqual("25/12/2026", model.Text);
  }

  [TestMethod]
  public void Separators_MoveToTheNextSegment() {
    DateInputModel model = Model("dd/MM/yyyy");

    Keys(model, "1", "/", "2", "/", "2", "0", "2", "6");

    Assert.AreEqual(new DateTime(2026, 2, 1), model.Value);
  }

  [TestMethod]
  public void Separators_AfterASegmentMovedOnByItself_AreIgnored() {
    DateInputModel model = Model("dd/MM/yyyy");

    Keys(model, "0", "1", "/", "1", "0", "/", "2", "0", "2", "6");

    Assert.AreEqual(new DateTime(2026, 10, 1), model.Value);
    Assert.AreEqual("01/10/2026", model.Text);
  }

  [TestMethod]
  public void Separators_MixOfCompleteAndIncompleteSegments() {
    DateInputModel model = Model("dd/MM/yyyy");

    Keys(model, "0", "1", "/", "2", "/", "2", "0", "2", "6");

    Assert.AreEqual(new DateTime(2026, 2, 1), model.Value);
  }

  [TestMethod]
  public void Separators_OfMoreThanOneCharacter_AreIgnoredAfterAnAutomaticMove() {
    DateInputModel model = Model("dd, MM yyyy");

    Keys(model, "0", "1", ",", " ", "1", "0", " ", "2", "0", "2", "6");

    Assert.AreEqual(new DateTime(2026, 10, 1), model.Value);
  }

  [TestMethod]
  public void Separators_AfterClickingASegment_MoveOn() {
    DateInputModel model = Model("dd/MM/yyyy");

    Keys(model, "0", "1");
    model.SelectAt(0);
    Keys(model, "/");

    Assert.AreEqual("MM", Selected(model));
  }

  [TestMethod]
  public void MonthLetters_PickTheMonthAndRepeatingCycles() {
    DateInputModel model = Model(value: new DateTime(2026, 3, 1));
    model.Key("ArrowRight");

    model.Key("j");
    Assert.AreEqual(1, model.Value!.Value.Month);
    model.Key("j");
    Assert.AreEqual(6, model.Value!.Value.Month);
    model.Key("j");
    Assert.AreEqual(7, model.Value!.Value.Month);

    model.Key("ArrowLeft");
    model.Key("ArrowRight");
    Keys(model, "a", "u");
    Assert.AreEqual(8, model.Value!.Value.Month);
    Assert.AreEqual("2026", Selected(model));
  }

  [TestMethod]
  public void ArrowUp_OnAnEmptySegment_StartsFromToday() {
    DateInputModel model = Model();

    model.Key("ArrowUp");

    Assert.AreEqual(DateTime.Today.Day.ToString("00", CultureInfo.InvariantCulture), Selected(model));
  }

  [TestMethod]
  public void OutOfRange_IsInvalid() {
    DateInputModel model = Model(value: new DateTime(2026, 3, 1));
    model.Max = new DateTime(2026, 12, 31);

    Keys(model, "End", "2", "0", "2", "7");

    Assert.AreEqual(new DateTime(2027, 3, 1), model.Value);
    Assert.IsFalse(model.IsValid);
  }

  [TestMethod]
  public void SelectAt_SelectsTheSegmentUnderTheCaret() {
    DateInputModel model = Model(value: new DateTime(2026, 3, 1));

    model.SelectAt(4);
    Assert.AreEqual("Mar", Selected(model));
    model.SelectAt(9);
    Assert.AreEqual("2026", Selected(model));
    model.SelectAt(0);
    Assert.AreEqual("01", Selected(model));
  }

  [TestMethod]
  public void TimeFormat_HandlesTwelveHourClockAndAmPm() {
    DateInputModel model = Model("dd/MM/yyyy hh:mm tt", new DateTime(2026, 3, 1, 9, 5, 0));

    Assert.AreEqual("01/03/2026 09:05 am", model.Text);
    Keys(model, "End", "p");
    Assert.AreEqual(new DateTime(2026, 3, 1, 21, 5, 0), model.Value);
  }

  [TestMethod]
  public void Paste_TakesAWholeDate() {
    DateInputModel model = Model();

    Assert.IsTrue(model.Paste("14 Jul 2026"));
    Assert.AreEqual(new DateTime(2026, 7, 14), model.Value);
    Assert.IsFalse(model.Paste("not a date"));
  }

  [TestMethod]
  public void DateOnlyFormat_KeepsTheTimeOfTheValueSet() {
    DateInputModel model = Model(value: new DateTime(2026, 3, 1, 10, 30, 0));

    model.Key("ArrowUp");

    Assert.AreEqual(new DateTime(2026, 3, 2, 10, 30, 0), model.Value);
  }
}
