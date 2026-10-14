using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Forms {
  public partial class FormRowDropdownEnum<TEnum> : ComponentBase {
    [Parameter]
    public string PropertyName { get; set; } = null!;

    [Parameter]
    public string? Caption { get; set; }

    [Parameter]
    public string? Icon { get; set; }

    [Parameter]
    public TEnum Value { get; set; } = default!;

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    [Parameter]
    public RenderFragment<TEnum>? ChildContent { get; set; }
    /*
     You can use the ChildContent to be more specific about how you want the <option> elements rendering, eg...
     <FormRowDropdownEnum PropertyName="@nameof(Ach.Frequency)" @bind-Value="_ach.Frequency" Icon="wave-sine">
       <ChildContent Context="freq">
         @if (freq == _ach.Frequency) {
           <option value="@freq">@freq is fab</option>
         } else {
           <option value="@freq">@freq</option>
         }
       </ChildContent>
     </FormRowDropdownEnum>
     */

    [Parameter]
    public EventCallback<TEnum> ValueChanged { get; set; }

    // Only a change made in the dropdown is reported back. A new Value coming in from the parent is not echoed back to it
    private async Task OnValueChanged(TEnum value) {
      if (Equals(Value, value)) {
        return;
      }
      Value = value;
      await ValueChanged.InvokeAsync(value);
    }

    IEnumerable<TEnum> EnumValues => Enum.GetValues(typeof(TEnum)).Cast<TEnum>();

    protected override void OnInitialized() {
      if (Value is not Enum) {
        throw new ArgumentException("Value must be of type Enum");
      }
    }
  }
}