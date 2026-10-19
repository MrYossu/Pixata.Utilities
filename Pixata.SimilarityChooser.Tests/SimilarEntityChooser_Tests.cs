using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pixata.SimilarityChooser.Tests;

[TestClass]
public class SimilarEntityChooser_Tests {
  private static SimilarEntityChooser<string> CreateMap(IEnumerable<string> names, SimilarEntityChooserOptions? options = null) =>
    SimilarEntityChooser<string>.CreateMap(names.Select((name, n) => new MatchingEntityOverview<string> { ID = n + 1, MatchText = name, Entity = name }), options);

  private static List<string> FindSimilar(IEnumerable<string> names, string search, SimilarEntityChooserOptions? options = null) =>
    CreateMap(names, options).FindSimilar(search).Select(match => match.Entity).ToList();

  private static SimilarEntityChooserOptions CompanyOptions => new() { IgnoredWords = SimilarEntityChooserOptions.CompanyNameWords };

  // The companies from the All Dev system that were refused as similar to Advanced Business Ventures Ltd
  [DataRow("YNBY Ltd")]
  [DataRow("2Give Ltd")]
  [DataRow("OneGiv Services Ltd")]
  [DataRow("Insight Business Management")]
  [DataRow("CDM Cavendish Ltd")]
  [DataTestMethod]
  public void SimilarEntityChooser_OneCommonWord_DoesNotMatch(string search) {
    Assert.AreEqual(0, FindSimilar(["Advanced Business Ventures Ltd"], search).Count);
    Assert.AreEqual(0, FindSimilar(["Advanced Business Ventures Ltd"], search, CompanyOptions).Count);
  }

  [TestMethod]
  public void SimilarEntityChooser_EveryWordOfTheShorterName_Matches() {
    CollectionAssert.AreEqual(new[] { "Advanced Business Ventures Ltd" }, FindSimilar(["Advanced Business Ventures Ltd"], "Advance Business Ventures"));
    Assert.AreEqual(0, FindSimilar(["Advanced Ventures"], "Smith Ventures").Count);
  }

  [TestMethod]
  public void SimilarEntityChooser_ShorterNameInTheSearch_Matches() =>
    CollectionAssert.AreEqual(new[] { "Syrup" }, FindSimilar(["Syrup", "Golden Rice"], "maple syrup"));

  [TestMethod]
  public void SimilarEntityChooser_MatchAllWordsOff_OneWordIsEnough() =>
    CollectionAssert.AreEqual(new[] { "Advanced Ventures" }, FindSimilar(["Advanced Ventures"], "Smith Ventures", new() { MatchAllWords = false }));

  [TestMethod]
  public void SimilarEntityChooser_SoundAlikeButSpeltDifferently_DoesNotMatch() {
    Assert.AreEqual(0, FindSimilar(["Coventry"], "Cavendish").Count);
    CollectionAssert.AreEqual(new[] { "Coventry" }, FindSimilar(["Coventry"], "Cavendish", new() { RequireCloseSpelling = false }));
  }

  [DataRow("Hummous", "humus")]
  [DataRow("Smyth", "Smith")]
  [DataRow("Colour", "color")]
  [DataRow("Apples", "apple")]
  [DataRow("Apple", "apples")]
  [DataRow("Businesses", "business")]
  [DataRow("Café Rouge", "cafe rouge")]
  [DataRow("Smith-Jones", "Smith Jones")]
  [DataRow("O'Brien", "obrien")]
  [DataTestMethod]
  public void SimilarEntityChooser_CloseWords_Match(string name, string search) =>
    CollectionAssert.AreEqual(new[] { name }, FindSimilar([name], search));

  [DataRow("Marketing", "Mark")]
  [DataRow("Application", "Apple")]
  [DataRow("Jones Marketing", "Mark Jones")]
  [DataTestMethod]
  public void SimilarEntityChooser_DifferentWords_DoNotMatch(string name, string search) =>
    Assert.AreEqual(0, FindSimilar([name], search).Count);

  [TestMethod]
  public void SimilarEntityChooser_IgnoredWords_AreLeftOut() {
    Assert.AreEqual(0, FindSimilar(["Smith Ltd"], "Smith Services").Count);
    CollectionAssert.AreEqual(new[] { "Smith Ltd" }, FindSimilar(["Smith Ltd"], "Smith Services", CompanyOptions));
    CollectionAssert.AreEqual(new[] { "Smith Ltd." }, FindSimilar(["Smith Ltd."], "smith limited", CompanyOptions));
  }

  [TestMethod]
  public void SimilarEntityChooser_NameMadeOnlyOfIgnoredWords_KeepsThem() {
    CollectionAssert.AreEqual(new[] { "The Company Ltd" }, FindSimilar(["The Company Ltd", "Acme Ltd"], "The Company Ltd", CompanyOptions));
    Assert.AreEqual(0, FindSimilar(["The Company Ltd"], "Acme", CompanyOptions).Count);
  }

  [TestMethod]
  public void SimilarEntityChooser_CloserNames_ComeFirst() {
    CollectionAssert.AreEqual(new[] { "Acme Ltd", "Acme Widgets Ltd" }, FindSimilar(["Acme Widgets Ltd", "Acme Ltd"], "Acme"));
    CollectionAssert.AreEqual(new[] { "Acme Ltd", "Acme Widgets Ltd" }, FindSimilar(["Acme Widgets Ltd", "Acme Ltd"], "Acme", CompanyOptions));
  }

  [TestMethod]
  public void SimilarEntityChooser_SameId_ReturnedOnce() {
    SimilarEntityChooser<string> chooser = SimilarEntityChooser<string>.CreateMap([
      new MatchingEntityOverview<string> { ID = 1, MatchText = "Acme", Entity = "Acme" },
      new MatchingEntityOverview<string> { ID = 1, MatchText = "Acme Widgets", Entity = "Acme Widgets" }
    ]);
    List<MatchingEntityOverview<string>> matches = chooser.FindSimilar("acme");
    Assert.AreEqual(1, matches.Count);
    Assert.AreEqual("Acme", matches[0].Entity);
  }

  [DataRow("")]
  [DataRow("   ")]
  [DataRow("-- & --")]
  [DataTestMethod]
  public void SimilarEntityChooser_NoWords_FindsNothing(string search) =>
    Assert.AreEqual(0, FindSimilar(["Acme"], search).Count);

  [TestMethod]
  public void SimilarEntityChooser_NullMatchText_IsSkipped() {
    SimilarEntityChooser<string> chooser = SimilarEntityChooser<string>.CreateMap([
      new MatchingEntityOverview<string> { ID = 1, MatchText = null!, Entity = "Nothing" },
      new MatchingEntityOverview<string> { ID = 2, MatchText = "Acme", Entity = "Acme" }
    ]);
    CollectionAssert.AreEqual(new[] { "Acme" }, chooser.FindSimilar("acme").Select(match => match.Entity).ToList());
  }
}
