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
