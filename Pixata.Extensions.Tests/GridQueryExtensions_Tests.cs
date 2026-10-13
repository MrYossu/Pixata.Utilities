using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Extensions.Grid;

namespace Pixata.Extensions.Tests;

[TestClass]
public class GridQueryExtensions_Tests {
  private record Address(string City);

  private record Person(int Id, string Name, Address? Address = null, int? Age = null);

  private static readonly List<Person> People = [.. Enumerable.Range(1, 25).Select(n => new Person(n, $"Person {n}"))];

  [TestMethod]
  public void ToGridResult_NoPageSize_ReturnsEverything() {
    GridResult<Person> result = People.ToGridResult(new GridRequest());

    Assert.AreEqual(25, result.Data.Count);
    Assert.AreEqual(25, result.Total);
  }

  [TestMethod]
  public void ToGridResult_PageSize_ReturnsThatPageAndTheFullTotal() {
    GridResult<Person> result = People.AsQueryable().ToGridResult(new GridRequest { Page = 3, PageSize = 10 });

    CollectionAssert.AreEqual(new[] { 21, 22, 23, 24, 25 }, result.Data.Select(p => p.Id).ToArray());
    Assert.AreEqual(25, result.Total);
  }

  [TestMethod]
  public void ToGridResult_PageBelowOne_IsTreatedAsTheFirstPage() =>
    Assert.AreEqual(1, People.ToGridResult(new GridRequest { Page = 0, PageSize = 10 }).Data[0].Id);

  [TestMethod]
  public void FieldAccessor_Getter_ReadsSimpleAndNestedFields() {
    Person person = new(7, "Fred", new("Leeds"), 42);

    Assert.AreEqual("Fred", FieldAccessor.Getter<Person>("Name")(person));
    Assert.AreEqual("Leeds", FieldAccessor.Getter<Person>("Address.City")(person));
    Assert.AreEqual(42, FieldAccessor.Getter<Person>("Age")(person));
  }

  [TestMethod]
  public void FieldAccessor_Getter_ReturnsNullWhenAnythingOnThePathIsNull() {
    Assert.IsNull(FieldAccessor.Getter<Person>("Address.City")(new(1, "Fred")));
    Assert.IsNull(FieldAccessor.Getter<Person>("Age")(new(1, "Fred")));
    Assert.IsNull(FieldAccessor.Getter<Person>("Name")(null!));
  }

  [TestMethod]
  public void FieldAccessor_Getter_IsCached() =>
    Assert.AreSame(FieldAccessor.Getter<Person>("Name"), FieldAccessor.Getter<Person>("Name"));

  [TestMethod]
  public void FieldAccessor_BadPath_ThrowsWithTheWholePathInTheMessage() {
    ArgumentException ex = Assert.ThrowsException<ArgumentException>(() => FieldAccessor.Getter<Person>("Address.Town"));

    StringAssert.Contains(ex.Message, "'Address.Town'");
    Assert.ThrowsException<ArgumentException>(() => FieldAccessor.Getter<Person>("name"));
    Assert.ThrowsException<ArgumentException>(() => FieldAccessor.Getter<Person>(""));
  }

  [TestMethod]
  public void FieldAccessor_FieldType_ReturnsTheTypeAtTheEndOfThePath() {
    Assert.AreEqual(typeof(string), FieldAccessor.FieldType(typeof(Person), "Address.City"));
    Assert.AreEqual(typeof(int?), FieldAccessor.FieldType(typeof(Person), "Age"));
  }

  [TestMethod]
  public void ToGridResult_Sorts_AreAppliedInPriorityOrderBeforePaging() {
    List<Person> people = [new(1, "B", Age: 30), new(2, "A", Age: 30), new(3, "C", Age: 20), new(4, "D", Age: 40)];

    GridResult<Person> result = people.ToGridResult(new GridRequest {
      PageSize = 3,
      Sorts = [new("Age", ListSortDirection.Descending), new("Name")]
    });

    CollectionAssert.AreEqual(new[] { 4, 2, 1 }, result.Data.Select(p => p.Id).ToArray());
    Assert.AreEqual(4, result.Total);
  }

  [TestMethod]
  public void ApplySorts_NestedFieldWithNullParent_SortsNullsFirstWithoutThrowing() {
    List<Person> people = [new(1, "A", new("York")), new(2, "B"), new(3, "C", new("Bath"))];

    int[] ids = [.. people.AsQueryable().ApplySorts([new("Address.City")]).Select(p => p.Id)];

    CollectionAssert.AreEqual(new[] { 2, 3, 1 }, ids);
  }

  [TestMethod]
  public void FieldAccessor_Lambda_MakesValueTypesNullableOnlyWhenThePathCanBeNull() {
    Assert.AreEqual(typeof(int), FieldAccessor.Lambda(typeof(Person), "Id").ReturnType);
    Assert.AreEqual(typeof(int?), FieldAccessor.Lambda(typeof(Person), "Age").ReturnType);
    Assert.AreEqual(typeof(int?), FieldAccessor.Lambda(typeof(Wrapper), "Person.Id").ReturnType);
    Assert.AreEqual("item => item.Id", FieldAccessor.Lambda(typeof(Person), "Id").ToString());
  }

  private record Wrapper(Person? Person);
}
