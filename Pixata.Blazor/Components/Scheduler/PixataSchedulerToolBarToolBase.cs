using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Components;

/// <summary>
/// The base for the tools that go in a scheduler's <c>SchedulerToolBar</c>
/// </summary>
public abstract class PixataSchedulerToolBarToolBase<TItem> : ComponentBase, IHandleEvent {
  [CascadingParameter]
  private protected PixataScheduler<TItem>? Scheduler { get; set; }

  /// <summary>
  /// The scheduler the tool is in
  /// </summary>
  private protected PixataScheduler<TItem> Owner =>
    Scheduler ?? throw new InvalidOperationException($"{GetType().Name.Split('`')[0]} must be inside a PixataScheduler's SchedulerToolBar");

  protected override void OnInitialized() =>
    _ = Owner;

  // The tools change the scheduler's state, so the scheduler handles their events and renders again (and the tools with it)
  Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) =>
    Scheduler is IHandleEvent handler ? handler.HandleEventAsync(callback, arg) : callback.InvokeAsync(arg);
}
