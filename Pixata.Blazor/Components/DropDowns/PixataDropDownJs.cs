using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components;

/// <summary>
/// Loads the drop-downs' JavaScript module (wwwroot/pixata-dropdowns.js) once per <see cref="IJSRuntime"/>, and calls it,
/// ignoring the errors you get when the circuit has gone or JavaScript isn't available yet (prerendering)
/// </summary>
internal static class PixataDropDownJs {
  public static readonly string ModulePath = PixataBlazorScript.Versioned("pixata-dropdowns.js");

  private static readonly ConditionalWeakTable<IJSRuntime, Task<IJSObjectReference>> Modules = new();

  private static async Task<IJSObjectReference> Module(IJSRuntime js) {
    Task<IJSObjectReference> module = Modules.GetValue(js, runtime => runtime.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask());
    try {
      return await module;
    }
    catch {
      // Don't cache a failed import, so the next call tries again
      Modules.Remove(js);
      throw;
    }
  }

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
    catch (ObjectDisposedException) {
    }
    catch (InvalidOperationException) {
      // JavaScript interop calls cannot be issued while prerendering
    }
  }
}
