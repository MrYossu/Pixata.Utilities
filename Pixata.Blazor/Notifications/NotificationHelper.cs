using System;

namespace Pixata.Blazor.Notifications;

public class NotificationHelper {
  public Action<Notification>? Receive { get; set; }

  public void Send(NotificationType type, string message, DateTime dateTime, Action? onClick = null) =>
    Receive?.Invoke(new() { Type = type, Message = message, DateTime = dateTime, Id = Guid.NewGuid().ToString(), OnClick = onClick });

  public static (string, string) GetIcon(NotificationType type) =>
    type switch {
      NotificationType.General => (GeneralIcon, "00b7d6"),
      NotificationType.Info => (InfoIcon, "00d6ce"),
      NotificationType.Success => (SuccessIcon, "00d67f"),
      NotificationType.Warning => (WarningIcon, "ffbe00"),
      NotificationType.Error => (ErrorIcon, "ff0000"),
      _ => (GeneralIcon, "00b7d6"),
    };

  public static string GeneralIcon = "far fa-bell";
  public static string InfoIcon = "far fa-info-circle";
  public static string SuccessIcon = "far fa-check";
  public static string WarningIcon = "far fa-exclamation-triangle";
  public static string ErrorIcon = "far fa-exclamation";
}

public enum NotificationType {
  General,
  Info,
  Success,
  Warning,
  Error,
  Payment,
}