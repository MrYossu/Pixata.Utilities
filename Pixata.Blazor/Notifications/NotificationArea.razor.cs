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
  [Inject]
  private ILogger<NotificationArea> Logger { get; set; } = null!;

  // Only ever changed inside InvokeAsync, so it's only touched on the renderer's synchronisation context
  private List<Notification> Notifications { get; } = [];

  // One per notification, so that dismissing a notification (or disposing the component) cancels its timers
  private readonly Dictionary<string, CancellationTokenSource> _timers = [];
  private readonly CancellationTokenSource _disposed = new();

  // Kept in a field so that Dispose can unsubscribe the same delegate
  private Action<Notification>? _receiveHandler;

  private const int FadeOutMs = 1900;

  protected override void OnInitialized() {
    _receiveHandler = notification => _ = OnNewNotification(notification);
    NotificationHelper.Receive += _receiveHandler;
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
        duplicates.AddRange(Notifications.Where(n => n != notification && n.Message == notification.Message && n.Show));
        StateHasChanged();
      });

      // A newer copy replaces any older ones with the same message
      duplicates.ForEach(d => _ = FadeOut(d));

      if (notification.Type != NotificationType.Error) {
        await Task.Delay(notification.Type == NotificationType.Success ? 5000 : 15000, cts.Token);
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
    if (_receiveHandler is not null) {
      NotificationHelper.Receive -= _receiveHandler;
    }
    _disposed.Cancel();
    foreach (CancellationTokenSource cts in _timers.Values) {
      cts.Dispose();
    }
    _timers.Clear();
    _disposed.Dispose();
  }
}
