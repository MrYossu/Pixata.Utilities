using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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
    /// Same a the ForEach method on List<T>, but works on IEnumerable<T></T>
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