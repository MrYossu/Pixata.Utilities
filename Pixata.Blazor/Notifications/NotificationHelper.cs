using System;
using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Notifications;

public class NotificationHelper {
  public Action<Notification>? Receive { get; set; }

  public void Send(NotificationType type, string message, DateTime dateTime, Action? onClick = null) =>
    Receive?.Invoke(new() { Type = type, Message = message, DateTime = dateTime, Id = Guid.NewGuid().ToString(), OnClick = onClick });

  /// <summary>
  /// Sends a notification whose message is HTML, eg to bold a reference. The HTML is shown as it is, so encode anything in it that came
  /// from a user. The HTML is also put in the notification's <see cref="Notification.Message"/>
  /// </summary>
  public void Send(NotificationType type, MarkupString message, DateTime dateTime, Action? onClick = null) =>
    Receive?.Invoke(new() { Type = type, Message = message.Value, Content = builder => builder.AddContent(0, message), DateTime = dateTime, Id = Guid.NewGuid().ToString(), OnClick = onClick });

  /// <summary>
  /// Sends a notification whose message is a render fragment, eg a list of validation problems. As there's no text to compare, these
  /// aren't removed when an identical one arrives, as plain text ones are
  /// </summary>
  public void Send(NotificationType type, RenderFragment message, DateTime dateTime, Action? onClick = null) =>
    Receive?.Invoke(new() { Type = type, Content = message, DateTime = dateTime, Id = Guid.NewGuid().ToString(), OnClick = onClick });

  /// <summary>
  /// The classes for a notification type's icon, from <see cref="GeneralIcon"/> and the other fields below
  /// </summary>
  public static string GetIconClass(NotificationType type) =>
    type switch {
      NotificationType.Info => InfoIcon,
      NotificationType.Success => SuccessIcon,
      NotificationType.Warning => WarningIcon,
      NotificationType.Error => ErrorIcon,
      _ => GeneralIcon
    };

  /// <summary>
  /// The class that colours a notification type's icon, using the <c>--pixata-notification-*</c> variables in pixata.css
  /// </summary>
  public static string GetColourClass(NotificationType type) =>
    type switch {
      NotificationType.Info => "pixata-blazor-notification-info",
      NotificationType.Success => "pixata-blazor-notification-success",
      NotificationType.Warning => "pixata-blazor-notification-warning",
      NotificationType.Error => "pixata-blazor-notification-error",
      _ => "pixata-blazor-notification-general"
    };

  [Obsolete("The icons are now coloured by the --pixata-notification-* variables in pixata.css, so the colours returned here are only the defaults. Use GetIconClass() for the icon, and GetColourClass() for the colour")]
  public static (string, string) GetIcon(NotificationType type) =>
    type switch {
      NotificationType.General => (GeneralIcon, "00b7d6"),
      NotificationType.Info => (InfoIcon, "00d6ce"),
      NotificationType.Success => (SuccessIcon, "00d67f"),
      NotificationType.Warning => (WarningIcon, "ffbe00"),
      NotificationType.Error => (ErrorIcon, "ff0000"),
      _ => (GeneralIcon, "00b7d6"),
    };

  // Font Awesome's solid icons, which are in the free set of both Font Awesome 5 and 6. Most of the regular (far) ones are Pro only
  public static string GeneralIcon = "fas fa-bell";
  public static string InfoIcon = "fas fa-info-circle";
  public static string SuccessIcon = "fas fa-check";
  public static string WarningIcon = "fas fa-exclamation-triangle";
  public static string ErrorIcon = "fas fa-exclamation";
}

public enum NotificationType {
  General,
  Info,
  Success,
  Warning,
  Error,
  Payment,
}