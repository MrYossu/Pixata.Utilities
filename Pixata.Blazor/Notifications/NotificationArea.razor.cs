using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Pixata.Blazor.Notifications;

public partial class NotificationArea : IDisposable {
  [Parameter]
  public NotificationHelper NotificationHelper { get; set; } = null!;
  [Parameter]
  public EventCallback<Notification> OpenNotification { get; set; }

  /// <summary>
  /// When true, this area only shows notifications if there's no other area on the page without <c>Fallback</c>, and then only
  /// if it's the first fallback area. Each <see cref="Pixata.Blazor.Containers.ApiResponseView{T}"/> in <c>Notifications</c>
  /// mode has one of these, so a notification shows once, however many views there are, and not at all if the layout has
  /// its own area
  /// </summary>
  [Parameter]
  public bool Fallback { get; set; }
  [Inject]
  private ILogger<NotificationArea> Logger { get; set; } = null!;

  // Only ever changed inside InvokeAsync, so it's only touched on the renderer's synchronisation context
  private List<Notification> Notifications { get; } = [];

  // One per notification, so that dismissing a notification (or disposing the component) cancels its timers
  private readonly Dictionary<string, CancellationTokenSource> _timers = [];
  private readonly CancellationTokenSource _disposed = new();

  private const int FadeOutMs = 1900;
  private static readonly TimeSpan MaxCloseAfter = TimeSpan.FromMilliseconds(int.MaxValue);

  // The helper this area was added to, so Dispose removes it from the same one even if the parameter has changed since
  private NotificationHelper? _helper;

  protected override void OnParametersSet() {
    if (!ReferenceEquals(_helper, NotificationHelper)) {
      _helper?.RemoveArea(this);
      _helper = NotificationHelper;
      _helper.AddArea(this);
    }
  }

  internal void Show(Notification notification) =>
    _ = OnNewNotification(notification);

  internal void Hide(string id) =>
    _ = HideNotification(id);

  // Started from NotificationHelper.Remove, so like OnNewNotification it must never throw
  private async Task HideNotification(string id) {
    try {
      Notification? notification = null;
      // Not only one that's showing, as a notification in more than one area is one object, so another area fading it out has
      // already set its Show to false
      await InvokeAsync(() => notification = Notifications.FirstOrDefault(n => n.Id == id));
      if (notification is not null) {
        await FadeOut(notification);
      }
    }
    catch (ObjectDisposedException) {
      // The component was disposed
    }
    catch (Exception ex) {
      Logger.LogError(ex, "Error removing notification");
    }
  }

  // How long a notification stays, or null for until it's dismissed
  private static TimeSpan? CloseAfter(Notification notification) {
    TimeSpan? closeAfter = notification.CloseAfter ?? notification.Type switch {
      NotificationType.Error => null,
      NotificationType.Success => TimeSpan.FromSeconds(5),
      _ => TimeSpan.FromSeconds(15)
    };
    // Zero and Timeout.InfiniteTimeSpan (minus a millisecond) both mean until it's dismissed, and so does a time too long for
    // Task.Delay, which is near enough
    return closeAfter > TimeSpan.Zero && closeAfter <= MaxCloseAfter ? closeAfter : null;
  }

  // This is started from an Action, so it must never throw, or the exception would be unobserved (and in Blazor Server, an exception in
  // async void code ends the circuit)
  private async Task OnNewNotification(Notification notification) {
    try {
      CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(_disposed.Token);
      List<Notification> duplicates = [];
      await InvokeAsync(() => {
        _timers[notification.Id] = cts;
        Notifications.Insert(0, notification);
        // Notifications sent as a render fragment have no message to compare
        if (notification.Message != "") {
          duplicates.AddRange(Notifications.Where(n => n != notification && n.Message == notification.Message && n.Show));
        }
        StateHasChanged();
      });

      // A newer copy replaces any older ones with the same message
      duplicates.ForEach(d => _ = FadeOut(d));

      if (CloseAfter(notification) is TimeSpan closeAfter) {
        await Task.Delay(closeAfter, cts.Token);
        await FadeOut(notification);
      }
    }
    catch (OperationCanceledException) {
      // Dismissed by hand, replaced by a newer copy, or the component was disposed
    }
    catch (ObjectDisposedException) {
      // The component was disposed while a timer was running
    }
    catch (Exception ex) {
      Logger.LogError(ex, "Error showing notification");
    }
  }

  // Hides the notification (which starts its fade-out animation), then removes it. Any timer it already had is cancelled, so this is the only
  // thing left that will remove it
  private async Task FadeOut(Notification notification) {
    try {
      CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(_disposed.Token);
      bool stillShowing = false;
      await InvokeAsync(() => {
        if (!Notifications.Contains(notification)) {
          return;
        }
        stillShowing = true;
        if (_timers.Remove(notification.Id, out CancellationTokenSource? previous)) {
          previous.Cancel();
          previous.Dispose();
        }
        _timers[notification.Id] = cts;
        notification.Show = false;
        StateHasChanged();
      });
      if (!stillShowing) {
        cts.Dispose();
        return;
      }
      await Task.Delay(FadeOutMs, cts.Token);
      await InvokeAsync(() => {
        Remove(notification);
        StateHasChanged();
      });
    }
    catch (OperationCanceledException) {
    }
    catch (ObjectDisposedException) {
    }
    catch (Exception ex) {
      Logger.LogError(ex, "Error removing notification");
    }
  }

  // Removing a notification that has already gone does nothing
  private void Remove(Notification notification) {
    Notifications.Remove(notification);
    if (_timers.Remove(notification.Id, out CancellationTokenSource? cts)) {
      cts.Cancel();
      cts.Dispose();
    }
  }

  private async Task OpenFromNotification(Notification notification) {
    Dismiss(notification);
    notification.OnClick?.Invoke();
    await OpenNotification.InvokeAsync(notification);
  }

  private void Dismiss(Notification notification) =>
    Remove(notification);

  public void Dispose() {
    _helper?.RemoveArea(this);
    _helper = null;
    _disposed.Cancel();
    foreach (CancellationTokenSource cts in _timers.Values) {
      cts.Dispose();
    }
    _timers.Clear();
    _disposed.Dispose();
  }
}
