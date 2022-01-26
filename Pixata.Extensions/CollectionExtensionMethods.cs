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
  }
}