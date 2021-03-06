using System;
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
  }
}