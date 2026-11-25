using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests.Components;

[TestClass]
public class ColorPicker_Tests {
  private BunitContext _ctx = null!;
  private readonly List<string?> _values = [];
  private readonly List<object?> _changes = [];

  [TestInitialize]
  public void Initialise() {
    _ctx = new();
    // The popup is placed and the gradient dragged by script, which doesn't matter here
    _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    _values.Clear();
    _changes.Clear();
  }

  [TestCleanup]
  public void Cleanup() =>
    _ctx.Dispose();

  private IRenderedComponent<PixataColorPicker> Render(string? value = null, Action<ComponentParameterCollectionBuilder<PixataColorPicker>>? more = null) =>
    _ctx.Render<PixataColorPicker>(p => {
      p.Add(c => c.Value, value)
        .Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => _values.Add(v)))
        .Add(c => c.OnChange, EventCallback.Factory.Create<object?>(this, v => _changes.Add(v)));
      more?.Invoke(p);
    });

  private static void Open(IRenderedComponent<PixataColorPicker> picker) =>
    picker.Find("button.pixata-blazor-colorpicker").Click();

  private static IElement Button(IRenderedComponent<PixataColorPicker> picker, string text) =>
    picker.FindAll(".pixata-blazor-colorpicker-popup button").Single(b => b.TextContent.Trim() == text || b.GetAttribute("aria-label") == text);

  private static void ShowPalette(IRenderedComponent<PixataColorPicker> picker) =>
    Button(picker, "Palette").Click();

  private static IElement Tile(IRenderedComponent<PixataColorPicker> picker, string hex) =>
    picker.FindAll(".pixata-blazor-colorpalette-tile").First(t => t.GetAttribute("title") == hex);

  // ---------- Reading and writing colours ----------

  [TestMethod]
  public void Colour_ReadsHexAndRgb() {
    Assert.AreEqual(new PixataColour(255, 0, 0), PixataColour.Parse("#f00"));
    Assert.AreEqual(new PixataColour(255, 0, 0, .53), PixataColour.Parse("#f008"));
    Assert.AreEqual(new PixataColour(40, 47, 137), PixataColour.Parse("#282F89"));
    Assert.AreEqual(new PixataColour(40, 47, 137, .5), PixataColour.Parse("#282f8980"));
    Assert.AreEqual(new PixataColour(40, 47, 137), PixataColour.Parse("rgb(40, 47, 137)"));
    Assert.AreEqual(new PixataColour(40, 47, 137, .5), PixataColour.Parse("rgba(40, 47, 137, 0.5)"));
    Assert.AreEqual(new PixataColour(40, 47, 137, .5), PixataColour.Parse("rgb(40 47 137 / 50%)"));
    Assert.AreEqual(new PixataColour(255, 128, 0), PixataColour.Parse("rgb(100%, 50%, 0%)"));
  }

  [TestMethod]
  public void Colour_RejectsAnythingElse() {
    Assert.IsNull(PixataColour.Parse(null));
    Assert.IsNull(PixataColour.Parse(""));
    Assert.IsNull(PixataColour.Parse("red"));
    Assert.IsNull(PixataColour.Parse("ff0000"));
    Assert.IsNull(PixataColour.Parse("#ff000"));
    Assert.IsNull(PixataColour.Parse("#gg0000"));
    Assert.IsNull(PixataColour.Parse("rgb(1, 2)"));
  }

  [TestMethod]
  public void Colour_WritesHexAndRgb() {
    Assert.AreEqual("#282f89", new PixataColour(40, 47, 137).ToHex());
    Assert.AreEqual("#282f8980", new PixataColour(40, 47, 137, .5).ToHex());
    Assert.AreEqual("rgb(40, 47, 137)", new PixataColour(40, 47, 137).ToRgb());
    Assert.AreEqual("rgba(40, 47, 137, 0.5)", new PixataColour(40, 47, 137, .5).ToRgb());
  }

  [TestMethod]
  public void Colour_GoesToAndFromHsv() {
    foreach (PixataColour colour in new PixataColour[] { new(255, 0, 0), new(40, 47, 137), new(0, 0, 0), new(255, 255, 255), new(128, 128, 128), new(230, 25, 230) }) {
      (double h, double s, double v) = colour.ToHsv();
      Assert.AreEqual(colour, PixataColour.FromHsv(h, s, v), colour.ToHex());
    }
    Assert.AreEqual(new PixataColour(0, 255, 0), PixataColour.FromHsv(120, 1, 1));
  }

  [TestMethod]
  public void Editor_StillComparesColoursAsHex() {
    Assert.AreEqual("#e61919", PixataEditorToolbarColourPicker.Hex("rgb(230, 25, 25)"));
    Assert.AreEqual("#aabbcc", PixataEditorToolbarColourPicker.Hex("#ABC"));
    Assert.AreEqual("#aabbcc", PixataEditorToolbarColourPicker.Hex("#aabbcc80"));
    Assert.AreEqual("red", PixataEditorToolbarColourPicker.Hex("Red"));
    Assert.IsNull(PixataEditorToolbarColourPicker.Hex("#zzz"));
    Assert.IsNull(PixataEditorToolbarColourPicker.Hex(" "));
    Assert.AreSame(PixataColorPicker.DefaultPaletteColors, PixataEditorColorTool.DefaultColors);
  }

  // ---------- The button ----------

  [TestMethod]
  public void Button_ShowsTheColour_OrNone() {
    IRenderedComponent<PixataColorPicker> red = Render("#ff0000");
    IRenderedComponent<PixataColorPicker> none = Render();

    StringAssert.Contains(red.Find(".pixata-blazor-colorpicker-value .pixata-blazor-colorpicker-swatch").GetAttribute("style"), "rgb(255, 0, 0)");
    Assert.AreEqual(1, none.FindAll(".pixata-blazor-colorpicker-value .pixata-blazor-colorpicker-none").Count);
    Assert.AreEqual("No colour", none.Find("button.pixata-blazor-colorpicker").GetAttribute("title"));
  }

  [TestMethod]
  public void Button_HasTheInputsClasses() {
    IRenderedComponent<PixataColorPicker> picker = Render("#ff0000", p => p.Add(c => c.Size, "sm").Add(c => c.Class, "mine"));

    string classes = picker.Find("button.pixata-blazor-colorpicker").GetAttribute("class")!;
    StringAssert.Contains(classes, "pixata-blazor-input ");
    StringAssert.Contains(classes, "pixata-blazor-input-sm");
    StringAssert.Contains(classes, "mine");
  }

  [TestMethod]
  public void Disabled_DoesNotOpen() {
    IRenderedComponent<PixataColorPicker> picker = Render("#ff0000", p => p.Add(c => c.Enabled, false));

    Assert.IsTrue(picker.Find("button.pixata-blazor-colorpicker").HasAttribute("disabled"));
    picker.InvokeAsync(() => picker.Instance.Open()).Wait();
    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-colorpicker-popup").Count);
  }

  // ---------- The popup ----------

  [TestMethod]
  public void Popup_OpensOnTheGradient_WithBothViews() {
    IRenderedComponent<PixataColorPicker> picker = Render("#ff0000");

    Open(picker);

    Assert.AreEqual(1, picker.FindAll(".pixata-blazor-colorgradient").Count);
    Assert.AreEqual(2, picker.FindAll(".pixata-blazor-colorpicker-view-button").Count);
    Assert.AreEqual("true", picker.Find("button.pixata-blazor-colorpicker").GetAttribute("aria-expanded"));
  }

  [TestMethod]
  public void Palette_PickAndApply_WritesRgbByDefault() {
    IRenderedComponent<PixataColorPicker> picker = Render("#ff0000");

    Open(picker);
    ShowPalette(picker);
    Tile(picker, "#000000").Click();
    Assert.AreEqual(0, _values.Count);
    Button(picker, "Apply").Click();

    CollectionAssert.AreEqual(new[] { "rgb(0, 0, 0)" }, _values);
    CollectionAssert.AreEqual(new object?[] { "rgb(0, 0, 0)" }, _changes);
    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-colorpicker-popup").Count);
  }

  [TestMethod]
  public void ValueFormat_Hex_WritesHex() {
    IRenderedComponent<PixataColorPicker> picker = Render("#ff0000", p => p.Add(c => c.ValueFormat, PixataColorFormat.Hex));

    Open(picker);
    ShowPalette(picker);
    Tile(picker, "#000000").Click();
    Button(picker, "Apply").Click();

    CollectionAssert.AreEqual(new[] { "#000000" }, _values);
  }

  [TestMethod]
  public void Cancel_ThrowsThePickAway() {
    IRenderedComponent<PixataColorPicker> picker = Render("#ff0000");

    Open(picker);
    ShowPalette(picker);
    Tile(picker, "#000000").Click();
    Button(picker, "Cancel").Click();

    Assert.AreEqual(0, _values.Count);
    Assert.AreEqual(0, _changes.Count);
    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-colorpicker-popup").Count);
  }

  [TestMethod]
  public void Escape_Closes() {
    IRenderedComponent<PixataColorPicker> picker = Render("#ff0000");

    Open(picker);
    picker.Find(".pixata-blazor-colorgradient").KeyDown("Escape");

    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-colorpicker-popup").Count);
    Assert.AreEqual(0, _values.Count);
  }

  [TestMethod]
  public void Apply_WithoutAChange_KeepsTheValueAsItWasWritten() {
    IRenderedComponent<PixataColorPicker> picker = Render("#FF0000");

    Open(picker);
    Button(picker, "Apply").Click();

    Assert.AreEqual(0, _values.Count);
    Assert.AreEqual(0, _changes.Count);
  }

  [TestMethod]
  public void WithoutButtons_APaletteColourIsTheValueStraightAway() {
    IRenderedComponent<PixataColorPicker> picker = Render("#ff0000", p => p.Add(c => c.ShowButtons, false));

    Open(picker);
    ShowPalette(picker);
    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-colorpicker-footer").Count);
    Tile(picker, "#000000").Click();

    CollectionAssert.AreEqual(new[] { "rgb(0, 0, 0)" }, _values);
    CollectionAssert.AreEqual(new object?[] { "rgb(0, 0, 0)" }, _changes);
    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-colorpicker-popup").Count);
  }

  [TestMethod]
  public void Clear_MakesTheValueNull() {
    IRenderedComponent<PixataColorPicker> picker = Render("#ff0000");

    Open(picker);
    Button(picker, "Clear").Click();
    Assert.AreEqual(1, picker.FindAll(".pixata-blazor-colorpicker-preview-new.pixata-blazor-colorpicker-none").Count);
    Button(picker, "Apply").Click();

    CollectionAssert.AreEqual(new string?[] { null }, _values);
  }

  [TestMethod]
  public void ShowClearButtonAndShowPreview_CanBeTurnedOff() {
    IRenderedComponent<PixataColorPicker> picker = Render("#ff0000", p => p.Add(c => c.ShowClearButton, false).Add(c => c.ShowPreview, false));

    Open(picker);

    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-colorpicker-clear").Count);
    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-colorpicker-preview").Count);
  }

  [TestMethod]
  public void Preview_CurrentColourGoesBackToIt() {
    IRenderedComponent<PixataColorPicker> picker = Render("#ff0000");

    Open(picker);
    ShowPalette(picker);
    Tile(picker, "#000000").Click();
    picker.Find(".pixata-blazor-colorpicker-preview-current").Click();
    Button(picker, "Apply").Click();

    Assert.AreEqual(0, _values.Count);
  }

  // ---------- The gradient ----------

  [TestMethod]
  public void Gradient_Dragging_PicksTheShade() {
    IRenderedComponent<PixataColorPicker> picker = Render("#ff0000", p => p.Add(c => c.ValueFormat, PixataColorFormat.Hex));

    Open(picker);
    picker.InvokeAsync(() => picker.Instance.OnGradientDrag(0, 0)).Wait();
    Button(picker, "Apply").Click();

    CollectionAssert.AreEqual(new[] { "#ffffff" }, _values);
  }

  [TestMethod]
  public void Gradient_ArrowKeysMoveAround() {
    IRenderedComponent<PixataColorPicker> picker = Render("#ff0000", p => p.Add(c => c.ValueFormat, PixataColorFormat.Hex));

    Open(picker);
    for (int i = 0; i < 5; i++) {
      picker.Find(".pixata-blazor-colorgradient").KeyDown(new KeyboardEventArgs { Key = "ArrowDown", ShiftKey = true });
    }
    Button(picker, "Apply").Click();

    CollectionAssert.AreEqual(new[] { "#800000" }, _values);
  }

  [TestMethod]
  public void Gradient_HueSlider_ChangesTheHue() {
    IRenderedComponent<PixataColorPicker> picker = Render("#ff0000", p => p.Add(c => c.ValueFormat, PixataColorFormat.Hex));

    Open(picker);
    picker.Find(".pixata-blazor-colorgradient-hue").Input("120");
    Button(picker, "Apply").Click();

    CollectionAssert.AreEqual(new[] { "#00ff00" }, _values);
  }

  [TestMethod]
  public void Gradient_OpacitySlider_GivesASeeThroughColour() {
    IRenderedComponent<PixataColorPicker> picker = Render("#ff0000");

    Open(picker);
    picker.Find(".pixata-blazor-colorgradient-alpha").Input("50");
    Button(picker, "Apply").Click();

    CollectionAssert.AreEqual(new[] { "rgba(255, 0, 0, 0.5)" }, _values);
  }

  [TestMethod]
  public void Gradient_HexBox_TakesAColour() {
    IRenderedComponent<PixataColorPicker> picker = Render("#ff0000", p => p.Add(c => c.ValueFormat, PixataColorFormat.Hex));

    Open(picker);
    Button(picker, "RGB").Click();
    picker.Find(".pixata-blazor-colorgradient-hex input").Change("282F89");
    Button(picker, "Apply").Click();

    CollectionAssert.AreEqual(new[] { "#282f89" }, _values);
  }

  [TestMethod]
  public void Gradient_HexBox_PutsTheColourBackIfItCantBeRead() {
    IRenderedComponent<PixataColorPicker> picker = Render("#ff0000");

    Open(picker);
    Button(picker, "RGB").Click();
    picker.Find(".pixata-blazor-colorgradient-hex input").Change("nonsense");

    Assert.AreEqual("#ff0000", picker.Find(".pixata-blazor-colorgradient-hex input").GetAttribute("value"));
  }

  [TestMethod]
  public void Gradient_RgbBoxes_SetEachChannel() {
    IRenderedComponent<PixataColorPicker> picker = Render("#ff0000");

    Open(picker);
    IElement[] boxes = [.. picker.FindAll(".pixata-blazor-colorgradient-channel input")];
    Assert.AreEqual(4, boxes.Length);
    boxes[1].Change("128");
    picker.FindAll(".pixata-blazor-colorgradient-channel input")[2].Change("300");
    Button(picker, "Apply").Click();

    CollectionAssert.AreEqual(new[] { "rgb(255, 128, 255)" }, _values);
  }

  [TestMethod]
  public void GradientView_CanHideTheOpacityEditor() {
    IRenderedComponent<PixataColorPicker> picker = Render("#ff0000", p => p
      .Add<PixataColorPickerGradientView>(c => c.ColorPickerViews, v => v.Add(g => g.ShowOpacityEditor, false).Add(g => g.Format, PixataColorFormat.Hex)));

    Open(picker);

    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-colorgradient-alpha").Count);
    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-colorpicker-view-button").Count);
    Assert.AreEqual(1, picker.FindAll(".pixata-blazor-colorgradient-hex").Count);
  }

  // ---------- The palette ----------

  [TestMethod]
  public void PaletteView_OnItsOwn_UsesItsColoursAndColumns() {
    IRenderedComponent<PixataColorPicker> picker = Render("#00ff00", p => p
      .Add<PixataColorPickerPaletteView>(c => c.ColorPickerViews, v => v
        .Add(g => g.Colors, new[] { "#ff0000", "#00ff00", "#0000ff", "rgb(0, 0, 0)", "not a colour" })
        .Add(g => g.Columns, 2)
        .Add(g => g.TileWidth, "30px")));

    Open(picker);

    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-colorgradient").Count);
    Assert.AreEqual(2, picker.FindAll(".pixata-blazor-colorpalette-row").Count);
    Assert.AreEqual(4, picker.FindAll(".pixata-blazor-colorpalette-tile").Count);
    Assert.AreEqual("#00ff00", picker.Find(".pixata-blazor-colorpalette-tile.pixata-blazor-selected").GetAttribute("title"));
    StringAssert.Contains(picker.Find(".pixata-blazor-colorpalette").GetAttribute("style"), "--pixata-blazor-colorpalette-tile-width: 30px");
  }

  [TestMethod]
  public void PaletteColors_SetsTheDefaultPalette() {
    IRenderedComponent<PixataColorPicker> picker = Render(null, p => p.Add(c => c.PaletteColors, new[] { "#123456" }));

    Open(picker);
    ShowPalette(picker);

    Assert.AreEqual("#123456", picker.Find(".pixata-blazor-colorpalette-tile").GetAttribute("title"));
  }

  [TestMethod]
  public void Palette_Keyboard_MovesAndPicks() {
    IRenderedComponent<PixataColorPicker> picker = Render("#ffffff", p => p.Add(c => c.ValueFormat, PixataColorFormat.Hex));

    Open(picker);
    ShowPalette(picker);
    IElement palette = picker.Find(".pixata-blazor-colorpalette");
    Assert.AreEqual(CellId(picker, 0), palette.GetAttribute("aria-activedescendant"));
    picker.Find(".pixata-blazor-colorpalette").KeyDown("ArrowDown");
    picker.Find(".pixata-blazor-colorpalette").KeyDown("ArrowRight");
    picker.Find(".pixata-blazor-colorpalette").KeyDown("Enter");
    Button(picker, "Apply").Click();

    CollectionAssert.AreEqual(new[] { PixataColorPicker.DefaultPaletteColors[11] }, _values);
  }

  private static string CellId(IRenderedComponent<PixataColorPicker> picker, int index) =>
    picker.FindAll(".pixata-blazor-colorpalette-tile")[index].Id!;

  [TestMethod]
  public void View_SetsTheViewItOpensIn_AndChangesAreRaised() {
    List<PixataColorPickerView> views = [];
    IRenderedComponent<PixataColorPicker> picker = Render("#ff0000", p => p
      .Add(c => c.View, PixataColorPickerView.Palette)
      .Add(c => c.ViewChanged, EventCallback.Factory.Create<PixataColorPickerView>(this, views.Add)));

    Open(picker);
    Assert.AreEqual(1, picker.FindAll(".pixata-blazor-colorpalette").Count);
    Button(picker, "Gradient").Click();

    CollectionAssert.AreEqual(new[] { PixataColorPickerView.Gradient }, views);
  }

  // ---------- Forms ----------

  [TestMethod]
  public void InAForm_NotifiesTheEditContext_AndShowsInvalid() {
    Stage model = new();
    EditContext context = new(model);
    ValidationMessageStore messages = new(context);
    bool notified = false;
    context.OnFieldChanged += (_, e) => notified = e.FieldIdentifier.FieldName == nameof(Stage.Colour);
    IRenderedComponent<PixataColorPicker> picker = _ctx.Render<PixataColorPicker>(p => p
      .AddCascadingValue(context)
      .Add(c => c.Value, model.Colour)
      .Add(c => c.ValueExpression, () => model.Colour)
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => model.Colour = v))
      .Add(c => c.ValueFormat, PixataColorFormat.Hex));

    messages.Add(context.Field(nameof(Stage.Colour)), "Please pick a colour");
    context.NotifyValidationStateChanged();
    picker.WaitForAssertion(() => StringAssert.Contains(picker.Find("button.pixata-blazor-colorpicker").GetAttribute("class"), "pixata-blazor-invalid"));

    Open(picker);
    ShowPalette(picker);
    Tile(picker, "#000000").Click();
    Button(picker, "Apply").Click();

    Assert.IsTrue(notified);
    Assert.AreEqual("#000000", model.Colour);
  }

  private class Stage {
    public string? Colour { get; set; }
  }
}
