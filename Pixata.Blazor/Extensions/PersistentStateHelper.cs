using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Caching.Memory;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pixata.Blazor.Extensions;

public class PersistentStateHelper<T> : IDisposable {
  private readonly PersistingComponentStateSubscription _subscription;
  private readonly IMemoryCache _cache;
  private readonly PersistentComponentState _applicationState;
  private readonly NavigationManager _navManager;
  private string _key = "";
  private T? _data;

  public PersistentStateHelper(IMemoryCache cache, PersistentComponentState applicationState, NavigationManager navManager) {
    _cache = cache;
    _applicationState = applicationState;
    _subscription = _applicationState.RegisterOnPersisting(Persist, RenderMode.InteractiveWebAssembly);
    _navManager = navManager;
  }

  /// <summary>
  /// Get data. Uses both persistent storage and a cache to ensure that data is only loaded once, even if the user navigates to another page within the same WASM assembly
  /// </summary>
  /// <param name="getData">A Func&lt;Task&lt;T&gt;&gt; that specifies how to get the data. This should use a method in an interface that is implemented in both the server and WASM assemblies</param>
  /// <param name="key">The name of the data item. If omitted, the current URI is used (path only, no https://domain). This makes the usage slightly cleaner for components that use one data item per URI</param>
  /// <returns>The data returned by the getData parameter</returns>
  public async Task<T> Get(Func<Task<T>> getData, string key = "") {
    _key = string.IsNullOrWhiteSpace(key)
      ? UriPath(_navManager.Uri)
      : key;
    // See if the data is in the state. If so, then it will also be in the cache, so save
    // it locally (so it can be persisted) and return it
    bool foundInState = _applicationState.TryTakeFromJson<T>(_key, out var dataFromState);
    if (foundInState) {
      _data = (dataFromState ?? default)!;
      return _data;
    }

    // See if the data is in the cache. If so, return it
    bool foundInCache = _cache.TryGetValue(_key, out T? dataFromCache);
    if (foundInCache) {
      return (dataFromCache ?? default)!;
    }

    // We don't have the data at all, so get it from the service and save it in the cache.
    // It will automatically be saved in the cache, so no need to do that manually
    _data = await getData();
    _cache.Set(_key, _data);
    return _data;
  }

  private Task Persist() {
    _applicationState.PersistAsJson(_key, _data);
    return Task.CompletedTask;
  }

  /// <summary>
  /// Remove the data from the cache. This will force the data to be reloaded next time it is requested
  /// <param name="key">The name of the entry to be removed. Defaults to the current URI (path only, no https://domain)</param>
  /// </summary>
  public void Remove(string key = "") =>
    _cache.Remove(string.IsNullOrWhiteSpace(key) ? _key : key);

  /// <summary>
  /// Gets the keys of all items stored in the cache. Intended for debugging purposes
  /// </summary>
  /// <returns>A string[] containing the keys</returns>
  public string[] GetKeys() =>
    (((IEnumerable<object>)(typeof(MemoryCache).GetProperty("Keys"))!.GetValue(_cache)!)).Select(o => o.ToString() ?? "").ToArray();

  public void Dispose() =>
    _subscription.Dispose();

  private static string UriPath(string uri) {
    if (!uri.Contains("//")) {
      return uri;
    }
    uri = uri.Substring(uri.IndexOf("//", StringComparison.Ordinal) + 2);
    if (!uri.StartsWith("/")) {
      uri = uri.Contains("/") ? uri.Substring(uri.IndexOf("/", StringComparison.Ordinal)) : "/";
    }
    return uri;
  }
}