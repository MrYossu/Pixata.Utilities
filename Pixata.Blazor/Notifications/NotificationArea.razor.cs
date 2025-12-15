using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Pixata.Extensions;

namespace Pixata.Blazor.Notifications;

public partial class NotificationArea : IDisposable {
  [Parameter]
  public NotificationHelper NotificationHelper { get; set; } = null!;
  [Parameter]
  public EventCallback<Notification> OpenNotification { get; set; }
  private ObservableCollection<Notification> Notifications { get; set; } = [];

  protected override void OnInitialized() =>
    NotificationHelper.Receive += async notification => await OnNewNotification(notification);

  private async Task OnNewNotification(Notification notification) {
    Notifications.Insert(0, notification);
    List<string> notifsToRemove = [];
    Notifications.Where(n => n.Message == notification.Message && n != notification).ForEach(n => {
      n.Show = false;
      notifsToRemove.Add(n.Id);
    });
    await InvokeAsync(StateHasChanged);
    await Task.Delay(1900);
    notifsToRemove.ForEach(nr => Notifications.Remove(Notifications.First(n => n.Id == nr)));
    await InvokeAsync(StateHasChanged);
    if (notification.Type != NotificationType.Error) {
      await Task.Delay(notification.Type == NotificationType.Success ? 5000 : 15000);
      notification.Show = false;
      await InvokeAsync(StateHasChanged);
      await Task.Delay(1900);
      Notifications.Remove(notification);
      await InvokeAsync(StateHasChanged);
    }
  }

  private void OpenFromNotification(Notification notification) {
    notification.OnClick?.Invoke();
    Dismiss(notification);
  }

  private void Dismiss(Notification notification) =>
    Notifications.Remove(notification);

  public void Dispose() =>
    NotificationHelper.Receive -= async _ => await OnNewNotification(_);
}
