using Microsoft.AspNetCore.Components;
using System;
using System.Linq;
using System.Threading.Tasks;
using Telerik.Blazor.Components;
using Telerik.DataSource;

namespace Pixata.Blazor.TelerikComponents.Components;

public class TelerikGridWithSettings<TItem> : TelerikGrid<TItem> {
  // Parameter to control where state is persisted
  public enum StatePersistenceMode {
    None,
    LocalStorage,
    QueryString
  }

  public StatePersistenceMode PersistenceMode { get; set; } = StatePersistenceMode.None;

  // Allow user to provide their own event handlers. This requires them to remember NOT to set the OnStateInit and OnStateChanged parameters, as that would override our internal handlers
  public EventCallback<GridStateEventArgs<TItem>> OnStateInitUser { get; set; }
  public EventCallback<GridStateEventArgs<TItem>> OnStateChangedUser { get; set; }

  protected override void OnInitialized() {
    OnStateInit = EventCallback.Factory.Create<GridStateEventArgs<TItem>>(this, HandleOnStateInit);
    OnStateChanged = EventCallback.Factory.Create<GridStateEventArgs<TItem>>(this, HandleOnStateChanged);
    base.OnInitialized();
  }

  private async Task HandleOnStateInit(GridStateEventArgs<TItem> args) {
    // Load state from local storage or query string if needed
    if (PersistenceMode == StatePersistenceMode.LocalStorage) {
      // TODO: Load state from local storage and apply to args.GridState
    } else if (PersistenceMode == StatePersistenceMode.QueryString) {
      // TODO: Load state from query string and apply to args.GridState
    }
    // Call user-supplied handler if set
    if (OnStateInitUser.HasDelegate) {
      await OnStateInitUser.InvokeAsync(args);
    }
  }

  private async Task HandleOnStateChanged(GridStateEventArgs<TItem> args) {
    // Save state to local storage or query string if needed
    if (PersistenceMode == StatePersistenceMode.LocalStorage) {
      // TODO: Save args.GridState to local storage
    } else if (PersistenceMode == StatePersistenceMode.QueryString) {
      // TODO: Save args.GridState to query string
    }
    if (args.GridState.FilterDescriptors.Any()) {
      Console.WriteLine("State changed - filters:");
      foreach (CompositeFilterDescriptor cfd in args.GridState.FilterDescriptors.Cast<CompositeFilterDescriptor>()) {
        Console.WriteLine($"Logical Operator: {cfd.LogicalOperator}");
        foreach (FilterDescriptor fd in cfd.FilterDescriptors.Cast<FilterDescriptor>()) {
          Console.WriteLine($"Field: {fd.Member}, Operator: {fd.Operator}, Value: {fd.Value}");
        }
      }
    }
    // Call user-supplied handler if set
    if (OnStateChangedUser.HasDelegate) {
      await OnStateChangedUser.InvokeAsync(args);
    }
  }
}