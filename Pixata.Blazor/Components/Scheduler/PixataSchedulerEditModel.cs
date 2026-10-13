using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Components.Forms;

namespace Pixata.Blazor.Components;

/// <summary>
/// What the built-in edit form edits: the appointment's fields, copied out of the data item so that the form works whatever
/// the field names are, and copied back when the user saves. For an all-day appointment, <see cref="Start"/> and
/// <see cref="End"/> are the first and last days it covers
/// </summary>
internal sealed class PixataSchedulerEditModel {
  public string Title { get; set; } = "";
  public string Description { get; set; } = "";
  public DateTime Start { get; set; }
  public DateTime End { get; set; }
  public bool IsAllDay { get; set; }
  public string? RecurrenceRule { get; set; }

  /// <summary>
  /// The chosen value of each resource, as the index of the resource's item (or "" for none)
  /// </summary>
  public Dictionary<string, string> Resources { get; } = [];

  public bool HasDescription { get; init; }
  public bool HasIsAllDay { get; init; }
  public bool HasRecurrence { get; init; }
  public bool IsException { get; init; }

  public static PixataSchedulerEditModel From<TItem>(object item, PixataSchedulerItemFields fields, IReadOnlyList<PixataSchedulerResource<TItem>> resources) {
    bool isAllDay = fields.GetIsAllDay(item);
    DateTime start = fields.GetStart(item);
    DateTime end = fields.GetEnd(item);
    PixataSchedulerEditModel model = new() {
      Title = fields.GetTitle(item),
      Description = fields.GetDescription(item),
      IsAllDay = isAllDay,
      Start = isAllDay ? start.Date : start,
      End = isAllDay ? PixataSchedulerDates.DaySpan(start, end).End.AddDays(-1) : end,
      RecurrenceRule = fields.GetRecurrenceRule(item),
      HasDescription = PixataSchedulerItemFields.Has(item, fields.Description),
      HasIsAllDay = PixataSchedulerItemFields.Has(item, fields.IsAllDay),
      HasRecurrence = PixataSchedulerItemFields.Has(item, fields.RecurrenceRule),
      IsException = fields.GetRecurrenceId(item) is { } id && !Equals(id, Guid.Empty) && !Equals(id, 0) && !Equals(id, "")
    };
    foreach (PixataSchedulerResource<TItem> resource in resources) {
      object? value = fields.GetField(item, resource.Field);
      int index = resource.Items.FindIndex(i => PixataSchedulerItemFields.ValuesMatch(i.Value, value));
      model.Resources[resource.Field] = index < 0 ? "" : index.ToString(CultureInfo.InvariantCulture);
    }
    return model;
  }

  /// <summary>
  /// Switches between an all-day and a timed appointment, keeping the dates
  /// </summary>
  public void SetAllDay(bool isAllDay, TimeSpan defaultStart, TimeSpan defaultLength) {
    if (isAllDay == IsAllDay) {
      return;
    }
    IsAllDay = isAllDay;
    if (isAllDay) {
      Start = Start.Date;
      End = End.Date < Start ? Start : End.Date;
    } else {
      Start = Start.Date + defaultStart;
      End = Start + defaultLength;
    }
  }

  /// <summary>
  /// Moves the start, keeping the length the same
  /// </summary>
  public void SetStart(DateTime start) {
    TimeSpan length = End - Start;
    Start = IsAllDay ? start.Date : start;
    End = Start + (length < TimeSpan.Zero ? TimeSpan.Zero : length);
  }

  public bool Validate(EditContext context, ValidationMessageStore messages, PixataSchedulerMessages text) {
    bool valid = true;
    if (string.IsNullOrWhiteSpace(Title)) {
      messages.Add(context.Field(nameof(Title)), text.TitleRequired);
      valid = false;
    }
    if (IsAllDay ? End.Date < Start.Date : End <= Start) {
      messages.Add(context.Field(nameof(End)), text.EndBeforeStart);
      valid = false;
    }
    return valid;
  }

  public void ApplyTo<TItem>(object item, PixataSchedulerItemFields fields, IReadOnlyList<PixataSchedulerResource<TItem>> resources) {
    fields.SetTitle(item, Title);
    if (HasDescription) {
      fields.SetDescription(item, Description);
    }
    if (HasIsAllDay) {
      fields.SetIsAllDay(item, IsAllDay);
    }
    fields.SetStart(item, IsAllDay ? Start.Date : Start);
    fields.SetEnd(item, IsAllDay ? End.Date : End);
    if (HasRecurrence && !IsException) {
      fields.SetRecurrenceRule(item, RecurrenceRule);
    }
    foreach (PixataSchedulerResource<TItem> resource in resources) {
      if (Resources.TryGetValue(resource.Field, out string? key)) {
        object? value = int.TryParse(key, out int index) && index >= 0 && index < resource.Items.Count ? resource.Items[index].Value : null;
        fields.SetField(item, resource.Field, value);
      }
    }
  }
}
