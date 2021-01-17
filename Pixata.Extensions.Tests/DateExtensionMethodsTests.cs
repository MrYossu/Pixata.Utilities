using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pixata.Extensions.Tests {
  [TestClass]
  public class DateExtensionMethodsTests {
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
  }
}