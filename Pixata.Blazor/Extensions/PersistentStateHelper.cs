using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace Pixata.Blazor.Extensions;

/// <summary>
/// Keeps the data loaded while a page is prerendered, so the interactive render can use it rather than loading it again. Each
/// instance remembers one value, so each component needs its own: create one with <c>new</c> and dispose of it with the component
/// </summary>
public class PersistentStateHelper<T> : IDisposable {
  private readonly PersistingComponentStateSubscription _subscription;
  private readonly PersistentComponentState _applicationState;
  private readonly NavigationManager _navManager;
  private readonly ILogger? _logger;
  private string _key = "";
  private bool _claimed;
  private T? _data;
  private Func<T, bool>? _persistWhen;

  // The keys in use, and by how many helpers, for each PersistentComponentState (so for each prerender, and each circuit). Two
  // helpers with the same key can't both be persisted, and whichever one was could be read back by the other, which would then
  // show the wrong data, so neither is persisted, and both load their data again once the page is interactive
  private static readonly ConditionalWeakTable<PersistentComponentState, Dictionary<string, int>> KeysInUse = new();

  // As PersistAsJson serialises the data, so the size measured is the size persisted
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  /// <summary>
  /// The most data, in bytes of JSON, to persist. Data bigger than this isn't persisted (and a warning is logged, if there's a
  /// logger), so it is loaded again once the page is interactive. In Blazor Server, the persisted data goes into the page, and
  /// the browser sends it back to the server in the message that starts the circuit, so data bigger than SignalR's
  /// <c>MaximumReceiveMessageSize</c> (32KB by default) stops the page ever becoming interactive. Null, the default, for no limit
  /// </summary>
  public int? MaxPersistedBytes { get; set; }

  public PersistentStateHelper(PersistentComponentState applicationState, NavigationManager navManager) : this(applicationState, navManager, null) {
  }

  /// <param name="applicationState">The page's persistent state</param>
  /// <param name="navManager">Used for the default key, which is the page's address</param>
  /// <param name="logger">Where to log a warning when data isn't persisted because it is bigger than <see cref="MaxPersistedBytes"/></param>
  public PersistentStateHelper(PersistentComponentState applicationState, NavigationManager navManager, ILogger? logger) {
    _applicationState = applicationState;
    // InteractiveAuto, as Blazor only runs a callback for the render modes it was registered for. With InteractiveWebAssembly
    // (as it was before v5.26.0), an app that prerenders InteractiveServer never persisted the data, so every page loaded it
    // twice. Auto is accepted both when the page's state goes to the server and when it goes to WebAssembly
    _subscription = _applicationState.RegisterOnPersisting(Persist, RenderMode.InteractiveAuto);
    _navManager = navManager;
    _logger = logger;
  }

  /// <summary>
  /// Get data. Uses persistent storage to ensure that data is only loaded once
  /// </summary>
  /// <param name="getData">A Func&lt;Task&lt;T&gt;&gt; that specifies how to get the data. This should use a method in an interface that is implemented in both the server and WASM assemblies</param>
  /// <param name="key">The name of the data item. If omitted, the current URI is used (path only, no https://domain). This makes the usage slightly cleaner for components that use one data item per URI</param>
  /// <param name="persistWhen">An optional predicate that says whether the data is worth persisting. If omitted, the data is persisted as long as it isn't null. Use this to avoid handing a transient prerender failure to the client as if it were the answer</param>
  /// <returns>The data returned by the getData parameter</returns>
  public async Task<T> Get(Func<Task<T>> getData, string key = "", Func<T, bool>? persistWhen = null) {
    Release();
    _key = string.IsNullOrWhiteSpace(key)
      ? UriPath(_navManager.Uri)
      : key;
    Claim();
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
    return _data!;
  }

  private Task Persist() {
    if (_data is not null && (_persistWhen is null || _persistWhen(_data)) && !IsShared() && !TooBig(_data)) {
      try {
        _applicationState.PersistAsJson(_key, _data);
      } catch (ArgumentException) {
        // Something other than a PersistentStateHelper has already persisted data under this key
      }
    }
    return Task.CompletedTask;
  }

  private bool TooBig(T data) {
    if (MaxPersistedBytes is not int max) {
      return false;
    }
    int size = JsonSerializer.SerializeToUtf8Bytes(data, JsonOptions).Length;
    if (size <= max) {
      return false;
    }
    _logger?.LogWarning("The data for {Key} wasn't persisted, as it is {Size:N0} bytes, which is more than MaxPersistedBytes ({Max:N0}). It will be loaded again once the page is interactive", _key, size, max);
    return true;
  }

  private Dictionary<string, int> Keys =>
    KeysInUse.GetValue(_applicationState, _ => []);

  private void Claim() {
    Dictionary<string, int> keys = Keys;
    lock (keys) {
      keys[_key] = keys.GetValueOrDefault(_key) + 1;
    }
    _claimed = true;
  }

  private void Release() {
    if (!_claimed) {
      return;
    }
    Dictionary<string, int> keys = Keys;
    lock (keys) {
      if (keys.GetValueOrDefault(_key) <= 1) {
        keys.Remove(_key);
      } else {
        keys[_key]--;
      }
    }
    _claimed = false;
  }

  private bool IsShared() {
    Dictionary<string, int> keys = Keys;
    lock (keys) {
      return keys.GetValueOrDefault(_key) > 1;
    }
  }

  public void Dispose() {
    _subscription.Dispose();
    Release();
  }

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