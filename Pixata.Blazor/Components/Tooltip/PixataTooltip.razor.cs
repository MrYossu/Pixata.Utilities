using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components;

/// <summary>
/// Which side of its target a <see cref="PixataTooltip"/> is shown on. If there isn't room there, it goes on the opposite
/// side. <see cref="Auto"/> picks the first side with room, starting above
/// </summary>
public enum PixataTooltipPosition {
  Top,
  Bottom,
  Left,
  Right,
  Auto
}

/// <summary>
/// What shows a <see cref="PixataTooltip"/>
/// </summary>
public enum PixataTooltipShowEvent {
  /// <summary>
  /// The pointer being over the target, the target having the keyboard focus, or a tap on a touch screen
  /// </summary>
  Hover,

  /// <summary>
  /// A click or tap on the target, or Enter or Space when it has the focus. Another click, a click elsewhere or Escape hides it
  /// </summary>
  Click
}

/// <summary>
/// What a <see cref="PixataTooltip"/>'s <c>Template</c> is given: the element it's being shown for
/// </summary>
public class PixataTooltipTemplateContext {
  /// <summary>
  /// The target's id, if it has one. For child content without a <c>TargetSelector</c>, the target is the child element the
  /// pointer is over (or that has the focus), so this, <see cref="Title"/> and <see cref="DataAttributes"/> are that element's
  /// </summary>
  public string? TargetId { get; init; }

  /// <summary>
  /// The target's <c>title</c> attribute, which is what the tooltip shows if it has no <c>Template</c> or <c>Text</c>. The
  /// attribute is taken off while the tooltip is shown, so the browser doesn't show its own tooltip as well
  /// </summary>
  public string? Title { get; init; }

  /// <summary>
  /// The target's <c>data-</c> attributes, without the "data-" (so <c>data-invoice-id</c> is "invoice-id"). Put what the
  /// tooltip needs to know about each target in these
  /// </summary>
  public Dictionary<string, string> DataAttributes { get; init; } = [];
}

/// <summary>
/// A tooltip, to explain something when it's hovered, focused or tapped. The target is either the child content, or the
/// elements matched by <see cref="TargetSelector"/>. The tooltip shows <see cref="Template"/>, <see cref="Text"/>, or the
/// target's <c>title</c> attribute
/// </summary>
public partial class PixataTooltip {
  internal static readonly string ModulePath = PixataBlazorScript.Versioned("Components/Tooltip/PixataTooltip.razor.js");

  [Inject]
  private IJSRuntime Js { get; set; } = null!;

  /// <summary>
  /// A CSS selector for the elements to show the tooltip for, such as <c>.overdue-icon</c> or <c>[title]</c>. With child
  /// content, only the elements in it are matched, otherwise any on the page are, including ones added later
  /// </summary>
  [Parameter]
  public string? TargetSelector { get; set; }

  /// <summary>
  /// The target, such as an icon or button. If there's a <see cref="TargetSelector"/> as well, the targets are the elements
  /// in this that it matches
  /// </summary>
  [Parameter]
  public RenderFragment? ChildContent { get; set; }

  /// <summary>
  /// What the tooltip shows, which can be any markup. It's given the target's id, title and <c>data-</c> attributes, so one
  /// tooltip can show something different for each of its targets
  /// </summary>
  [Parameter]
  public RenderFragment<PixataTooltipTemplateContext>? Template { get; set; }

  /// <summary>
  /// The text the tooltip shows, if it has no <see cref="Template"/>. Without either, it shows the target's <c>title</c>
  /// attribute, and isn't shown for a target without one
  /// </summary>
  [Parameter]
  public string? Text { get; set; }

  /// <summary>
  /// Which side of the target the tooltip is shown on. Defaults to <see cref="PixataTooltipPosition.Top"/>. If there isn't
  /// room there, it goes on the opposite side
  /// </summary>
  [Parameter]
  public PixataTooltipPosition Position { get; set; } = PixataTooltipPosition.Top;

  /// <summary>
  /// What shows the tooltip. Defaults to <see cref="PixataTooltipShowEvent.Hover"/>, which includes the keyboard focus and a
  /// tap on a touch screen
  /// </summary>
  [Parameter]
  public PixataTooltipShowEvent ShowOn { get; set; } = PixataTooltipShowEvent.Hover;

  /// <summary>
  /// How long, in milliseconds, the pointer has to be over a target before the tooltip shows. Defaults to 100. The keyboard
  /// focus and a tap show it straight away
  /// </summary>
  [Parameter]
  public int ShowDelay { get; set; } = 100;

  /// <summary>
  /// How long, in milliseconds, the tooltip stays after the pointer leaves the target, so it can be moved onto the tooltip
  /// (to select its text, say). Defaults to 300
  /// </summary>
  [Parameter]
  public int HideDelay { get; set; } = 300;

  /// <summary>
  /// The tooltip's width, as a CSS width. By default it's as wide as its content, up to
  /// <c>--pixata-blazor-tooltip-max-width</c>
  /// </summary>
  [Parameter]
  public string? Width { get; set; }

  /// <summary>
  /// The tooltip's height, as a CSS height. By default it's as tall as its content. If the content is taller, it scrolls
  /// </summary>
  [Parameter]
  public string? Height { get; set; }

  /// <summary>
  /// A CSS class for the tooltip
  /// </summary>
  [Parameter]
  public string? Class { get; set; }

  /// <summary>
  /// The tooltip's id, which the target's <c>aria-describedby</c> points to while it's shown
  /// </summary>
  [Parameter]
  public string? Id { get; set; }

  private readonly string _key = Guid.NewGuid().ToString("N")[..12];
  private PixataTooltipTemplateContext? _context;
  private bool _place;
  private string? _sentOptions;
  private IJSObjectReference? _module;
  private DotNetObjectReference<PixataTooltip>? _self;
  private bool _loading;
  private bool _disposed;

  private string TooltipId =>
    string.IsNullOrWhiteSpace(Id) ? $"pixata-tooltip-{_key}" : Id;

  private string TargetsId =>
    $"pixata-tooltip-targets-{_key}";

  private string? ContentStyle =>
    string.Join("; ", new[] { string.IsNullOrWhiteSpace(Width) ? null : $"width: {Width}", string.IsNullOrWhiteSpace(Height) ? null : $"height: {Height}" }.OfType<string>()) is { Length: > 0 } style
      ? style
      : null;

  /// <summary>
  /// Whether the tooltip is being shown
  /// </summary>
  public bool IsShown =>
    _context is not null;

  protected override void OnParametersSet() {
    if (ChildContent is null && string.IsNullOrWhiteSpace(TargetSelector)) {
      throw new InvalidOperationException("A PixataTooltip needs a TargetSelector, or its target as its child content");
    }
  }

  // What the script needs to know. Sent when the tooltip first renders, and again if any of it changes
  private string Options() =>
    JsonSerializer.Serialize(new {
      key = _key,
      tooltipId = TooltipId,
      targetsId = ChildContent is null ? null : TargetsId,
      selector = string.IsNullOrWhiteSpace(TargetSelector) ? null : TargetSelector,
      content = Template is not null || !string.IsNullOrEmpty(Text),
      showOn = ShowOn == PixataTooltipShowEvent.Click ? "click" : "hover",
      showDelay = Math.Max(ShowDelay, 0),
      hideDelay = Math.Max(HideDelay, 0),
      position = Position.ToString().ToLowerInvariant()
    });

  protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (_disposed || _loading) {
      // Anything this render needs doing is done once the script has loaded
      return;
    }
    try {
      if (_module is null) {
        _loading = true;
        try {
          IJSObjectReference module = await Js.InvokeAsync<IJSObjectReference>("import", ModulePath);
          _self ??= DotNetObjectReference.Create(this);
          _sentOptions = Options();
          await module.InvokeVoidAsync("attach", _sentOptions, _self);
          _module = module;
        } finally {
          _loading = false;
        }
        if (_disposed) {
          // Disposed while the script was loading
          await DisposeAsync();
          return;
        }
      }
      string options = Options();
      if (options != _sentOptions) {
        _sentOptions = options;
        await _module.InvokeVoidAsync("update", options);
      }
      if (_place && _context is not null) {
        _place = false;
        await _module.InvokeVoidAsync("place", _key);
      }
    } catch (Exception ex) when (ex is JSDisconnectedException or OperationCanceledException or InvalidOperationException) {
      // The page has gone, or JavaScript isn't available yet (prerendering), in which case this is tried again after the next
      // render
    }
  }

  /// <summary>
  /// Called by the script when the tooltip is to be shown for a target
  /// </summary>
  [JSInvokable]
  [EditorBrowsable(EditorBrowsableState.Never)]
  public void ShowFor(string? targetId, string? title, Dictionary<string, string>? dataAttributes) {
    _context = new() { TargetId = string.IsNullOrEmpty(targetId) ? null : targetId, Title = title, DataAttributes = dataAttributes ?? [] };
    _place = true;
    StateHasChanged();
  }

  /// <summary>
  /// Called by the script when the tooltip is to be hidden
  /// </summary>
  [JSInvokable]
  [EditorBrowsable(EditorBrowsableState.Never)]
  public void Hidden() {
    if (_context is null) {
      return;
    }
    _context = null;
    StateHasChanged();
  }

  /// <summary>
  /// Hides the tooltip, if it's shown
  /// </summary>
  public async Task Hide() {
    if (_module is not null && _context is not null) {
      try {
        await _module.InvokeVoidAsync("hide", _key);
      } catch (Exception ex) when (ex is JSDisconnectedException or OperationCanceledException) {
        // The page has gone
      }
    }
    Hidden();
  }

  public async ValueTask DisposeAsync() {
    _disposed = true;
    if (_module is not null) {
      try {
        await _module.InvokeVoidAsync("detach", _key);
        await _module.DisposeAsync();
      } catch (Exception ex) when (ex is JSDisconnectedException or OperationCanceledException) {
        // The page has gone, and the script with it
      }
      _module = null;
    }
    _self?.Dispose();
    _self = null;
    GC.SuppressFinalize(this);
  }
}
