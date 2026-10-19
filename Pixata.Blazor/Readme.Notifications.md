# Notifications

>Part of [Pixata.Blazor](Readme.md)

A simple toast-style notification system - a service you send messages to, and a component that displays them. There is a [live demo](https://test.pixata.co.uk/Notifications) on the sample site.

## Setup

`NotificationHelper` is registered for you by [`AddPixataBlazor()`](Readme.md#registering-dependencies).

Put a `NotificationArea` somewhere that is always on screen, typically in your layout, and pass it the injected helper...

```xml
@inject NotificationHelper NotificationHelper

<NotificationArea NotificationHelper="NotificationHelper" />
```

The icons are built in, so you don't need an icon font, and the notifications follow your theme's colours. See [icons and colours](#icons-and-colours) below.

## Sending a notification

Inject the helper wherever you need it and call `Send()`...

```csharp
NotificationHelper.Send(NotificationType.Success, "Your changes have been saved", DateTime.Now);
```

The types are `General`, `Info`, `Success`, `Warning`, `Error` and `Payment`. Each one gets its own icon and colour.

You can pass an optional `Action` as the last parameter, which is run if the user clicks the notification...

```csharp
NotificationHelper.Send(NotificationType.Info, "A new order has come in", DateTime.Now,
                        () => NavigationManager.NavigateTo(RoutesHelper.Orders));
```

## Formatted messages

If plain text isn't enough, say for a list of validation problems, or to bold a reference, you can send a render fragment instead of a string...

```razor
NotificationHelper.Send(NotificationType.Error, @<div>
  <p>The order couldn't be saved...</p>
  <ul>
    @foreach (string problem in problems) {
      <li>@problem</li>
    }
  </ul>
</div>, DateTime.Now);
```

...or, from C# code, some HTML as a `MarkupString`...

```csharp
NotificationHelper.Send(NotificationType.Success, new MarkupString($"Order <strong>{WebUtility.HtmlEncode(order.Reference)}</strong> has been saved"), DateTime.Now);
```

The HTML is shown as it is, so encode anything in it that came from a user, as above. A notification sent as a render fragment isn't removed when an identical one arrives, as there's no text to compare. Both overloads were added in v5.4.0.

The `NotificationArea` also has an `OpenNotification` event callback, which is raised with the `Notification` when one is clicked, in case you would rather handle that in one place than pass an action to every call.

## How long they stay

This is deliberately not configurable, as the whole point is that the user doesn't have to dismiss them...

| Type | Behaviour |
| --- | --- |
| `Error` | Stays until the user dismisses it |
| `Success` | Fades away after about five seconds |
| Everything else | Fades away after about fifteen seconds |

Every notification has a close button, so the user can get rid of one early. If a second notification arrives with the same message as one already on screen, the older one is removed, so a repeated error doesn't stack up.

As of v4.5.0, dismissing a notification cancels its timers, notifications sent in quick succession (a double-clicked save button, or a list of failures) are handled safely, and the area unsubscribes from the helper when it's disposed. Previously, removing a notification that had already gone threw an exception which, in Blazor Server, ended the circuit. Clicking the close button no longer counts as clicking the notification, and `OpenNotification` is now raised as described above.

## Icons and colours

As of v5.5.0, the notifications draw their own icons (a bell, an "i" in a circle, a tick, a warning triangle and an exclamation mark) as inline SVG, so you don't need an icon font, and nothing is loaded from another site, which matters if you have a strict Content Security Policy. If you'd rather have Font Awesome's icons, which were the default before v5.5.0, call this once in `Program.cs`...

```csharp
NotificationHelper.UseFontAwesomeIcons();
```

That sets the icons to Font Awesome's solid ones (`fas fa-check` and so on), which are in the free set of both Font Awesome 5 and 6. To use other icons from an icon font, set the classes yourself. They are static fields on `NotificationHelper` (`GeneralIcon`, `InfoIcon`, `SuccessIcon`, `WarningIcon` and `ErrorIcon`). A field left empty (the default) shows the built-in icon...

```csharp
NotificationHelper.SuccessIcon = "fa-regular fa-circle-check";
```

If you show notifications in a component of your own, `<PixataIcon Icon="@NotificationHelper.GetDisplayIcon(type)" />` shows the same icon as the notification area, and `NotificationHelper.GetColourClass(type)` gives the class that colours the circle behind it.

The toast, and the circle behind each icon, take their colours from variables in pixata.css, so you can change them to suit your palette...

```css
:root {
  --pixata-notification-success-bg: #198754;
  --pixata-notification-error-bg: var(--pixata-danger);
}
```

| Variable | What it sets | Default |
| --- | --- | --- |
| `--pixata-notification-bg` | The toast's background | `--pixata-blazor-surface-bg` (white) |
| `--pixata-notification-color` | The toast's text, and its close button | `--pixata-blazor-text-color` |
| `--pixata-notification-border-radius` | The toast's corners | `--pixata-border-radius` |
| `--pixata-notification-shadow` | The toast's shadow | A soft grey shadow |
| `--pixata-notification-icon-color` | The icon itself | White |
| `--pixata-notification-general-bg`, `--pixata-notification-info-bg`, `--pixata-notification-success-bg`, `--pixata-notification-warning-bg` and `--pixata-notification-error-bg` | The circle behind the icon, for each type. `Payment` notifications use the general colour | Blue, turquoise, green, amber and red |

The toast follows the components' surface and text colours by default, so if you've set `--pixata-blazor-surface-bg` and `--pixata-blazor-text-color` for a dark theme, the notifications pick them up. The close button's cross is drawn in the colour of the text around it, so it shows on a dark background as well as a light one. You can set it separately with `--pixata-close-button-color`.

The notification area's classes all start with `pixata-blazor-notification`, so your own styles (eg for a class called `message`) can't land on them by accident...

| Class | The element |
| --- | --- |
| `pixata-blazor-notification-area` | The area at the top right of the window that holds the notifications |
| `pixata-blazor-notification` | One notification. It also has `pixata-blazor-notification-new` while it's showing, and `pixata-blazor-notification-remove` while it fades out |
| `pixata-blazor-notification-icon` | The circle around the icon, along with the colour class for its type, eg `pixata-blazor-notification-success` |
| `pixata-blazor-notification-message` | The message |
| `pixata-blazor-notification-dismiss` | The close button's container |

>**Changed in v5.5.0.** The toast's background was always white, its close button was always black, and the default icons needed Font Awesome, so the notifications didn't suit a dark theme or a site without Font Awesome. The inner classes were `notifications-area`, `notification`, `new`, `remove`, `icon`, `message` and `dismiss`, and are now the ones in the table above. The styles have moved from the component's own scoped CSS into pixata.css. If you want Font Awesome's icons back, call `UseFontAwesomeIcons()` as above.

>**Changed in v5.4.0.** The default icons used to be the regular (`far`) ones, most of which are only in Font Awesome Pro, so with the free set you got a placeholder. The colours were hard-coded, so `NotificationHelper.GetIcon()`, which returned them, is now obsolete. Use `GetIconClass()` for the icon's classes, and `GetColourClass()` for the class that colours it. A long message also no longer runs under the close button.

## Using them for API errors

An [`ApiResponseView`](Readme.ApiResponseView.md) can report its errors as notifications rather than as an inline message. Set this once in `Program.cs`...

```csharp
ApiResponseViewConfig.FeedbackType = ApiResponseViewFeedbackType.Notifications;
```

...and the view renders its own `NotificationArea`, so you don't need to add one to the page.
