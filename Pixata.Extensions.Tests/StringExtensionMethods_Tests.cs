using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pixata.Extensions.Tests {
  [TestClass]
  public class StringExtensionMethods_Tests {
    #region SplitCamelCase

    [DataRow("", "")]
    [DataRow("Hello", "Hello")]
    [DataRow("HelloWorld", "Hello World")]
    [DataTestMethod]
    public void StringExtensionMethods_SplitCamelCase(string input, string output) =>
      Assert.AreEqual(output, input.SplitCamelCase());

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