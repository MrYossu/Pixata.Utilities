using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Pixata.Extensions;

public static class MemoryCacheExtensions {
  private sealed class LockState {
    public SemaphoreSlim Semaphore { get; } = new(1, 1);
    public int RefCount;
  }

  private static readonly ConcurrentDictionary<string, LockState> Locks = new();

  public static async Task<T> GetOrCreateSafeAsync<T>(this IMemoryCache cache, string key, Func<CancellationToken, Task<T>> factory, TimeSpan? ttl = null, CancellationToken cancellationToken = default) {
    if (cache.TryGetValue(key, out T cached)) {
      return cached;
    }

    LockState state = Locks.GetOrAdd(key, _ => new LockState());
    Interlocked.Increment(ref state.RefCount);

    await state.Semaphore.WaitAsync(cancellationToken);
    try {
      if (cache.TryGetValue(key, out cached)) {
        return cached;
      }

      T value = await factory(cancellationToken);
      MemoryCacheEntryOptions options = new();

      if (ttl.HasValue) {
        options.AbsoluteExpirationRelativeToNow = ttl;
      }

      cache.Set(key, value, options);
      return value;
    }
    finally {
      state.Semaphore.Release();
      if (Interlocked.Decrement(ref state.RefCount) == 0) {
        Locks.TryRemove(new KeyValuePair<string, LockState>(key, state));
        state.Semaphore.Dispose();
      }
    }
  }
}