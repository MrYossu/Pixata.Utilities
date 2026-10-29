using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components;

/// <summary>
/// A multi-line text box. It has the same parameters as Telerik's TextArea, so markup can usually move across by changing
/// the component's name
/// </summary>
public partial class PixataTextArea {
  [Inject]
  private IJSRuntime Js { get; set; } = null!;

  /// <summary>
  /// How many lines of text it's high, unless <see cref="ResizeMode"/> is <see cref="PixataTextAreaResizeMode.Auto"/>, when
  /// it's the starting height. Defaults to 3
  /// </summary>
  [Parameter]
  public int Rows { get; set; } = 3;

  /// <summary>
  /// How the user can resize it, or <see cref="PixataTextAreaResizeMode.Auto"/> to grow with its text instead of scrolling.
  /// Defaults to <see cref="PixataTextAreaResizeMode.Vertical"/>
  /// </summary>
  [Parameter]
  public PixataTextAreaResizeMode ResizeMode { get; set; } = PixataTextAreaResizeMode.Vertical;

  /// <summary>
  /// The same as setting <see cref="ResizeMode"/> to <see cref="PixataTextAreaResizeMode.Auto"/>
  /// </summary>
  [Parameter]
  public bool AutoSize { get; set; }

  /// <summary>
  /// Shows how many characters have been typed under the text, out of <see cref="PixataTextInputBase.MaxLength"/> if it's set
  /// </summary>
  [Parameter]
  public bool ShowCharacterCount { get; set; }

  private readonly string _countId = $"pixata-textarea-count-{Guid.NewGuid():N}";
  private bool _resizeNeeded = true;

  private PixataTextAreaResizeMode EffectiveResizeMode =>
    AutoSize ? PixataTextAreaResizeMode.Auto : ResizeMode;

  private string WrapperClass =>
    PixataCss.Classes(PixataCss.Input(Size, Rounded, FillMode), "pixata-blazor-textarea",
      $"pixata-blazor-textarea-resize-{EffectiveResizeMode.ToString().ToLowerInvariant()}",
      PixataCss.Disabled(Enabled), InvalidClass, Class);

  private string? CountId =>
    ShowCharacterCount ? _countId : null;

  // Adds the count to any aria-describedby passed in, rather than replacing it
  private string? DescribedBy {
    get {
      string? passedIn = AdditionalAttributes is not null && AdditionalAttributes.TryGetValue("aria-describedby", out object? value) ? value?.ToString() : null;
      string ids = string.Join(" ", new[] { passedIn, CountId }.Where(id => !string.IsNullOrWhiteSpace(id)));
      return ids == "" ? null : ids;
    }
  }

  private string CharacterCount =>
    MaxLength is int max ? $"{Text.Length} / {max}" : $"{Text.Length}";

  protected override void OnTextSetFromOutside() =>
    _resizeNeeded = true;

  protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (EffectiveResizeMode == PixataTextAreaResizeMode.Auto && _resizeNeeded) {
      // Typing resizes it in the browser, so this is only needed when the text comes from outside
      _resizeNeeded = false;
      await PixataJs.InvokeVoid(Js, "autoSizeTextArea", InputElement);
    }
  }
}

/// <summary>
/// How a <see cref="PixataTextArea"/> can be resized
/// </summary>
public enum PixataTextAreaResizeMode {
  /// <summary>
  /// The user can't resize it
  /// </summary>
  None,

  /// <summary>
  /// The user can make it taller or shorter (the default)
  /// </summary>
  Vertical,

  /// <summary>
  /// The user can make it wider or narrower
  /// </summary>
  Horizontal,

  /// <summary>
  /// The user can resize it both ways
  /// </summary>
  Both,

  /// <summary>
  /// It grows and shrinks to fit its text, rather than scrolling, and the user can't resize it
  /// </summary>
  Auto
}
