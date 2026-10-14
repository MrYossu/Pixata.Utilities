using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

// The layout tests set up series and axes directly, without rendering them
#pragma warning disable BL0005

namespace Pixata.Blazor.Tests.Components;

[TestClass]
public class Chart_Tests {
  private class Sale {
    public string Month { get; set; } = "";
    public double Amount { get; set; }
    public double? Other { get; set; }
    public string? Colour { get; set; }
    public bool Explode { get; set; }
  }

  private static List<Sale> Sales() => [
    new() { Month = "Jan", Amount = 10, Other = 5 },
    new() { Month = "Feb", Amount = 20, Other = null },
    new() { Month = "Mar", Amount = 30, Other = 15 }
  ];

  private static CultureInfo _culture = CultureInfo.CurrentCulture;

  [TestInitialize]
  public void SetCulture() {
    _culture = CultureInfo.CurrentCulture;
    CultureInfo.CurrentCulture = new("en-GB");
  }

  [TestCleanup]
  public void ResetCulture() =>
    CultureInfo.CurrentCulture = _culture;

  #region Value scale

  [TestMethod]
  [DataRow(0, 55000, 10000)]
  [DataRow(0, 21.8, 5)]
  [DataRow(0, 0.4, 0.05)]
  [DataRow(0, 9.3, 1)]
  [DataRow(0, 1, 0.2)]
  public void ValueScale_AutoMajorUnit_IsRound(double min, double max, double expected) =>
    Assert.AreEqual(expected, PixataChartValueScale.AutoMajorUnit(min, max), 1e-12);

  [TestMethod]
  public void ValueScale_Create_IncludesZeroAndLeavesHeadroom() {
    PixataChartValueScale scale = PixataChartValueScale.Create(28000, 55000, null, null, null, null, false, false, 0, 0);
    Assert.AreEqual(0, scale.Min);
    Assert.AreEqual(60000, scale.Max);
    Assert.AreEqual(10000, scale.MajorUnit);
    Assert.AreEqual(2000, scale.MinorUnit);
  }

  [TestMethod]
  public void ValueScale_Create_HandlesNegativeValues() {
    PixataChartValueScale scale = PixataChartValueScale.Create(-30, 50, null, null, null, null, false, false, 0, 0);
    Assert.AreEqual(-40, scale.Min);
    Assert.AreEqual(60, scale.Max);
    Assert.AreEqual(10, scale.MajorUnit);
  }

  [TestMethod]
  public void ValueScale_Create_UsesTheUsersRange() {
    PixataChartValueScale scale = PixataChartValueScale.Create(0.19, 0.33, 0, 0.4, null, null, false, false, 0, 0);
    Assert.AreEqual(0, scale.Min);
    Assert.AreEqual(0.4, scale.Max);
    Assert.AreEqual(0.05, scale.MajorUnit, 1e-12);
    Assert.AreEqual(9, scale.MajorTicks().Count);
    Assert.IsTrue(scale.MajorTicks().Contains(0.3), "Ticks are free of floating point noise");
  }

  [TestMethod]
  public void ValueScale_Create_UsesALargerUnitWhenTheAxisIsShort() {
    PixataChartValueScale tall = PixataChartValueScale.Create(0, 55000, null, null, null, null, false, false, 300, 26);
    PixataChartValueScale cramped = PixataChartValueScale.Create(0, 55000, null, null, null, null, false, false, 100, 26);
    Assert.AreEqual(10000, tall.MajorUnit);
    Assert.AreEqual(20000, cramped.MajorUnit);
    Assert.IsTrue(100 / ((cramped.Max - cramped.Min) / cramped.MajorUnit) >= 26);
  }

  [TestMethod]
  public void ValueScale_Create_KeepsTheUsersMajorUnit() =>
    Assert.AreEqual(10000, PixataChartValueScale.Create(0, 55000, null, null, 10000, null, false, false, 50, 26).MajorUnit);

  [TestMethod]
  public void ValueScale_Create_PercentRunsFromZeroToOne() {
    PixataChartValueScale scale = PixataChartValueScale.Create(0, 1, null, null, null, null, false, true, 0, 0);
    Assert.AreEqual(0, scale.Min);
    Assert.AreEqual(1, scale.Max);
    Assert.AreEqual(0.2, scale.MajorUnit, 1e-12);
  }

  [TestMethod]
  public void ValueScale_Create_WithNoDataRunsFromZeroToOne() {
    PixataChartValueScale scale = PixataChartValueScale.Create(double.NaN, double.NaN, null, null, null, null, false, false, 0, 0);
    Assert.AreEqual(0, scale.Min);
    Assert.AreEqual(1, scale.Max);
  }

  [TestMethod]
  public void ValueScale_NarrowRange_DoesNotStartAtZero() =>
    Assert.IsTrue(PixataChartValueScale.Create(90, 100, null, null, null, null, true, false, 0, 0).Min >= 80);

  [TestMethod]
  [DataRow(1, 2)]
  [DataRow(2, 5)]
  [DataRow(5, 10)]
  [DataRow(0.5, 1)]
  [DataRow(20, 50)]
  public void ValueScale_NextUnit_IsTheNextRoundNumber(double unit, double expected) =>
    Assert.AreEqual(expected, PixataChartValueScale.NextUnit(unit), 1e-12);

  [TestMethod]
  public void ValueScale_MinorTicks_LeaveOutTheMajorOnes() {
    PixataChartValueScale scale = PixataChartValueScale.Create(0, 9, null, 10, 5, 1, false, false, 0, 0);
    CollectionAssert.AreEqual(new double[] { 1, 2, 3, 4, 6, 7, 8, 9 }, scale.MinorTicks().ToArray());
  }

  [TestMethod]
  public void ValueScale_ToPixel_RunsUpTheScreen() {
    PixataChartValueScale scale = PixataChartValueScale.Create(0, 9, null, 10, null, null, false, false, 0, 0);
    scale.Start = 200;
    scale.Length = 100;
    Assert.AreEqual(200, scale.ToPixel(0));
    Assert.AreEqual(150, scale.ToPixel(5));
    Assert.AreEqual(100, scale.ToPixel(10));
  }

  #endregion

  #region Category scale, columns and labels

  [TestMethod]
  public void CategoryScale_SplitsTheAxisIntoBands() {
    PixataChartCategoryScale scale = new(4, 0, 400, false);
    Assert.AreEqual(100, scale.BandWidth);
    Assert.AreEqual(50, scale.Centre(0));
    Assert.AreEqual(100, scale.BandStart(1));
    Assert.AreEqual(2, scale.IndexAt(250));
    Assert.AreEqual(-1, scale.IndexAt(450));
  }

  [TestMethod]
  public void CategoryScale_Justified_PutsTheEndsOnTheEdges() {
    PixataChartCategoryScale scale = new(5, 0, 400, true);
    Assert.AreEqual(0, scale.Centre(0));
    Assert.AreEqual(400, scale.Centre(4));
    Assert.AreEqual(100, scale.BandWidth);
  }

  [TestMethod]
  public void ColumnSlot_UsesGapAndSpacing() {
    (double offset0, double width) = PixataChartCategoryScale.ColumnSlot(100, 2, 0, 1.5, 0.4);
    (double offset1, _) = PixataChartCategoryScale.ColumnSlot(100, 2, 1, 1.5, 0.4);
    Assert.AreEqual(100 / 3.9, width, 1e-9);
    Assert.AreEqual(1.5 * width / 2, offset0, 1e-9);
    Assert.AreEqual(offset0 + width * 1.4, offset1, 1e-9);
    Assert.AreEqual(100, offset1 + width + 1.5 * width / 2, 1e-9, "The columns and gaps fill the band");
  }

  [TestMethod]
  public void LabelFitter_LabelsThatFit_StayFlat() {
    PixataChartLabelFit fit = PixataChartLabelFitter.Fit([20, 25, 22], 15, 60, null, null, 200);
    Assert.AreEqual(0, fit.Angle);
    Assert.AreEqual(1, fit.Step);
    Assert.AreEqual(15, fit.AreaHeight);
  }

  [TestMethod]
  public void LabelFitter_LabelsThatDontFit_TurnAndThinOut() {
    PixataChartLabelFit turned = PixataChartLabelFitter.Fit([80, 120, 60], 15, 40, null, null, 200);
    Assert.AreEqual(-45, turned.Angle);
    Assert.AreEqual(1, turned.Step);
    PixataChartLabelFit thinned = PixataChartLabelFitter.Fit([80, 120, 60], 15, 10, null, null, 200);
    Assert.AreEqual(-45, thinned.Angle);
    Assert.IsTrue(thinned.Step >= 2);
  }

  [TestMethod]
  public void LabelFitter_ExplicitAngle_IsKeptButLabelsStillThinOut() {
    PixataChartLabelFit fit = PixataChartLabelFitter.Fit([80, 80], 15, 30, 0, null, 200);
    Assert.AreEqual(0, fit.Angle);
    Assert.AreEqual(3, fit.Step);
  }

  [TestMethod]
  public void LabelFitter_UserStep_IsKept() =>
    Assert.AreEqual(1, PixataChartLabelFitter.Fit([80, 80], 15, 10, null, 1, 200).Step);

  [TestMethod]
  public void LabelFitter_LongTurnedLabels_AreShortened() {
    PixataChartLabelFit fit = PixataChartLabelFitter.Fit([400], 15, 10, null, null, 80);
    Assert.IsTrue(fit.MaxWidth < 400);
    Assert.IsTrue(fit.AreaHeight <= 80);
  }

  [TestMethod]
  public void LabelFitter_Shows_UsesStepAndSkip() {
    bool[] shown = Enumerable.Range(0, 7).Select(i => PixataChartLabelFitter.Shows(i, 2, 1)).ToArray();
    CollectionAssert.AreEqual(new[] { false, true, false, true, false, true, false }, shown);
  }

  [TestMethod]
  public void LabelFitter_Angle_ReadsNumbersAndAuto() {
    Assert.IsNull(PixataChartLabelFitter.Angle("auto"));
    Assert.IsNull(PixataChartLabelFitter.Angle(null));
    Assert.AreEqual(-90, PixataChartLabelFitter.Angle(-90));
    Assert.AreEqual(45, PixataChartLabelFitter.Angle("45"));
  }

  #endregion

  #region Stacking, pies and paths

  [TestMethod]
  public void Stacking_Normal_StacksPositiveUpAndNegativeDown() {
    PixataChartStackedValue?[][] stacked = PixataChartStacking.Stack([[1, 2, null], [3, -1, 4]], false);
    Assert.AreEqual(new PixataChartStackedValue(0, 1, .25, 4), stacked[0][0]);
    Assert.AreEqual(new PixataChartStackedValue(1, 4, .75, 4), stacked[1][0]);
    Assert.AreEqual(0, stacked[1][1]!.Value.From);
    Assert.AreEqual(-1, stacked[1][1]!.Value.To);
    Assert.IsNull(stacked[0][2]);
    Assert.AreEqual(new PixataChartStackedValue(0, 4, 1, 4), stacked[1][2]);
  }

  [TestMethod]
  public void Stacking_Percent_ScalesEachCategoryToOne() {
    PixataChartStackedValue?[][] stacked = PixataChartStacking.Stack([[1, 10], [3, 30]], true);
    Assert.AreEqual(0.25, stacked[0][0]!.Value.To, 1e-12);
    Assert.AreEqual(1, stacked[1][0]!.Value.To, 1e-12);
    Assert.AreEqual(0.25, stacked[0][1]!.Value.To, 1e-12);
  }

  [TestMethod]
  public void Pie_Slices_GoClockwiseFromTheStartAngle() {
    List<PixataChartPieSlice> slices = PixataChartPie.Slices([1, 1, 2], [], 90);
    Assert.AreEqual(3, slices.Count);
    Assert.AreEqual(90, slices[0].StartAngle);
    Assert.AreEqual(0, slices[0].EndAngle);
    Assert.AreEqual(-90, slices[2].StartAngle);
    Assert.AreEqual(-270, slices[2].EndAngle);
    Assert.AreEqual(0.5, slices[2].Percentage);
    Assert.AreEqual(45, slices[0].MidAngle);
  }

  [TestMethod]
  public void Pie_Slices_SkipHiddenNegativeAndMissingValues() {
    List<PixataChartPieSlice> slices = PixataChartPie.Slices([1, null, -3, 1, 2], [4], 0);
    CollectionAssert.AreEqual(new[] { 0, 3 }, slices.Select(s => s.Index).ToArray());
    Assert.AreEqual(180, slices[0].Sweep);
  }

  [TestMethod]
  public void Pie_PointAt_MeasuresAnticlockwiseFromThreeOClock() {
    (double x, double y) = PixataChartPie.PointAt(100, 100, 10, 90);
    Assert.AreEqual(100, x, 1e-9);
    Assert.AreEqual(90, y, 1e-9);
  }

  [TestMethod]
  public void Pie_SegmentPath_DrawsAWholeCircleAsTwoArcs() {
    Assert.AreEqual(2, PixataChartPie.SegmentPath(50, 50, 10, 90, -270).Split('A').Length - 1);
    StringAssert.StartsWith(PixataChartPie.SegmentPath(50, 50, 10, 90, 0), "M 50 50 L 50 40 A 10 10 0 0 1 60 50");
  }

  [TestMethod]
  public void Pie_Spread_MovesOverlappingLabelsApart() {
    List<PixataChartPieLabel> labels = [
      new() { Index = 0, Right = true, Y = 50 },
      new() { Index = 1, Right = true, Y = 52 },
      new() { Index = 2, Right = true, Y = 98 },
      new() { Index = 3, Right = false, Y = 50 }
    ];
    PixataChartPie.Spread(labels, 15, 0, 100);
    Assert.AreEqual(50, labels[0].Y);
    Assert.AreEqual(65, labels[1].Y);
    Assert.AreEqual(98, labels[2].Y);
    Assert.AreEqual(50, labels[3].Y, "Labels on the other side don't move");
    PixataChartPie.Spread(labels.Take(3), 40, 0, 100);
    Assert.AreEqual(100, labels[2].Y, "Labels are kept inside the area");
    Assert.AreEqual(60, labels[1].Y);
    Assert.AreEqual(20, labels[0].Y);
  }

  [TestMethod]
  public void Paths_Linear_BreaksAtGaps() =>
    Assert.AreEqual("M 0 10 L 10 20 M 30 5", PixataChartPaths.Linear([(0, 10), (10, 20), null, (30, 5)]));

  [TestMethod]
  public void Paths_Step_RunsAcrossEachBand() =>
    Assert.AreEqual("M 0 10 H 10 V 20 H 20", PixataChartPaths.Step([(5, 10), (15, 20)], [(0, 10), (10, 20)]));

  [TestMethod]
  public void Paths_Smooth_DoesNotOvershoot() {
    string path = PixataChartPaths.Smooth([(0, 100), (10, 50), (20, 50), (30, 0)]);
    StringAssert.StartsWith(path, "M 0 100 C");
    double[] ys = path.Replace("M", "").Replace("C", "").Split(',', ' ').Where(s => s != "").Select(double.Parse).Where((_, i) => i % 2 == 1).ToArray();
    Assert.IsTrue(ys.All(y => y is >= 0 and <= 100));
  }

  #endregion

  #region Formatting, measuring and colours

  [TestMethod]
  public void Formatter_Format_TakesCompositeAndPlainFormats() {
    Assert.AreEqual("25%", PixataChartFormatter.Format(0.25, "{0:P0}"));
    Assert.AreEqual("1,234.5", PixataChartFormatter.Format(1234.5, "N1"));
    Assert.AreEqual("£5", PixataChartFormatter.Format(5, "{0:C0}"));
    Assert.AreEqual("42000", PixataChartFormatter.Format(42000.0, null));
    Assert.AreEqual("", PixataChartFormatter.Format(null, "N2"));
  }

  [TestMethod]
  public void Formatter_ApplyTemplate_FillsInPlaceholders() {
    Sale sale = new() { Month = "Jan", Amount = 3 };
    Dictionary<string, object?> values = new() { ["category"] = "Jan", ["value"] = 0.5, ["percentage"] = 0.126, ["dataItem.Month"] = sale.Month };
    Func<string, object?> resolve = name => values.GetValueOrDefault(name);
    Assert.AreEqual("Jan: 0.5", PixataChartFormatter.ApplyTemplate("#= category #: #=value#", resolve));
    Assert.AreEqual("13%", PixataChartFormatter.ApplyTemplate("#= format('{0:P0}', percentage) #", resolve));
    Assert.AreEqual("0.50!", PixataChartFormatter.ApplyTemplate("#=something.format(\"{0:N2}\",value)#!", resolve));
    Assert.AreEqual("Month Jan", PixataChartFormatter.ApplyTemplate("Month #= dataItem.Month #", resolve));
    Assert.AreEqual("no placeholders", PixataChartFormatter.ApplyTemplate("no placeholders", resolve));
  }

  [TestMethod]
  public void Formatter_ApplyTemplate_AllowsHashesInsideFormats() {
    Func<string, object?> resolve = name => name == "value" ? 1234.5 : null;
    Assert.AreEqual("1,235 units", PixataChartFormatter.ApplyTemplate("#= format('{0:#,##0}', value) # units", resolve));
    Assert.AreEqual("[1234.5]", PixataChartFormatter.ApplyTemplate("[#= format(\"{0:0.##}\", value) #]", resolve));
  }

  [TestMethod]
  public void Formatter_Lines_SplitsAtEscapedAndRealNewLines() =>
    CollectionAssert.AreEqual(new[] { "a", "b", "c" }, PixataChartFormatter.Lines("a\\nb\nc"));

  [TestMethod]
  public void Formatter_ToDouble_ConvertsWhatItCan() {
    Assert.AreEqual(1.5, PixataChartFormatter.ToDouble("1.5"));
    Assert.AreEqual(3, PixataChartFormatter.ToDouble(3));
    Assert.AreEqual(2.5, PixataChartFormatter.ToDouble(2.5m));
    Assert.IsNull(PixataChartFormatter.ToDouble(null));
    Assert.IsNull(PixataChartFormatter.ToDouble(double.NaN));
    Assert.IsNull(PixataChartFormatter.ToDouble("abc"));
    Assert.IsNull(PixataChartFormatter.ToDouble(new object()));
  }

  [TestMethod]
  public void TextMeasurer_UsesMeasuredWidthsAndNotesMissingOnes() {
    PixataChartTextMeasurer measurer = new();
    double estimate = measurer.Width("Hello", PixataChartTextMeasurer.AxisLabel);
    Assert.IsTrue(estimate > 0);
    Assert.AreEqual(1, measurer.Missing.Count);
    Assert.IsTrue(measurer.Store(PixataChartTextMeasurer.AxisLabel, 13, ["Hello"], [31.5]));
    Assert.AreEqual(31.5, measurer.Width("Hello", PixataChartTextMeasurer.AxisLabel));
    Assert.AreEqual(13, measurer.FontSize(PixataChartTextMeasurer.AxisLabel));
    Assert.AreEqual(0, measurer.Missing.Count);
    Assert.IsFalse(measurer.Store(PixataChartTextMeasurer.AxisLabel, 13, ["Hello"], [31.5]), "Nothing new");
  }

  [TestMethod]
  public void TextMeasurer_ReadsTheFontSizeFromAFont() =>
    Assert.AreEqual(20, new PixataChartTextMeasurer().FontSize(PixataChartTextMeasurer.Label, "bold 20px sans-serif"));

  [TestMethod]
  public void TextMeasurer_Truncate_AddsAnEllipsis() {
    Assert.AreEqual("Short", PixataChartTextMeasurer.Truncate("Short", 30, 40));
    string truncated = PixataChartTextMeasurer.Truncate("Yorkshire and the Humber", 150, 60);
    StringAssert.EndsWith(truncated, "…");
    Assert.IsTrue(truncated.Length < 12);
  }

  [TestMethod]
  [DataRow("#ffd246", true)]
  [DataRow("#ff6358", false)]
  [DataRow("#2d73f5", false)]
  [DataRow("#fff", true)]
  [DataRow("rgb(250, 250, 250)", true)]
  [DataRow("var(--x)", false)]
  public void Palette_IsLight_PicksTheTooltipTextColour(string colour, bool light) =>
    Assert.AreEqual(light, PixataChartPalette.IsLight(colour));

  [TestMethod]
  public void Palette_Css_UsesTheSeriesVariables() {
    Assert.AreEqual("var(--pixata-blazor-chart-series-1, #ff6358)", PixataChartPalette.Css(0));
    Assert.AreEqual("var(--pixata-blazor-chart-series-1, #ff6358)", PixataChartPalette.Css(6));
    Assert.AreEqual("#aa46be", PixataChartPalette.Hex(5));
  }

  #endregion

  #region Layout

  private static PixataChartScene Layout(IReadOnlyList<PixataChartSeries> series, PixataChartCategoryAxis? categoryAxis = null, IReadOnlyList<PixataChartValueAxis>? valueAxes = null, double width = 600, double height = 300) =>
    PixataChartLayout.Build(new() {
      Series = series,
      CategoryAxis = categoryAxis,
      ValueAxes = valueAxes ?? [],
      Width = width,
      Height = height
    });

  [TestMethod]
  public void Layout_Columns_StandSideBySideAtTheirHeight() {
    List<Sale> sales = Sales();
    PixataChartScene scene = Layout([
      new() { Name = "A", Data = sales, Field = nameof(Sale.Amount), CategoryField = nameof(Sale.Month) },
      new() { Name = "B", Data = sales, Field = nameof(Sale.Other), CategoryField = nameof(Sale.Month) }
    ]);
    CollectionAssert.AreEqual(new object[] { "Jan", "Feb", "Mar" }, scene.Categories);
    Assert.AreEqual(5, scene.Points.Count, "Feb's missing value has no column");
    PixataChartScenePoint a0 = scene.Points.Single(p => p.SeriesIndex == 0 && p.PointIndex == 0);
    PixataChartScenePoint b0 = scene.Points.Single(p => p.SeriesIndex == 1 && p.PointIndex == 0);
    PixataChartScenePoint a2 = scene.Points.Single(p => p.SeriesIndex == 0 && p.PointIndex == 2);
    Assert.IsTrue(b0.X > a0.X);
    Assert.AreEqual(a0.Y + a0.Height, b0.Y + b0.Height, 1e-9, "Both stand on zero");
    Assert.AreEqual(a0.Height * 3, a2.Height, 1e-6);
    Assert.AreEqual(a0.Height, b0.Height * 2, 1e-6);
    Assert.AreEqual(sales[0], a0.DataItem);
  }

  [TestMethod]
  public void Layout_Categories_ComeFromTheAxisThenTheData() {
    PixataChartScene scene = Layout(
      [new() { Data = Sales(), Field = nameof(Sale.Amount), CategoryField = nameof(Sale.Month) }],
      new() { Categories = ["Dec", "Jan"] });
    CollectionAssert.AreEqual(new object[] { "Dec", "Jan", "Feb", "Mar" }, scene.Categories);
    Assert.IsFalse(scene.Points.Any(p => p.PointIndex == 0));
  }

  [TestMethod]
  public void Layout_PlainNumbers_MatchTheCategoriesByPosition() {
    PixataChartScene scene = Layout([new() { Data = [1.0, 2.0, 3.0, 4.0] }], new() { Categories = ["A", "B"] });
    Assert.AreEqual(4, scene.Categories.Count);
    Assert.AreEqual(4, scene.Points.Count);
    Assert.AreEqual(3.0, scene.Points[2].Value);
  }

  [TestMethod]
  public void Layout_StackedColumns_SitOnTopOfEachOther() {
    List<Sale> sales = Sales();
    PixataChartSeries first = new() { Data = sales, Field = nameof(Sale.Amount), CategoryField = nameof(Sale.Month), Stack = new() };
    PixataChartScene scene = Layout([first, new() { Data = sales, Field = nameof(Sale.Other), CategoryField = nameof(Sale.Month) }]);
    PixataChartScenePoint bottom = scene.Points.Single(p => p.SeriesIndex == 0 && p.PointIndex == 0);
    PixataChartScenePoint top = scene.Points.Single(p => p.SeriesIndex == 1 && p.PointIndex == 0);
    Assert.AreEqual(bottom.X, top.X);
    Assert.AreEqual(bottom.Y, top.Y + top.Height, 1e-9);
  }

  [TestMethod]
  public void Layout_Stack100_UsesAPercentAxis() {
    List<Sale> sales = Sales();
    PixataChartSeries first = new() { Data = sales, Field = nameof(Sale.Amount), CategoryField = nameof(Sale.Month), Stack = new() { Type = PixataChartSeriesStackType.Stack100 } };
    PixataChartScene scene = Layout([first, new() { Data = sales, Field = nameof(Sale.Other), CategoryField = nameof(Sale.Month) }]);
    string[] labels = scene.AxisTexts.Where(t => t.Class == PixataChartTextMeasurer.AxisLabel && t.Anchor == "end").Select(t => t.Lines[0]).ToArray();
    CollectionAssert.AreEqual(new[] { "0%", "20%", "40%", "60%", "80%", "100%" }, labels);
    PixataChartScenePoint top = scene.Points.Single(p => p.SeriesIndex == 1 && p.PointIndex == 0);
    Assert.AreEqual(scene.Plot.Y, top.Y, 1e-9, "Each category fills the axis");
    Assert.AreEqual(5 / 15.0, top.Percentage, 1e-9);
  }

  [TestMethod]
  public void Layout_StackGroups_StandSideBySide() {
    List<Sale> sales = Sales();
    PixataChartScene scene = Layout([
      new() { Data = sales, Field = nameof(Sale.Amount), CategoryField = nameof(Sale.Month), Stack = new() { Group = "a" } },
      new() { Data = sales, Field = nameof(Sale.Amount), CategoryField = nameof(Sale.Month), Stack = new() { Group = "a" } },
      new() { Data = sales, Field = nameof(Sale.Amount), CategoryField = nameof(Sale.Month), Stack = new() { Group = "b" } }
    ]);
    double[] xs = scene.Points.Where(p => p.PointIndex == 0).Select(p => p.X).ToArray();
    Assert.AreEqual(xs[0], xs[1]);
    Assert.IsTrue(xs[2] > xs[0]);
  }

  [TestMethod]
  [DataRow(PixataChartSeriesMissingValues.Gap, 2, 2)]
  [DataRow(PixataChartSeriesMissingValues.Interpolate, 1, 2)]
  [DataRow(PixataChartSeriesMissingValues.Zero, 1, 3)]
  public void Layout_Lines_HandleMissingValues(PixataChartSeriesMissingValues missing, int runs, int points) {
    PixataChartScene scene = Layout([new() { Type = PixataChartSeriesType.Line, Data = Sales(), Field = nameof(Sale.Other), CategoryField = nameof(Sale.Month), MissingValues = missing }]);
    Assert.AreEqual(runs, scene.Paths.Single().D.Count(c => c == 'M'));
    Assert.AreEqual(points, scene.Points.Count);
  }

  [TestMethod]
  public void Layout_Lines_InterpolateByDefault() =>
    Assert.AreEqual(1, Layout([new() { Type = PixataChartSeriesType.Line, Data = [1.0, null!, 3.0] }]).Paths.Single().D.Count(c => c == 'M'));

  [TestMethod]
  public void Layout_LineStyles_ProduceDifferentPaths() {
    string Path(PixataChartSeriesStyle style) =>
      Layout([new() { Type = PixataChartSeriesType.Line, Data = [1.0, 3.0, 2.0], Style = style }]).Paths.Single().D;
    StringAssert.Contains(Path(PixataChartSeriesStyle.Smooth), "C");
    StringAssert.Contains(Path(PixataChartSeriesStyle.Step), "H");
    StringAssert.Contains(Path(PixataChartSeriesStyle.Normal), "L");
  }

  [TestMethod]
  public void Layout_LinesAndColumns_SharePointPositions() {
    PixataChartScene scene = Layout([
      new() { Data = [1.0, 2.0] },
      new() { Type = PixataChartSeriesType.Line, Data = [1.0, 2.0] }
    ]);
    PixataChartScenePoint marker = scene.Points.First(p => p.Kind == PixataChartPointKind.Marker);
    Assert.AreEqual(scene.CategoryScale!.Centre(0), marker.X, 1e-9);
    Assert.AreEqual(PixataChartPointKind.Column, scene.Points[0].Kind, "Columns are drawn first");
  }

  [TestMethod]
  public void Layout_SecondValueAxis_GoesOnTheRightAndHasItsOwnScale() {
    PixataChartScene scene = Layout(
      [new() { Data = [100.0, 200.0] }, new() { Type = PixataChartSeriesType.Line, Data = [0.5, 1.0], Axis = "small" }],
      new() { AxisCrossingValue = [0, 10] },
      [new(), new() { Name = "small", Min = 0, Max = 1 }]);
    Assert.IsTrue(scene.AxisLines.Any(l => l.X1 == scene.Plot.Right && l.X2 == scene.Plot.Right && l.Y1 == scene.Plot.Y));
    Assert.IsTrue(scene.AxisTexts.Any(t => t.Anchor == "start" && t.Lines[0] == "1"));
    PixataChartScenePoint marker = scene.Points.Single(p => p.Kind == PixataChartPointKind.Marker && p.PointIndex == 1);
    Assert.AreEqual(scene.Plot.Y, marker.Y, 1e-9, "1 is the top of the second axis");
  }

  [TestMethod]
  public void Layout_LongCategories_TurnAndThinOutWhenNarrow() {
    string[] names = Enumerable.Range(1, 12).Select(i => $"A long category name {i}").ToArray();
    PixataChartScene wide = Layout([new() { Data = Enumerable.Repeat<object>(1.0, 12) }], new() { Categories = names }, width: 3000);
    PixataChartScene narrow = Layout([new() { Data = Enumerable.Repeat<object>(1.0, 12) }], new() { Categories = names }, width: 320);
    List<PixataChartSceneText> wideLabels = wide.AxisTexts.Where(t => t.Anchor != "end" && t.Class == PixataChartTextMeasurer.AxisLabel).ToList();
    List<PixataChartSceneText> narrowLabels = narrow.AxisTexts.Where(t => t.Rotation != 0).ToList();
    Assert.AreEqual(12, wideLabels.Count);
    Assert.IsTrue(wideLabels.All(t => t.Rotation == 0));
    Assert.IsTrue(narrowLabels.Count is > 0 and < 12);
    Assert.IsTrue(narrowLabels.All(t => t.Rotation == -45));
  }

  [TestMethod]
  public void Layout_GridLines_FollowTheSettings() {
    PixataChartValueAxis axis = new() { Max = 10, MajorUnit = 5, MinorGridLines = new PixataChartValueAxisMinorGridLines { Visible = true } };
    PixataChartCategoryAxis categoryAxis = new() { MajorGridLines = new PixataChartCategoryAxisMajorGridLines { Visible = false } };
    PixataChartScene scene = Layout([new() { Data = [1.0, 2.0] }], categoryAxis, [axis]);
    Assert.AreEqual(3, scene.GridLines.Count(l => !l.Class.Contains("minor")), "Major lines at 0, 5 and 10, none for the categories");
    Assert.AreEqual(8, scene.GridLines.Count(l => l.Class.Contains("minor")));
  }

  [TestMethod]
  public void Layout_HiddenSeries_IsLeftOutButStaysInTheLegend() {
    PixataChartSeries hidden = new() { Name = "Hidden", Data = [5.0], Visible = false };
    PixataChartScene scene = Layout([new() { Name = "Shown", Data = [1.0] }, hidden, new() { Data = [2.0] }, new() { Name = "Not in legend", Data = [3.0], VisibleInLegend = false }]);
    Assert.IsFalse(scene.Points.Any(p => p.SeriesIndex == 1));
    CollectionAssert.AreEqual(new[] { "Shown", "Hidden" }, scene.LegendEntries.Select(e => e.Text).ToArray());
    Assert.IsFalse(scene.LegendEntries[1].Visible);
  }

  [TestMethod]
  public void Layout_Colours_ComeFromTheFieldTheSeriesOrThePalette() {
    List<Sale> sales = [new() { Month = "Jan", Amount = 1, Colour = "red" }, new() { Month = "Feb", Amount = 2 }];
    PixataChartScene scene = Layout([
      new() { Data = sales, Field = nameof(Sale.Amount), CategoryField = nameof(Sale.Month), ColorField = nameof(Sale.Colour) },
      new() { Data = sales, Field = nameof(Sale.Amount), CategoryField = nameof(Sale.Month), Color = "#123456" }
    ]);
    Assert.AreEqual("red", scene.Points[0].Colour);
    Assert.AreEqual(PixataChartPalette.Css(0), scene.Points[1].Colour);
    Assert.AreEqual("#ff6358", scene.Points[1].ReportedColour);
    Assert.AreEqual("#123456", scene.Points[2].Colour);
  }

  [TestMethod]
  public void Layout_Colours_FromTheDataCannotAddOtherStyles() {
    List<Sale> sales = [
      new() { Month = "Jan", Amount = 1, Colour = "red;position:fixed;inset:0" },
      new() { Month = "Feb", Amount = 2, Colour = "url(//example.com/a.png)" },
      new() { Month = "Mar", Amount = 3, Colour = "rgb(0 0 0 / 50%)" }
    ];
    PixataChartScene scene = Layout([new() { Data = sales, Field = nameof(Sale.Amount), CategoryField = nameof(Sale.Month), ColorField = nameof(Sale.Colour) }]);
    Assert.AreEqual(PixataChartPalette.Css(0), scene.Points[0].Colour);
    Assert.AreEqual(PixataChartPalette.Css(0), scene.Points[1].Colour);
    Assert.AreEqual("rgb(0 0 0 / 50%)", scene.Points[2].Colour);
    PixataChartScene pie = Layout([new() { Type = PixataChartSeriesType.Pie, Data = sales, Field = nameof(Sale.Amount), ColorField = nameof(Sale.Colour) }]);
    Assert.AreEqual(PixataChartPalette.Css(0), pie.Points[0].Colour);
    Assert.AreEqual(PixataChartPalette.Css(0), pie.LegendEntries[0].Colour);
    Assert.AreEqual("rgb(0 0 0 / 50%)", pie.Points[2].Colour);
  }

  [TestMethod]
  public void Layout_ColumnLabels_UseTheTemplate() {
    PixataChartSeries series = new() { Name = "S", Data = Sales(), Field = nameof(Sale.Amount), CategoryField = nameof(Sale.Month), Labels = new() { Visible = true, Template = "#= category #: #= value #" } };
    PixataChartScene scene = Layout([series]);
    CollectionAssert.AreEqual(new[] { "Jan: 10", "Feb: 20", "Mar: 30" }, scene.Labels.Select(l => l.Lines[0]).ToArray());
    Assert.IsTrue(scene.Labels[0].Y < scene.Points[0].Y, "Outside the end of the column");
  }

  [TestMethod]
  public void Layout_Pie_DividesTheCircleAndListsEverySegment() {
    List<Sale> sales = Sales();
    PixataChartSeries pie = new() { Type = PixataChartSeriesType.Pie, Data = sales, Field = nameof(Sale.Other), CategoryField = nameof(Sale.Month) };
    PixataChartScene scene = Layout([pie, new() { Data = [1.0] }]);
    Assert.IsTrue(scene.IsPie);
    Assert.AreEqual(2, scene.Points.Count, "Feb's missing value has no segment");
    Assert.IsTrue(scene.Points.All(p => p.Kind == PixataChartPointKind.Segment));
    Assert.AreEqual(0.25, scene.Points[0].Percentage, 1e-9);
    CollectionAssert.AreEqual(new[] { "Jan", "Feb", "Mar" }, scene.LegendEntries.Select(e => e.Text).ToArray());
    pie.HiddenPoints.Add(2);
    PixataChartScene hidden = Layout([pie]);
    Assert.AreEqual(1, hidden.Points.Count);
    Assert.AreEqual(1, hidden.Points[0].Percentage, 1e-9);
    Assert.IsFalse(hidden.LegendEntries[2].Visible);
  }

  [TestMethod]
  public void Layout_Pie_LabelsSitOutsideWithConnectors() {
    PixataChartSeries pie = new() { Type = PixataChartSeriesType.Pie, Data = Sales(), Field = nameof(Sale.Amount), CategoryField = nameof(Sale.Month), Labels = new() { Visible = true, Template = "#= category #: #= format('{0:P0}', percentage) #" } };
    PixataChartScene scene = Layout([pie]);
    CollectionAssert.AreEquivalent(new[] { "Jan: 17%", "Feb: 33%", "Mar: 50%" }, scene.Labels.Select(l => l.Lines[0]).ToArray());
    Assert.AreEqual(3, scene.Connectors.Count);
  }

  // Empty data, all zero, all negative, one category, nulls, tiny and huge ranges, each stacked and not, with and without
  // labels, at a normal size and squashed, mustn't throw or put NaN or infinity into the SVG
  private static void AssertFinite(PixataChartScene scene) {
    static string Text(object o) => o.ToString() ?? "";
    foreach (PixataChartScenePoint p in scene.Points) {
      Assert.IsTrue(double.IsFinite(p.X) && double.IsFinite(p.Y) && double.IsFinite(p.Width) && double.IsFinite(p.Height), $"point {p.SeriesIndex}/{p.PointIndex}");
      Assert.IsFalse((p.Path ?? "").Contains("NaN") || (p.Path ?? "").Contains("∞"), p.Path);
    }
    foreach (PixataChartSceneLine l in scene.GridLines.Concat(scene.AxisLines)) {
      Assert.IsTrue(double.IsFinite(l.X1) && double.IsFinite(l.Y1) && double.IsFinite(l.X2) && double.IsFinite(l.Y2), Text(l));
    }
    foreach (PixataChartSceneText t in scene.AxisTexts.Concat(scene.Labels)) {
      Assert.IsTrue(double.IsFinite(t.X) && double.IsFinite(t.Y), Text(t));
    }
    foreach (PixataChartScenePath path in scene.Paths.Concat(scene.Connectors)) {
      Assert.IsFalse(path.D.Contains("NaN") || path.D.Contains("∞"), path.D);
    }
  }

  [TestMethod]
  [DataRow(PixataChartSeriesType.Column)]
  [DataRow(PixataChartSeriesType.Line)]
  [DataRow(PixataChartSeriesType.Pie)]
  public void Layout_AwkwardData_IsDrawnWithFiniteNumbers(PixataChartSeriesType type) {
    object?[][] datasets = [
      [],
      [0.0, 0.0, 0.0],
      [-5.0, -10.0, -1.0],
      [42.0],
      [null, null],
      [null, 3.0, null],
      [1e-12, 2e-12],
      [1e300, -1e300],
      [1e15, 1e15 + 1],
      [5.0, 5.0],
      [-0.0001, 1e9]
    ];
    foreach (object?[] data in datasets) {
      foreach (bool stacked in new[] { false, true }) {
        foreach (bool labels in new[] { false, true }) {
          PixataChartSeries series = new() { Type = type, Name = "S", Data = data.Cast<object>().ToList(), Labels = labels ? new() { Visible = true } : null, Stack = stacked ? new() : null };
          PixataChartSeries other = new() { Type = type, Name = "T", Data = data.Cast<object>().ToList() };
          foreach ((double w, double h) in new[] { (600.0, 300.0), (1.0, 1.0), (50.0, 30.0) }) {
            foreach (bool percent in new[] { false, true }) {
              if (percent && series.Stack is not null) {
                series.Stack.Type = PixataChartSeriesStackType.Stack100;
              }
              try {
                PixataChartScene scene = Layout([series, other], width: w, height: h);
                AssertFinite(scene);
              } catch (Exception ex) {
                Assert.Fail($"{type} [{string.Join(",", data)}] stacked={stacked} labels={labels} {w}x{h} percent={percent}: {ex}");
              }
            }
          }
        }
      }
    }
  }

  [TestMethod]
  public void Layout_Pie_ExplodedSegmentsMoveOut() {
    List<Sale> sales = [new() { Month = "A", Amount = 1, Explode = true }, new() { Month = "B", Amount = 1 }];
    PixataChartScene scene = Layout([new() { Type = PixataChartSeriesType.Pie, Data = sales, Field = nameof(Sale.Amount), ExplodeField = nameof(Sale.Explode) }]);
    string centre = $"M {PixataChartFormatter.Svg(scene.Plot.X + scene.Plot.Width / 2)} {PixataChartFormatter.Svg(scene.Plot.Y + scene.Plot.Height / 2)}";
    Assert.IsFalse(scene.Points[0].Path!.StartsWith(centre));
    Assert.IsTrue(scene.Points[1].Path!.StartsWith(centre));
  }

  #endregion

  #region Rendering

  private static RenderFragment Component<T>(params (string Name, object? Value)[] parameters) where T : IComponent =>
    Component<T>(null, parameters);

  private static RenderFragment Component<T>(RenderFragment? content, params (string Name, object? Value)[] parameters) where T : IComponent => builder => {
    builder.OpenComponent<T>(0);
    foreach ((string name, object? value) in parameters) {
      builder.AddComponentParameter(1, name, value);
    }
    if (content is not null) {
      builder.AddComponentParameter(2, "ChildContent", content);
    }
    builder.CloseComponent();
  };

  private static RenderFragment Fragments(params RenderFragment[] fragments) => builder => {
    foreach (RenderFragment fragment in fragments) {
      builder.AddContent(0, fragment);
    }
  };

  private static RenderFragment ColumnSeries(string name, IEnumerable<object> data, string field = nameof(Sale.Amount), RenderFragment? content = null) =>
    Component<PixataChartSeries>(content, (nameof(PixataChartSeries.Name), name), (nameof(PixataChartSeries.Data), data), (nameof(PixataChartSeries.Field), field), (nameof(PixataChartSeries.CategoryField), nameof(Sale.Month)));

  private static IRenderedComponent<PixataChart> RenderChart(BunitContext context, RenderFragment content, Action<ComponentParameterCollectionBuilder<PixataChart>>? parameters = null) =>
    context.Render<PixataChart>(p => {
      p.AddChildContent(content);
      parameters?.Invoke(p);
    });

  [TestMethod]
  public void Chart_RendersColumnsAxesAndLegend() {
    using BunitContext context = PixataTestContext.Create();
    List<Sale> sales = Sales();
    IRenderedComponent<PixataChart> cut = RenderChart(context, Component<PixataChartSeriesItems>(Fragments(ColumnSeries("Amount", sales), ColumnSeries("Other", sales, nameof(Sale.Other)))));
    Assert.AreEqual(5, cut.FindAll("rect.pixata-blazor-chart-column").Count);
    Assert.AreEqual(2, cut.FindAll("button.pixata-blazor-chart-legend-item").Count);
    Assert.IsTrue(cut.FindAll("text.pixata-blazor-chart-axis-label").Select(t => t.TextContent).Contains("Feb"));
    Assert.IsTrue(cut.Find("div.pixata-blazor-chart").ClassList.Contains("pixata-blazor-chart-animate"));
  }

  [TestMethod]
  public void Chart_HasChartAndPointRoles() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataChart> cut = RenderChart(context, Fragments(
      Component<PixataChartTitle>((nameof(PixataChartTitle.Text), "Sales"), (nameof(PixataChartTitle.Description), "Monthly sales")),
      Component<PixataChartSeriesItems>(ColumnSeries("Amount", Sales()))));
    IElement svg = cut.Find("svg.pixata-blazor-chart-surface");
    Assert.AreEqual("graphics-document", svg.GetAttribute("role"));
    Assert.AreEqual("chart", svg.GetAttribute("aria-roledescription"));
    Assert.AreEqual("Sales", svg.GetAttribute("aria-label"));
    Assert.AreEqual("0", svg.GetAttribute("tabindex"));
    Assert.AreEqual("Monthly sales", cut.Find($"#{svg.GetAttribute("aria-describedby")}").TextContent);
    IElement column = cut.Find("rect.pixata-blazor-chart-column");
    Assert.AreEqual("graphics-symbol", column.GetAttribute("role"));
    Assert.AreEqual("Amount, Jan: 10", column.GetAttribute("aria-label"));
    Assert.AreEqual("Sales", cut.Find(".pixata-blazor-chart-title").TextContent);
  }

  [TestMethod]
  public void Chart_ClickingAColumn_RaisesOnSeriesClick() {
    using BunitContext context = PixataTestContext.Create();
    List<Sale> sales = Sales();
    PixataChartSeriesClickEventArgs? clicked = null;
    IRenderedComponent<PixataChart> cut = RenderChart(context, Component<PixataChartSeriesItems>(ColumnSeries("Amount", sales)),
      p => p.Add(c => c.OnSeriesClick, (PixataChartSeriesClickEventArgs e) => clicked = e));
    cut.FindAll("rect.pixata-blazor-chart-column")[1].Click();
    Assert.IsNotNull(clicked);
    Assert.AreEqual("Feb", clicked.Category);
    Assert.AreEqual(1, clicked.CategoryIndex);
    Assert.AreEqual(20, clicked.Value);
    Assert.AreEqual("Amount", clicked.SeriesName);
    Assert.AreEqual(0, clicked.SeriesIndex);
    Assert.AreEqual("#ff6358", clicked.SeriesColor);
    Assert.AreSame(sales[1], clicked.DataItem);
  }

  [TestMethod]
  public void Chart_ClickingALegendItem_TogglesTheSeries() {
    using BunitContext context = PixataTestContext.Create();
    List<Sale> sales = Sales();
    PixataChartLegendItemClickEventArgs? clicked = null;
    IRenderedComponent<PixataChart> cut = RenderChart(context, Component<PixataChartSeriesItems>(Fragments(ColumnSeries("Amount", sales), ColumnSeries("Other", sales, nameof(Sale.Other)))),
      p => p.Add(c => c.OnLegendItemClick, (PixataChartLegendItemClickEventArgs e) => clicked = e));
    cut.FindAll("button.pixata-blazor-chart-legend-item")[0].Click();
    Assert.AreEqual(2, cut.FindAll("rect.pixata-blazor-chart-column").Count);
    Assert.AreEqual("false", cut.FindAll("button.pixata-blazor-chart-legend-item")[0].GetAttribute("aria-pressed"));
    Assert.IsTrue(cut.FindAll("button.pixata-blazor-chart-legend-item")[0].ClassList.Contains("pixata-blazor-chart-legend-item-hidden"));
    Assert.AreEqual(new PixataChartLegendItemClickEventArgs { SeriesIndex = 0, Text = "Amount", Visible = false }.Text, clicked!.Text);
    Assert.IsFalse(clicked.Visible);
    Assert.AreEqual(0, clicked.SeriesIndex);
    cut.FindAll("button.pixata-blazor-chart-legend-item")[0].Click();
    Assert.AreEqual(5, cut.FindAll("rect.pixata-blazor-chart-column").Count);
    Assert.IsTrue(clicked.Visible);
  }

  [TestMethod]
  public void Chart_PieLegend_TogglesSegments() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataChart> cut = RenderChart(context, Component<PixataChartSeriesItems>(Component<PixataChartSeries>(
      (nameof(PixataChartSeries.Type), PixataChartSeriesType.Pie), (nameof(PixataChartSeries.Data), Sales()), (nameof(PixataChartSeries.Field), nameof(Sale.Amount)), (nameof(PixataChartSeries.CategoryField), nameof(Sale.Month)))));
    Assert.AreEqual(3, cut.FindAll("path.pixata-blazor-chart-segment").Count);
    Assert.IsTrue(cut.Find("div.pixata-blazor-chart").ClassList.Contains("pixata-blazor-chart-pie"));
    cut.FindAll("button.pixata-blazor-chart-legend-item")[1].Click();
    Assert.AreEqual(2, cut.FindAll("path.pixata-blazor-chart-segment").Count);
  }

  [TestMethod]
  public void Chart_Tooltip_IsOffByDefault() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataChart> cut = RenderChart(context, Component<PixataChartSeriesItems>(ColumnSeries("Amount", Sales())));
    cut.Find("rect.pixata-blazor-chart-column").PointerEnter();
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-chart-tooltip").Count);
    Assert.IsTrue(cut.Find("rect.pixata-blazor-chart-column").ClassList.Contains("pixata-blazor-chart-point-active"), "The point is still highlighted");
  }

  [TestMethod]
  public void Chart_Tooltip_ShowsOnHoverAndStaysAfterATap() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataChart> cut = RenderChart(context, Fragments(
      Component<PixataChartTooltip>((nameof(PixataChartTooltip.Visible), true), (nameof(PixataChartTooltip.Format), "N1")),
      Component<PixataChartSeriesItems>(ColumnSeries("Amount", Sales()))));
    cut.FindAll("rect.pixata-blazor-chart-column")[2].PointerEnter();
    IElement tooltip = cut.Find(".pixata-blazor-chart-tooltip");
    Assert.AreEqual("30.0", tooltip.TextContent.Trim());
    Assert.AreEqual("tooltip", tooltip.GetAttribute("role"));
    StringAssert.Contains(tooltip.GetAttribute("style"), "--pixata-blazor-chart-tooltip-bg:var(--pixata-blazor-chart-series-1");
    cut.Find("svg").PointerLeave(new PointerEventArgs { PointerType = "touch" });
    Assert.AreEqual(1, cut.FindAll(".pixata-blazor-chart-tooltip").Count, "A tap's pointer leaving doesn't hide it");
    cut.Find("svg").PointerLeave(new PointerEventArgs { PointerType = "mouse" });
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-chart-tooltip").Count);
  }

  [TestMethod]
  public void Chart_TappingAPoint_ShowsItsTooltip() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataChart> cut = RenderChart(context, Fragments(
      Component<PixataChartTooltip>((nameof(PixataChartTooltip.Visible), true)),
      Component<PixataChartSeriesItems>(ColumnSeries("Amount", Sales()))));
    cut.FindAll("rect.pixata-blazor-chart-column")[0].Click();
    Assert.AreEqual("10", cut.Find(".pixata-blazor-chart-tooltip").TextContent.Trim());
    cut.Find("rect.pixata-blazor-chart-background").Click();
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-chart-tooltip").Count);
  }

  [TestMethod]
  public void Chart_TooltipTemplate_GetsThePoint() {
    using BunitContext context = PixataTestContext.Create();
    RenderFragment<PixataChartSeriesTooltipTemplateContext> template = c => b => b.AddContent(0, $"{c.SeriesName} {c.Category} {c.FormattedValue} {((Sale)c.DataItem!).Month}");
    IRenderedComponent<PixataChart> cut = RenderChart(context, Fragments(
      Component<PixataChartTooltip>((nameof(PixataChartTooltip.Visible), true), (nameof(PixataChartTooltip.Template), template)),
      Component<PixataChartSeriesItems>(ColumnSeries("Amount", Sales()))));
    cut.FindAll("rect.pixata-blazor-chart-column")[1].PointerEnter();
    Assert.AreEqual("Amount Feb 20 Feb", cut.Find(".pixata-blazor-chart-tooltip").TextContent.Trim());
  }

  [TestMethod]
  public void Chart_SeriesTooltip_OverridesTheChartTooltip() {
    using BunitContext context = PixataTestContext.Create();
    List<Sale> sales = Sales();
    IRenderedComponent<PixataChart> cut = RenderChart(context, Component<PixataChartSeriesItems>(Fragments(
      ColumnSeries("Amount", sales, content: Component<PixataChartSeriesTooltip>((nameof(PixataChartSeriesTooltip.Visible), true), (nameof(PixataChartSeriesTooltip.Background), "black"))),
      ColumnSeries("Other", sales, nameof(Sale.Other)))));
    cut.FindAll("rect.pixata-blazor-chart-column")[0].PointerEnter();
    StringAssert.Contains(cut.Find(".pixata-blazor-chart-tooltip").GetAttribute("style"), "--pixata-blazor-chart-tooltip-bg:black");
    cut.FindAll("rect.pixata-blazor-chart-column")[4].PointerEnter();
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-chart-tooltip").Count);
  }

  [TestMethod]
  public void Chart_SharedTooltip_ListsTheCategory() {
    using BunitContext context = PixataTestContext.Create();
    List<Sale> sales = Sales();
    IRenderedComponent<PixataChart> cut = RenderChart(context, Fragments(
      Component<PixataChartTooltip>((nameof(PixataChartTooltip.Visible), true), (nameof(PixataChartTooltip.Shared), true)),
      Component<PixataChartSeriesItems>(Fragments(ColumnSeries("Amount", sales), ColumnSeries("Other", sales, nameof(Sale.Other))))));
    cut.FindAll("rect.pixata-blazor-chart-band")[2].PointerEnter();
    IElement tooltip = cut.Find(".pixata-blazor-chart-tooltip-shared");
    Assert.AreEqual("Mar", tooltip.QuerySelector(".pixata-blazor-chart-tooltip-category")!.TextContent);
    CollectionAssert.AreEqual(new[] { "30", "15" }, tooltip.QuerySelectorAll(".pixata-blazor-chart-tooltip-value").Select(e => e.TextContent).ToArray());
    Assert.AreEqual(2, cut.FindAll("rect.pixata-blazor-chart-point-active").Count, "Both columns in the category are highlighted");
  }

  [TestMethod]
  public void Chart_SharedTemplate_GetsEveryPoint() {
    using BunitContext context = PixataTestContext.Create();
    List<Sale> sales = Sales();
    RenderFragment<PixataChartSeriesTooltipSharedTemplateContext> template = c => b => b.AddContent(0, $"{c.Category}: {string.Join(",", c.Points.Select(p => p.FormattedValue))}");
    IRenderedComponent<PixataChart> cut = RenderChart(context, Fragments(
      Component<PixataChartTooltip>((nameof(PixataChartTooltip.Visible), true), (nameof(PixataChartTooltip.Shared), true), (nameof(PixataChartTooltip.SharedTemplate), template)),
      Component<PixataChartSeriesItems>(Fragments(ColumnSeries("Amount", sales), ColumnSeries("Other", sales, nameof(Sale.Other))))));
    cut.FindAll("rect.pixata-blazor-chart-column")[0].PointerEnter();
    Assert.AreEqual("Jan: 10,5", cut.Find(".pixata-blazor-chart-tooltip").TextContent.Trim());
  }

  [TestMethod]
  public void Chart_Keyboard_MovesRoundThePointsAndClicks() {
    using BunitContext context = PixataTestContext.Create();
    List<Sale> sales = Sales();
    List<PixataChartSeriesClickEventArgs> clicks = [];
    IRenderedComponent<PixataChart> cut = RenderChart(context, Fragments(
      Component<PixataChartTooltip>((nameof(PixataChartTooltip.Visible), true)),
      Component<PixataChartSeriesItems>(Fragments(ColumnSeries("Amount", sales), ColumnSeries("Other", sales, nameof(Sale.Other))))),
      p => p.Add(c => c.OnSeriesClick, (PixataChartSeriesClickEventArgs e) => clicks.Add(e)));
    cut.Find("svg").Focus();
    IElement svg = cut.Find("svg");
    string? first = svg.GetAttribute("aria-activedescendant");
    Assert.IsNotNull(first);
    Assert.IsTrue(cut.Find($"#{first}").ClassList.Contains("pixata-blazor-chart-point-focused"));
    Assert.AreEqual("10", cut.Find(".pixata-blazor-chart-tooltip").TextContent.Trim());
    Assert.AreEqual("Amount, Jan: 10", cut.Find("[aria-live]").TextContent);
    cut.Find("svg").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
    Assert.AreEqual("Amount, Feb: 20", cut.Find($"#{cut.Find("svg").GetAttribute("aria-activedescendant")}").GetAttribute("aria-label"));
    cut.Find("svg").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
    Assert.AreEqual("Other, Jan: 5", cut.Find($"#{cut.Find("svg").GetAttribute("aria-activedescendant")}").GetAttribute("aria-label"), "Feb has no point in Other, so it goes to the nearest");
    cut.Find("svg").KeyDown(new KeyboardEventArgs { Key = "End" });
    cut.Find("svg").KeyDown(new KeyboardEventArgs { Key = "Enter" });
    Assert.AreEqual("Mar", clicks.Single().Category);
    Assert.AreEqual("Other", clicks.Single().SeriesName);
    cut.Find("svg").KeyDown(new KeyboardEventArgs { Key = "Escape" });
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-chart-tooltip").Count);
  }

  [TestMethod]
  public void Chart_ClickingWithTheMouse_DoesNotJumpToTheFirstPoint() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataChart> cut = RenderChart(context, Component<PixataChartSeriesItems>(ColumnSeries("Amount", Sales())));
    cut.Find("svg").PointerDown();
    cut.Find("svg").Focus();
    Assert.IsNull(cut.Find("svg").GetAttribute("aria-activedescendant"));
  }

  [TestMethod]
  public void Chart_Escape_DoesNotBubbleWhileTheTooltipIsOpen() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataChart> cut = RenderChart(context, Fragments(
      Component<PixataChartTooltip>((nameof(PixataChartTooltip.Visible), true)),
      Component<PixataChartSeriesItems>(ColumnSeries("Amount", Sales()))));
    cut.Find("rect.pixata-blazor-chart-column").PointerEnter();
    Assert.IsTrue(cut.Find("svg").HasAttribute("blazor:onkeydown:stoppropagation") || cut.Markup.Contains("stoppropagation", StringComparison.OrdinalIgnoreCase));
  }

  [TestMethod]
  public void Chart_LegendAndTitle_FollowTheirPositions() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataChart> cut = RenderChart(context, Fragments(
      Component<PixataChartTitle>((nameof(PixataChartTitle.Text), "Sales"), (nameof(PixataChartTitle.Position), PixataChartTitlePosition.Bottom), (nameof(PixataChartTitle.Align), PixataChartTitleAlign.Left)),
      Component<PixataChartLegend>((nameof(PixataChartLegend.Position), PixataChartLegendPosition.Left), (nameof(PixataChartLegend.Align), PixataChartLegendAlign.End)),
      Component<PixataChartSeriesItems>(ColumnSeries("Amount", Sales()))));
    IElement legend = cut.Find(".pixata-blazor-chart-legend");
    Assert.IsTrue(legend.ClassList.Contains("pixata-blazor-chart-legend-left"));
    Assert.IsTrue(legend.ClassList.Contains("pixata-blazor-chart-legend-vertical"));
    Assert.IsTrue(legend.ClassList.Contains("pixata-blazor-chart-legend-align-end"));
    IElement title = cut.Find(".pixata-blazor-chart-title");
    Assert.IsTrue(title.ClassList.Contains("pixata-blazor-chart-title-bottom"));
    Assert.IsTrue(title.ClassList.Contains("pixata-blazor-chart-title-align-left"));
    Assert.IsTrue(title.PreviousElementSibling!.ClassList.Contains("pixata-blazor-chart-body"), "The title comes after the chart");
  }

  [TestMethod]
  public async Task Chart_OnANarrowScreen_MovesASideLegendUnderneath() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataChart> cut = RenderChart(context, Component<PixataChartSeriesItems>(ColumnSeries("Amount", Sales())));
    Assert.IsTrue(cut.Find(".pixata-blazor-chart-legend").ClassList.Contains("pixata-blazor-chart-legend-right"));
    await cut.InvokeAsync(() => cut.Instance.OnPlotResized(360, 340, 300));
    Assert.IsTrue(cut.Find(".pixata-blazor-chart-legend").ClassList.Contains("pixata-blazor-chart-legend-bottom"));
    Assert.IsTrue(cut.Find(".pixata-blazor-chart-legend").ClassList.Contains("pixata-blazor-chart-legend-horizontal"));
    Assert.IsTrue(cut.Find("div.pixata-blazor-chart").ClassList.Contains("pixata-blazor-chart-compact"));
    Assert.AreEqual("0 0 340 300", cut.Find("svg").GetAttribute("viewBox"), "The drawing is the size the browser reported");
  }

  [TestMethod]
  public void Chart_LegendCanBeHidden() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataChart> cut = RenderChart(context, Fragments(
      Component<PixataChartLegend>((nameof(PixataChartLegend.Visible), false)),
      Component<PixataChartSeriesItems>(ColumnSeries("Amount", Sales()))));
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-chart-legend").Count);
  }

  [TestMethod]
  [Timeout(10000)]
  public void Chart_ChangedSettings_RedrawTheChart() {
    using BunitContext context = PixataTestContext.Create();
    RenderFragment Content(PixataChartSeriesType type) =>
      Fragments(
        Component<PixataChartCategoryAxes>(Component<PixataChartCategoryAxis>((nameof(PixataChartCategoryAxis.Categories), new object[] { "A", "B" }))),
        Component<PixataChartSeriesItems>(Component<PixataChartSeries>((nameof(PixataChartSeries.Type), type), (nameof(PixataChartSeries.Data), new object[] { 1.0, 2.0 }))));
    IRenderedComponent<PixataChart> cut = RenderChart(context, Content(PixataChartSeriesType.Column));
    Assert.AreEqual(2, cut.FindAll("rect.pixata-blazor-chart-column").Count);
    cut.Render(p => p.AddChildContent(Content(PixataChartSeriesType.Line)));
    Assert.AreEqual(0, cut.FindAll("rect.pixata-blazor-chart-column").Count);
    Assert.AreEqual(2, cut.FindAll("g.pixata-blazor-chart-marker").Count);
    Assert.AreEqual(1, cut.FindAll("path.pixata-blazor-chart-line").Count);
  }

  [TestMethod]
  public void Chart_RemovedSeries_Disappear() {
    using BunitContext context = PixataTestContext.Create();
    List<Sale> sales = Sales();
    IRenderedComponent<PixataChart> cut = RenderChart(context, Component<PixataChartSeriesItems>(Fragments(ColumnSeries("Amount", sales), ColumnSeries("Other", sales, nameof(Sale.Other)))));
    cut.Render(p => p.AddChildContent(Component<PixataChartSeriesItems>(ColumnSeries("Amount", sales))));
    Assert.AreEqual(3, cut.FindAll("rect.pixata-blazor-chart-column").Count);
    Assert.AreEqual(1, cut.FindAll("button.pixata-blazor-chart-legend-item").Count);
  }

  [TestMethod]
  public async Task Chart_Refresh_RedrawsDataChangedInPlace() {
    using BunitContext context = PixataTestContext.Create();
    List<Sale> sales = Sales();
    IRenderedComponent<PixataChart> cut = RenderChart(context, Component<PixataChartSeriesItems>(ColumnSeries("Amount", sales)));
    sales.Add(new() { Month = "Apr", Amount = 40 });
    await cut.InvokeAsync(cut.Instance.Refresh);
    cut.WaitForAssertion(() => Assert.AreEqual(4, cut.FindAll("rect.pixata-blazor-chart-column").Count));
  }

  [TestMethod]
  public void Chart_SeriesVisibleParameter_HidesTheSeries() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataChart> cut = RenderChart(context, Component<PixataChartSeriesItems>(Component<PixataChartSeries>(
      (nameof(PixataChartSeries.Name), "A"), (nameof(PixataChartSeries.Data), new object[] { 1.0 }), (nameof(PixataChartSeries.Visible), false))));
    Assert.AreEqual(0, cut.FindAll("rect.pixata-blazor-chart-column").Count);
    cut.Find("button.pixata-blazor-chart-legend-item").Click();
    Assert.AreEqual(1, cut.FindAll("rect.pixata-blazor-chart-column").Count, "The legend can show it");
  }

  [TestMethod]
  public void Chart_LabelsAndMarkers_Render() {
    using BunitContext context = PixataTestContext.Create();
    Func<PixataChartSeriesLabelsTemplateContext, string> text = c => $"{c.Category}={c.Value}";
    IRenderedComponent<PixataChart> cut = RenderChart(context, Component<PixataChartSeriesItems>(Component<PixataChartSeries>(
      Fragments(
        Component<PixataChartSeriesLabels>((nameof(PixataChartSeriesLabels.Visible), true), (nameof(PixataChartSeriesLabels.TextTemplate), text)),
        Component<PixataChartSeriesMarkers>((nameof(PixataChartSeriesMarkers.Type), PixataChartSeriesMarkersType.Square), (nameof(PixataChartSeriesMarkers.Size), 10.0))),
      (nameof(PixataChartSeries.Type), PixataChartSeriesType.Line), (nameof(PixataChartSeries.Data), Sales()), (nameof(PixataChartSeries.Field), nameof(Sale.Amount)), (nameof(PixataChartSeries.CategoryField), nameof(Sale.Month)))));
    CollectionAssert.AreEqual(new[] { "Jan=10", "Feb=20", "Mar=30" }, cut.FindAll("text.pixata-blazor-chart-label").Select(t => t.TextContent).ToArray());
    IElement marker = cut.Find("g.pixata-blazor-chart-marker rect.pixata-blazor-chart-marker-shape");
    Assert.AreEqual("10", marker.GetAttribute("width"));
  }

  [TestMethod]
  public void Chart_ChangedLabelTemplate_RedrawsTheLabels() {
    using BunitContext context = PixataTestContext.Create();
    RenderFragment Content(string prefix) {
      Func<PixataChartSeriesLabelsTemplateContext, string> text = c => $"{prefix}{c.Value}";
      return Component<PixataChartSeriesItems>(Component<PixataChartSeries>(
        Component<PixataChartSeriesLabels>((nameof(PixataChartSeriesLabels.Visible), true), (nameof(PixataChartSeriesLabels.TextTemplate), text)),
        (nameof(PixataChartSeries.Data), new object[] { 1.0, 2.0 })));
    }
    IRenderedComponent<PixataChart> cut = RenderChart(context, Content("a"));
    CollectionAssert.AreEqual(new[] { "a1", "a2" }, cut.FindAll("text.pixata-blazor-chart-label").Select(t => t.TextContent).ToArray());
    cut.Render(p => p.AddChildContent(Content("b")));
    CollectionAssert.AreEqual(new[] { "b1", "b2" }, cut.FindAll("text.pixata-blazor-chart-label").Select(t => t.TextContent).ToArray());
  }

  [TestMethod]
  public void Chart_SeriesShownAgain_KeepsItsPlaceInTheLegendAndItsColour() {
    using BunitContext context = PixataTestContext.Create();
    List<Sale> sales = Sales();
    // As Razor renders @if (showFirst) { <PixataChartSeries ... /> } <PixataChartSeries ... />, with its own sequence numbers
    RenderFragment Content(bool showFirst) =>
      Component<PixataChartSeriesItems>(builder => {
        if (showFirst) {
          builder.AddContent(0, ColumnSeries("Amount", sales));
        }
        builder.AddContent(1, ColumnSeries("Other", sales, nameof(Sale.Other)));
      });
    IRenderedComponent<PixataChart> cut = RenderChart(context, Content(true));
    cut.Render(p => p.AddChildContent(Content(false)));
    cut.Render(p => p.AddChildContent(Content(true)));
    CollectionAssert.AreEqual(new[] { "Amount", "Other" }, cut.FindAll(".pixata-blazor-chart-legend-text").Select(t => t.TextContent).ToArray());
    StringAssert.Contains(cut.FindAll("rect.pixata-blazor-chart-column")[0].GetAttribute("style"), "--pixata-blazor-chart-series-1");
    Assert.AreEqual("Amount, Jan: 10", cut.FindAll("rect.pixata-blazor-chart-column")[0].GetAttribute("aria-label"));
  }

  [TestMethod]
  public void Chart_ValueAxisShownAgain_KeepsItsSide() {
    using BunitContext context = PixataTestContext.Create();
    RenderFragment Axis(string name) =>
      Component<PixataChartValueAxis>(Component<PixataChartValueAxisTitle>((nameof(PixataChartValueAxisTitle.Text), name)), (nameof(PixataChartValueAxis.Name), name));
    RenderFragment Content(bool showFirst) => Fragments(
      Component<PixataChartSeriesItems>(ColumnSeries("Amount", Sales())),
      Component<PixataChartCategoryAxes>(Component<PixataChartCategoryAxis>((nameof(PixataChartCategoryAxis.AxisCrossingValue), new object[] { 0, 100 }))),
      Component<PixataChartValueAxes>(builder => {
        if (showFirst) {
          builder.AddContent(0, Axis("left"));
        }
        builder.AddContent(1, Axis("right"));
      }));
    IRenderedComponent<PixataChart> cut = RenderChart(context, Content(true));
    cut.Render(p => p.AddChildContent(Content(false)));
    cut.Render(p => p.AddChildContent(Content(true)));
    IElement[] titles = [.. cut.FindAll("text.pixata-blazor-chart-axis-title")];
    double X(string text) => double.Parse(titles.Single(t => t.TextContent == text).GetAttribute("x")!, CultureInfo.InvariantCulture);
    Assert.IsTrue(X("left") < X("right"), "The first declared axis is on the left again");
  }

  [TestMethod]
  public async Task Chart_RemovedWhileItsScriptIsAttaching_IsDetachedWhenTheScriptAnswers() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = context.JSInterop.SetupModule("./_content/Pixata.Blazor/Components/Chart/PixataChart.razor.js");
    // attach() returns a private type, so its handler is set up by reflection
    Type resultType = typeof(PixataChart).GetNestedType("AttachResult", BindingFlags.NonPublic)!;
    MethodInfo setup = typeof(BunitJSInteropSetupExtensions).GetMethod(nameof(BunitJSInteropSetupExtensions.Setup), [typeof(BunitJSInterop), typeof(string), typeof(InvocationMatcher)])!.MakeGenericMethod(resultType);
    object handler = setup.Invoke(null, [module, "attach", (InvocationMatcher)(_ => true)])!;
    RenderChart(context, Component<PixataChartSeriesItems>(ColumnSeries("Amount", Sales())));
    await context.DisposeComponentsAsync();
    Assert.IsFalse(module.Invocations["detach"].Any());
    object result = Activator.CreateInstance(resultType, true)!;
    resultType.GetProperty("Id")!.SetValue(result, 5);
    handler.GetType().GetMethod("SetResult")!.Invoke(handler, [result]);
    for (int i = 0; i < 100 && !module.Invocations["detach"].Any(); i++) {
      await Task.Delay(10);
    }
    Assert.IsTrue(module.Invocations["detach"].Any(i => Equals(i.Arguments[0], 5)), "The script's attachment is removed");
  }

  [TestMethod]
  public void Chart_AxisTitlesAndFormats_Render() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataChart> cut = RenderChart(context, Fragments(
      Component<PixataChartSeriesItems>(ColumnSeries("Amount", Sales())),
      Component<PixataChartCategoryAxes>(Component<PixataChartCategoryAxis>(Component<PixataChartCategoryAxisTitle>((nameof(PixataChartCategoryAxisTitle.Text), "Month")))),
      Component<PixataChartValueAxes>(Component<PixataChartValueAxis>(Fragments(
        Component<PixataChartValueAxisTitle>((nameof(PixataChartValueAxisTitle.Text), "Sales")),
        Component<PixataChartValueAxisLabels>((nameof(PixataChartValueAxisLabels.Format), "{0:C0}"))),
        (nameof(PixataChartValueAxis.Max), 40.0)))));
    string[] titles = cut.FindAll("text.pixata-blazor-chart-axis-title").Select(t => t.TextContent).ToArray();
    CollectionAssert.AreEquivalent(new[] { "Month", "Sales" }, titles);
    string[] labels = cut.FindAll("text.pixata-blazor-chart-axis-label").Select(t => t.TextContent).ToArray();
    Assert.IsTrue(labels.Contains("£40"));
    Assert.IsTrue(labels.Contains("£0"));
  }

  #endregion
}
