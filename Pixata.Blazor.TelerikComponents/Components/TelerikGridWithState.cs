using System;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor.Components;

namespace Pixata.Blazor.TelerikComponents.Components;

public class TelerikGridWithState<TItem> : TelerikGrid<TItem> {
  [Inject]
  public ILocalStorageService LocalStorage { get; set; } = null!;

  [Parameter]
  public string StorageKey { get; set; } = "";

  // Allow user to provide their own event handlers. This requires them to remember NOT to set the OnStateInit and OnStateChanged parameters, as that would override our internal handlers
  [Parameter]
  public EventCallback<GridStateEventArgs<TItem>> OnStateInitPre { get; set; }
  [Parameter]
  public EventCallback<GridStateEventArgs<TItem>> OnStateInitPost { get; set; }
  [Parameter]
  public EventCallback<GridStateEventArgs<TItem>> OnStateChangedPre { get; set; }
  [Parameter]
  public EventCallback<GridStateEventArgs<TItem>> OnStateChangedPost { get; set; }

  protected override void OnInitialized() {
    OnStateInit = EventCallback.Factory.Create<GridStateEventArgs<TItem>>(this, HandleOnStateInit);
    OnStateChanged = EventCallback.Factory.Create<GridStateEventArgs<TItem>>(this, HandleOnStateChanged);
    base.OnInitialized();
  }

  private async Task HandleOnStateInit(GridStateEventArgs<TItem> args) {
    if (OnStateInitPre.HasDelegate) {
      await OnStateInitPre.InvokeAsync(args);
    }
    if (!string.IsNullOrWhiteSpace(StorageKey)) {
      try {
        GridState<TItem>? state = await LocalStorage.GetItemAsync<GridState<TItem>>(StorageKey);
        if (state is not null) {
          args.GridState = state;
        }
      }
      catch (Exception ex) {
        // No, we don't normally swallow exceptions, but as JS calls cannot be issued during pre-rendering, the local storage code will raise an exception. We can safely ignore it, as the grid state should be restored during the interactive rendering
      }
    }
    if (OnStateInitPost.HasDelegate) {
      await OnStateInitPost.InvokeAsync(args);
    }
  }

  private async Task HandleOnStateChanged(GridStateEventArgs<TItem> args) {
    if (OnStateChangedPre.HasDelegate) {
      await OnStateChangedPre.InvokeAsync(args);
    }
    if (!string.IsNullOrWhiteSpace(StorageKey)) {
      GridState<TItem>? state = GetState();
      try {
        if (state is not null) {
          await LocalStorage.SetItemAsync(StorageKey, state);
        }
      }
      catch (Exception ex) {
        // See the comments above about why we swallow this exception
      }
    }
    if (OnStateChangedPost.HasDelegate) {
      await OnStateChangedPost.InvokeAsync(args);
    }
  }
}