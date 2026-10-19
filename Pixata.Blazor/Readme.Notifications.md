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

The icons use Font Awesome classes, so you'll need Font Awesome loaded for them to show. See [icons and colours](#icons-and-colours) below.

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

The HTML is shown as it is, so encode anything in it that came from a user, as above. A notification sent as a render fragment isn't removed when an identical one arrives, as there's no text to compare. Both overloads were added in v6.0.0.

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

The default icons are Font Awesome's solid ones (`fas fa-check` and so on), which are in the free set of both Font Awesome 5 and 6. If you use a different icon set, or prefer other icons, the class names are static fields on `NotificationHelper` (`GeneralIcon`, `InfoIcon`, `SuccessIcon`, `WarningIcon` and `ErrorIcon`), so you can change them once in `Program.cs`...

```csharp
NotificationHelper.SuccessIcon = "fa-regular fa-circle-check";
```

The colour behind each icon comes from a variable in pixata.css, so you can change them to suit your palette...

```css
:root {
  --pixata-notification-success-bg: #198754;
  --pixata-notification-error-bg: var(--pixata-danger);
}
```

The variables are `--pixata-notification-general-bg`, `--pixata-notification-info-bg`, `--pixata-notification-success-bg`, `--pixata-notification-warning-bg` and `--pixata-notification-error-bg`, and `--pixata-notification-icon-color` sets the colour of the icon itself. `Payment` notifications use the general colour.

>**Changed in v6.0.0.** The default icons used to be the regular (`far`) ones, most of which are only in Font Awesome Pro, so with the free set you got a placeholder. The colours were hard-coded, so `NotificationHelper.GetIcon()`, which returned them, is now obsolete. Use `GetIconClass()` for the icon's classes, and `GetColourClass()` for the class that colours it. A long message also no longer runs under the close button.

## Using them for API errors

An [`ApiResponseView`](Readme.ApiResponseView.md) can report its errors as notifications rather than as an inline message. Set this once in `Program.cs`...

```csharp
ApiResponseViewConfig.FeedbackType = ApiResponseViewFeedbackType.Notifications;
```

...and the view renders its own `NotificationArea`, so you don't need to add one to the page.
