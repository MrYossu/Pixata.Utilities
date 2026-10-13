using System;
using System.Collections.Generic;

namespace Pixata.Blazor.Components;

/// <summary>
/// Raised by <c>OnCreate</c> when the user saves a new appointment. <see cref="Item"/> is the new data item; add it to your
/// data. It is also raised when the user changes one occurrence of a recurring appointment, for the new appointment that
/// replaces that occurrence (its recurrence id field points at the series)
/// </summary>
public class PixataSchedulerCreateEventArgs : EventArgs {
  /// <summary>
  /// The new appointment. Cast it to your data type
  /// </summary>
  public object Item { get; set; } = null!;
}

/// <summary>
/// Raised by <c>OnEdit</c> just before the edit form opens, either for a new appointment (<see cref="IsNew"/>) or an existing
/// one. Set <see cref="IsCancelled"/> to stop the form opening, eg to show your own form instead
/// </summary>
public class PixataSchedulerEditEventArgs : EventArgs {
  /// <summary>
  /// The appointment about to be edited: a copy of the data item, or the new item for a new appointment
  /// </summary>
  public object? Item { get; set; }

  /// <summary>
  /// True if this is a new appointment
  /// </summary>
  public bool IsNew { get; set; }

  /// <summary>
  /// Set to true to stop the edit form opening
  /// </summary>
  public bool IsCancelled { get; set; }

  /// <summary>
  /// For a recurring appointment, whether the user chose to edit one occurrence or the whole series
  /// </summary>
  public PixataSchedulerRecurrenceEditMode EditMode { get; set; } = PixataSchedulerRecurrenceEditMode.Series;

  /// <summary>
  /// For a new appointment, whether the user started it in the all-day row
  /// </summary>
  public bool IsAllDay { get; set; }

  /// <summary>
  /// For a new appointment, the start of the slot the user chose
  /// </summary>
  public DateTime Start { get; set; }

  /// <summary>
  /// For a new appointment, the end of the slot the user chose
  /// </summary>
  public DateTime End { get; set; }
}

/// <summary>
/// Raised by <c>OnUpdate</c> when the user saves changes to an appointment (in the edit form, or by dragging or resizing it).
/// <see cref="Item"/> is an updated copy of the data item; replace the original (find it by its id) with it
/// </summary>
public class PixataSchedulerUpdateEventArgs : EventArgs {
  /// <summary>
  /// The updated copy of the appointment. Cast it to your data type
  /// </summary>
  public object Item { get; set; } = null!;

  /// <summary>
  /// <see cref="PixataSchedulerRecurrenceEditMode.Occurrence"/> when one occurrence of a recurring appointment was changed or
  /// deleted, in which case <see cref="Item"/> is the series with that occurrence added to its exceptions; otherwise
  /// <see cref="PixataSchedulerRecurrenceEditMode.Series"/>
  /// </summary>
  public PixataSchedulerRecurrenceEditMode EditMode { get; set; } = PixataSchedulerRecurrenceEditMode.Series;
}

/// <summary>
/// Raised by <c>OnCancel</c> when the user closes the edit form without saving
/// </summary>
public class PixataSchedulerCancelEventArgs : EventArgs {
  /// <summary>
  /// The appointment being edited, with any changes the user made in the form
  /// </summary>
  public object Item { get; set; } = null!;
}

/// <summary>
/// Raised by <c>OnDelete</c> when the user deletes an appointment, or the whole series of a recurring appointment. (Deleting
/// one occurrence raises <c>OnUpdate</c> instead, for the series with that occurrence added to its exceptions)
/// </summary>
public class PixataSchedulerDeleteEventArgs : EventArgs {
  /// <summary>
  /// The appointment to delete. Cast it to your data type
  /// </summary>
  public object Item { get; set; } = null!;

  /// <summary>
  /// <see cref="PixataSchedulerRecurrenceEditMode.Series"/>; when the item is a recurring appointment, any exceptions to it
  /// (items whose recurrence id is its id) should usually be deleted too
  /// </summary>
  public PixataSchedulerRecurrenceEditMode EditMode { get; set; } = PixataSchedulerRecurrenceEditMode.Series;
}

/// <summary>
/// Passed to <c>OnItemRender</c> for each appointment, so you can add CSS classes to it
/// </summary>
public class PixataSchedulerItemRenderEventArgs : EventArgs {
  /// <summary>
  /// The appointment about to be rendered
  /// </summary>
  public object Item { get; set; } = null!;

  /// <summary>
  /// Extra CSS classes for the appointment's element
  /// </summary>
  public string? Class { get; set; }
}

/// <summary>
/// Passed to <c>OnCellRender</c> for each slot, so you can add CSS classes to it
/// </summary>
public class PixataSchedulerCellRenderEventArgs : EventArgs {
  /// <summary>
  /// Extra CSS classes for the slot's element
  /// </summary>
  public string? Class { get; set; }

  /// <summary>
  /// The start of the slot
  /// </summary>
  public DateTime Start { get; set; }

  /// <summary>
  /// The end of the slot
  /// </summary>
  public DateTime End { get; set; }

  /// <summary>
  /// True for a slot in the all-day row
  /// </summary>
  public bool IsAllDay { get; set; }

  /// <summary>
  /// When the scheduler is grouped by resource, the resource field names and values of the slot's group
  /// </summary>
  public List<KeyValuePair<string, object?>> Resources { get; set; } = [];
}

/// <summary>
/// The base for the arguments of the appointment click events
/// </summary>
public abstract class PixataSchedulerItemEventArgs : EventArgs {
  /// <summary>
  /// The appointment. For an occurrence of a recurring appointment, this is a copy of the series with the occurrence's start
  /// and end
  /// </summary>
  public object Item { get; set; } = null!;

  /// <summary>
  /// The browser's event arguments (a <c>MouseEventArgs</c> or <c>KeyboardEventArgs</c>)
  /// </summary>
  public EventArgs EventArgs { get; set; } = EventArgs.Empty;

  /// <summary>
  /// Whether the scheduler should render again after your handler. It always does, so this can be left alone
  /// </summary>
  public bool ShouldRender { get; set; }
}

/// <summary>
/// Raised by <c>OnItemClick</c> when the user clicks (or taps) an appointment
/// </summary>
public class PixataSchedulerItemClickEventArgs : PixataSchedulerItemEventArgs;

/// <summary>
/// Raised by <c>OnItemDoubleClick</c> when the user double-clicks (or double-taps) an appointment
/// </summary>
public class PixataSchedulerItemDoubleClickEventArgs : PixataSchedulerItemEventArgs;

/// <summary>
/// Raised by <c>OnItemContextMenu</c> when the user right-clicks (or long-presses) an appointment. The browser's own menu is
/// suppressed when there is a handler
/// </summary>
public class PixataSchedulerItemContextMenuEventArgs : PixataSchedulerItemEventArgs;

/// <summary>
/// The context of a <c>SlotTemplate</c>
/// </summary>
public class PixataSchedulerSlotTemplateContext {
  /// <summary>
  /// The start of the slot
  /// </summary>
  public DateTime Start { get; set; }

  /// <summary>
  /// The end of the slot
  /// </summary>
  public DateTime End { get; set; }

  /// <summary>
  /// When the scheduler is grouped by resource, the resource field names and values of the slot's group
  /// </summary>
  public List<KeyValuePair<string, object?>> Resources { get; set; } = [];
}

/// <summary>
/// The context of an <c>AllDaySlotTemplate</c>
/// </summary>
public class PixataSchedulerAllDaySlotTemplateContext : PixataSchedulerSlotTemplateContext;

/// <summary>
/// The context of a <c>SchedulerResourceGroupHeaderTemplate</c>
/// </summary>
public class PixataSchedulerResourceGroupHeaderTemplateContext {
  /// <summary>
  /// The resource's display text
  /// </summary>
  public string Text { get; set; } = "";

  /// <summary>
  /// The resource's value
  /// </summary>
  public object? Value { get; set; }

  /// <summary>
  /// The appointment field the resource is for
  /// </summary>
  public string Field { get; set; } = "";
}
