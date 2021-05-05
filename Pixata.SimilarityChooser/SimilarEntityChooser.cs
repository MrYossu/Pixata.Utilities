using System.Collections.Generic;
using System.Linq;
using Pixata.Extensions;

namespace Pixata.SimilarityChooser {
  public class SimilarEntityChooser<T> {
    private readonly Dictionary<string, List<MatchingEntityOverview<T>>> _map;

    /// <summary>
    /// Create a new instance of a SimilarityChooser, which allows you to find similar words to those in the collection
    /// </summary>
    /// <param name="entities">A collection of entities that contains a string property to be used for the similarity search</param>
    public static SimilarEntityChooser<T> CreateMap<T>(IEnumerable<MatchingEntityOverview<T>> entities) =>
      new(entities);

    private SimilarEntityChooser(IEnumerable<MatchingEntityOverview<T>> entities) {
      _map = new Dictionary<string, List<MatchingEntityOverview<T>>>();
      foreach (MatchingEntityOverview<T> entity in entities) {
        foreach (string word in entity.MatchText.Split(' ')) {
          DoubleMetaphone mp = new(StripPunctuation(word).RemoveDiacritics());
          AddToMap(_map, mp.PrimaryKey, entity);
          if (mp.AlternateKey != null) {
            AddToMap(_map, mp.AlternateKey, entity);
          }
        }
      }
    }

    private static void AddToMap<T>(IDictionary<string, List<MatchingEntityOverview<T>>> wordsMap, string key, MatchingEntityOverview<T> c) {
      if (!wordsMap.ContainsKey(key)) {
        wordsMap[key] = new List<MatchingEntityOverview<T>> { c };
      } else {
        wordsMap[key].Add(c);
      }
    }

    /// <summary>
    /// Find similar words to the one passed in. Assumes that CreateMap() has already been called to build the SimilarityChooser
    /// </summary>
    /// <param name="word">The word to be used for comparison</param>
    /// <returns>A collection of potential matches</returns>
    public List<MatchingEntityOverview<T>> FindSimilar(string word) =>
      word
        .RemoveDiacritics()
        .Split(' ')
        .Select(bit => new DoubleMetaphone(StripPunctuation(bit)))
        .Where(mp => mp.PrimaryKey.Trim() != "" && _map.ContainsKey(mp.PrimaryKey))
        .SelectMany(mp => _map[mp.PrimaryKey])
        .GroupBy(c => c.ID)
        .OrderByDescending(g => g.Count())
        .Select(g => g.First())
        .ToList();

    private string StripPunctuation(string s) =>
      new(s.ToCharArray().Where(c => !char.IsPunctuation(c)).ToArray());
  }
}