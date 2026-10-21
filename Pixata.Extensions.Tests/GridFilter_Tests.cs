using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Extensions.Grid;

namespace Pixata.Extensions.Tests;

[TestClass]
public class GridFilter_Tests {
  public enum Pet {
    Cat,
    Dog,
    Fish
  }

  public record Address(string City);

  public record Person(int Id, string? Name, decimal Salary, DateTime Born, Pet Pet, bool Manager, int? Age = null, Address? Address = null);

  private static readonly List<Person> People = [
    new(1, "Alice", 100m, new(1980, 1, 1), Pet.Cat, true, 40, new("Leeds")),
    new(2, "bob", 200m, new(1990, 6, 15), Pet.Dog, false, null, new("York")),
    new(3, "Carol", 300m, new(2000, 12, 31), Pet.Fish, false, 25),
    new(4, null, 400m, new(1990, 6, 15), Pet.Dog, true, 33, new("leeds")),
    new(5, "", 500m, new(1975, 3, 3), Pet.Cat, false)
  ];

  private static int[] Ids(params GridFilterDescriptor[] filters) =>
    Ids(GridFilterLogic.And, filters);

  private static int[] Ids(GridFilterLogic logic, params GridFilterDescriptor[] filters) =>
    [.. People.AsQueryable().ApplyFilters([new GridCompositeFilterDescriptor(logic, [.. filters])]).Select(p => p.Id)];

  [TestMethod]
  public void StringOperators_IgnoreCaseAndCopeWithNulls() {
    CollectionAssert.AreEqual(new[] { 1, 3 }, Ids(new GridFilterDescriptor("Name", GridFilterOperator.Contains, "L")));
    CollectionAssert.AreEqual(new[] { 2, 4, 5 }, Ids(new GridFilterDescriptor("Name", GridFilterOperator.DoesNotContain, "l")));
    CollectionAssert.AreEqual(new[] { 2 }, Ids(new GridFilterDescriptor("Name", GridFilterOperator.StartsWith, "B")));
    CollectionAssert.AreEqual(new[] { 3 }, Ids(new GridFilterDescriptor("Name", GridFilterOperator.EndsWith, "OL")));
    CollectionAssert.AreEqual(new[] { 2 }, Ids(new GridFilterDescriptor("Name", GridFilterOperator.IsEqualTo, "BOB")));
    CollectionAssert.AreEqual(new[] { 1, 3, 4, 5 }, Ids(new GridFilterDescriptor("Name", GridFilterOperator.IsNotEqualTo, "bob")));
    CollectionAssert.AreEqual(new[] { 4 }, Ids(new GridFilterDescriptor("Name", GridFilterOperator.IsNull)));
    CollectionAssert.AreEqual(new[] { 5 }, Ids(new GridFilterDescriptor("Name", GridFilterOperator.IsEmpty)));
    CollectionAssert.AreEqual(new[] { 4, 5 }, Ids(new GridFilterDescriptor("Name", GridFilterOperator.IsNullOrEmpty)));
    CollectionAssert.AreEqual(new[] { 1, 2, 3 }, Ids(new GridFilterDescriptor("Name", GridFilterOperator.IsNotNullOrEmpty)));
  }

  [TestMethod]
  public void NumberAndDateComparisons_Work() {
    CollectionAssert.AreEqual(new[] { 1, 2 }, Ids(new GridFilterDescriptor("Salary", GridFilterOperator.IsLessThan, 300)));
    CollectionAssert.AreEqual(new[] { 3, 4, 5 }, Ids(new GridFilterDescriptor("Salary", GridFilterOperator.IsGreaterThanOrEqualTo, 300m)));
    CollectionAssert.AreEqual(new[] { 2, 4 }, Ids(new GridFilterDescriptor("Born", GridFilterOperator.IsEqualTo, new DateTime(1990, 6, 15))));
    CollectionAssert.AreEqual(new[] { 3 }, Ids(new GridFilterDescriptor("Born", GridFilterOperator.IsGreaterThan, new DateOnly(1990, 6, 15))));
  }

  public record Visit(int Id, DateTime When, DateTime? Left);

  private static readonly List<Visit> Visits = [
    new(1, new(2026, 10, 20, 23, 59, 59), null),
    new(2, new(2026, 10, 21), new(2026, 10, 21, 9, 30, 0)),
    new(3, new(2026, 10, 21, 14, 15, 0), new(2026, 10, 22, 1, 0, 0)),
    new(4, new(2026, 10, 22, 0, 0, 0), new(2026, 10, 20, 8, 0, 0))
  ];

  private static int[] VisitIds(GridFilterLogic logic, params GridFilterDescriptor[] filters) =>
    [.. Visits.AsQueryable().ApplyFilters([new GridCompositeFilterDescriptor(logic, [.. filters])]).Select(v => v.Id)];

  private static int[] VisitIds(string member, GridFilterOperator op, object? value) =>
    VisitIds(GridFilterLogic.And, new GridFilterDescriptor(member, op, value) { IgnoreTime = true });

  [TestMethod]
  public void IgnoreTime_ComparesTheDateOnly() {
    DateTime day = new(2026, 10, 21, 12, 0, 0);
    CollectionAssert.AreEqual(new[] { 2, 3 }, VisitIds("When", GridFilterOperator.IsEqualTo, day));
    CollectionAssert.AreEqual(new[] { 1, 4 }, VisitIds("When", GridFilterOperator.IsNotEqualTo, day));
    CollectionAssert.AreEqual(new[] { 1 }, VisitIds("When", GridFilterOperator.IsLessThan, day));
    CollectionAssert.AreEqual(new[] { 1, 2, 3 }, VisitIds("When", GridFilterOperator.IsLessThanOrEqualTo, day));
    CollectionAssert.AreEqual(new[] { 4 }, VisitIds("When", GridFilterOperator.IsGreaterThan, day));
    CollectionAssert.AreEqual(new[] { 2, 3, 4 }, VisitIds("When", GridFilterOperator.IsGreaterThanOrEqualTo, day));
    CollectionAssert.AreEqual(new[] { 2, 3 }, VisitIds("When", GridFilterOperator.IsEqualTo, new DateOnly(2026, 10, 21)));
  }

  [TestMethod]
  public void IgnoreTime_WorksOnNullableDates() {
    DateTime day = new(2026, 10, 21);
    CollectionAssert.AreEqual(new[] { 2 }, VisitIds("Left", GridFilterOperator.IsEqualTo, day));
    CollectionAssert.AreEqual(new[] { 1, 3, 4 }, VisitIds("Left", GridFilterOperator.IsNotEqualTo, day));
    CollectionAssert.AreEqual(new[] { 3 }, VisitIds("Left", GridFilterOperator.IsGreaterThan, day));
    CollectionAssert.AreEqual(new[] { 1 }, VisitIds("Left", GridFilterOperator.IsNull, null));
  }

  [TestMethod]
  public void WithoutIgnoreTime_TheTimeIsCompared() =>
    CollectionAssert.AreEqual(new[] { 2 }, VisitIds(GridFilterLogic.And, new GridFilterDescriptor("When", GridFilterOperator.IsEqualTo, new DateTime(2026, 10, 21))));

  [TestMethod]
  public void IgnoreTime_WorksInAnOrComposite() =>
    CollectionAssert.AreEqual(new[] { 1, 4 }, VisitIds(GridFilterLogic.Or,
      new GridFilterDescriptor("When", GridFilterOperator.IsEqualTo, new DateTime(2026, 10, 20)) { IgnoreTime = true },
      new GridFilterDescriptor("When", GridFilterOperator.IsEqualTo, new DateTime(2026, 10, 22)) { IgnoreTime = true }));

  [TestMethod]
  public void RemoveTime_SetsIgnoreTimeOnTheNamedMembersOnly() {
    GridRequest request = new() {
      Filters = [
        new(GridFilterLogic.And, [new("When", GridFilterOperator.IsEqualTo, new DateTime(2026, 10, 21)), new("Left", GridFilterOperator.IsNotNull)]),
        new(GridFilterLogic.And, [new("Id", GridFilterOperator.IsGreaterThan, 0)])
      ]
    };
    GridRequest result = request.RemoveTime("When");
    Assert.AreSame(request, result);
    Assert.IsTrue(request.Filters[0].Filters[0].IgnoreTime);
    Assert.IsFalse(request.Filters[0].Filters[1].IgnoreTime);
    Assert.IsFalse(request.Filters[1].Filters[0].IgnoreTime);
    CollectionAssert.AreEqual(new[] { 2, 3 }, Visits.ToGridResult(request).Data.Select(v => v.Id).ToArray());
  }

  [TestMethod]
  public void IgnoreTime_SurvivesJson() {
    GridFilterDescriptor filter = new("When", GridFilterOperator.IsEqualTo, new DateTime(2026, 10, 21)) { IgnoreTime = true };
    GridFilterDescriptor copy = JsonSerializer.Deserialize<GridFilterDescriptor>(JsonSerializer.Serialize(filter))!;
    Assert.IsTrue(copy.IgnoreTime);
    CollectionAssert.AreEqual(new[] { 2, 3 }, VisitIds(GridFilterLogic.And, copy));
  }

  [TestMethod]
  public void NullableValues_CanBeComparedAndTestedForNull() {
    CollectionAssert.AreEqual(new[] { 2, 5 }, Ids(new GridFilterDescriptor("Age", GridFilterOperator.IsNull)));
    CollectionAssert.AreEqual(new[] { 1, 4 }, Ids(new GridFilterDescriptor("Age", GridFilterOperator.IsGreaterThan, 30)));
  }

  [TestMethod]
  public void EnumsAndBools_Work() {
    CollectionAssert.AreEqual(new[] { 2, 4 }, Ids(new GridFilterDescriptor("Pet", GridFilterOperator.IsEqualTo, Pet.Dog)));
    CollectionAssert.AreEqual(new[] { 2, 4 }, Ids(new GridFilterDescriptor("Pet", GridFilterOperator.IsEqualTo, "dog")));
    CollectionAssert.AreEqual(new[] { 3 }, Ids(new GridFilterDescriptor("Pet", GridFilterOperator.IsGreaterThan, Pet.Dog)));
    CollectionAssert.AreEqual(new[] { 1, 4 }, Ids(new GridFilterDescriptor("Manager", GridFilterOperator.IsEqualTo, true)));
  }

  [TestMethod]
  public void NestedFields_AreNullSafe() =>
    CollectionAssert.AreEqual(new[] { 1, 4 }, Ids(new GridFilterDescriptor("Address.City", GridFilterOperator.IsEqualTo, "LEEDS")));

  [TestMethod]
  public void IsContainedIn_MatchesAnyOfTheValues() =>
    CollectionAssert.AreEqual(new[] { 1, 3, 5 }, Ids(new GridFilterDescriptor("Pet", GridFilterOperator.IsContainedIn, new[] { "Cat", "Fish" })));

  [TestMethod]
  public void Composites_CombineWithTheirLogic_AndWithEachOther() {
    CollectionAssert.AreEqual(new[] { 1, 3, 5 }, Ids(GridFilterLogic.Or,
      new GridFilterDescriptor("Pet", GridFilterOperator.IsEqualTo, Pet.Cat),
      new GridFilterDescriptor("Pet", GridFilterOperator.IsEqualTo, Pet.Fish)));

    int[] ids = [.. People.AsQueryable().ApplyFilters([
      new(GridFilterLogic.Or, [new("Pet", GridFilterOperator.IsEqualTo, Pet.Cat), new("Pet", GridFilterOperator.IsEqualTo, Pet.Dog)]),
      new(GridFilterLogic.And, [new("Manager", GridFilterOperator.IsEqualTo, true)])
    ]).Select(p => p.Id)];
    CollectionAssert.AreEqual(new[] { 1, 4 }, ids);
  }

  [TestMethod]
  public void ToGridResult_AppliesFiltersAndSearchBeforePaging() {
    GridResult<Person> result = People.ToGridResult(new GridRequest {
      PageSize = 1,
      Filters = [new(GridFilterLogic.And, [new("Salary", GridFilterOperator.IsGreaterThan, 100)])],
      Search = new(GridFilterLogic.Or, [new("Name", GridFilterOperator.Contains, "o"), new("Address.City", GridFilterOperator.Contains, "o")])
    });

    Assert.AreEqual(2, result.Total);
    Assert.AreEqual(2, result.Data[0].Id);
  }

  [TestMethod]
  public void Request_SurvivesAJsonRoundTrip() {
    GridRequest request = new() {
      Filters = [
        new(GridFilterLogic.And, [new("Born", GridFilterOperator.IsEqualTo, new DateTime(1990, 6, 15))]),
        new(GridFilterLogic.Or, [new("Pet", GridFilterOperator.IsContainedIn, new[] { Pet.Dog })]),
        new(GridFilterLogic.And, [new("Salary", GridFilterOperator.IsLessThan, 300.5m)])
      ]
    };

    GridRequest copy = JsonSerializer.Deserialize<GridRequest>(JsonSerializer.Serialize(request))!;

    CollectionAssert.AreEqual(new[] { 2 }, People.ToGridResult(copy).Data.Select(p => p.Id).ToArray());
  }

  [TestMethod]
  public void TextOperatorsOnNumbers_Throw() =>
    Assert.ThrowsException<ArgumentException>(() => Ids(new GridFilterDescriptor("Salary", GridFilterOperator.Contains, 1)));
}
