using System;
using System.Collections.Generic;

namespace Pixata.Blazor.Sample.Models;

/// <summary>
/// An appointment for the scheduler sample page. The property names are the scheduler's defaults, so neither scheduler needs
/// its field parameters setting
/// </summary>
public class SchedulerSampleAppointment {
  public Guid Id { get; set; } = Guid.NewGuid();
  public string Title { get; set; } = "";
  public string Description { get; set; } = "";
  public DateTime Start { get; set; }
  public DateTime End { get; set; }
  public bool IsAllDay { get; set; }
  public string? RecurrenceRule { get; set; }
  public List<DateTime>? RecurrenceExceptions { get; set; }
  public Guid? RecurrenceId { get; set; }
  public bool ReadOnly { get; set; }
  public string Room { get; set; } = "";
  public string Manager { get; set; } = "";

  public SchedulerSampleAppointment Copy() =>
    (SchedulerSampleAppointment)MemberwiseClone();
}

/// <summary>
/// A resource (a meeting room or a manager) for the scheduler sample page
/// </summary>
public class SchedulerSampleResource {
  public string Text { get; set; } = "";
  public string Value { get; set; } = "";
  public string Color { get; set; } = "";
}
