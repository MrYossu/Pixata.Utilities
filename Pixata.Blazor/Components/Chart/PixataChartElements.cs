using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Components;

/// <summary>
/// The chart's title. Put it inside a <see cref="PixataChart"/>
/// </summary>
public class PixataChartTitle : PixataChartSettingsBase {
  [Parameter]
  public string? Text { get; set; }

  /// <summary>
  /// Whether the title goes above (the default) or below the chart
  /// </summary>
  [Parameter]
  public PixataChartTitlePosition? Position { get; set; }

  /// <summary>
  /// How the title lines up. Centred by default
  /// </summary>
  [Parameter]
  public PixataChartTitleAlign? Align { get; set; }

  /// <summary>
  /// The text colour, as any CSS colour
  /// </summary>
  [Parameter]
  public string? Color { get; set; }

  /// <summary>
  /// The background colour, as any CSS colour
  /// </summary>
  [Parameter]
  public string? Background { get; set; }

  /// <summary>
  /// The font, in CSS <c>font</c> shorthand, eg <c>bold 18px sans-serif</c>
  /// </summary>
  [Parameter]
  public string? Font { get; set; }

  [Parameter]
  public bool? Visible { get; set; }

  /// <summary>
  /// A longer description of the chart, read out by screen readers
  /// </summary>
  [Parameter]
  public string? Description { get; set; }

  internal override void Attach() {
    if (Chart is not null) {
      Chart.Title = this;
    }
  }

  internal override void Detach() {
    if (Chart?.Title == this) {
      Chart.Title = null;
    }
  }
}

/// <summary>
/// The chart's legend, which lists the series (or, for a pie, its segments). Clicking or tapping an item shows or hides
/// its series. Put it inside a <see cref="PixataChart"/>. Without one, the legend is shown on the right
/// </summary>
public class PixataChartLegend : PixataChartSettingsBase {
  /// <summary>
  /// Where the legend goes. On the right by default. On a narrow chart, a legend on the left or right moves below it
  /// </summary>
  [Parameter]
  public PixataChartLegendPosition? Position { get; set; }

  [Parameter]
  public bool? Visible { get; set; }

  /// <summary>
  /// How the items line up along the legend. Centred by default
  /// </summary>
  [Parameter]
  public PixataChartLegendAlign? Align { get; set; }

  /// <summary>
  /// Whether the items go across or down. By default they go across in a legend above or below the chart, and down in one
  /// to the side
  /// </summary>
  [Parameter]
  public PixataChartLegendOrientation? Orientation { get; set; }

  /// <summary>
  /// The background colour, as any CSS colour
  /// </summary>
  [Parameter]
  public string? Background { get; set; }

  /// <summary>
  /// Lists the items in reverse order
  /// </summary>
  [Parameter]
  public bool? Reverse { get; set; }

  internal override void Attach() {
    if (Chart is not null) {
      Chart.Legend = this;
    }
  }

  internal override void Detach() {
    if (Chart?.Legend == this) {
      Chart.Legend = null;
    }
  }
}

/// <summary>
/// The tooltip shown when the user hovers over, taps or moves the keyboard focus to a data point. Tooltips are off unless
/// <see cref="Visible"/> is true. Put it inside a <see cref="PixataChart"/>; a <see cref="PixataChartSeriesTooltip"/>
/// overrides it for one series
/// </summary>
public class PixataChartTooltip : PixataChartSettingsBase {
  [Parameter]
  public bool? Visible { get; set; }

  /// <summary>
  /// Shows one tooltip for the whole category, listing the value of every series in it, rather than one for the point
  /// </summary>
  [Parameter]
  public bool? Shared { get; set; }

  /// <summary>
  /// The background colour, as any CSS colour. By default a tooltip takes the colour of its point
  /// </summary>
  [Parameter]
  public string? Background { get; set; }

  /// <summary>
  /// The text colour, as any CSS colour
  /// </summary>
  [Parameter]
  public string? Color { get; set; }

  /// <summary>
  /// The font, in CSS <c>font</c> shorthand
  /// </summary>
  [Parameter]
  public string? Font { get; set; }

  /// <summary>
  /// The format of the value, either a composite format such as <c>{0:C}</c> or a plain one such as <c>N2</c>
  /// </summary>
  [Parameter]
  public string? Format { get; set; }

  /// <summary>
  /// The content of a (non-shared) tooltip
  /// </summary>
  [Parameter]
  public RenderFragment<PixataChartSeriesTooltipTemplateContext>? Template { get; set; }

  /// <summary>
  /// The content of a shared tooltip
  /// </summary>
  [Parameter]
  public RenderFragment<PixataChartSeriesTooltipSharedTemplateContext>? SharedTemplate { get; set; }

  internal override void Attach() {
    if (Chart is not null) {
      Chart.Tooltip = this;
    }
  }

  internal override void Detach() {
    if (Chart?.Tooltip == this) {
      Chart.Tooltip = null;
    }
  }
}

/// <summary>
/// What the chart shows over its plot area when none of its series has any data. Without one, the chart shows "No data".
/// Put it inside a <see cref="PixataChart"/>
/// </summary>
public class PixataChartNoDataTemplate : PixataChartSettingsBase {
  /// <summary>
  /// Whether anything is shown when there is no data. Set it to false to show the empty chart
  /// </summary>
  [Parameter]
  public bool? Visible { get; set; }

  /// <summary>
  /// The content shown in place of the default "No data"
  /// </summary>
  [Parameter]
  public RenderFragment? ChildContent { get; set; }

  internal override void Attach() {
    if (Chart is not null) {
      Chart.NoDataTemplate = this;
    }
  }

  internal override void Detach() {
    if (Chart?.NoDataTemplate == this) {
      Chart.NoDataTemplate = null;
    }
  }

  // The content usually shows the page's own state, so the chart draws it again whenever the page gives it new content
  protected override void OnParametersSet() {
    base.OnParametersSet();
    Chart?.Invalidate();
  }
}

/// <summary>
/// Content shown in the hole of a donut chart, such as a total. Put it inside a <see cref="PixataChart"/>
/// </summary>
public class PixataChartDonutCenterTemplate : PixataChartSettingsBase {
  [Parameter]
  public RenderFragment? ChildContent { get; set; }

  internal override void Attach() {
    if (Chart is not null) {
      Chart.DonutCenterTemplate = this;
    }
  }

  internal override void Detach() {
    if (Chart?.DonutCenterTemplate == this) {
      Chart.DonutCenterTemplate = null;
    }
  }

  // The content usually shows the page's own state, so the chart draws it again whenever the page gives it new content
  protected override void OnParametersSet() {
    base.OnParametersSet();
    Chart?.Invalidate();
  }
}
