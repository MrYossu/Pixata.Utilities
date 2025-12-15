using System;

namespace Pixata.Blazor.Notifications;

public class Notification {
  public string Message { get; set; } = "";
  public NotificationType Type { get; set; }
  public DateTime DateTime { get; set; } = DateTime.Now;
  public string Id { get; set; } = "";
  public bool Show { get; set; } = true;
  public Action? OnClick { get; set; }
}
