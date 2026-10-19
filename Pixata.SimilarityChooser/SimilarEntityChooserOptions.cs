using System.Collections.Generic;

namespace Pixata.SimilarityChooser;

/// <summary>
/// Options for <c>SimilarEntityChooser&lt;T&gt;.CreateMap()</c>
/// </summary>
public class SimilarEntityChooserOptions {
  /// <summary>
  /// Words that are left out of the names and the search text, as they are too common to tell two names apart (eg "Ltd" in company names).
  /// Case, accents and punctuation don't matter, so "ltd." is the same as "Ltd". A name made only of these words keeps them all, so it can
  /// still be found. Defaults to none. <see cref="CompanyNameWords"/> is a ready-made list for company names
  /// </summary>
  public IEnumerable<string> IgnoredWords { get; set; } = [];

  /// <summary>
  /// When true (the default), a name only matches when every word of the shorter of the name and the search text is close to a word of the
  /// longer one, so "Smith Ventures" doesn't match "Advanced Ventures", but "Advance Business Ventures" matches "Advanced Business Ventures
  /// Ltd". When false, one close word is enough, as it was before v2.0.0
  /// </summary>
  public bool MatchAllWords { get; set; } = true;

  /// <summary>
  /// When true (the default), two words that sound alike (have the same Double Metaphone key) are only close if they are spelt alike as
  /// well, ie no more than a third of the letters of the longer word need changing to get from one to the other. This stops "Cavendish"
  /// matching "Coventry", which sound alike as far as the key goes. When false, sounding alike is enough, as it was before v2.0.0
  /// </summary>
  public bool RequireCloseSpelling { get; set; } = true;

  /// <summary>
  /// Words that are common in company names, for use as <see cref="IgnoredWords"/>
  /// </summary>
  public static IReadOnlyList<string> CompanyNameWords { get; } = [
    "Ltd", "Limited", "LLP", "PLC", "CIC", "Inc", "LLC", "Co", "Company", "Services", "Business", "Management", "Group", "Holdings",
    "Solutions", "UK", "The", "And", "Of"
  ];
}
