using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pixata.Extensions.Tests {
  [TestClass]
  public class DateExtensionMethodsTests {
    #region ToPrettyString

    [TestMethod]
    public void DateExtensionMethods_14thJan2021() {
      DateTime d = new(2021, 1, 14);
      Assert.AreEqual("14th January 2021", d.ToPrettyString());
    }

    [TestMethod]
    public void DateExtensionMethods_31stDec2021() {
      DateTime d = new(2021, 12, 31);
      Assert.AreEqual("31st December 2021", d.ToPrettyString());
    }

    #endregion

    #region EndOfMonth

    [TestMethod]
    public void DateExtensionMethods_EndOfMonth_14thJan2021() {
      DateTime d = new(2021, 1, 14);
      Assert.AreEqual(new DateTime(2021, 1, 31, 23, 59, 59), d.EndOfMonth());
    }

    [TestMethod]
    public void DateExtensionMethods_EndOfMonth_28thFeb1900() {
      // As 1900 was the start of a new century, it was not a leap year, so the end of Feb would have been the 28th
      DateTime d = new(1900, 2, 28);
      Assert.AreEqual(new DateTime(1900, 2, 28, 23, 59, 59), d.EndOfMonth());
    }

    [TestMethod]
    public void DateExtensionMethods_EndOfMonth_28thFeb1904() {
      // 1904 was a leap year
      DateTime d = new(1904, 2, 28);
      Assert.AreEqual(new DateTime(1904, 2, 29, 23, 59, 59), d.EndOfMonth());
    }

    #endregion
  }
}