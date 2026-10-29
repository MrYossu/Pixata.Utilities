using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components;

/// <summary>
/// Loads wwwroot/pixata-dialogs.js, the script that stacks, focuses, moves and resizes the dialogs and windows, once per
/// <see cref="IJSRuntime"/> (so once per circuit in Blazor Server and once per app in WASM), so that they all share one stack
/// </summary>
internal static class PixataLayerJs {
  public static readonly string ModulePath = PixataBlazorScript.Versioned("pixata-dialogs.js");

  private static readonly ConditionalWeakTable<IJSRuntime, Task<IJSObjectReference>> Modules = new();

  private static async Task<IJSObjectReference> Module(IJSRuntime js) {
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
  /// Calls a function in the module, ignoring the errors you get when the circuit has gone or JavaScript isn't available yet
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
    catch (JSException) {
      // The element passed in has gone from the page, so there's nothing to do
    }
    catch (InvalidOperationException) {
      // JavaScript interop calls cannot be issued while prerendering
    }
  }

  /// <summary>
  /// Calls a function in the module and returns its result, or <paramref name="fallback"/> if the circuit has gone or
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
    catch (JSException) {
      // The element passed in has gone from the page, so there's nothing to do
    }
    catch (InvalidOperationException) {
    }
    return fallback;
  }
}
