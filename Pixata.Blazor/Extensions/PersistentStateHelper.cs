using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Pixata.Blazor.Extensions;

public class PersistentStateHelper<T> : IDisposable {
  private readonly PersistingComponentStateSubscription _subscription;
  private readonly PersistentComponentState _applicationState;
  private readonly NavigationManager _navManager;
  private string _key = "";
  private T? _data;
  private Func<T, bool>? _persistWhen;

  public PersistentStateHelper(PersistentComponentState applicationState, NavigationManager navManager) {
    _applicationState = applicationState;
    _subscription = _applicationState.RegisterOnPersisting(Persist, RenderMode.InteractiveWebAssembly);
    _navManager = navManager;
  }

  /// <summary>
  /// Get data. Uses persistent storage to ensure that data is only loaded once
  /// </summary>
  /// <param name="getData">A Func&lt;Task&lt;T&gt;&gt; that specifies how to get the data. This should use a method in an interface that is implemented in both the server and WASM assemblies</param>
  /// <param name="key">The name of the data item. If omitted, the current URI is used (path only, no https://domain). This makes the usage slightly cleaner for components that use one data item per URI</param>
  /// <param name="persistWhen">An optional predicate that says whether the data is worth persisting. If omitted, the data is persisted as long as it isn't null. Use this to avoid handing a transient prerender failure to the client as if it were the answer</param>
  /// <returns>The data returned by the getData parameter</returns>
  public async Task<T> Get(Func<Task<T>> getData, string key = "", Func<T, bool>? persistWhen = null) {
    _key = string.IsNullOrWhiteSpace(key)
      ? UriPath(_navManager.Uri)
      : key;
    _persistWhen = persistWhen;
    bool foundInState;
    T? dataFromState = default;
    try {
      foundInState = _applicationState.TryTakeFromJson<T>(_key, out dataFromState);
    } catch (JsonException) {
      // TryTakeFromJson throws (rather than returning false) if the persisted value can't be deserialised into T. A
      // persisted value that can't be read is no better than one that was never written, and the fallback already
      // exists, so fetch the data, exactly as a cold load does. Letting this escape instead takes the whole component
      // out through the nearest ErrorBoundary, which is a worse outcome than the double fetch this class avoids
      foundInState = false;
    }
    _data = foundInState
      ? dataFromState
      : await getData();
    return _data;
  }

  private Task Persist() {
    if (_data is not null && (_persistWhen is null || _persistWhen(_data))) {
      _applicationState.PersistAsJson(_key, _data);
    }
    return Task.CompletedTask;
  }

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