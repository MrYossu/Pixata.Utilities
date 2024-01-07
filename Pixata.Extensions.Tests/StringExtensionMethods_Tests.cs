using System;
using System.Collections.Generic;
using System.Linq;
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

    [DataRow("", true, "")]
    [DataRow("", false, "")]
    [DataRow("Hello", true, "Hello")]
    [DataRow("Hello", false, "Hello")]
    [DataRow("HelloWorld", true, "Hello world")]
    [DataRow("HelloWorld", false, "Hello World")]
    [DataTestMethod]
    public void StringExtensionMethods_SplitCamelCase(string input, bool toLower, string output) =>
      Assert.AreEqual(output, input.SplitCamelCase(toLower));

    #endregion

    #region SplitEnumCamelCase and SplitEnumValueCamelCase

    public enum Animals {
      HairyGibbon = 0,
      BigFatOrangUtan = 1
    }

    [DataRow(Animals.HairyGibbon, true, "Hairy gibbon")]
    [DataRow(Animals.HairyGibbon, false, "Hairy Gibbon")]
    [DataRow(Animals.BigFatOrangUtan, true, "Big fat orang utan")]
    [DataRow(Animals.BigFatOrangUtan, false, "Big Fat Orang Utan")]
    [DataTestMethod]
    public void StringExtensionMethods_SplitEnumCamelCase(Animals animal, bool toLower, string output) =>
      Assert.AreEqual(output, animal.SplitEnumCamelCase(toLower));

    [DataRow(0, true, "Hairy gibbon")]
    [DataRow(0, false, "Hairy Gibbon")]
    [DataRow(1, true, "Big fat orang utan")]
    [DataRow(1, false, "Big Fat Orang Utan")]
    [DataTestMethod]
    public void StringExtensionMethods_SplitEnumValueCamelCaseValid(int id, bool toLower, string output) =>
      Assert.AreEqual(output, id.SplitEnumValueCamelCase<Animals>(toLower));

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

    #region OtherLines

    [DataTestMethod]
    [DynamicData(nameof(GetOtherLinesTestData), DynamicDataSourceType.Method)]
    public void StringExtensionMethods_OtherLines(string multi, IEnumerable<string> otherLines) {
      IEnumerable<string> res = multi.OtherLines();
      //Assert.AreEqual(otherLines.Count(), res.Count());
      CollectionAssert.AreEqual(otherLines.ToList(), multi.OtherLines().ToList());
    }

    private static IEnumerable<object[]> GetOtherLinesTestData() {
      yield return new object[] { "", new List<string>() };
      yield return new object[] { "1 Jim Street", new List<string>() };
      yield return new object[] { "1 Jim Street\nJimsville", new List<string>{ "Jimsville" } };
      yield return new object[] { "1 Jim Street\nJimsville\nJV1 7YT", new List<string>{ "Jimsville", "JV1 7YT" } };
      yield return new object[] { $"1 Jim Street{Environment.NewLine}Jimsville", new List<string> { "Jimsville" } };
      yield return new object[] { $"1 Jim Street{Environment.NewLine}Jimsville{Environment.NewLine}JV1 7YT", new List<string> { "Jimsville", "JV1 7YT" } };
    }

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

    #region ToTitleCase
    [DataRow("", "")]
    [DataRow("This is an example", "This Is An Example")]
    [DataTestMethod]
    public void StringExtensionMethods_ToTitleCase(string value, string expected) =>
      Assert.AreEqual(expected, value.ToTitleCase());
    #endregion
  }
}