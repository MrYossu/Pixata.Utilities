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

The icons use Font Awesome classes, so you'll need Font Awesome loaded for them to show. If you use a different icon set, the class names are static fields on `NotificationHelper` (`GeneralIcon`, `InfoIcon`, `SuccessIcon`, `WarningIcon` and `ErrorIcon`), so you can change them once in `Program.cs`.

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

The `NotificationArea` also has an `OpenNotification` event callback, which is raised with the `Notification` when one is clicked, in case you would rather handle that in one place than pass an action to every call.

## How long they stay

This is deliberately not configurable, as the whole point is that the user doesn't have to dismiss them...

| Type | Behaviour |
| --- | --- |
| `Error` | Stays until the user dismisses it |
| `Success` | Fades away after about five seconds |
| Everything else | Fades away after about fifteen seconds |

Every notification has a close button, so the user can get rid of one early. If a second notification arrives with the same message as one already on screen, the older one is removed, so a repeated error doesn't stack up.

## Using them for API errors

An [`ApiResponseView`](Readme.ApiResponseView.md) can report its errors as notifications rather than as an inline message. Set this once in `Program.cs`...

```csharp
ApiResponseViewConfig.FeedbackType = ApiResponseViewFeedbackType.Notifications;
```

...and the view renders its own `NotificationArea`, so you don't need to add one to the page.
