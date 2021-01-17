using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pixata.Extensions.Tests {
  [TestClass]
  public class IntExtensionMethods_Tests {
    #region OrdinalSuffix

    [DataRow(1, "st")]
    [DataRow(2, "nd")]
    [DataRow(3, "rd")]
    [DataRow(4, "th")]
    [DataRow(5, "th")]
    [DataRow(6, "th")]
    [DataRow(7, "th")]
    [DataRow(8, "th")]
    [DataRow(9, "th")]
    [DataRow(10, "th")]
    [DataRow(11, "th")]
    [DataRow(12, "th")]
    [DataRow(13, "th")]
    [DataRow(14, "th")]
    [DataRow(15, "th")]
    [DataRow(16, "th")]
    [DataRow(17, "th")]
    [DataRow(18, "th")]
    [DataRow(19, "th")]
    [DataRow(20, "th")]
    [DataRow(21, "st")]
    [DataRow(22, "nd")]
    [DataRow(23, "rd")]
    [DataRow(24, "th")]
    [DataRow(25, "th")]
    [DataRow(26, "th")]
    [DataRow(27, "th")]
    [DataRow(28, "th")]
    [DataRow(29, "th")]
    [DataRow(30, "th")]
    [DataRow(31, "st")]
    [DataRow(32, "nd")]
    [DataRow(33, "rd")]
    [DataRow(34, "th")]
    [DataRow(35, "th")]
    [DataRow(36, "th")]
    [DataRow(37, "th")]
    [DataRow(38, "th")]
    [DataRow(39, "th")]
    [DataRow(40, "th")]
    [DataTestMethod]
    public void IntExtensionMethods_OrdinalSuffix_All(int n, string suffix) =>
      Assert.AreEqual(suffix, n.OrdinalSuffix());

    #endregion
  }
}