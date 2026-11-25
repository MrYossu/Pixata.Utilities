using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests;

/// <summary>
/// A page that shows a date picker (or range picker) bound to a date, and takes it off the page while it reloads after the
/// date changes, until <see cref="Reload"/> completes. Supported: <see cref="PixataDatePicker{TValue}"/>,
/// <see cref="PixataDateRangePicker{TValue}"/> and <see cref="PixataTimePicker{TValue}"/> of <see cref="DateTime"/>
/// </summary>
public class RemovedOnChangeHost<TPicker> : ComponentBase where TPicker : IComponent {
  [Parameter]
  public Task Reload { get; set; } = Task.CompletedTask;

  public DateTime Value { get; private set; } = new(2026, 10, 13);
  public DateTime End { get; private set; } = new(2026, 10, 20);
  public bool Loading { get; private set; }

  protected override void BuildRenderTree(RenderTreeBuilder builder) {
    if (Loading) {
      builder.AddMarkupContent(0, "<p>Loading...</p>");
      return;
    }
    builder.OpenComponent<TPicker>(1);
    if (typeof(TPicker) == typeof(PixataDateRangePicker<DateTime>)) {
      builder.AddAttribute(2, nameof(PixataDateRangePicker<DateTime>.StartValue), Value);
      builder.AddAttribute(3, nameof(PixataDateRangePicker<DateTime>.EndValue), End);
      builder.AddAttribute(4, nameof(PixataDateRangePicker<DateTime>.StartValueChanged), EventCallback.Factory.Create<DateTime>(this, v => Value = v));
      builder.AddAttribute(5, nameof(PixataDateRangePicker<DateTime>.EndValueChanged), EventCallback.Factory.Create<DateTime>(this, async v => {
        End = v;
        await ReloadData();
      }));
    } else {
      builder.AddAttribute(6, nameof(PixataDatePicker<DateTime>.Value), Value);
      builder.AddAttribute(7, nameof(PixataDatePicker<DateTime>.ValueChanged), EventCallback.Factory.Create<DateTime>(this, async v => {
        Value = v;
        await ReloadData();
      }));
    }
    builder.CloseComponent();
  }

  private async Task ReloadData() {
    Loading = true;
    StateHasChanged();
    await Reload;
    Loading = false;
  }
}
