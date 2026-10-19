using System;
using Microsoft.AspNetCore.Components;
using Pixata.Blazor.Components;

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
  /// The classes for a notification type's icon, from <see cref="GeneralIcon"/> and the other fields below. This is <c>""</c> for a type
  /// that uses the built-in icon, which is all of them unless you've set the fields. To show the icon, use <see cref="GetDisplayIcon"/>
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
  /// What to pass to <see cref="PixataIcon"/> to show a notification type's icon. This is the classes you've set in <see cref="GeneralIcon"/>
  /// or the other fields below, or the built-in SVG icon if you haven't set any, so the icons don't need an icon font
  /// </summary>
  public static object GetDisplayIcon(NotificationType type) =>
    GetIconClass(type) is { Length: > 0 } classes
      ? classes
      : type switch {
        NotificationType.Info => PixataSvgIcons.Info,
        NotificationType.Success => PixataSvgIcons.Check,
        NotificationType.Warning => PixataSvgIcons.Warning,
        NotificationType.Error => PixataSvgIcons.Exclamation,
        _ => PixataSvgIcons.Bell
      };

  /// <summary>
  /// Shows Font Awesome's icons rather than the built-in ones, by setting <see cref="GeneralIcon"/> and the other fields below to Font
  /// Awesome's solid icons, which are in the free set of both Font Awesome 5 and 6. These were the default icons before v5.5.0. Call it once,
  /// eg in <c>Program.cs</c>
  /// </summary>
  public static void UseFontAwesomeIcons() {
    GeneralIcon = "fas fa-bell";
    InfoIcon = "fas fa-info-circle";
    SuccessIcon = "fas fa-check";
    WarningIcon = "fas fa-exclamation-triangle";
    ErrorIcon = "fas fa-exclamation";
  }

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

  // The classes for an icon font's icons, eg "fas fa-check". Left empty, the built-in SVG icon is shown, so no icon font is needed. See
  // UseFontAwesomeIcons()
  public static string GeneralIcon = "";
  public static string InfoIcon = "";
  public static string SuccessIcon = "";
  public static string WarningIcon = "";
  public static string ErrorIcon = "";
}

public enum NotificationType {
  General,
  Info,
  Success,
  Warning,
  Error,
  Payment,
}