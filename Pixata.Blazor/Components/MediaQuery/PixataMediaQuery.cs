using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components;

/// <summary>
/// Tells you whether a CSS media query matches, such as <c>(max-width: 720px)</c>, once the page is interactive and again
/// whenever that changes, so a page can change what it shows below a breakpoint. Renders nothing
/// </summary>
public sealed class PixataMediaQuery : ComponentBase, IAsyncDisposable {
  [Inject]
  private IJSRuntime Js { get; set; } = null!;

  /// <summary>
  /// The CSS media query, such as <c>(max-width: 720px)</c> or <c>(prefers-color-scheme: dark)</c>. Changing it starts
  /// watching the new query, and raises <see cref="OnChange"/> with whether that matches
  /// </summary>
  [Parameter, EditorRequired]
  public string Media { get; set; } = "";

  /// <summary>
  /// Raised with whether the query matches, once the page is interactive, and again whenever that changes. It isn't raised
  /// while the page is being prerendered, as there's no browser to ask
  /// </summary>
  [Parameter]
  public EventCallback<bool> OnChange { get; set; }

  private DotNetObjectReference<PixataMediaQuery>? _self;
  // The query being watched, or being started, and the browser's id for it once it has started
  private string? _media;
  private int _id;
  private bool _disposed;

  protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (!_disposed && Media != _media) {
      await Watch(Media);
    }
  }

  private async Task Watch(string media) {
    _media = media;
    await StopWatching();
    if (string.IsNullOrWhiteSpace(media)) {
      return;
    }
    _self ??= DotNetObjectReference.Create(this);
    // The id and whether it matches (1) or not (0), or null if JavaScript isn't available, such as when the circuit has gone
    int[]? started = await PixataJs.Invoke<int[]?>(Js, "initMediaQuery", null, media, _self);
    if (started is not [int id, int matches]) {
      if (_media == media) {
        // Try again the next time the component renders
        _media = null;
      }
      return;
    }
    if (_disposed || _media != media) {
      // Disposed, or given another query, while the browser was starting this one
      await PixataJs.InvokeVoid(Js, "disposeMediaQuery", id);
      return;
    }
    _id = id;
    await OnChange.InvokeAsync(matches == 1);
  }

  private async Task StopWatching() {
    if (_id != 0) {
      int id = _id;
      _id = 0;
      await PixataJs.InvokeVoid(Js, "disposeMediaQuery", id);
    }
  }

  /// <summary>
  /// Called by the browser when the query starts or stops matching
  /// </summary>
  [JSInvokable]
  public Task MediaChanged(int id, bool matches) =>
    // A change to a query this has stopped watching, that was on its way when it stopped, is ignored
    id == _id && !_disposed ? InvokeAsync(() => OnChange.InvokeAsync(matches)) : Task.CompletedTask;

  public async ValueTask DisposeAsync() {
    _disposed = true;
    await StopWatching();
    _self?.Dispose();
  }
}
