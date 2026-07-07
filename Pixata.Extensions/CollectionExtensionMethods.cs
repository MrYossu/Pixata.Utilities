using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Pixata.Extensions {
  public static class CollectionExtensionMethods {
    /// <summary>
    /// Converts an IEnumerable<T> to an ObservableCollection<T>
    /// </summary>
    /// <typeparam name="T">Generic type</typeparam>
    /// <param name="collection">The collection</param>
    /// <returns>An ObservableCollection<T></returns>
    public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> collection) =>
      collection != null ? new ObservableCollection<T>(collection) : new ObservableCollection<T>();

    /// <summary>
    ///     Asynchronously converts an IEnumerable<T> to an ObservableCollection<T>
    /// </summary>
    /// <remarks>
    ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
    ///     that any asynchronous operations have completed before calling another method on this context.
    /// </remarks>
    /// <typeparam name="T">
    ///     The type of the elements of <paramref name="collection" />.
    /// </typeparam>
    /// <param name="collection">
    ///     An <see cref="IQueryable{T}" /> from which the ObservableCollection is created
    /// </param>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains a <see cref="ObservableCollection{T}" /> that contains elements from the input sequence.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="collection" /> is <see langword="null" />.
    /// </exception>
    public static async Task<ObservableCollection<T>> ToObservableCollectionAsync<T>([NotNull] this IQueryable<T> collection, CancellationToken cancellationToken = default) {
    List<T> list = new();
      await foreach (var element in collection.AsAsyncEnumerable().WithCancellation(cancellationToken)) {
        list.Add(element);
      }
      return list.ToObservableCollection();
    }

    /// <summary>
    /// Removes items from an ObservableCollection based on a predicate. Mimics the List&lt;T&gt;.RemoveAll method that doesn't exist for ObservableCollections
    /// </summary>
    /// <typeparam name="T">Generic type parameter</typeparam>
    /// <param name="coll">The ObservableCollection</param>
    /// <param name="p">A predicate that specifies which items to remove</param>
    /// <returns></returns>
    public static ObservableCollection<T> RemoveWhere<T>(this ObservableCollection<T> coll, Predicate<T> p) =>
      coll.Where(t1 => !p(t1)).Select(t => t).ToObservableCollection();

    /// <summary>
    /// Same as the List<T>.ForEach method, but works on IEnumerable<T>
    /// </summary>
    /// <typeparam name="T">Generic type</typeparam>
    /// <param name="items">The collection</param>
    /// <param name="action">The action you want to perform on each entry in the collection</param>
    public static void ForEach<T>(this IEnumerable<T> items, Action<T> action) {
      if (items == null) {
        return;
      }

      foreach (T item in items) {
        action(item);
      }
    }

    /// <summary>
    /// Flattens a self-referencing hierarchy of objects into a flat collection
    /// </summary>
    /// <typeparam name="T">Generic type</typeparam>
    /// <param name="collection">The collection</param>
    /// <param name="f">A Func that gets the child entities from the parent, eg e => e.children</param>
    /// <returns></returns>
    public static IEnumerable<T> Flatten<T>(this IEnumerable<T> collection, Func<T, IEnumerable<T>> f) {
      List<T> list = collection.ToList();
      return list.SelectMany(c => f(c).Flatten(f)).Concat(list);
    }

    /// <summary>
    /// <para>
    /// Synchronises a collection of entities with a collection of DTOs, updating existing entities, removing missing entities, and adding new entities.
    /// </para>
    /// <para>
    /// Useful when converting a DTO with a collection navigation property into an EF Core entity, and don't want to have to write the boilerplate code to handle adding, updating and removing.
    /// </para>
    /// <para>
    /// Sample usage. If you had an entity that had a collection of Note entities, and wanted to update the notes from a DTO, you could do...
    /// </para>
    ///<code>
    /// myEntity.Notes.Synchronise(dto.Notes,
    ///   note =&gt; note.Id,         // How to get the key of the entity
    ///   note =&gt; note.Id,         // How to get the key of the DTO (probably the same as above)
    ///   note =&gt; note.Id &lt;= 0,   // How to determine if the DTO is new
    ///   dto =&gt; new Note {        // How to create a new entity from the DTO
    ///     Text = dto.Text 
    ///   }, 
    ///   (note, dto) =&gt; {         // How to update an existing entity from the DTO
    ///     note.Text = dto.Text; 
    ///   });
    ///</code>
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity</typeparam>
    /// <typeparam name="TDto">The type of the DTO</typeparam>
    /// <typeparam name="TKey">The type of the key used to identify entities and DTOs, eg int, string, Guid</typeparam>
    /// <param name="entities">The collection of entities to synchronise</param>
    /// <param name="dtos">The collection of DTOs to synchronise with the entities</param>
    /// <param name="entityKey">A function to extract the key from an entity, eg x =&gt; x.Id</param>
    /// <param name="dtoKey">A function to extract the key from a DTO, eg x =&gt; x.Id</param>
    /// <param name="isNew">A function to determine if a DTO represents a new entity, eg x =&gt; x.Id == 0</param>
    /// <param name="create">A function to create a new entity from a DTO, eg dto =&gt; new Entity { Name = dto.Name }</param>
    /// <param name="update">An action to update an existing entity with a DTO, eg (x, dto) =&gt; { x.Name = dto.Name; }</param>
    public static void Synchronise<TEntity, TDto, TKey>(this ICollection<TEntity> entities, IEnumerable<TDto> dtos, Func<TEntity, TKey> entityKey, Func<TDto, TKey> dtoKey, Func<TDto, bool> isNew, Func<TDto, TEntity> create, Action<TEntity, TDto> update) where TKey : notnull {
      List<TDto> newDtos = dtos
        .Where(isNew)
        .ToList();
      Dictionary<TKey, TDto> existingDtos = dtos
        .Where(dto => !isNew(dto))
        .ToDictionary(dtoKey);
      foreach (TEntity entity in entities.ToList()) {
        TKey key = entityKey(entity);
        if (!existingDtos.TryGetValue(key, out TDto? dto)) {
          entities.Remove(entity);
          continue;
        }
        update(entity, dto);
        existingDtos.Remove(key);
      }
      foreach (TDto dto in newDtos) {
        entities.Add(create(dto));
      }
    }
  }
}