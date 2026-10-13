using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Pixata.Blazor.Components;

/// <summary>
/// Holds the chart's category axis. Put it inside a <see cref="PixataChart"/>, and put a
/// <see cref="PixataChartCategoryAxis"/> inside it
/// </summary>
public class PixataChartCategoryAxes : ComponentBase {
  [Parameter]
  public RenderFragment? ChildContent { get; set; }

  protected override void BuildRenderTree(RenderTreeBuilder builder) =>
    builder.AddContent(0, ChildContent);
}

/// <summary>
/// Holds the chart's value axes. Put it inside a <see cref="PixataChart"/>, and put one or more
/// <see cref="PixataChartValueAxis"/> inside it
/// </summary>
public class PixataChartValueAxes : ComponentBase {
  [Parameter]
  public RenderFragment? ChildContent { get; set; }

  protected override void BuildRenderTree(RenderTreeBuilder builder) =>
    builder.AddContent(0, ChildContent);
}

/// <summary>
/// The settings shared by the category and value axes
/// </summary>
public abstract class PixataChartAxisBase : PixataChartSettingsBase {
  /// <summary>
  /// The axis's name, which series use (in their <c>Axis</c> parameter) to say which value axis they are plotted against
  /// </summary>
  [Parameter]
  public string? Name { get; set; }

  /// <summary>
  /// The colour of the axis line and labels, as any CSS colour
  /// </summary>
  [Parameter]
  public string? Color { get; set; }

  /// <summary>
  /// Whether the axis is shown
  /// </summary>
  [Parameter]
  public bool? Visible { get; set; }

  /// <summary>
  /// On the category axis, the category indexes at which each value axis crosses it, in the order of the value axes. An index
  /// at or past the number of categories puts that value axis on the right, so <c>new object[] { 0, 100 }</c> puts the first
  /// value axis on the left and the second on the right
  /// </summary>
  [Parameter]
  public object[]? AxisCrossingValue { get; set; }

  [Parameter]
  public RenderFragment? ChildContent { get; set; }

  internal PixataChartAxisTitleBase? Title { get; set; }

  internal PixataChartAxisLabelsBase? Labels { get; set; }

  internal PixataChartGridLinesBase? MajorGridLines { get; set; }

  internal PixataChartGridLinesBase? MinorGridLines { get; set; }

  internal bool IsVisible => Visible ?? true;

  protected override void BuildRenderTree(RenderTreeBuilder builder) =>
    RenderCascading(builder, this, ChildContent);
}

/// <summary>
/// The axis along the bottom of a column or line chart, showing the categories. Put it inside
/// <see cref="PixataChartCategoryAxes"/>
/// </summary>
public class PixataChartCategoryAxis : PixataChartAxisBase {
  /// <summary>
  /// The categories, for series without a <c>CategoryField</c>. Series with a <c>CategoryField</c> add their categories
  /// after these
  /// </summary>
  [Parameter]
  public object[]? Categories { get; set; }

  /// <summary>
  /// Puts the first and last line points on the edges of the chart, rather than in the middle of their categories. Ignored
  /// when there are column series
  /// </summary>
  [Parameter]
  public bool? Justified { get; set; }

  internal override void Attach() =>
    Chart?.AddCategoryAxis(this);

  internal override void Detach() =>
    Chart?.RemoveCategoryAxis(this);
}

/// <summary>
/// An axis up the side of a column or line chart, showing values. Put one or more inside <see cref="PixataChartValueAxes"/>
/// </summary>
public class PixataChartValueAxis : PixataChartAxisBase {
  /// <summary>
  /// The lowest value on the axis. By default it is worked out from the data, and includes zero
  /// </summary>
  [Parameter]
  public double? Min { get; set; }

  /// <summary>
  /// The highest value on the axis. By default it is worked out from the data
  /// </summary>
  [Parameter]
  public double? Max { get; set; }

  /// <summary>
  /// The interval between the labels and major grid lines. By default it is worked out from the range and the height
  /// </summary>
  [Parameter]
  public double? MajorUnit { get; set; }

  /// <summary>
  /// The interval between the minor grid lines. A fifth of <see cref="MajorUnit"/> by default
  /// </summary>
  [Parameter]
  public double? MinorUnit { get; set; }

  /// <summary>
  /// Lets the automatic range start above zero when all the values are well above it
  /// </summary>
  [Parameter]
  public bool? NarrowRange { get; set; }

  internal override void Attach() =>
    Chart?.AddValueAxis(this);

  internal override void Detach() =>
    Chart?.RemoveValueAxis(this);
}

/// <summary>
/// The settings shared by the axis titles
/// </summary>
public abstract class PixataChartAxisTitleBase : PixataChartSettingsBase {
  [CascadingParameter]
  protected PixataChartAxisBase? Axis { get; set; }

  [Parameter]
  public string? Text { get; set; }

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

  [Parameter]
  public bool? Visible { get; set; }

  internal override void Attach() {
    if (Axis is not null) {
      Axis.Title = this;
    }
  }

  internal override void Detach() {
    if (Axis?.Title == this) {
      Axis.Title = null;
    }
  }
}

/// <summary>
/// The category axis's title. Put it inside a <see cref="PixataChartCategoryAxis"/>
/// </summary>
public class PixataChartCategoryAxisTitle : PixataChartAxisTitleBase {
}

/// <summary>
/// A value axis's title. Put it inside a <see cref="PixataChartValueAxis"/>
/// </summary>
public class PixataChartValueAxisTitle : PixataChartAxisTitleBase {
}

/// <summary>
/// The settings shared by the axis labels
/// </summary>
public abstract class PixataChartAxisLabelsBase : PixataChartSettingsBase {
  [CascadingParameter]
  protected PixataChartAxisBase? Axis { get; set; }

  /// <summary>
  /// The format of the labels, either a composite format such as <c>{0:C0}</c> or a plain one such as <c>N0</c>
  /// </summary>
  [Parameter]
  public string? Format { get; set; }

  /// <summary>
  /// The text of each label, with the placeholder <c>#= value #</c> (and, for categories with a data item,
  /// <c>#= dataItem.Property #</c>), or a formatted one such as <c>#= format('{0:N1}', value) #</c>
  /// </summary>
  [Parameter]
  public string? Template { get; set; }

  [Parameter]
  public bool? Visible { get; set; }

  /// <summary>
  /// Shows every nth label. By default, category labels are thinned out automatically when they don't fit
  /// </summary>
  [Parameter]
  public double? Step { get; set; }

  /// <summary>
  /// The number of labels to leave out at the start
  /// </summary>
  [Parameter]
  public double? Skip { get; set; }

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

  [Parameter]
  public RenderFragment? ChildContent { get; set; }

  internal PixataChartAxisLabelsRotationBase? Rotation { get; set; }

  internal override void Attach() {
    if (Axis is not null) {
      Axis.Labels = this;
    }
  }

  internal override void Detach() {
    if (Axis?.Labels == this) {
      Axis.Labels = null;
    }
  }

  protected override void BuildRenderTree(RenderTreeBuilder builder) =>
    RenderCascading(builder, this, ChildContent);
}

/// <summary>
/// The category axis's labels. Put it inside a <see cref="PixataChartCategoryAxis"/>
/// </summary>
public class PixataChartCategoryAxisLabels : PixataChartAxisLabelsBase {
}

/// <summary>
/// A value axis's labels. Put it inside a <see cref="PixataChartValueAxis"/>
/// </summary>
public class PixataChartValueAxisLabels : PixataChartAxisLabelsBase {
}

/// <summary>
/// The rotation of axis labels
/// </summary>
public abstract class PixataChartAxisLabelsRotationBase : PixataChartSettingsBase {
  [CascadingParameter]
  protected PixataChartAxisLabelsBase? Labels { get; set; }

  /// <summary>
  /// The angle in degrees (negative is anticlockwise), or <c>"auto"</c> (the default for category labels), which turns
  /// the labels to -45 degrees when they don't fit side by side
  /// </summary>
  [Parameter]
  public object? Angle { get; set; }

  internal override void Attach() {
    if (Labels is not null) {
      Labels.Rotation = this;
    }
  }

  internal override void Detach() {
    if (Labels?.Rotation == this) {
      Labels.Rotation = null;
    }
  }
}

/// <summary>
/// The rotation of the category axis's labels. Put it inside a <see cref="PixataChartCategoryAxisLabels"/>
/// </summary>
public class PixataChartCategoryAxisLabelsRotation : PixataChartAxisLabelsRotationBase {
}

/// <summary>
/// The rotation of a value axis's labels. Put it inside a <see cref="PixataChartValueAxisLabels"/>
/// </summary>
public class PixataChartValueAxisLabelsRotation : PixataChartAxisLabelsRotationBase {
}

/// <summary>
/// The settings shared by the grid lines
/// </summary>
public abstract class PixataChartGridLinesBase : PixataChartSettingsBase {
  [CascadingParameter]
  protected PixataChartAxisBase? Axis { get; set; }

  [Parameter]
  public bool? Visible { get; set; }

  /// <summary>
  /// The colour of the lines, as any CSS colour
  /// </summary>
  [Parameter]
  public string? Color { get; set; }

  /// <summary>
  /// The width of the lines, in pixels
  /// </summary>
  [Parameter]
  public double? Width { get; set; }

  [Parameter]
  public PixataChartDashType? DashType { get; set; }

  /// <summary>
  /// Draws every nth line
  /// </summary>
  [Parameter]
  public double? Step { get; set; }

  /// <summary>
  /// The number of lines to leave out at the start
  /// </summary>
  [Parameter]
  public double? Skip { get; set; }

  internal abstract bool IsMajor { get; }

  internal override void Attach() {
    if (Axis is null) {
      return;
    }
    if (IsMajor) {
      Axis.MajorGridLines = this;
    } else {
      Axis.MinorGridLines = this;
    }
  }

  internal override void Detach() {
    if (Axis?.MajorGridLines == this) {
      Axis.MajorGridLines = null;
    }
    if (Axis?.MinorGridLines == this) {
      Axis.MinorGridLines = null;
    }
  }
}

/// <summary>
/// The vertical lines between the categories, shown by default. Put it inside a <see cref="PixataChartCategoryAxis"/>
/// </summary>
public class PixataChartCategoryAxisMajorGridLines : PixataChartGridLinesBase {
  internal override bool IsMajor => true;
}

/// <summary>
/// Vertical lines through the middle of the categories, hidden by default. Put it inside a
/// <see cref="PixataChartCategoryAxis"/>
/// </summary>
public class PixataChartCategoryAxisMinorGridLines : PixataChartGridLinesBase {
  internal override bool IsMajor => false;
}

/// <summary>
/// The horizontal lines at each major unit, shown by default. Put it inside a <see cref="PixataChartValueAxis"/>
/// </summary>
public class PixataChartValueAxisMajorGridLines : PixataChartGridLinesBase {
  internal override bool IsMajor => true;
}

/// <summary>
/// The horizontal lines at each minor unit, hidden by default. Put it inside a <see cref="PixataChartValueAxis"/>
/// </summary>
public class PixataChartValueAxisMinorGridLines : PixataChartGridLinesBase {
  internal override bool IsMajor => false;
}
