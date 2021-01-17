using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pixata.Extensions.Tests {
  [TestClass]
  public class StringExtensionMethods_Tests {
    [DataRow("", "")]
    [DataRow("Hello", "Hello")]
    [DataRow("HelloWorld", "Hello World")]
    [DataTestMethod]
    public void StringExtensionMethods_SplitCamelCase(string input, string output) =>
      Assert.AreEqual(output, input.SplitCamelCase());
  }
}