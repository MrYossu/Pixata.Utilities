using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pixata.Extensions.Tests {
  [TestClass]
  public class DateExtensionMethodsTests {
    #region ToPrettyString

    [TestMethod]
    public void DateExtensionMethods_ToPrettyString_14thJan2021() {
      DateTime d = new(2021, 1, 14);
      Assert.AreEqual("14th January 2021", d.ToPrettyString());
    }

    [TestMethod]
    public void DateExtensionMethods_ToPrettyString_IncludeTime_14thJan2021() {
      DateTime d = new(2021, 1, 14, 16, 20, 0);
      Assert.AreEqual("14th January 2021 16:20", d.ToPrettyString(true));
    }

    [TestMethod]
    public void DateExtensionMethods_ToPrettyString_7thApril1961() {
      DateTime d = new(1961, 4, 7);
      Assert.AreEqual("7th April 1961", d.ToPrettyString());
    }

    [TestMethod]
    public void DateExtensionMethods_ToPrettyString_31stDec2021() {
      DateTime d = new(2021, 12, 31);
      Assert.AreEqual("31st December 2021", d.ToPrettyString());
    }

    [TestMethod]
    public void DateExtensionMethods_ToPrettyString_Nullable_14thJan2021() {
      DateTime? d = new(2021, 1, 14);
      Assert.AreEqual("14th January 2021", d.ToPrettyString());
    }

    [TestMethod]
    public void DateExtensionMethods_ToPrettyString_Nullable_Null() {
      DateTime? d = null;
      Assert.AreEqual("", d.ToPrettyString());
    }

    #endregion

    #region IsWithin

    [TestMethod]
    public void DateExtensionMethods_IsWithin_True() {
      DateTime start = new(2020, 1, 1);
      DateTime end = new(2020, 2, 1);
      DateTime date = new(2020, 1, 15);
      Assert.IsTrue(date.IsWithin(start, end));
    }

    [TestMethod]
    public void DateExtensionMethods_IsWithin_False() {
      DateTime start = new(2020, 1, 1);
      DateTime end = new(2020, 2, 1);
      DateTime date = new(2020, 2, 15);
      Assert.IsFalse(date.IsWithin(start, end));
    }

    #endregion

    #region EndOfDay

    [TestMethod]
    public void DateExtensionMethods_EndOfDay_14thJan2021() {
      DateTime d = new(2021, 1, 14);
      Assert.AreEqual(new DateTime(2021, 1, 14, 23, 59, 59).AddMilliseconds(999), d.EndOfDay());
    }

    [TestMethod]
    public void DateExtensionMethods_EndOfDay_28thFeb1900() {
      DateTime d = new(1900, 2, 28);
      Assert.AreEqual(new DateTime(1900, 2, 28, 23, 59, 59).AddMilliseconds(999), d.EndOfDay());
    }

    [TestMethod]
    public void DateExtensionMethods_EndOfDay_28thFeb1904() {
      DateTime d = new(1904, 2, 28);
      Assert.AreEqual(new DateTime(1904, 2, 28, 23, 59, 59).AddMilliseconds(999), d.EndOfDay());
    }

    #endregion

    #region StartOfMonth

    [TestMethod]
    public void DateExtensionMethods_StartOfMonth_14thJan2021() {
      DateTime d = new(2021, 1, 14);
      Assert.AreEqual(new DateTime(2021, 1, 1), d.StartOfMonth());
    }

    #endregion

    #region EndOfMonth

    [TestMethod]
    public void DateExtensionMethods_EndOfMonth_14thJan2021() {
      DateTime d = new(2021, 1, 14);
      Assert.AreEqual(new DateTime(2021, 1, 31, 23, 59, 59).AddMilliseconds(999), d.EndOfMonth());
    }

    [TestMethod]
    public void DateExtensionMethods_EndOfMonth_28thFeb1900() {
      // As 1900 was the start of a new century, it was not a leap year, so the end of Feb would have been the 28th
      DateTime d = new(1900, 2, 28);
      Assert.AreEqual(new DateTime(1900, 2, 28, 23, 59, 59).AddMilliseconds(999), d.EndOfMonth());
    }

    [TestMethod]
    public void DateExtensionMethods_EndOfMonth_28thFeb1904() {
      // 1904 was a leap year
      DateTime d = new(1904, 2, 28);
      Assert.AreEqual(new DateTime(1904, 2, 29, 23, 59, 59).AddMilliseconds(999), d.EndOfMonth());
    }

    #endregion

    #region DateRangeToString

    // The month names come from the current culture, so every test in this class runs as en-GB
    private CultureInfo _culture = CultureInfo.CurrentCulture;

    [TestInitialize]
    public void UseUkCulture() {
      _culture = CultureInfo.CurrentCulture;
      CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-GB");
    }

    [TestCleanup]
    public void RestoreCulture() =>
      CultureInfo.CurrentCulture = _culture;

    [TestMethod]
    public void DateExtensionMethods_DateRangeToString_DateOnly_Short() {
      Assert.AreEqual("2nd Nov 26 - 10th Jan 27", (new DateOnly(2026, 11, 2), new DateOnly(2027, 1, 10)).DateRangeToString());
      Assert.AreEqual("2nd Nov - 10th Dec 26", (new DateOnly(2026, 11, 2), new DateOnly(2026, 12, 10)).DateRangeToString());
      Assert.AreEqual("2nd-30th Nov 26", (new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 30)).DateRangeToString());
      Assert.AreEqual("2nd Nov 26", (new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 2)).DateRangeToString());
    }

    [TestMethod]
    public void DateExtensionMethods_DateRangeToString_DateOnly_Long() {
      Assert.AreEqual("2 November 2026 to 10 January 2027", (new DateOnly(2026, 11, 2), new DateOnly(2027, 1, 10)).DateRangeToString(DateRangeFormat.Long));
      Assert.AreEqual("2 November to 10 December 2026", (new DateOnly(2026, 11, 2), new DateOnly(2026, 12, 10)).DateRangeToString(DateRangeFormat.Long));
      Assert.AreEqual("2 to 30 November 2026", (new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 30)).DateRangeToString(DateRangeFormat.Long));
      Assert.AreEqual("2 November 2026", (new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 2)).DateRangeToString(DateRangeFormat.Long));
    }

    [TestMethod]
    public void DateExtensionMethods_DateRangeToString_DateOnly_Nullable() {
      DateOnly? none = null;
      DateOnly? from = new(2026, 11, 2);
      DateOnly to = new(2027, 1, 10);
      Assert.AreEqual("", (none, none).DateRangeToString());
      Assert.AreEqual("From 2nd Nov 26", (from, none).DateRangeToString());
      Assert.AreEqual("To 10th Jan 27", (none, to).DateRangeToString());
      Assert.AreEqual("From 2 November 2026", (from.Value, none).DateRangeToString(DateRangeFormat.Long));
      Assert.AreEqual("Until 10 January 2027", (none, to).DateRangeToString(DateRangeFormat.Long));
      Assert.AreEqual("2 November 2026 to 10 January 2027", (from, (DateOnly?)to).DateRangeToString(DateRangeFormat.Long));
    }

    [TestMethod]
    public void DateExtensionMethods_DateRangeToString_DateTime_ShortIsUnchanged() {
      DateTime from = new(2026, 11, 2, 9, 30, 0);
      DateTime to = new(2026, 11, 30, 16, 0, 0);
      Assert.AreEqual("2nd-30th Nov 26", (from, to).DateRangeToString());
      Assert.AreEqual("2nd Nov 09:30 - 30th Nov 26 16:00", (from, to).DateRangeToString(true));
      Assert.AreEqual((from, to).DateRangeToString(true), (from, to).DateRangeToString(DateRangeFormat.Short, true));
    }

    [TestMethod]
    public void DateExtensionMethods_DateRangeToString_DateTime_Long() {
      DateTime from = new(2026, 11, 2, 9, 30, 0);
      Assert.AreEqual("2 to 30 November 2026", (from, new DateTime(2026, 11, 30, 16, 0, 0)).DateRangeToString(DateRangeFormat.Long));
      Assert.AreEqual("2 November 2026 09:30 to 16:00", (from, new DateTime(2026, 11, 2, 16, 0, 0)).DateRangeToString(DateRangeFormat.Long, true));
      Assert.AreEqual("2 November 09:30 to 30 November 2026 16:00", (from, new DateTime(2026, 11, 30, 16, 0, 0)).DateRangeToString(DateRangeFormat.Long, true));
      Assert.AreEqual("2 November 2026 09:30 to 10 January 2027 16:00", (from, new DateTime(2027, 1, 10, 16, 0, 0)).DateRangeToString(DateRangeFormat.Long, true));
      Assert.AreEqual("From 2 November 2026 09:30", (from, (DateTime?)null).DateRangeToString(DateRangeFormat.Long, true));
    }

    #endregion
  }
}