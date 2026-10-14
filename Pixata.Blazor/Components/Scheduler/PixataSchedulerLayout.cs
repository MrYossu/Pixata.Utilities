using System.Collections.Generic;
using System.Linq;

namespace Pixata.Blazor.Components;

/// <summary>
/// Where an appointment sits among the ones it overlaps in a day column: it is in lane <see cref="Lane"/> of
/// <see cref="Lanes"/>, so it takes up 1/<see cref="Lanes"/> of the column's width
/// </summary>
internal readonly record struct PixataSchedulerLane(int Lane, int Lanes);

/// <summary>
/// The scheduler's layout maths: laying overlapping appointments out side by side in a day column, and stacking bars (all-day
/// appointments, the month view, the timeline) into rows. Kept apart from the component so it can be unit tested
/// </summary>
internal static class PixataSchedulerLayout {
  /// <summary>
  /// Lays appointments out side by side in a time column. Appointments that overlap (directly, or through others) form a
  /// cluster, and every appointment in a cluster is given the same number of lanes, so they line up. Each appointment goes
  /// in the first lane that is free when it starts
  /// </summary>
  /// <param name="items">The start and end of each appointment (eg in ticks). An end that isn't after the start is treated as a moment</param>
  /// <returns>The lane for each appointment, in the same order as <paramref name="items"/></returns>
  public static PixataSchedulerLane[] Columns(IReadOnlyList<(long Start, long End)> items) {
    PixataSchedulerLane[] result = new PixataSchedulerLane[items.Count];
    List<int> order = Order(items);
    List<int> cluster = [];
    List<long> laneEnds = [];
    int[] lanes = new int[items.Count];
    long clusterEnd = long.MinValue;

    void CloseCluster() {
      foreach (int index in cluster) {
        result[index] = new(lanes[index], laneEnds.Count);
      }
      cluster.Clear();
      laneEnds.Clear();
    }

    foreach (int index in order) {
      (long start, long end) = Normalise(items[index]);
      if (cluster.Count > 0 && start >= clusterEnd) {
        CloseCluster();
      }
      int lane = laneEnds.FindIndex(laneEnd => laneEnd <= start);
      if (lane < 0) {
        lane = laneEnds.Count;
        laneEnds.Add(end);
      } else {
        laneEnds[lane] = end;
      }
      lanes[index] = lane;
      cluster.Add(index);
      clusterEnd = cluster.Count == 1 ? end : (end > clusterEnd ? end : clusterEnd);
    }
    CloseCluster();
    return result;
  }

  /// <summary>
  /// Stacks bars into rows, so that no two bars in a row overlap. Bars are placed in order of start (longer ones first when
  /// they start together), each in the first row that is free
  /// </summary>
  /// <param name="items">The start and end of each bar (eg in ticks, or column numbers)</param>
  /// <returns>The row for each bar, in the same order as <paramref name="items"/></returns>
  public static int[] Rows(IReadOnlyList<(long Start, long End)> items) {
    int[] result = new int[items.Count];
    List<long> rowEnds = [];
    foreach (int index in Order(items)) {
      (long start, long end) = Normalise(items[index]);
      int row = rowEnds.FindIndex(rowEnd => rowEnd <= start);
      if (row < 0) {
        row = rowEnds.Count;
        rowEnds.Add(end);
      } else {
        rowEnds[row] = end;
      }
      result[index] = row;
    }
    return result;
  }

  /// <summary>
  /// The number of rows <see cref="Rows"/> used
  /// </summary>
  public static int RowCount(IEnumerable<int> rows) =>
    rows.DefaultIfEmpty(-1).Max() + 1;

  // Earliest first, and the longest first when they start together, so that long appointments get the leftmost lanes
  private static List<int> Order(IReadOnlyList<(long Start, long End)> items) =>
    Enumerable.Range(0, items.Count)
      .OrderBy(i => items[i].Start)
      .ThenByDescending(i => Normalise(items[i]).End)
      .ThenBy(i => i)
      .ToList();

  // An appointment with no length still takes up a moment, so it overlaps others that start at the same time
  private static (long Start, long End) Normalise((long Start, long End) item) =>
    item.End > item.Start ? item : (item.Start, item.Start + 1);
}
