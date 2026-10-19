using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Pixata.Extensions;

namespace Pixata.SimilarityChooser;

public class SimilarEntityChooser<T> {
  // A word that is at least this long matches a word that is the same with a few letters added, eg "apple" and "apples"
  private const int MinimumStemLength = 4;
  private const int MaximumAddedLetters = 3;

  private record Word(string Text, string PrimaryKey, string? AlternateKey);

  private record Name(MatchingEntityOverview<T> Entity, List<Word> Words);

  private readonly SimilarEntityChooserOptions _options;
  private readonly HashSet<string> _ignoredWords;
  private readonly List<Name> _names = [];

  // Every word is indexed by its sound, its spelling and its first few letters, so the names that might match a search can be found
  // without comparing it with every name in the map. The values are indexes into _names
  private readonly Dictionary<string, List<int>> _index = [];

  /// <summary>
  /// Create a new instance of a SimilarityChooser, which allows you to find similar words to those in the collection
  /// </summary>
  /// <param name="entities">A collection of entities that contains a string property to be used for the similarity search</param>
  /// <param name="options">Words to ignore, and how close a name has to be to match. See <see cref="SimilarEntityChooserOptions"/></param>
  public static SimilarEntityChooser<T> CreateMap<T>(IEnumerable<MatchingEntityOverview<T>> entities, SimilarEntityChooserOptions? options = null) =>
    new(entities, options ?? new());

  private SimilarEntityChooser(IEnumerable<MatchingEntityOverview<T>> entities, SimilarEntityChooserOptions options) {
    _options = options;
    _ignoredWords = options.IgnoredWords.SelectMany(SplitWords).ToHashSet();
    foreach (MatchingEntityOverview<T> entity in entities) {
      List<Word> words = GetWords(entity.MatchText);
      if (words.Count == 0) {
        continue;
      }
      _names.Add(new(entity, words));
      foreach (string key in words.SelectMany(IndexKeys).Distinct()) {
        if (!_index.TryGetValue(key, out List<int>? indexes)) {
          indexes = [];
          _index[key] = indexes;
        }
        indexes.Add(_names.Count - 1);
      }
    }
  }

  /// <summary>
  /// Find the entities whose names are similar to the text passed in. Assumes that CreateMap() has already been called to build the
  /// SimilarityChooser. The closest matches come first
  /// </summary>
  /// <param name="word">The text to be used for comparison. This can be one word or several</param>
  /// <returns>A collection of potential matches</returns>
  public List<MatchingEntityOverview<T>> FindSimilar(string word) {
    List<Word> searchWords = GetWords(word);
    if (searchWords.Count == 0) {
      return [];
    }
    return searchWords
      .SelectMany(IndexKeys)
      .Where(_index.ContainsKey)
      .SelectMany(key => _index[key])
      .Distinct()
      .Select(index => (Index: index, Name: _names[index], Score: Score(searchWords, _names[index].Words)))
      .Where(match => match.Score is not null)
      .OrderByDescending(match => match.Score)
      .ThenBy(match => match.Name.Words.Count)
      .ThenBy(match => match.Index)
      .GroupBy(match => match.Name.Entity.ID)
      .Select(group => group.First().Name.Entity)
      .ToList();
  }

  // How much of the two names matched, from 0 to 1, or null if they don't match
  private double? Score(List<Word> searchWords, List<Word> nameWords) {
    int matchedSearchWords = searchWords.Count(searchWord => nameWords.Any(nameWord => AreClose(searchWord, nameWord)));
    int matchedNameWords = nameWords.Count(nameWord => searchWords.Any(searchWord => AreClose(searchWord, nameWord)));
    bool matches = _options.MatchAllWords
      ? searchWords.Count <= nameWords.Count ? matchedSearchWords == searchWords.Count : matchedNameWords == nameWords.Count
      : matchedSearchWords > 0;
    return matches ? (matchedSearchWords + matchedNameWords) / (double)(searchWords.Count + nameWords.Count) : null;
  }

  private bool AreClose(Word a, Word b) {
    if (a.Text == b.Text || IsStemOf(a.Text, b.Text) || IsStemOf(b.Text, a.Text)) {
      return true;
    }
    if (!SoundAlike(a, b)) {
      return false;
    }
    return !_options.RequireCloseSpelling || EditDistance(a.Text, b.Text) <= Math.Max(a.Text.Length, b.Text.Length) / 3;
  }

  // Catches plurals and the like, eg "apple" and "apples", or "advance" and "advanced", whose keys are different
  private static bool IsStemOf(string stem, string word) =>
    stem.Length >= MinimumStemLength && word.Length > stem.Length && word.Length - stem.Length <= MaximumAddedLetters
    && word.StartsWith(stem, StringComparison.Ordinal);

  private static bool SoundAlike(Word a, Word b) =>
    a.PrimaryKey != "" && b.PrimaryKey != ""
    && (a.PrimaryKey == b.PrimaryKey || a.PrimaryKey == b.AlternateKey || a.AlternateKey == b.PrimaryKey
        || (a.AlternateKey is not null && a.AlternateKey == b.AlternateKey));

  // The number of letters that have to be added, removed or changed to get from one word to the other (the Levenshtein distance)
  private static int EditDistance(string a, string b) {
    int[] previous = Enumerable.Range(0, b.Length + 1).ToArray();
    int[] current = new int[b.Length + 1];
    for (int i = 1; i <= a.Length; i++) {
      current[0] = i;
      for (int j = 1; j <= b.Length; j++) {
        int substitution = previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1);
        current[j] = Math.Min(substitution, Math.Min(previous[j] + 1, current[j - 1] + 1));
      }
      (previous, current) = (current, previous);
    }
    return previous[b.Length];
  }

  private static IEnumerable<string> IndexKeys(Word word) {
    yield return $"w:{word.Text}";
    if (word.Text.Length >= MinimumStemLength) {
      yield return $"p:{word.Text[..MinimumStemLength]}";
    }
    if (word.PrimaryKey != "") {
      yield return $"k:{word.PrimaryKey}";
    }
    if (!string.IsNullOrEmpty(word.AlternateKey)) {
      yield return $"k:{word.AlternateKey}";
    }
  }

  // The words of a name, leaving out the ignored ones, unless that would leave nothing
  private List<Word> GetWords(string? text) {
    List<string> words = SplitWords(text).ToList();
    List<string> kept = words.Where(word => !_ignoredWords.Contains(word)).ToList();
    return (kept.Count > 0 ? kept : words)
      .Select(word => {
        DoubleMetaphone metaphone = new(word);
        return new Word(word, metaphone.PrimaryKey, metaphone.AlternateKey);
      })
      .ToList();
  }

  // Splits text into lower-case words without accents. Spaces, hyphens, slashes, commas and the like separate words, so "Smith-Jones" is
  // two words, and other punctuation is dropped, so "O'Brien", "A.B.C." and "AT&T" are one word each
  private static IEnumerable<string> SplitWords(string? text) {
    StringBuilder word = new();
    foreach (char c in (text ?? "").RemoveDiacritics()) {
      if (char.IsLetterOrDigit(c)) {
        word.Append(char.ToLowerInvariant(c));
      } else if (IsSeparator(c) && word.Length > 0) {
        yield return word.ToString();
        word.Clear();
      }
    }
    if (word.Length > 0) {
      yield return word.ToString();
    }
  }

  private static bool IsSeparator(char c) =>
    char.IsWhiteSpace(c)
    || char.GetUnicodeCategory(c) is UnicodeCategory.DashPunctuation or UnicodeCategory.OpenPunctuation or UnicodeCategory.ClosePunctuation
    || c is '/' or '\\' or ',' or ';' or ':' or '+' or '|';
}
