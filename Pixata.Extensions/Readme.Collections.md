# Collection extension methods

>Part of [Pixata.Extensions](Readme.md)

Extension methods for working with collections, found in `CollectionExtensionMethods`.

## Observable collections

`ToObservableCollection<T>()` - Converts any collection that implements `IEnumerable<T>` into an `ObservableCollection<T>`. Provides a neater syntax than passing the collection to the `ObservableCollection`'s constructor.

`ToObservableCollectionAsync<T>()` - An async version of `ToObservableCollection()`, with overloads for `IQueryable<T>` and `IAsyncEnumerable<T>`. Queries whose provider supports async enumeration (eg EF Core) are streamed asynchronously, others are enumerated synchronously. This package does not reference EF Core, so it can be used client-side (eg Blazor WASM) without dragging EF Core in.

`RemoveWhere<T>()` - Removes items from an `ObservableCollection` based on a predicate. Mimics the `List<T>.RemoveAll()` method that doesn't exist for `ObservableCollection`s.

## General collections

`ForEach<T>()` - Allows you to use `ForEach` on any collection that implements `IEnumerable<T>`, as opposed to the similarly named method built in to the .NET framework that requires you to cast the collection to a `List<T>` first.

`Flatten<T>()` - Enables you to flatten a hierarchical collection.

## Synchronising entities with DTOs

`Synchronise()` synchronises a collection of entities with a collection of DTOs by updating existing entities, removing entities that are no longer present in the DTO collection, and adding new entities.

This is useful when converting a DTO containing a collection navigation property into an EF Core entity, avoiding the need to write repetitive code to handle collection additions, updates, and removals.

Example usage: suppose an entity has a collection of `Note` entities and you want to update the notes from a DTO...

```csharp
myEntity.Notes.Synchronise(dto.Notes,
  note => note.Id,         // How to get the key of the entity
  note => note.Id,         // How to get the key of the DTO (probably the same as above)
  note => note.Id <= 0,    // How to determine if the DTO is new
  dto => new Note {        // How to create a new entity from the DTO
    Text = dto.Text
  },
  (note, dto) => {         // How to update an existing entity from the DTO
    note.Text = dto.Text;
  });
```
