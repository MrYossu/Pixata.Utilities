using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests.Components;

[TestClass]
public class Heatmap_Tests {
  private static readonly string[] Rows = ["Mon", "Tue"];
  private static readonly string[] Columns = ["09", "10", "11"];

  private static double?[,] Values() =>
    new double?[,] {
      { 0, 5, 10 },
      { null, 2.5, 10 }
    };

  private static IRenderedComponent<PixataHeatmap> Render(BunitContext context, Action<ComponentParameterCollectionBuilder<PixataHeatmap>>? extra = null) =>
    context.Render<PixataHeatmap>(p => {
      p.Add(c => c.RowLabels, Rows)
        .Add(c => c.ColumnLabels, Columns)
        .Add(c => c.Values, Values());
      extra?.Invoke(p);
    });

  [TestMethod]
  public void Renders_ATableWithTheLabelsAndACellPerValue() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataHeatmap> cut = Render(context);
    CollectionAssert.AreEqual(Columns, cut.FindAll("th[scope=col]").Select(th => th.TextContent).ToArray());
    CollectionAssert.AreEqual(Rows, cut.FindAll("th[scope=row]").Select(th => th.TextContent).ToArray());
    Assert.AreEqual(6, cut.FindAll("td.pixata-blazor-heatmap-cell").Count);
  }

  [TestMethod]
  public void Cells_AreColouredByHowFarUpTheScaleTheyAre() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataHeatmap> cut = Render(context);
    IReadOnlyList<IElement> cells = cut.FindAll("td.pixata-blazor-heatmap-cell");
    Assert.AreEqual("--pixata-blazor-heatmap-level: 0%", cells[0].GetAttribute("style"));
    Assert.AreEqual("--pixata-blazor-heatmap-level: 50%", cells[1].GetAttribute("style"));
    Assert.AreEqual("--pixata-blazor-heatmap-level: 100%", cells[2].GetAttribute("style"));
    Assert.AreEqual("--pixata-blazor-heatmap-level: 25%", cells[4].GetAttribute("style"));
  }

  [TestMethod]
  public void HighCells_GetTheStrongClass_SoTheirTextShows() {
    using BunitContext context = PixataTestContext.Create();
    IReadOnlyList<IElement> cells = Render(context).FindAll("td.pixata-blazor-heatmap-cell");
    Assert.IsFalse(cells[1].ClassList.Contains("pixata-blazor-heatmap-cell-strong"));
    Assert.IsTrue(cells[2].ClassList.Contains("pixata-blazor-heatmap-cell-strong"));
  }

  [TestMethod]
  public void NullValue_IsAnEmptyCell() {
    using BunitContext context = PixataTestContext.Create();
    IElement cell = Render(context).FindAll("td.pixata-blazor-heatmap-cell")[3];
    Assert.IsTrue(cell.ClassList.Contains("pixata-blazor-heatmap-cell-empty"));
    Assert.IsNull(cell.GetAttribute("style"));
    Assert.AreEqual("Tue, 09: No data", cell.GetAttribute("title"));
  }

  [TestMethod]
  public void MinAndMax_SetTheScale() {
    using BunitContext context = PixataTestContext.Create();
    IReadOnlyList<IElement> cells = Render(context, p => p.Add(c => c.Min, 5).Add(c => c.Max, 20)).FindAll("td.pixata-blazor-heatmap-cell");
    Assert.AreEqual("--pixata-blazor-heatmap-level: 0%", cells[0].GetAttribute("style"));
    Assert.AreEqual("--pixata-blazor-heatmap-level: 33.33%", cells[2].GetAttribute("style"));
  }

  [TestMethod]
  public void Values_AreHiddenTextByDefault_AndShownWhenAskedFor() {
    using BunitContext context = PixataTestContext.Create();
    IElement hidden = Render(context).FindAll("td.pixata-blazor-heatmap-cell")[1];
    Assert.AreEqual("5", hidden.QuerySelector(".pixata-blazor-visually-hidden")!.TextContent);

    IElement shown = Render(context, p => p.Add(c => c.ShowValues, true).Add(c => c.Format, "{0:N1}")).FindAll("td.pixata-blazor-heatmap-cell")[4];
    Assert.IsNull(shown.QuerySelector(".pixata-blazor-visually-hidden"));
    Assert.AreEqual("2.5", shown.TextContent.Trim());
  }

  [TestMethod]
  public void Tooltip_CanBeCustomised() {
    using BunitContext context = PixataTestContext.Create();
    IElement cell = Render(context, p => p.Add(c => c.CellTooltip, (PixataHeatmapCell c) => $"{c.Value} at {c.ColumnLabel} on row {c.Row}")).FindAll("td.pixata-blazor-heatmap-cell")[2];
    Assert.AreEqual("10 at 11 on row 0", cell.GetAttribute("title"));
  }

  [TestMethod]
  public void Legend_ShowsTheEndsOfTheScale() {
    using BunitContext context = PixataTestContext.Create();
    IReadOnlyList<IElement> ends = Render(context).FindAll(".pixata-blazor-heatmap-legend-text");
    Assert.AreEqual("0", ends[0].TextContent);
    Assert.AreEqual("10", ends[1].TextContent);
  }

  [TestMethod]
  public void Legend_IsLeftOutWhenTurnedOffOrThereAreNoValues() {
    using BunitContext context = PixataTestContext.Create();
    Assert.AreEqual(0, Render(context, p => p.Add(c => c.ShowLegend, false)).FindAll(".pixata-blazor-heatmap-legend").Count);
    IRenderedComponent<PixataHeatmap> empty = context.Render<PixataHeatmap>(p => p
      .Add(c => c.RowLabels, ["A"])
      .Add(c => c.ColumnLabels, ["B"])
      .Add(c => c.Values, new double?[1, 1]));
    Assert.AreEqual(0, empty.FindAll(".pixata-blazor-heatmap-legend").Count);
  }

  [TestMethod]
  public void Title_NamesTheTable() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataHeatmap> cut = Render(context, p => p.Add(c => c.Title, "Visitors"));
    string titleId = cut.Find(".pixata-blazor-heatmap-title").Id!;
    Assert.AreEqual(titleId, cut.Find("table").GetAttribute("aria-labelledby"));
  }

  [TestMethod]
  public void Values_OfTheWrongSize_Throw() {
    using BunitContext context = PixataTestContext.Create();
    Assert.ThrowsException<InvalidOperationException>(() => context.Render<PixataHeatmap>(p => p
      .Add(c => c.RowLabels, Rows)
      .Add(c => c.ColumnLabels, Columns)
      .Add(c => c.Values, new double?[3, 3])));
  }
}
