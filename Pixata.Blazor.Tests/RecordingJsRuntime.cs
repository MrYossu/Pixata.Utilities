using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Tests;

/// <summary>
/// A JavaScript runtime whose <c>import</c> returns a new <see cref="RecordingJsModule"/> straight away, for tests that need to
/// see what a component asks its module to do
/// </summary>
public class RecordingJsRuntime : IJSRuntime {
  /// <summary>
  /// Every module imported, in order
  /// </summary>
  public List<RecordingJsModule> Modules { get; } = [];

  /// <summary>
  /// The calls made to all the modules, in order
  /// </summary>
  public List<(string Identifier, object?[] Args)> Calls { get; } = [];

  /// <summary>
  /// The first module imported
  /// </summary>
  public RecordingJsModule Module =>
    Modules[0];

  public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) {
    if (identifier != "import") {
      return ValueTask.FromResult(default(TValue)!);
    }
    RecordingJsModule module = new(Calls);
    Modules.Add(module);
    return ValueTask.FromResult((TValue)(object)module);
  }

  public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
    InvokeAsync<TValue>(identifier, args);
}

/// <summary>
/// Records each call and its arguments. Like a real module, it throws <see cref="ObjectDisposedException"/> once it has been
/// disposed, which a component does when it's removed from the page
/// </summary>
public class RecordingJsModule(List<(string Identifier, object?[] Args)>? allCalls = null) : IJSObjectReference {
  public List<(string Identifier, object?[] Args)> Calls { get; } = [];
  public bool Disposed { get; private set; }

  public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) {
    ObjectDisposedException.ThrowIf(Disposed, this);
    Calls.Add((identifier, args ?? []));
    allCalls?.Add((identifier, args ?? []));
    return ValueTask.FromResult(default(TValue)!);
  }

  public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
    InvokeAsync<TValue>(identifier, args);

  public ValueTask DisposeAsync() {
    Disposed = true;
    return ValueTask.CompletedTask;
  }
}
