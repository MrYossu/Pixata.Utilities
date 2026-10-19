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
}
