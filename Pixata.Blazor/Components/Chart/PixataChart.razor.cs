using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components;

/// <summary>
/// A chart drawn as SVG, with column, line, pie and donut series. Configure it with child components: the series go in
/// <see cref="PixataChartSeriesItems"/>, the axes in <see cref="PixataChartCategoryAxes"/> and
/// <see cref="PixataChartValueAxes"/>, plus an optional <see cref="PixataChartTitle"/>, <see cref="PixataChartLegend"/> and
/// <see cref="PixataChartTooltip"/>, <see cref="PixataChartNoDataTemplate"/> and <see cref="PixataChartDonutCenterTemplate"/>.
/// It resizes with its container
/// </summary>
public partial class PixataChart : IAsyncDisposable {
  internal static readonly string ModulePath = PixataBlazorScript.Versioned("Components/Chart/PixataChart.razor.js");

  /// <summary>
  /// Below this width (in pixels) the chart is laid out for a phone: a legend on the left or right moves underneath
  /// </summary>
  public const double CompactWidth = 400;

  [Inject]
  private IJSRuntime Js { get; set; } = null!;

  /// <summary>
  /// The width of the chart, as a CSS length. By default it fills its container
  /// </summary>
  [Parameter]
  public string? Width { get; set; }

  /// <summary>
  /// The height of the chart, as a CSS length. 400px by default (set by the <c>--pixata-blazor-chart-height</c> variable)
  /// </summary>
  [Parameter]
  public string? Height { get; set; }

  [Parameter]
  public string? Class { get; set; }

  [Parameter]
  public string? Id { get; set; }

  /// <summary>
  /// Whether the columns grow and the lines and segments fade in when the chart first appears. True by default
  /// </summary>
  [Parameter]
  public bool? Transitions { get; set; }

  /// <summary>
  /// The series, axes, title, legend and tooltip
  /// </summary>
  [Parameter]
  public RenderFragment? ChildContent { get; set; }

  /// <summary>
  /// Raised when the user clicks or taps a column, line point or pie or donut segment, or presses Enter or Space when the keyboard
  /// focus is on one
  /// </summary>
  [Parameter]
  public EventCallback<PixataChartSeriesClickEventArgs> OnSeriesClick { get; set; }

  /// <summary>
  /// Raised when the user clicks or taps a legend item, after the item's series (or pie or donut segment) has been shown or
  /// hidden
  /// </summary>
  [Parameter]
  public EventCallback<PixataChartLegendItemClickEventArgs> OnLegendItemClick { get; set; }

  [Parameter(CaptureUnmatchedValues = true)]
  public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

  internal PixataChartTitle? Title { get; set; }
  internal PixataChartLegend? Legend { get; set; }
  internal PixataChartTooltip? Tooltip { get; set; }
  internal PixataChartNoDataTemplate? NoDataTemplate { get; set; }
  internal PixataChartDonutCenterTemplate? DonutCenterTemplate { get; set; }

  private readonly List<PixataChartSeries> _series = [];
  private readonly List<PixataChartCategoryAxis> _categoryAxes = [];
  private readonly List<PixataChartValueAxis> _valueAxes = [];
  private readonly PixataChartTextMeasurer _measurer = new();
  private readonly string _uid = $"pixata-chart-{Guid.NewGuid():N}"[..24];

  private ElementReference _element;
  private ElementReference _plot;
  private ElementReference _surface;
  private IJSObjectReference? _module;
  private DotNetObjectReference<PixataChart>? _dotNetReference;
  private int _handle;
  private bool _attaching;
  private bool _disposed;
  private bool _measuring;

  private double _chartWidth;
  private double _plotWidth;
  private double _plotHeight;
  private PixataChartScene _scene = new();

  // The point (series index, point index) the pointer or keyboard is on, and whether the keyboard put it there
  private (int Series, int Point)? _activePoint;
  private int? _activeCategory;
  private bool _tooltipShown;
  private bool _keyboardFocus;
  private bool _pointerDown;
  private string _announcement = "";

  internal void AddSeries(PixataChartSeries series) {
    if (!_series.Contains(series)) {
      _series.Add(series);
    }
  }

  internal void RemoveSeries(PixataChartSeries series) =>
    _series.Remove(series);

  internal void AddCategoryAxis(PixataChartCategoryAxis axis) {
    if (!_categoryAxes.Contains(axis)) {
      _categoryAxes.Add(axis);
    }
  }

  internal void RemoveCategoryAxis(PixataChartCategoryAxis axis) =>
    _categoryAxes.Remove(axis);

  internal void AddValueAxis(PixataChartValueAxis axis) {
    if (!_valueAxes.Contains(axis)) {
      _valueAxes.Add(axis);
    }
  }

  internal void RemoveValueAxis(PixataChartValueAxis axis) =>
    _valueAxes.Remove(axis);

  // The series and value axes in the order the last render of their container declared them. A series (or axis) is added to
  // the end of the list when it is created, so one that is shown again by an @if would otherwise move to the end, taking
  // another series' place in the legend and its colour
  private List<PixataChartSeries>? _declaredSeries;
  private List<PixataChartValueAxis>? _declaredValueAxes;

  /// <summary>
  /// Called by <see cref="PixataChartSeriesItems"/> as it renders, before its series are given their parameters (in the
  /// order they are declared)
  /// </summary>
  internal void BeginDeclaringSeries() =>
    _declaredSeries = [];

  internal void SeriesDeclared(PixataChartSeries series) =>
    _declaredSeries?.Add(series);

  internal void BeginDeclaringValueAxes() =>
    _declaredValueAxes = [];

  internal void ValueAxisDeclared(PixataChartValueAxis axis) =>
    _declaredValueAxes?.Add(axis);

  /// <summary>
  /// Puts the items that were declared back in declaration order, in the places they hold between them. Anything that wasn't
  /// given its parameters again (because none of them could have changed) keeps its place
  /// </summary>
  private static void Reorder<T>(List<T> items, ref List<T>? declared) where T : class {
    if (declared is null) {
      return;
    }
    List<T> order = declared.Where(items.Contains).Distinct().ToList();
    declared = null;
    int next = 0;
    for (int i = 0; i < items.Count && next < order.Count; i++) {
      if (order.Contains(items[i])) {
        items[i] = order[next++];
      }
    }
  }

  /// <summary>
  /// Redraws the chart because one of its settings components changed
  /// </summary>
  internal void Invalidate() {
    if (!_disposed) {
      StateHasChanged();
    }
  }

  /// <summary>
  /// Redraws the chart, eg after you changed its data in place, or resized its container in a way the chart can't see
  /// </summary>
  public void Refresh() {
    if (!_disposed) {
      _ = InvokeAsync(StateHasChanged);
    }
  }

  /// <summary>
  /// The series, as the chart sees them (in the order they were declared)
  /// </summary>
  internal IReadOnlyList<PixataChartSeries> Series => _series;

  internal bool IsCompact => _chartWidth > 0 && _chartWidth < CompactWidth;

  private bool IsTooltipShared => Tooltip?.Shared == true && !_scene.IsPie;

  private PixataChartLegendPosition LegendPosition {
    get {
      PixataChartLegendPosition position = Legend?.Position ?? PixataChartLegendPosition.Right;
      return IsCompact && position is PixataChartLegendPosition.Left or PixataChartLegendPosition.Right ? PixataChartLegendPosition.Bottom : position;
    }
  }

  private bool LegendVertical =>
    Legend?.Orientation is PixataChartLegendOrientation orientation
      ? orientation == PixataChartLegendOrientation.Vertical
      : LegendPosition is PixataChartLegendPosition.Left or PixataChartLegendPosition.Right;

  private bool TitleVisible => Title is not null && (Title.Visible ?? true) && !string.IsNullOrEmpty(Title.Text);

  private string ChartClass =>
    PixataCss.Classes("pixata-blazor-chart",
      IsCompact ? "pixata-blazor-chart-compact" : null,
      Transitions ?? true ? "pixata-blazor-chart-animate" : null,
      _scene.IsPie ? "pixata-blazor-chart-pie" : null,
      _scene.IsDonut ? "pixata-blazor-chart-donut" : null,
      Class);

  private string? ChartStyle {
    get {
      string style = $"{(string.IsNullOrWhiteSpace(Width) ? "" : $"width:{Width};")}{(string.IsNullOrWhiteSpace(Height) ? "" : $"height:{Height};")}";
      return style == "" ? null : style;
    }
  }

  private string ClipId => $"{_uid}-clip";

  private string PointId(PixataChartScenePoint point) =>
    $"{_uid}-p{point.SeriesIndex}-{point.PointIndex}";

  /// <summary>
  /// Works out where everything goes, at the size the browser last reported (or a guess, before it has)
  /// </summary>
  private PixataChartScene BuildScene() {
    (double width, double height) = _plotWidth > 0 && _plotHeight > 0 ? (_plotWidth, _plotHeight) : GuessSize();
    Reorder(_series, ref _declaredSeries);
    Reorder(_valueAxes, ref _declaredValueAxes);
    _measurer.ClearMissing();
    _scene = PixataChartLayout.Build(new() {
      Series = _series,
      CategoryAxis = _categoryAxes.FirstOrDefault(),
      ValueAxes = _valueAxes,
      Width = width,
      Height = height,
      Measurer = _measurer
    });
    return _scene;
  }

  private (double Width, double Height) GuessSize() {
    double height = 300;
    if (Height is { } text && text.EndsWith("px", StringComparison.OrdinalIgnoreCase) && double.TryParse(text[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out double px)) {
      height = px - (TitleVisible ? 32 : 0) - (Legend?.Position is PixataChartLegendPosition.Top or PixataChartLegendPosition.Bottom ? 32 : 0);
    }
    return (600, Math.Max(height, 100));
  }

  protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (_disposed) {
      return;
    }
    if (firstRender) {
      AttachResult? result = null;
      bool failed = false;
      _attaching = true;
      try {
        _module = await Js.InvokeAsync<IJSObjectReference>("import", ModulePath);
        _dotNetReference = DotNetObjectReference.Create(this);
        result = await _module.InvokeAsync<AttachResult?>("attach", _element, _plot, _dotNetReference);
        _handle = result?.Id ?? 0;
      }
      catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or InvalidOperationException or JSException) {
        failed = true;
      }
      finally {
        _attaching = false;
      }
      if (_disposed) {
        // The chart was removed while the script was loading or attaching, and DisposeAsync left the cleaning up to this
        await DisposeAsync();
        return;
      }
      if (failed) {
        return;
      }
      if (result is not null && SetSize(result.ChartWidth, result.Width, result.Height)) {
        StateHasChanged();
        return;
      }
    }
    await MeasureMissingText();
  }

  /// <summary>
  /// Asks the browser for the widths of the text the layout had to guess, and redraws if they make a difference
  /// </summary>
  private async Task MeasureMissingText() {
    if (_module is null || _measuring || _measurer.Missing.Count == 0) {
      return;
    }
    _measuring = true;
    try {
      object[] requests = _measurer.Missing
        .GroupBy(m => m.Style)
        .Select(g => (object)new {
          style = g.Key,
          cssClass = g.Key.Split('|')[0],
          font = g.Key.Contains('|') ? g.Key[(g.Key.IndexOf('|') + 1)..] : null,
          texts = g.Select(m => m.Text).ToArray()
        })
        .ToArray();
      Dictionary<string, string[]> texts = _measurer.Missing.GroupBy(m => m.Style).ToDictionary(g => g.Key, g => g.Select(m => m.Text).ToArray());
      MeasureResult[]? results = await _module.InvokeAsync<MeasureResult[]?>("measureText", _surface, requests);
      if (results is null || _disposed) {
        return;
      }
      bool changed = false;
      foreach (MeasureResult result in results) {
        if (texts.TryGetValue(result.Style, out string[]? measured)) {
          changed |= _measurer.Store(result.Style, result.FontSize, measured, result.Widths);
        }
      }
      if (changed) {
        StateHasChanged();
      }
    }
    catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or InvalidOperationException or JSException) {
    }
    finally {
      _measuring = false;
    }
  }

  private bool SetSize(double chartWidth, double width, double height) {
    if (width <= 0 || height <= 0) {
      return false;
    }
    bool changed = Math.Abs(chartWidth - _chartWidth) >= 1 || Math.Abs(width - _plotWidth) >= 1 || Math.Abs(height - _plotHeight) >= 1;
    _chartWidth = chartWidth;
    _plotWidth = width;
    _plotHeight = height;
    return changed;
  }

  /// <summary>
  /// Called by the browser when the chart changes size
  /// </summary>
  [JSInvokable]
  public Task OnPlotResized(double chartWidth, double width, double height) {
    if (!_disposed && SetSize(chartWidth, width, height)) {
      StateHasChanged();
    }
    return Task.CompletedTask;
  }

  /// <summary>
  /// Called by the browser once the page's fonts have loaded, as text measured before then may have been the wrong size
  /// </summary>
  [JSInvokable]
  public Task OnFontsLoaded() {
    if (!_disposed && _measurer.HasMeasurements) {
      _measurer.Clear();
      StateHasChanged();
    }
    return Task.CompletedTask;
  }

  #region Pointer, touch and keyboard

  private PixataChartScenePoint? ActivePoint =>
    _activePoint is { } key ? _scene.Points.FirstOrDefault(p => p.SeriesIndex == key.Series && p.PointIndex == key.Point) : null;

  private bool IsActive(PixataChartScenePoint point) =>
    (_activePoint is { } key && key.Series == point.SeriesIndex && key.Point == point.PointIndex)
    || (IsTooltipShared && _activeCategory == point.PointIndex && _tooltipShown);

  private bool IsFocused(PixataChartScenePoint point) =>
    _keyboardFocus && _activePoint is { } key && key.Series == point.SeriesIndex && key.Point == point.PointIndex;

  private void PointerEnterPoint(PixataChartScenePoint point) {
    _keyboardFocus = false;
    Activate(point);
  }

  private async Task ClickPoint(PixataChartScenePoint point) {
    _keyboardFocus = false;
    Activate(point);
    await OnSeriesClick.InvokeAsync(point.ToSeriesPoint<PixataChartSeriesClickEventArgs>());
  }

  private void PointerEnterBand(int category) {
    if (IsTooltipShared) {
      _keyboardFocus = false;
      _activePoint = null;
      _activeCategory = category;
      _tooltipShown = TooltipEnabled(null);
    }
  }

  private void ClickBand(int category) {
    if (IsTooltipShared) {
      PointerEnterBand(category);
    } else {
      HideTooltip();
    }
  }

  private void PointerLeavePlot(PointerEventArgs e) {
    // A tap ends with the pointer leaving, which would hide the tooltip the tap has just shown
    if (e.PointerType != "touch" && !_keyboardFocus) {
      HideTooltip();
    }
  }

  private void Activate(PixataChartScenePoint point) {
    _activePoint = (point.SeriesIndex, point.PointIndex);
    _activeCategory = point.PointIndex;
    _tooltipShown = IsTooltipShared ? TooltipEnabled(null) : TooltipEnabled(point.Series);
  }

  private void HideTooltip() {
    _activePoint = null;
    _activeCategory = null;
    _tooltipShown = false;
  }

  private void HandlePointerDown() =>
    _pointerDown = true;

  private void HandleFocus() {
    if (!_pointerDown && _activePoint is null) {
      PixataChartScenePoint? first = Navigable().FirstOrDefault();
      if (first is not null) {
        Focus(first);
      }
    }
    _pointerDown = false;
  }

  private void HandleFocusOut(FocusEventArgs e) {
    _keyboardFocus = false;
    _pointerDown = false;
    HideTooltip();
  }

  private async Task HandleKeyDown(KeyboardEventArgs e) {
    List<PixataChartScenePoint> points = Navigable().ToList();
    if (points.Count == 0) {
      return;
    }
    PixataChartScenePoint? current = ActivePoint;
    if (e.Key == "Escape") {
      HideTooltip();
      return;
    }
    if (current is null) {
      if (e.Key is "ArrowLeft" or "ArrowRight" or "ArrowUp" or "ArrowDown" or "Home" or "End") {
        Focus(points[0]);
      }
      return;
    }
    if (e.Key is "Enter" or " ") {
      await OnSeriesClick.InvokeAsync(current.ToSeriesPoint<PixataChartSeriesClickEventArgs>());
      return;
    }
    PixataChartScenePoint? next = _scene.IsPie ? NextInPie(points, current, e.Key) : NextInGrid(points, current, e.Key);
    if (next is not null) {
      Focus(next);
    }
  }

  private IEnumerable<PixataChartScenePoint> Navigable() =>
    _scene.Points.OrderBy(p => p.SeriesIndex).ThenBy(p => p.PointIndex);

  private static PixataChartScenePoint? NextInPie(List<PixataChartScenePoint> points, PixataChartScenePoint current, string key) {
    int index = points.IndexOf(current);
    return key switch {
      "ArrowRight" or "ArrowDown" => points[(index + 1) % points.Count],
      "ArrowLeft" or "ArrowUp" => points[(index - 1 + points.Count) % points.Count],
      "Home" => points[0],
      "End" => points[^1],
      _ => null
    };
  }

  private static PixataChartScenePoint? NextInGrid(List<PixataChartScenePoint> points, PixataChartScenePoint current, string key) {
    List<PixataChartScenePoint> series = points.Where(p => p.SeriesIndex == current.SeriesIndex).ToList();
    int index = series.IndexOf(current);
    switch (key) {
      case "ArrowRight":
        return index < series.Count - 1 ? series[index + 1] : null;
      case "ArrowLeft":
        return index > 0 ? series[index - 1] : null;
      case "Home":
        return series[0];
      case "End":
        return series[^1];
      case "ArrowUp":
      case "ArrowDown":
        List<int> seriesIndexes = points.Select(p => p.SeriesIndex).Distinct().ToList();
        int position = seriesIndexes.IndexOf(current.SeriesIndex) + (key == "ArrowDown" ? 1 : -1);
        if (position < 0 || position >= seriesIndexes.Count) {
          return null;
        }
        return points.Where(p => p.SeriesIndex == seriesIndexes[position]).OrderBy(p => Math.Abs(p.PointIndex - current.PointIndex)).First();
      default:
        return null;
    }
  }

  private void Focus(PixataChartScenePoint point) {
    _keyboardFocus = true;
    Activate(point);
    _announcement = point.Description;
  }

  #endregion

  #region Legend

  private async Task ClickLegendItem(PixataChartLegendEntry entry) {
    if (entry.SeriesIndex >= _series.Count) {
      return;
    }
    PixataChartSeries series = _series[entry.SeriesIndex];
    bool visible;
    if (entry.PointIndex is int point) {
      visible = series.HiddenPoints.Contains(point);
      foreach ((int s, int p) in (entry.Linked ?? []).Prepend((entry.SeriesIndex, point))) {
        if (s >= _series.Count) {
          continue;
        }
        if (visible) {
          _series[s].HiddenPoints.Remove(p);
        } else {
          _series[s].HiddenPoints.Add(p);
        }
      }
    } else {
      visible = !series.IsVisible;
      series.VisibleOverride = visible;
    }
    HideTooltip();
    _keyboardFocus = false;
    await OnLegendItemClick.InvokeAsync(new() {
      SeriesIndex = entry.SeriesIndex,
      PointIndex = entry.PointIndex,
      Text = entry.Text,
      Visible = visible
    });
  }

  private IEnumerable<PixataChartLegendEntry> LegendEntries =>
    Legend?.Reverse == true ? Enumerable.Reverse(_scene.LegendEntries) : _scene.LegendEntries;

  #endregion

  #region Tooltip

  private bool TooltipEnabled(PixataChartSeries? series) =>
    series?.SeriesTooltip?.Visible ?? Tooltip?.Visible ?? false;

  private string FormattedValue(PixataChartScenePoint point) =>
    PixataChartFormatter.Format(point.Value, point.Series.SeriesTooltip?.Format ?? Tooltip?.Format);

  /// <summary>
  /// The tooltip's position classes and style. Point tooltips sit above their point (below it near the top), pulled in at
  /// the ends; shared ones sit beside their category
  /// </summary>
  private (string Class, string Style) TooltipPlacement(double x, double y, bool beside, double band) {
    string Px(double value) =>
      $"{PixataChartFormatter.Svg(value)}px";
    double width = _scene.Width;
    if (beside) {
      bool leftSide = x > width / 2;
      double left = leftSide ? x - band / 2 - 4 : x + band / 2 + 4;
      return (leftSide ? "pixata-blazor-chart-tooltip-before" : "pixata-blazor-chart-tooltip-after", $"left:{Px(left)};top:{Px(y)};");
    }
    string vertical = y < 48 ? "pixata-blazor-chart-tooltip-below" : "pixata-blazor-chart-tooltip-above";
    string horizontal = x < 70 ? "pixata-blazor-chart-tooltip-start" : x > width - 70 ? "pixata-blazor-chart-tooltip-end" : "";
    return (PixataCss.Classes(vertical, horizontal), $"left:{Px(x)};top:{Px(y)};");
  }

  private static string? TooltipColours(string? background, string? colour, string pointColour, string reportedColour) {
    string bg = background ?? pointColour;
    string text = colour ?? (PixataChartPalette.IsLight(background ?? reportedColour) ? "var(--pixata-blazor-chart-tooltip-dark-color)" : "var(--pixata-blazor-chart-tooltip-color)");
    return $"--pixata-blazor-chart-tooltip-bg:{bg};color:{text};";
  }

  #endregion

  #region No data and the donut's centre

  private bool ShowNoData =>
    !_scene.HasData && (NoDataTemplate?.Visible ?? true);

  /// <summary>
  /// The style that puts the donut's centre content in its hole. The plot is drawn to fit the SVG's viewBox, which is the
  /// size of the plot area, so its coordinates are pixels. The content gets the square that fits inside the hole
  /// </summary>
  private string DonutCenterStyle {
    get {
      (double x, double y, double radius) = _scene.Hole;
      double side = radius * Math.Sqrt(2);
      return $"left:{PixataChartFormatter.Svg(x)}px;top:{PixataChartFormatter.Svg(y)}px;width:{PixataChartFormatter.Svg(side)}px;height:{PixataChartFormatter.Svg(side)}px;";
    }
  }

  #endregion

  #region Rendering helpers

  private static string Num(double value) =>
    PixataChartFormatter.Svg(value);

  private static string? ColourStyle(string? colour, string property = "--pixata-blazor-chart-point-color") =>
    string.IsNullOrWhiteSpace(colour) ? null : $"{property}:{colour};";

  private static string? TextStyle(PixataChartSceneText text) {
    string style = $"{(string.IsNullOrWhiteSpace(text.Colour) ? "" : $"fill:{text.Colour};")}{(string.IsNullOrWhiteSpace(text.Font) ? "" : $"font:{text.Font};")}";
    return style == "" ? null : style;
  }

  private static string? LineStyle(PixataChartSceneLine line) {
    string style = $"{(string.IsNullOrWhiteSpace(line.Colour) ? "" : $"stroke:{line.Colour};")}{(line.Width is double w ? $"stroke-width:{Num(w)};" : "")}{(line.DashArray is null ? "" : $"stroke-dasharray:{line.DashArray};")}";
    return style == "" ? null : style;
  }

  private static string? PathStyle(PixataChartScenePath path) {
    string style = $"{ColourStyle(path.Colour)}{(path.Width is double w ? $"stroke-width:{Num(w)};" : "")}{(path.DashArray is null ? "" : $"stroke-dasharray:{path.DashArray};")}{(path.Opacity is double o ? $"opacity:{Num(o)};" : "")}";
    return style == "" ? null : style;
  }

  private static string PointStyle(PixataChartScenePoint point) =>
    $"{ColourStyle(point.Colour)}{(point.Opacity is double o ? $"opacity:{Num(o)};" : "")}{(point.MarkerBackground is null ? "" : $"--pixata-blazor-chart-marker-bg:{point.MarkerBackground};")}";

  private string PointClass(string kind, PixataChartScenePoint point, string? extra = null) =>
    PixataCss.Classes($"pixata-blazor-chart-{kind}",
      IsActive(point) ? "pixata-blazor-chart-point-active" : null,
      IsFocused(point) ? "pixata-blazor-chart-point-focused" : null,
      extra);

  /// <summary>
  /// Renders the settings components (the child content) with the chart cascaded to them, through the host that stops them
  /// rendering again every time the chart redraws
  /// </summary>
  private void RenderSettings(RenderTreeBuilder builder) {
    builder.OpenComponent<CascadingValue<PixataChart>>(0);
    builder.AddComponentParameter(1, nameof(CascadingValue<PixataChart>.Value), this);
    builder.AddComponentParameter(2, nameof(CascadingValue<PixataChart>.IsFixed), true);
    builder.AddComponentParameter(3, nameof(CascadingValue<PixataChart>.ChildContent), (RenderFragment)(content => {
      content.OpenComponent<PixataChartSettingsHost>(0);
      content.AddComponentParameter(1, nameof(PixataChartSettingsHost.ChildContent), ChildContent);
      content.CloseComponent();
    }));
    builder.CloseComponent();
  }

  /// <summary>
  /// Renders a piece of SVG text. This is built by hand because Razor treats a &lt;text&gt; tag in a template as its own
  /// </summary>
  private static RenderFragment RenderText(PixataChartSceneText text) => builder => {
    builder.OpenElement(0, "text");
    builder.AddAttribute(1, "class", text.Class);
    builder.AddAttribute(2, "x", Num(text.X));
    builder.AddAttribute(3, "y", Num(text.Y));
    builder.AddAttribute(4, "text-anchor", text.Anchor);
    builder.AddAttribute(5, "transform", Rotate(text));
    builder.AddAttribute(6, "style", TextStyle(text));
    if (text.Lines.Length == 1) {
      builder.AddContent(7, text.Lines[0]);
    } else {
      for (int i = 0; i < text.Lines.Length; i++) {
        builder.OpenElement(8, "tspan");
        builder.AddAttribute(9, "x", Num(text.X));
        builder.AddAttribute(10, "dy", i == 0 ? "0" : Num(text.LineHeight));
        builder.AddContent(11, text.Lines[i]);
        builder.CloseElement();
      }
    }
    if (text.Title is not null) {
      builder.OpenElement(12, "title");
      builder.AddContent(13, text.Title);
      builder.CloseElement();
    }
    builder.CloseElement();
  };

  private static string? Rotate(PixataChartSceneText text) =>
    text.Rotation == 0 ? null : $"rotate({Num(text.Rotation)} {Num(text.X)} {Num(text.Y)})";

  private string LegendClass =>
    PixataCss.Classes("pixata-blazor-chart-legend",
      $"pixata-blazor-chart-legend-{LegendPosition.ToString().ToLowerInvariant()}",
      LegendVertical ? "pixata-blazor-chart-legend-vertical" : "pixata-blazor-chart-legend-horizontal",
      $"pixata-blazor-chart-legend-align-{(Legend?.Align ?? PixataChartLegendAlign.Center).ToString().ToLowerInvariant()}");

  private string TitleClass =>
    PixataCss.Classes("pixata-blazor-chart-title",
      $"pixata-blazor-chart-title-{(Title?.Position ?? PixataChartTitlePosition.Top).ToString().ToLowerInvariant()}",
      $"pixata-blazor-chart-title-align-{(Title?.Align ?? PixataChartTitleAlign.Center).ToString().ToLowerInvariant()}");

  private string? TitleStyle {
    get {
      string style = $"{(string.IsNullOrWhiteSpace(Title?.Color) ? "" : $"color:{Title.Color};")}{(string.IsNullOrWhiteSpace(Title?.Background) ? "" : $"background:{Title.Background};")}{(string.IsNullOrWhiteSpace(Title?.Font) ? "" : $"font:{Title.Font};")}";
      return style == "" ? null : style;
    }
  }

  #endregion

  public async ValueTask DisposeAsync() {
    _disposed = true;
    if (_attaching) {
      // OnAfterRenderAsync detaches the chart once the script has answered
      return;
    }
    try {
      if (_module is not null) {
        if (_handle != 0) {
          await _module.InvokeVoidAsync("detach", _handle);
        }
        await _module.DisposeAsync();
      }
    }
    catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException) {
    }
    _dotNetReference?.Dispose();
    GC.SuppressFinalize(this);
  }

  private sealed class AttachResult {
    public int Id { get; set; }
    public double ChartWidth { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
  }

  private sealed class MeasureResult {
    public string Style { get; set; } = "";
    public double FontSize { get; set; }
    public double[] Widths { get; set; } = [];
  }
}
