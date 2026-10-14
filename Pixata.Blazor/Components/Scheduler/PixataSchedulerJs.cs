using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components;

/// <summary>
/// Loads the scheduler's JavaScript module (wwwroot/pixata-scheduler.js) once per <see cref="IJSRuntime"/>, which handles
/// dragging and resizing appointments, and scrolling the views
/// </summary>
internal static class PixataSchedulerJs {
  public const string ModulePath = "./_content/Pixata.Blazor/pixata-scheduler.js";

  private static readonly ConditionalWeakTable<IJSRuntime, Task<IJSObjectReference>> Modules = new();

  private static async Task<IJSObjectReference> Module(IJSRuntime js) {
    Task<IJSObjectReference> module = Modules.GetValue(js, runtime => runtime.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask());
    try {
      return await module;
    }
    catch {
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
    catch (InvalidOperationException) {
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
    catch (InvalidOperationException) {
    }
    return fallback;
  }
}
