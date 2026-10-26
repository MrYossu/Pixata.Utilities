using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components;

/// <summary>
/// What <see cref="PixataDateInput{TValue}"/> and <see cref="PixataDatePicker{TValue}"/> have in common: the segmented input,
/// the value conversion and the form integration. <typeparamref name="TValue"/> can be <see cref="DateTime"/>,
/// <see cref="DateOnly"/> or a nullable version of either
/// </summary>
public abstract class PixataDateInputBase<TValue> : ComponentBase, IAsyncDisposable {
  [Inject]
  private IJSRuntime Js { get; set; } = null!;

  [CascadingParameter]
  private EditContext? EditContext { get; set; }

  [Parameter]
  public TValue? Value { get; set; }

  [Parameter]
  public EventCallback<TValue> ValueChanged { get; set; }

  /// <summary>
  /// Identifies the bound field for validation. Set automatically when you use <c>@bind-Value</c>
  /// </summary>
  [Parameter]
  public Expression<Func<TValue>>? ValueExpression { get; set; }

  /// <summary>
  /// Fires when the user has finished changing the value, which is when they press Enter or leave the input (or pick a date
  /// in the picker's calendar), rather than on every key. The argument is the new value
  /// </summary>
  [Parameter]
  public EventCallback<object?> OnChange { get; set; }

  /// <summary>
  /// Fires when the input loses focus
  /// </summary>
  [Parameter]
  public EventCallback OnBlur { get; set; }

  /// <summary>
  /// A .NET date format, such as "dd/MM/yyyy" or "d". Defaults to the current culture's short date pattern
  /// </summary>
  [Parameter]
  public string Format { get; set; } = "d";

  /// <summary>
  /// The earliest date allowed. Defaults to 1st January 1900, as in Telerik
  /// </summary>
  [Parameter]
  public DateTime Min { get; set; } = new(1900, 1, 1);

  /// <summary>
  /// The latest date allowed. Defaults to 31st December 2099, as in Telerik
  /// </summary>
  [Parameter]
  public DateTime Max { get; set; } = new(2099, 12, 31, 23, 59, 59);

  /// <summary>
  /// Shown when the value is null and the input doesn't have focus
  /// </summary>
  [Parameter]
  public string? Placeholder { get; set; }

  [Parameter]
  public bool Enabled { get; set; } = true;

  [Parameter]
  public bool ReadOnly { get; set; }

  /// <summary>
  /// Shows an x button that clears the value. Only for nullable types
  /// </summary>
  [Parameter]
  public bool ShowClearButton { get; set; }

  /// <summary>
  /// When true (the default), the selection moves to the next part of the date once the current one is complete
  /// </summary>
  [Parameter]
  public bool AutoSwitchParts { get; set; } = true;

  /// <summary>
  /// For a two-digit year format, the highest two-digit year that is in the 2000s. Defaults to 68, so 68 means 2068 and 69
  /// means 1969
  /// </summary>
  [Parameter]
  public int TwoDigitYearMax { get; set; } = 68;

  /// <summary>
  /// The width, as a CSS width
  /// </summary>
  [Parameter]
  public string? Width { get; set; }

  /// <summary>
  /// <c>sm</c>, <c>md</c> (the default) or <c>lg</c>. See <see cref="PixataThemeConstants.Size"/>
  /// </summary>
  [Parameter]
  public string? Size { get; set; }

  /// <summary>
  /// The id of the input, for a label's <c>for</c>
  /// </summary>
  [Parameter]
  public string? Id { get; set; }

  [Parameter]
  public string? Class { get; set; }

  [Parameter]
  public int? TabIndex { get; set; }

  /// <summary>
  /// The <c>inputmode</c> of the input, which picks the keyboard on a phone. If not set, it's "numeric" when the format only
  /// has numbers in it
  /// </summary>
  [Parameter]
  public string? InputMode { get; set; }

  protected ElementReference InputElement;
  private protected DateInputModel Model = null!;
  protected bool Focused;
  private IJSObjectReference? _module;
  private DotNetObjectReference<PixataDateInputBase<TValue>>? _dotNet;
  private string? _modelFormat;
  private DateTime? _lastValue;
  private DateTime? _valueAtFocus;
  private bool _initialised;
  private bool _disposed;
  private FieldIdentifier? _field;

  // The text the input is first rendered with. It never changes, so Blazor never touches the input's value again, which
  // would lose the selection. From then on, the JavaScript sets the text
  protected string InitialText = "";

  private static readonly Type UnderlyingType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);

  protected static bool IsNullable =>
    Nullable.GetUnderlyingType(typeof(TValue)) is not null;

  protected DateTime? CurrentDate =>
    ToDateTime(Value);

  protected bool IsInvalid =>
    !Model.IsValid || (_field is FieldIdentifier fieldId && EditContext!.GetValidationMessages(fieldId).Any());

  protected bool HasValue =>
    !Model.IsEmpty;

  protected string SizeClass =>
    $"pixata-blazor-dateinput-{(string.IsNullOrWhiteSpace(Size) ? PixataThemeConstants.Size.Medium : Size)}";

  protected string? EffectiveInputMode =>
    InputMode ?? (Model.IsNumeric ? "numeric" : null);

  protected override void OnInitialized() {
    if (UnderlyingType != typeof(DateTime) && UnderlyingType != typeof(DateOnly)) {
      throw new InvalidOperationException($"{GetType().Name.Split('`')[0]} only works with DateTime, DateOnly and their nullable versions, not {typeof(TValue).Name}");
    }
    if (EditContext is not null && ValueExpression is not null) {
      _field = FieldIdentifier.Create(ValueExpression);
    }
  }

  protected override async Task OnParametersSetAsync() {
    if (Format != _modelFormat) {
      Model = new(Format) { AutoSwitchParts = AutoSwitchParts, TwoDigitYearMax = TwoDigitYearMax, Min = Min, Max = Max };
      _modelFormat = Format;
      Model.SetValue(CurrentDate);
      _lastValue = CurrentDate;
      if (!_initialised) {
        InitialText = Model.IsEmpty && !string.IsNullOrEmpty(Placeholder) ? "" : Model.Text;
      }
      await PushState();
    }
    Model.AutoSwitchParts = AutoSwitchParts;
    Model.TwoDigitYearMax = TwoDigitYearMax;
    Model.Min = Min;
    Model.Max = Max;
    // A value set from outside (rather than typed) replaces whatever is in the input
    if (CurrentDate != _lastValue) {
      _lastValue = CurrentDate;
      Model.SetValue(CurrentDate);
      await PushState();
    }
  }

  protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (firstRender) {
      try {
        IJSObjectReference module = await Js.InvokeAsync<IJSObjectReference>("import", PixataBlazorScript.Path);
        // The user can leave the page while the module is being imported, in which case the input has already gone
        if (_disposed) {
          await module.DisposeAsync();
          return;
        }
        _module = module;
        _dotNet = DotNetObjectReference.Create(this);
        await _module.InvokeVoidAsync("initDateInput", InputElement, _dotNet);
        _initialised = true;
        // A value set while the module was being imported (such as a grid filter restored from its saved state) couldn't be
        // pushed then, so the input still shows the text it was first rendered with
        if (State().Text != InitialText) {
          await PushState();
        }
      } catch (JSException) {
        // The input has gone from the page
      } catch (JSDisconnectedException) {
        // So has the page
      }
    }
  }

  public async ValueTask FocusAsync() =>
    await InputElement.FocusAsync();

  [JSInvokable]
  public async Task<DateInputState> OnKey(string key) {
    if (Enabled && !ReadOnly) {
      Model.Key(key);
      await UpdateValueFromModel();
    }
    return State();
  }

  [JSInvokable]
  public DateInputState OnCaret(int caret) {
    Model.SelectAt(caret);
    return State();
  }

  [JSInvokable]
  public DateInputState OnInputFocus() {
    Focused = true;
    _valueAtFocus = CurrentDate;
    Model.SelectFirst();
    return State();
  }

  [JSInvokable]
  public async Task<DateInputState> OnInputBlur() {
    Focused = false;
    await OnBlur.InvokeAsync();
    await RaiseChangeIfNeeded();
    StateHasChanged();
    return State();
  }

  [JSInvokable]
  public async Task<DateInputState> OnPaste(string text) {
    if (Enabled && !ReadOnly && Model.Paste(text)) {
      await UpdateValueFromModel();
    }
    return State();
  }

  /// <summary>
  /// Raises <see cref="OnChange"/> if the value is different from when the input got focus. Called on blur and on Enter
  /// </summary>
  protected async Task RaiseChangeIfNeeded() {
    if (CurrentDate != _valueAtFocus) {
      _valueAtFocus = CurrentDate;
      await OnChange.InvokeAsync(Value);
    }
  }

  protected async Task Clear() {
    Model.Clear();
    await UpdateValueFromModel();
    await PushState();
    await RaiseChangeIfNeeded();
  }

  /// <summary>
  /// Sets the value from somewhere other than the input, such as the picker's calendar
  /// </summary>
  protected async Task SetValue(DateTime? date) {
    Model.SetValue(date);
    await UpdateValueFromModel();
    await PushState();
  }

  // A complete, valid date becomes the value. A nullable value becomes null when the input is empty or incomplete, as in
  // Telerik. A non-nullable one keeps its last good value, and the input shows as invalid until it's fixed
  private async Task UpdateValueFromModel() {
    DateTime? date = Model.IsValid ? Model.Value : null;
    if (date is null && !IsNullable) {
      StateHasChanged();
      return;
    }
    if (date != CurrentDate) {
      _lastValue = date;
      Value = FromDateTime(date);
      await ValueChanged.InvokeAsync(Value);
      if (_field is FieldIdentifier fieldId) {
        EditContext!.NotifyFieldChanged(fieldId);
      }
    }
    StateHasChanged();
  }

  // An empty input shows its format while it has focus, and the placeholder (if there is one) when it doesn't
  protected DateInputState State() {
    string text = !Focused && Model.IsEmpty && !string.IsNullOrEmpty(Placeholder) ? "" : Model.Text;
    (int start, int end) = Focused ? Model.Selection : (-1, -1);
    return new(text, start, end);
  }

  protected async Task PushState() {
    if (_module is not null && !_disposed) {
      try {
        await _module.InvokeVoidAsync("setDateInputState", InputElement, State());
      } catch (JSException) {
        // The input has gone from the page
      } catch (JSDisconnectedException) {
        // So has the page
      }
    }
  }

  protected IJSObjectReference? Module =>
    _module;

  private static DateTime? ToDateTime(TValue? value) => value switch {
    DateTime dateTime => dateTime,
    DateOnly dateOnly => dateOnly.ToDateTime(TimeOnly.MinValue),
    _ => null
  };

  protected static TValue? FromDateTime(DateTime? date) {
    if (date is not DateTime value) {
      return default;
    }
    object result = UnderlyingType == typeof(DateOnly) ? DateOnly.FromDateTime(value) : value;
    return (TValue)result;
  }

  public virtual async ValueTask DisposeAsync() {
    _disposed = true;
    if (_module is not null) {
      try {
        await _module.InvokeVoidAsync("disposeDateInput", InputElement);
        await _module.DisposeAsync();
      } catch (JSException) {
        // The input has already gone
      } catch (JSDisconnectedException) {
        // The circuit has gone, so there's nothing to clean up
      }
    }
    _dotNet?.Dispose();
    GC.SuppressFinalize(this);
  }
}

/// <summary>
/// The text and selection for the input, sent to the JavaScript after each key press
/// </summary>
public record DateInputState(string Text, int Start, int End);
