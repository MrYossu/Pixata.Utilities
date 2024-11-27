using System;
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
  /// <returns>The data returned by the getData parameter</returns>
  public async Task<T> Get(Func<Task<T>> getData, string key = "") {
    _key = string.IsNullOrWhiteSpace(key)
      ? UriPath(_navManager.Uri)
      : key;
    bool foundInState = _applicationState.TryTakeFromJson<T>(_key, out var dataFromState);
    _data = foundInState
      ? dataFromState
      : await getData();
    return _data;
  }

  private Task Persist() {
    _applicationState.PersistAsJson(_key, _data);
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