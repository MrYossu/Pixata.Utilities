using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Pixata.Blazor.Components;

/// <summary>
/// The base for the setting tags that go inside a scheduler's <c>SchedulerResources</c> or <c>SchedulerSettings</c>. They
/// render nothing; they tell the scheduler how to behave
/// </summary>
public abstract class PixataSchedulerSettingBase<TItem> : ComponentBase, IDisposable {
  [CascadingParameter]
  private protected PixataScheduler<TItem>? Scheduler { get; set; }

  private string _signature = "";

  protected override void OnInitialized() {
    if (Scheduler is null) {
      throw new InvalidOperationException($"{GetType().Name.Split('`')[0]} must be inside a PixataScheduler");
    }
    Prepare();
    _signature = Signature();
    Scheduler.AddSetting(this);
  }

  protected override void OnParametersSet() {
    Prepare();
    string signature = Signature();
    if (signature != _signature) {
      _signature = signature;
      Scheduler?.ViewSettingsChanged();
    }
  }

  /// <summary>
  /// The setting's simple values as text, so it can tell when they change
  /// </summary>
  internal abstract string Signature();

  /// <summary>
  /// Works out anything the setting needs from its parameters, before its signature is taken
  /// </summary>
  internal virtual void Prepare() {
  }

  protected override bool ShouldRender() =>
    false;

  public void Dispose() {
    Scheduler?.RemoveSetting(this);
    GC.SuppressFinalize(this);
  }
}

/// <summary>
/// A type of resource (a meeting room, a person, some equipment) that appointments can be linked to. Goes inside the
/// scheduler's <c>SchedulerResources</c>. Each appointment has a field (named by <see cref="Field"/>) holding the value of its
/// resource. Appointments are coloured with the colour of the first resource they match, the edit form has a drop-down for
/// each resource, and the scheduler can be grouped by resource (see <see cref="PixataSchedulerGroupSettings{TItem}"/>)
/// </summary>
public class PixataSchedulerResource<TItem> : PixataSchedulerSettingBase<TItem> {
  /// <summary>
  /// The name of the appointment field that holds the resource's value
  /// </summary>
  [Parameter, EditorRequired]
  public string Field { get; set; } = "";

  /// <summary>
  /// The label for the resource in the edit form. Defaults to <see cref="Field"/>
  /// </summary>
  [Parameter]
  public string? Title { get; set; }

  /// <summary>
  /// The resource values
  /// </summary>
  [Parameter]
  public IEnumerable<object>? Data { get; set; }

  /// <summary>
  /// The field of each resource that holds its display text. <c>Text</c> by default
  /// </summary>
  [Parameter]
  public string TextField { get; set; } = "Text";

  /// <summary>
  /// The field of each resource that holds the value stored in the appointment. <c>Value</c> by default
  /// </summary>
  [Parameter]
  public string ValueField { get; set; } = "Value";

  /// <summary>
  /// The field of each resource that holds its colour (any CSS colour). <c>Color</c> by default. Resources without one
  /// leave the appointments their default colour
  /// </summary>
  [Parameter]
  public string ColorField { get; set; } = "Color";

  /// <summary>
  /// An icon shown next to the resource's label in the edit form: a <see cref="PixataSvgIcon"/> or an icon font class
  /// </summary>
  [Parameter]
  public object? Icon { get; set; }

  /// <summary>
  /// The resource values as (value, text, colour)
  /// </summary>
  internal List<(object? Value, string Text, string? Colour)> Items { get; private set; } = [];

  internal override void Prepare() {
    if (string.IsNullOrWhiteSpace(Field)) {
      throw new InvalidOperationException("A PixataSchedulerResource needs a Field");
    }
    Items = (Data ?? []).Select(item => (
      PixataFieldReader.GetValue(item, ValueField),
      PixataFieldReader.GetText(item, TextField),
      HasColour(item) ? PixataFieldReader.GetValue(item, ColorField)?.ToString() : null)).ToList();
  }

  private bool HasColour(object item) {
    try {
      return PixataFieldReader.GetValue(item, ColorField) is not null;
    }
    catch (InvalidOperationException) {
      return false;
    }
  }

  /// <summary>
  /// The resource with the given value, if there is one
  /// </summary>
  internal (object? Value, string Text, string? Colour)? Find(object? value) {
    foreach ((object? Value, string Text, string? Colour) item in Items) {
      if (PixataSchedulerItemFields.ValuesMatch(item.Value, value)) {
        return item;
      }
    }
    return null;
  }

  internal override string Signature() =>
    $"{Field}|{Title}|{TextField}|{ValueField}|{ColorField}|{string.Join(",", Items.Select(i => $"{i.Value}:{i.Text}:{i.Colour}"))}";
}

/// <summary>
/// Groups the scheduler's views by resource. Goes inside the scheduler's <c>SchedulerSettings</c>
/// </summary>
public class PixataSchedulerGroupSettings<TItem> : PixataSchedulerSettingBase<TItem> {
  /// <summary>
  /// The fields (the <c>Field</c> of the <see cref="PixataSchedulerResource{TItem}"/>s) to group by. With more than one,
  /// the groups are nested in the order given
  /// </summary>
  [Parameter]
  public List<string>? Resources { get; set; }

  /// <summary>
  /// Whether the groups sit side by side (the default) or one above the other
  /// </summary>
  [Parameter]
  public PixataSchedulerGroupOrientation Orientation { get; set; } = PixataSchedulerGroupOrientation.Horizontal;

  internal override string Signature() =>
    $"{string.Join(",", Resources ?? [])}|{Orientation}";
}

/// <summary>
/// The size and title of the edit form's dialog. Goes inside the scheduler's <c>SchedulerSettings</c>. Sizes are CSS lengths
/// </summary>
public class PixataSchedulerPopupEditSettings<TItem> : PixataSchedulerSettingBase<TItem> {
  /// <summary>
  /// Extra CSS classes for the dialog
  /// </summary>
  [Parameter]
  public string? Class { get; set; }

  [Parameter]
  public string? Width { get; set; }

  [Parameter]
  public string? Height { get; set; }

  [Parameter]
  public string? MinWidth { get; set; }

  [Parameter]
  public string? MaxWidth { get; set; }

  [Parameter]
  public string? MinHeight { get; set; }

  [Parameter]
  public string? MaxHeight { get; set; }

  /// <summary>
  /// The dialog's title. Defaults to "Add event" or "Edit event"
  /// </summary>
  [Parameter]
  public string? Title { get; set; }

  internal override string Signature() =>
    $"{Class}|{Width}|{Height}|{MinWidth}|{MaxWidth}|{MinHeight}|{MaxHeight}|{Title}";
}

/// <summary>
/// Replaces the content of the edit form. Goes inside the scheduler's <c>SchedulerSettings</c>
/// </summary>
public class PixataSchedulerPopupEditFormSettings<TItem> : PixataSchedulerSettingBase<TItem> {
  /// <summary>
  /// The form's fields. The context is a <see cref="PixataSchedulerEditFormContext{TItem}"/>, whose <c>Item</c> is the copy
  /// of the appointment being edited; bind your inputs to its properties. The form validates the item with data annotations,
  /// and the scheduler checks that it has a title and ends after it starts. The Save, Cancel and Delete buttons are still
  /// shown unless you also set <see cref="ButtonsTemplate"/>
  /// </summary>
  [Parameter]
  public RenderFragment<PixataSchedulerEditFormContext<TItem>>? FormTemplate { get; set; }

  /// <summary>
  /// The form's buttons. Call the context's <c>Save</c>, <c>Cancel</c> and <c>Delete</c> methods from them (or use a submit
  /// button, which saves)
  /// </summary>
  [Parameter]
  public RenderFragment<PixataSchedulerEditFormContext<TItem>>? ButtonsTemplate { get; set; }

  internal override string Signature() =>
    $"{FormTemplate is null}|{ButtonsTemplate is null}";
}

/// <summary>
/// The context of the edit form's templates
/// </summary>
public class PixataSchedulerEditFormContext<TItem> {
  internal PixataSchedulerEditFormContext(TItem item, bool isNew, PixataSchedulerRecurrenceEditMode editMode, EditContext editContext,
    Func<Task> save, Func<Task> cancel, Func<Task>? delete) {
    Item = item;
    IsNew = isNew;
    EditMode = editMode;
    EditContext = editContext;
    _save = save;
    _cancel = cancel;
    _delete = delete;
  }

  private readonly Func<Task> _save;
  private readonly Func<Task> _cancel;
  private readonly Func<Task>? _delete;

  /// <summary>
  /// The copy of the appointment being edited. Your changes are only passed back (in <c>OnCreate</c> or <c>OnUpdate</c>) when
  /// the user saves
  /// </summary>
  public TItem Item { get; }

  /// <summary>
  /// True for a new appointment
  /// </summary>
  public bool IsNew { get; }

  /// <summary>
  /// For a recurring appointment, whether the user is editing one occurrence or the whole series
  /// </summary>
  public PixataSchedulerRecurrenceEditMode EditMode { get; }

  /// <summary>
  /// The form's edit context, for validation
  /// </summary>
  public EditContext EditContext { get; }

  /// <summary>
  /// True if the appointment can be deleted from the form
  /// </summary>
  public bool CanDelete => _delete is not null;

  /// <summary>
  /// Validates the form and, if it is valid, saves the appointment and closes the form
  /// </summary>
  public Task Save() =>
    _save();

  /// <summary>
  /// Closes the form without saving
  /// </summary>
  public Task Cancel() =>
    _cancel();

  /// <summary>
  /// Deletes the appointment (asking first if need be)
  /// </summary>
  public Task Delete() =>
    _delete?.Invoke() ?? Task.CompletedTask;
}
