using System;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Pixata.Blazor.TelerikComponents.Helpers;
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
    await InvokeStateCallback(OnStateInitPre, args);
    if (!string.IsNullOrWhiteSpace(StorageKey)) {
      try {
        GridState<TItem>? state = await LocalStorage.GetItemAsync<GridState<TItem>>(StorageKey);
        if (state is not null) {
          // Filter values and their member types don't survive the round trip through JSON, so they have to be put back
          // before the state is used, otherwise the grid will fall over when it renders the filter cells
          TelerikGridStateHelper.RepairFilterDescriptors(state);
          args.GridState = state;
        }
      }
      catch (Exception ex) {
        // No, we don't normally swallow exceptions, but as JS calls cannot be issued during pre-rendering, the local storage code will raise an exception. We can safely ignore it, as the grid state should be restored during the interactive rendering
      }
    }
    await InvokeStateCallback(OnStateInitPost, args);
  }

  private async Task HandleOnStateChanged(GridStateEventArgs<TItem> args) {
    await InvokeStateCallback(OnStateChangedPre, args);
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
    await InvokeStateCallback(OnStateChangedPost, args);
  }

  // The grid raises its state events while the page is still being pre-rendered, when JS calls cannot be issued. Our own local
  // storage calls have always been wrapped in a try/catch for that reason (see above), but until version 12.3.20 these callbacks
  // weren't, so a handler that used local storage (or anything else needing JS) brought the whole page down. It only did so on a
  // full page load, as navigating to the page from elsewhere in the app skips pre-rendering, which made it look intermittent
  private static async Task InvokeStateCallback(EventCallback<GridStateEventArgs<TItem>> callback, GridStateEventArgs<TItem> args) {
    if (!callback.HasDelegate) {
      return;
    }
    try {
      await callback.InvokeAsync(args);
    }
    catch (InvalidOperationException) {
      // Only InvalidOperationException is swallowed, as that is what a JS call during pre-rendering throws. Anything else the
      // handler throws is a genuine problem in the consuming code, so it is left to bubble up. Both events are raised again
      // during the interactive render, when JS is available, so a handler skipped here does get another go
    }
  }
}