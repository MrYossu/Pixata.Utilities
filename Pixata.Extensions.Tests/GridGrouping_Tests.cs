using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Extensions.Grid;

namespace Pixata.Extensions.Tests;

[TestClass]
public class GridGrouping_Tests {
  public record Person(int Id, string Team, bool Manager, decimal Salary);

  private static readonly List<Person> People = [
    new(1, "B", false, 10m),
    new(2, "A", true, 20m),
    new(3, "B", true, 30m),
    new(4, "A", false, 40m),
    new(5, "C", false, 50m),
    new(6, "A", false, 60m)
  ];

  [TestMethod]
  public void Aggregate_CalculatesEachRequestedType() {
    Dictionary<string, GridFieldAggregates> results = People.AsQueryable().Aggregate([
      new GridAggregateDescriptor("Salary", GridAggregateType.Sum),
      new GridAggregateDescriptor("Salary", GridAggregateType.Average),
      new GridAggregateDescriptor("Salary", GridAggregateType.Min),
      new GridAggregateDescriptor("Salary", GridAggregateType.Max),
      new GridAggregateDescriptor("Team", GridAggregateType.Count)
    ]);

    Assert.AreEqual(210m, results["Salary"].Sum);
    Assert.AreEqual(35m, results["Salary"].Average);
    Assert.AreEqual(10m, results["Salary"].Min);
    Assert.AreEqual(60m, results["Salary"].Max);
    Assert.AreEqual(6, results["Team"].Count);
    Assert.IsNull(results["Team"].Sum);
  }

  [TestMethod]
  public void Aggregate_OfNoItems_LeavesMinMaxAndAverageNull() {
    Dictionary<string, GridFieldAggregates> results = People.AsQueryable().Where(p => p.Id > 99).Aggregate([
      new GridAggregateDescriptor("Salary", GridAggregateType.Min),
      new GridAggregateDescriptor("Salary", GridAggregateType.Average),
      new GridAggregateDescriptor("Salary", GridAggregateType.Sum)
    ]);

    Assert.IsNull(results["Salary"].Min);
    Assert.IsNull(results["Salary"].Average);
    Assert.AreEqual(0m, results["Salary"].Sum);
  }

  [TestMethod]
  public void ToGridResult_Groups_SortByGroupAndCountAndAggregateWholeGroups() {
    GridResult<Person> result = People.ToGridResult(new GridRequest {
      PageSize = 3,
      Groups = [new("Team")],
      Sorts = [new("Salary", ListSortDirection.Descending)],
      Aggregates = [new("Salary", GridAggregateType.Sum)]
    });

    // The page is the first three items in group order: team A, by salary descending
    CollectionAssert.AreEqual(new[] { 6, 4, 2 }, result.Data.Select(p => p.Id).ToArray());
    Assert.AreEqual(1, result.Groups!.Count);
    Assert.AreEqual("A", result.Groups[0].Key);
    Assert.AreEqual(3, result.Groups[0].Count);
    Assert.AreEqual(120m, result.Groups[0].Aggregates["Salary"].Sum);
    Assert.AreEqual(210m, result.Aggregates!["Salary"].Sum);
  }

  [TestMethod]
  public void ToGridResult_NestedGroups_HaveSubgroupsAndItemsAtTheBottom() {
    GridResult<Person> result = People.ToGridResult(new GridRequest { Groups = [new("Team"), new("Manager", ListSortDirection.Descending)] });

    CollectionAssert.AreEqual(new[] { "A", "B", "C" }, result.Groups!.Select(g => (string)g.Key!).ToArray());
    GridGroup<Person> teamA = result.Groups![0];
    Assert.AreEqual(0, teamA.Items.Count);
    CollectionAssert.AreEqual(new object[] { true, false }, teamA.Subgroups.Select(g => g.Key).ToArray());
    CollectionAssert.AreEqual(new[] { 4, 6 }, teamA.Subgroups[1].Items.Select(p => p.Id).ToArray());
    Assert.AreEqual(2, teamA.Subgroups[1].Count);
  }

  [TestMethod]
  public void ToGridResult_PageInTheMiddleOfAGroup_StillCountsTheWholeGroup() {
    GridResult<Person> result = People.ToGridResult(new GridRequest { Page = 2, PageSize = 2, Groups = [new("Team")] });

    // Team A has three people, so the second page starts with its last one, then team B's first
    CollectionAssert.AreEqual(new[] { "A", "B" }, result.Groups!.Select(g => (string)g.Key!).ToArray());
    Assert.AreEqual(1, result.Groups![0].Items.Count);
    Assert.AreEqual(3, result.Groups[0].Count);
  }
}
