namespace Pixata.SimilarityChooser {
  public class MatchingEntityOverview<T> {
    public int ID { get; set; }
    public string MatchText { get; set; }
    public T Entity { get; set; }
  }
}