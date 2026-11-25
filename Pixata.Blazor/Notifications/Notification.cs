using System;
using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Notifications;

public class Notification {
  public string Message { get; set; } = "";

  /// <summary>
  /// Shown instead of <see cref="Message"/> when set, for a message with formatting, such as a list
  /// </summary>
  public RenderFragment? Content { get; set; }
  public NotificationType Type { get; set; }
  public DateTime DateTime { get; set; } = DateTime.Now;
  public string Id { get; set; } = "";
  public bool Show { get; set; } = true;
  public Action? OnClick { get; set; }

  /// <summary>
  /// How long the notification stays before it fades out. Null, the default, for the usual time for its type: 5 seconds for
  /// <see cref="NotificationType.Success"/>, until it's dismissed for <see cref="NotificationType.Error"/>, and 15 seconds for the
  /// others. <see cref="TimeSpan.Zero"/> or <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> for until it's dismissed
  /// </summary>
  public TimeSpan? CloseAfter { get; set; }
}
