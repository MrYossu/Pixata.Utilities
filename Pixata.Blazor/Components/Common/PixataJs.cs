using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components;

/// <summary>
/// Loads the shared JavaScript module (wwwroot/pixata-blazor.js) once per <see cref="IJSRuntime"/>, so once per circuit in
/// Blazor Server and once per app in WASM. The components call it rather than injecting a service, so they work without
/// anything having to be registered
/// </summary>
internal static class PixataJs {
  public const string ModulePath = "./_content/Pixata.Blazor/pixata-blazor.js";

  private static readonly ConditionalWeakTable<IJSRuntime, Task<IJSObjectReference>> Modules = new();

  public static async Task<IJSObjectReference> Module(IJSRuntime js) {
    Task<IJSObjectReference> module = Modules.GetValue(js, runtime => runtime.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask());
    try {
      return await module;
    }
    catch {
      // Don't cache a failed import (eg one attempted while prerendering), so the next call tries again
      Modules.Remove(js);
      throw;
    }
  }

  /// <summary>
  /// Calls a function in the shared module, ignoring the errors you get when the circuit has gone (the user closed the tab)
  /// or JavaScript isn't available yet (prerendering)
  /// </summary>
  public static async Task InvokeVoid(IJSRuntime js, string identifier, params object?[] args) {
    try {
      IJSObjectReference module = await Module(js);
      await module.InvokeVoidAsync(identifier, args);
    }
    catch (JSDisconnectedException) {
    }
    catch (TaskCanceledException) {
    }
    catch (InvalidOperationException) {
      // JavaScript interop calls cannot be issued while prerendering
    }
  }

  /// <summary>
  /// Calls a function in the shared module and returns its result, or <paramref name="fallback"/> if the circuit has gone or
  /// JavaScript isn't available yet
  /// </summary>
  public static async Task<T> Invoke<T>(IJSRuntime js, string identifier, T fallback, params object?[] args) {
    try {
      IJSObjectReference module = await Module(js);
      return await module.InvokeAsync<T>(identifier, args);
    }
    catch (JSDisconnectedException) {
    }
    catch (TaskCanceledException) {
    }
    catch (InvalidOperationException) {
    }
    return fallback;
  }
}
