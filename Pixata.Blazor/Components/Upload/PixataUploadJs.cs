using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components;

/// <summary>
/// Loads wwwroot/pixata-upload.js, the browser side of <see cref="PixataFileSelect"/> and <see cref="PixataUpload"/>, once per
/// <see cref="IJSRuntime"/>
/// </summary>
internal static class PixataUploadJs {
  public const string ModulePath = "./_content/Pixata.Blazor/pixata-upload.js";

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
