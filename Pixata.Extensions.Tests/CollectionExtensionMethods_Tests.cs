using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pixata.Extensions.Tests {
  [TestClass]
  public class CollectionExtensionMethods_Tests {
    #region ToObservableCollectionAsync

    [TestMethod]
    public async Task CollectionExtensionMethods_ToObservableCollectionAsync_AsyncEnumerable() {
      ObservableCollection<int> result = await AsyncRange(1, 5).ToObservableCollectionAsync();
      CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, result);
    }

    /// <summary>
    /// An IQueryable whose provider supports async enumeration (as EF Core's does) should be streamed asynchronously
    /// </summary>
    [TestMethod]
    public async Task CollectionExtensionMethods_ToObservableCollectionAsync_AsyncQueryable_EnumeratesAsynchronously() {
      AsyncQueryable<string> queryable = new(new[] { "Jim", "Spriggs" });
      ObservableCollection<string> result = await ((IQueryable<string>)queryable).ToObservableCollectionAsync();
      CollectionAssert.AreEqual(new[] { "Jim", "Spriggs" }, result);
      Assert.AreEqual(1, queryable.AsyncEnumerations);
      Assert.AreEqual(0, queryable.SyncEnumerations);
    }

    /// <summary>
    /// An IQueryable whose provider doesn't support async enumeration should fall back to enumerating synchronously
    /// </summary>
    [TestMethod]
    public async Task CollectionExtensionMethods_ToObservableCollectionAsync_PlainQueryable_FallsBackToSync() {
      ObservableCollection<int> result = await new[] { 1, 2, 3 }.AsQueryable().ToObservableCollectionAsync();
      CollectionAssert.AreEqual(new[] { 1, 2, 3 }, result);
    }

    [TestMethod]
    public async Task CollectionExtensionMethods_ToObservableCollectionAsync_PassesCancellationTokenToSource() {
      using CancellationTokenSource cts = new();
      await cts.CancelAsync();
      await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => AsyncRange(1, 5).ToObservableCollectionAsync(cts.Token));
    }

    /// <summary>
    /// Both overloads should throw when called, rather than when the returned task is awaited
    /// </summary>
    [TestMethod]
    public void CollectionExtensionMethods_ToObservableCollectionAsync_Null_ThrowsSynchronously() {
      Assert.ThrowsException<ArgumentNullException>(() => ((IQueryable<int>)null!).ToObservableCollectionAsync());
      Assert.ThrowsException<ArgumentNullException>(() => ((IAsyncEnumerable<int>)null!).ToObservableCollectionAsync());
    }

    private static async IAsyncEnumerable<int> AsyncRange(int start, int count, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
      for (int i = 0; i < count; i++) {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        yield return start + i;
      }
    }

    /// <summary>
    /// Stands in for EF Core's EntityQueryable&lt;T&gt;, which is both an IQueryable&lt;T&gt; and an IAsyncEnumerable&lt;T&gt;
    /// </summary>
    private class AsyncQueryable<T> : IQueryable<T>, IAsyncEnumerable<T> {
      private readonly IQueryable<T> _inner;

      public AsyncQueryable(IEnumerable<T> source) =>
        _inner = source.AsQueryable();

      public int AsyncEnumerations { get; private set; }
      public int SyncEnumerations { get; private set; }

      public Type ElementType => _inner.ElementType;
      public Expression Expression => _inner.Expression;
      public IQueryProvider Provider => _inner.Provider;

      public IEnumerator<T> GetEnumerator() {
        SyncEnumerations++;
        return _inner.GetEnumerator();
      }

      IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

      public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
        AsyncEnumerations++;
        foreach (T item in _inner) {
          cancellationToken.ThrowIfCancellationRequested();
          await Task.Yield();
          yield return item;
        }
      }
    }

    #endregion
  }
}
