using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pixata.Extensions.Tests {
  [TestClass]
  public class DateExtensionMethodsTests {
    [TestMethod]
    public void DateExtensionMethods_Test() {
      DateTime d = new(2021,1,14);
      Assert.AreEqual("14th January 2021",d.ToPrettyString());
    }
  }
}