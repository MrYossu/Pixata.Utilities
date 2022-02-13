using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pixata.Extensions.Tests {
  [TestClass]
  public class StringExtensionMethods_Tests {
    #region JoinStr

    [DataRow(new[] { "" }, "")]
    [DataRow(new[] { "Jim" }, "Jim")]
    [DataRow(new[] { "Jim", "Spriggs" }, "Jim, Spriggs")]
    [DataTestMethod]
    public void StringExtensionMethods_JoinStr_NoSep(IEnumerable<string> strs, string output) =>
      Assert.AreEqual(strs.JoinStr(), output);

    [DataRow(new[] { "" }, "", "")]
    [DataRow(new[] { "Jim" }, ",", "Jim")]
    [DataRow(new[] { "Jim", "Spriggs" }, ",", "Jim,Spriggs")]
    [DataRow(new[] { "Jim", "Spriggs" }, " ", "Jim Spriggs")]
    [DataTestMethod]
    public void StringExtensionMethods_JoinStrWithSep(IEnumerable<string> strs, string separator, string output) =>
      Assert.AreEqual(strs.JoinStr(separator), output);

    #endregion

    #region SplitCamelCase

    [DataRow("", "")]
    [DataRow("Hello", "Hello")]
    [DataRow("HelloWorld", "Hello World")]
    [DataTestMethod]
    public void StringExtensionMethods_SplitCamelCase(string input, string output) =>
      Assert.AreEqual(output, input.SplitCamelCase());

    #endregion

    #region SplitEnumCamelCase and SplitEnumValueCamelCase

    public enum Animals {
      HairyGibbon = 0,
      BigFatOrangUtan = 1
    }

    [DataRow(Animals.HairyGibbon, "Hairy Gibbon")]
    [DataRow(Animals.BigFatOrangUtan, "Big Fat Orang Utan")]
    [DataTestMethod]
    public void StringExtensionMethods_SplitEnumCamelCase(Animals animal, string output) =>
      Assert.AreEqual(output, animal.SplitEnumCamelCase());

    [DataRow(0, "Hairy Gibbon")]
    [DataRow(1, "Big Fat Orang Utan")]
    [DataTestMethod]
    public void StringExtensionMethods_SplitEnumValueCamelCaseValid(int id, string output) =>
      Assert.AreEqual(output, id.SplitEnumValueCamelCase<Animals>());

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void StringExtensionMethods_SplitEnumValueCamelCaseInvalid() =>
      2.SplitEnumValueCamelCase<Animals>();

    #endregion

    #region FirstLine

    [DataRow("", "")]
    [DataRow(@"1 Acacia Mews
West Byfleet", "1 Acacia Mews")]
    [DataRow("Jim\nSpriggs", "Jim")]
    [DataTestMethod]
    public void StringExtensionMethods_FirstLine(string multi, string output) =>
      Assert.AreEqual(output, multi.FirstLine());

    #endregion

    #region RemoveDiacritics

    // Note that I made up these names for testing purposes. Any resemblance to any genuine names, living or dead is purely co-incidental!
    [DataRow("", "")]
    [DataRow("Jim", "jim")]
    [DataRow("Jòhn", "john")]
    [DataRow("Jôhn", "john")]
    [DataRow("Jðhn", "john")]
    [DataRow("Jàne", "jane")]
    [DataRow("Jâne", "jane")]
    [DataRow("Jåne", "jane")]
    [DataRow("", "")]
    [DataTestMethod]
    public void StringExtensionMethods_RemoveDiacritics(string value, string expected) =>
      Assert.AreEqual(expected, value.RemoveDiacritics());

    #endregion
  }
}