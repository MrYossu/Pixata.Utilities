using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components;

/// <summary>
/// The base of <see cref="PixataDialog"/> and <see cref="PixataWindow"/>, which float above the page in a layer of their own.
/// While one is visible, its markup is moved to the end of the page's body, so that it isn't clipped or covered by the
/// container it was declared in (it stays where it was declared as far as Blazor is concerned, so cascading values such as
/// an <c>EditForm</c>'s context still reach its content). Every dialog and window joins one stack: the one most recently
/// opened or used is in front, modal ones keep the focus inside them, Esc closes the one that has the focus, and each one
/// gives the focus back to whatever had it before it opened when it closes
/// </summary>
/// <remarks>Not intended to be derived from outside this package</remarks>
public abstract class PixataLayerBase : ComponentBase, IHandleEvent, IAsyncDisposable {
  private readonly string _generatedId = $"pixata-blazor-{Guid.NewGuid():N}";
  private DotNetObjectReference<PixataLayerBase>? _dotNetReference;
  private PixataDialogTexts? _texts;
  private int _layerId;
  private string? _layerWrapperId;
  private PixataLayerOptions? _layerOptions;
  private bool _opening;
  private bool _disposed;

  private protected PixataLayerBase() {
  }

  [Inject]
  private IJSRuntime Js { get; set; } = null!;

  [Inject]
  private IServiceProvider Services { get; set; } = null!;

  /// <summary>
  /// Whether the component is shown. Use <c>@bind-Visible</c>, so that it is updated when the user closes it
  /// </summary>
  [Parameter]
  public bool Visible { get; set; }

  /// <summary>
  /// Raised with <c>false</c> when the user closes the component. If you handle this yourself (rather than using
  /// <c>@bind-Visible</c>), the component stays open until you set <see cref="Visible"/> to false, so you can stop it closing,
  /// eg while there are unsaved changes
  /// </summary>
  [Parameter]
  public EventCallback<bool> VisibleChanged { get; set; }

  /// <summary>
  /// The id of the component's element. One is generated if you don't set it
  /// </summary>
  [Parameter]
  public string? Id { get; set; }

  /// <summary>
  /// Extra CSS classes for the component's element
  /// </summary>
  [Parameter]
  public string? Class { get; set; }

  /// <summary>
  /// Any other attributes (eg <c>data-*</c>, or an <c>aria-label</c> when there's no title) are rendered on the component's element
  /// </summary>
  [Parameter(CaptureUnmatchedValues = true)]
  public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

  /// <summary>
  /// The placeholder the component renders where it was declared. The layer is moved out of it while visible
  /// </summary>
  protected ElementReference AnchorElement { get; set; }

  /// <summary>
  /// The element that holds the overlay (if any) and the layer, and is moved to the end of the body
  /// </summary>
  protected ElementReference WrapperElement { get; set; }

  /// <summary>
  /// The dialog or window element itself
  /// </summary>
  protected ElementReference LayerElement { get; set; }

  /// <summary>
  /// The id of the component's element
  /// </summary>
  protected string ElementId =>
    string.IsNullOrWhiteSpace(Id) ? _generatedId : Id;

  /// <summary>
  /// The text for the titlebar buttons, from the <see cref="PixataDialogTexts"/> registered with the app, or the English
  /// defaults if there isn't one
  /// </summary>
  internal PixataDialogTexts Texts =>
    _texts ??= Services.GetService<PixataDialogTexts>() ?? new();

  private protected abstract PixataLayerOptions LayerOptions { get; }

  protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (_opening) {
      return;
    }
    if (_layerId != 0 && (!Visible || WrapperElement.Id != _layerWrapperId)) {
      await CloseLayer();
    }
    if (Visible && _layerId == 0) {
      await OpenLayer();
    } else if (Visible && LayerOptions != _layerOptions) {
      _layerOptions = LayerOptions;
      await PixataLayerJs.InvokeVoid(Js, "updateLayer", _layerId, _layerOptions);
    }
  }

  private async Task OpenLayer() {
    _opening = true;
    PixataLayerOptions options = LayerOptions;
    string? wrapperId = WrapperElement.Id;
    int id;
    try {
      _dotNetReference ??= DotNetObjectReference.Create(this);
      id = await PixataLayerJs.Invoke(Js, "openLayer", 0, AnchorElement, WrapperElement, LayerElement, _dotNetReference, options);
    }
    finally {
      _opening = false;
    }
    if (id == 0) {
      return;
    }
    if (_disposed || !Visible || WrapperElement.Id != wrapperId) {
      // Closed (or re-rendered from scratch) while the script was opening it
      await PixataLayerJs.InvokeVoid(Js, "closeLayer", id);
      if (!_disposed && Visible) {
        StateHasChanged();
      }
      return;
    }
    _layerId = id;
    _layerWrapperId = wrapperId;
    _layerOptions = options;
  }

  private async Task CloseLayer() {
    int id = _layerId;
    _layerId = 0;
    _layerWrapperId = null;
    _layerOptions = null;
    await PixataLayerJs.InvokeVoid(Js, "closeLayer", id);
  }

  /// <summary>
  /// Closes the component as if the user had: raises <see cref="VisibleChanged"/> if it's handled, and leaves it to the
  /// handler to set <see cref="Visible"/>, or hides the component itself if it isn't
  /// </summary>
  protected async Task Close() {
    if (!Visible) {
      return;
    }
    if (VisibleChanged.HasDelegate) {
      await VisibleChanged.InvokeAsync(false);
    } else {
      Visible = false;
      StateHasChanged();
    }
  }

  /// <summary>
  /// Called by the script when Esc is pressed while a modal layer is open but nothing has the focus
  /// </summary>
  [JSInvokable]
  public Task OnEscapeKey() =>
    InvokeAsync(HandleEscape);

  /// <summary>
  /// Closes the component if it closes on Esc
  /// </summary>
  protected abstract Task HandleEscape();

  /// <summary>
  /// Turns a script measurement into a CSS length, eg <c>120px</c>
  /// </summary>
  protected static string Pixels(double value) =>
    $"{Math.Round(value).ToString(System.Globalization.CultureInfo.InvariantCulture)}px";

  // Keystrokes and clicks inside the layer are delivered here, so the component doesn't re-render (and re-render its content)
  // after every one. Handlers that change what the component shows call StateHasChanged themselves
  Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) =>
    callback.InvokeAsync(arg);

  public async ValueTask DisposeAsync() {
    _disposed = true;
    if (_layerId != 0) {
      await CloseLayer();
    }
    _dotNetReference?.Dispose();
    GC.SuppressFinalize(this);
  }
}
