using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests.Components;

[TestClass]
public class Editor_Tests {
  private static readonly string ModulePath = PixataEditor.ModulePath;

  private class Notes {
    public string Text { get; set; } = "";
  }

  // The editor's module, set up so that create() returns a handle as it does in the browser
  private static BunitJSModuleInterop SetupModule(BunitContext context) {
    BunitJSModuleInterop module = context.JSInterop.SetupModule(ModulePath);
    module.Setup<int>("create", _ => true).SetResult(1);
    return module;
  }

  private static IReadOnlyList<JSRuntimeInvocation> Calls(BunitJSModuleInterop module, string identifier) =>
    module.Invocations[identifier];

  private static JsonElement Json(object? value) =>
    JsonSerializer.SerializeToElement(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));

  private static T LastArgs<T>(BunitJSModuleInterop module) where T : PixataEditorToolCommandArgsBase =>
    (T)Calls(module, "execute").Last().Arguments[2]!;

  private static IElement Tool(IRenderedComponent<PixataEditor> cut, string command) =>
    cut.Find($".pixata-blazor-editor-toolbar > .pixata-blazor-editor-toolbar-item .pixata-blazor-editor-tool-{command}");

  // Rendering

  [TestMethod]
  public void DefaultTools_RenderAnAccessibleToolbar() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>(p => p.Add(c => c.Value, "<p>Hi</p>"));
    IElement toolbar = cut.Find("[role=toolbar]");
    Assert.AreEqual(6, cut.FindAll(".pixata-blazor-editor-toolbar > .pixata-blazor-editor-toolbar-item").Count);
    IElement bold = cut.Find("button[aria-label=Bold]");
    Assert.AreEqual("false", bold.GetAttribute("aria-pressed"));
    Assert.IsTrue(bold.HasAttribute("data-pixata-editor-tool"));
    Assert.AreEqual("combobox", cut.Find(".pixata-blazor-editor-tool-format").GetAttribute("role"));
    Assert.AreEqual("dialog", cut.Find("button[aria-label='Insert link']").GetAttribute("aria-haspopup"));
    Assert.AreEqual(4, cut.FindAll("[role=group]").Count);
    Assert.IsNotNull(toolbar.GetAttribute("aria-controls"));
  }

  [TestMethod]
  public void AllTools_IncludeColoursTablesAndViewHtml() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>(p => p.Add(c => c.Tools, PixataEditorToolSets.All));
    Assert.IsNotNull(cut.Find(".pixata-blazor-editor-tool-fore-color"));
    Assert.IsNotNull(cut.Find(".pixata-blazor-editor-tool-back-color"));
    Assert.IsNotNull(cut.Find(".pixata-blazor-editor-tool-font-family"));
    Assert.IsNotNull(cut.Find(".pixata-blazor-editor-tool-font-size"));
    Assert.IsNotNull(cut.Find(".pixata-blazor-editor-tool-insert-table"));
    Assert.IsNotNull(cut.Find("button[aria-label='View HTML']"));
    Assert.IsNotNull(cut.Find("button[aria-label='Merge cells']"));
  }

  [TestMethod]
  public void EmptyTools_HidesTheToolbar() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = SetupModule(context);
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>(p => p.Add(c => c.Tools, new List<PixataEditorTool>()));
    Assert.AreEqual(0, cut.FindAll("[role=toolbar]").Count);
    Assert.IsNull(Calls(module, "create").Single().Arguments[2]);
  }

  [TestMethod]
  public void ToolsThatCantBeUsedYet_StartDisabled() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>(p => p.Add(c => c.Tools, PixataEditorToolSets.All));
    Assert.IsTrue(cut.Find("button[aria-label=Undo]").HasAttribute("disabled"));
    Assert.IsTrue(cut.Find("button[aria-label='Delete table']").HasAttribute("disabled"));
    Assert.IsFalse(cut.Find("button[aria-label=Bold]").HasAttribute("disabled"));
  }

  [TestMethod]
  public void IframeMode_IsTheDefault() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = SetupModule(context);
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>(p => p.Add(c => c.AriaLabel, "Notes"));
    Assert.AreEqual("Notes", cut.Find("iframe.pixata-blazor-editor-iframe").GetAttribute("title"));
    Assert.AreEqual("iframe", Json(Calls(module, "create").Single().Arguments[4]).GetProperty("editMode").GetString());
  }

  [TestMethod]
  public void DivMode_UsesADiv_AndChangingTheModeRecreatesTheEditor() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = SetupModule(context);
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>(p => p.Add(c => c.EditMode, PixataEditorEditMode.Div));
    Assert.AreEqual(0, cut.FindAll("iframe").Count);
    Assert.IsNotNull(cut.Find("div.pixata-blazor-editor-mount"));
    Assert.AreEqual("div", Json(Calls(module, "create").Single().Arguments[4]).GetProperty("editMode").GetString());
    cut.Render(p => p.Add(c => c.EditMode, PixataEditorEditMode.Iframe));
    Assert.AreEqual(1, Calls(module, "destroy").Count);
    Assert.AreEqual(2, Calls(module, "create").Count);
    Assert.AreEqual(1, cut.FindAll("iframe").Count);
  }

  [TestMethod]
  public void HeightAndWidth_AreApplied() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>(p => p.Add(c => c.Height, "400px").Add(c => c.Width, "50%"));
    Assert.AreEqual("height: 400px;width: 50%;", cut.Find(".pixata-blazor-editor").GetAttribute("style"));
  }

  [TestMethod]
  public void BeforeTheEditorLoads_TheSanitisedValueIsShown() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>(p => p.Add(c => c.Value, "<p onclick=\"steal()\">Hello</p><script>steal()</script>"));
    IElement prerender = cut.Find(".pixata-blazor-editor-prerender");
    Assert.AreEqual("<p>Hello</p>", prerender.InnerHtml);
  }

  // Value

  [TestMethod]
  public void Create_IsGivenTheSanitisedValueAndOptions() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = SetupModule(context);
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>(p => p
      .Add(c => c.Value, "<p>Hi<img src=\"x\" onerror=\"steal()\"></p>")
      .Add(c => c.Placeholder, "Type here")
      .Add(c => c.DebounceDelay, 300)
      .Add(c => c.SpellCheck, "false"));
    JSRuntimeInvocation create = Calls(module, "create").Single();
    Assert.AreEqual("<p>Hi<img src=\"x\"></p>", create.Arguments[5]);
    JsonElement options = Json(create.Arguments[4]);
    Assert.AreEqual("Type here", options.GetProperty("placeholder").GetString());
    Assert.AreEqual(300, options.GetProperty("debounceDelay").GetInt32());
    Assert.AreEqual("false", options.GetProperty("spellCheck").GetString());
    Assert.AreEqual("Editor", options.GetProperty("ariaLabel").GetString());
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-editor-prerender").Count);
  }

  [TestMethod]
  public async Task ValueFromTheEditor_IsSanitisedBeforeValueChanged() {
    using BunitContext context = PixataTestContext.Create();
    SetupModule(context);
    string? changed = null;
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>(p => p
      .Add(c => c.Value, "<p>Hi</p>")
      .Add(c => c.ValueChanged, (string v) => changed = v));
    await cut.InvokeAsync(() => cut.Instance.OnEditorValueChanged("<p onmouseover=\"steal()\">Hi <a href=\"javascript:steal()\">there</a></p><script>steal()</script>"));
    Assert.AreEqual("<p>Hi <a>there</a></p>", changed);
  }

  [TestMethod]
  public async Task NewValue_IsSentToTheEditor_ButItsOwnValueIsNotEchoedBack() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = SetupModule(context);
    string value = "<p>One</p>";
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>(p => p
      .Add(c => c.Value, value)
      .Add(c => c.ValueChanged, (string v) => value = v));
    await cut.InvokeAsync(() => cut.Instance.OnEditorValueChanged("<p>Two</p>"));
    cut.Render(p => p.Add(c => c.Value, value));
    Assert.AreEqual(0, module.Invocations["setValue"].Count);
    cut.Render(p => p.Add(c => c.Value, "<p>Three</p><script>steal()</script>"));
    JSRuntimeInvocation setValue = Calls(module, "setValue").Single();
    Assert.AreEqual("<p>Three</p>", setValue.Arguments[1]);
  }

  [TestMethod]
  public async Task OnChange_IsRaisedOnBlur_OnlyIfTheValueChanged() {
    using BunitContext context = PixataTestContext.Create();
    SetupModule(context);
    string value = "<p>One</p>";
    List<string> events = [];
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>(p => p
      .Add(c => c.Value, value)
      .Add(c => c.ValueChanged, (string v) => value = v)
      .Add(c => c.OnBlur, () => events.Add("blur"))
      .Add(c => c.OnChange, (string v) => events.Add($"change {v}")));
    await cut.InvokeAsync(cut.Instance.OnEditorFocus);
    await cut.InvokeAsync(cut.Instance.OnEditorBlur);
    CollectionAssert.AreEqual(new[] { "blur" }, events);
    await cut.InvokeAsync(cut.Instance.OnEditorFocus);
    await cut.InvokeAsync(() => cut.Instance.OnEditorValueChanged("<p>Two</p>"));
    cut.Render(p => p.Add(c => c.Value, value));
    await cut.InvokeAsync(cut.Instance.OnEditorBlur);
    CollectionAssert.AreEqual(new[] { "blur", "blur", "change <p>Two</p>" }, events);
  }

  [TestMethod]
  public async Task EditForm_ValidationMessage_MakesTheEditorInvalid() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = SetupModule(context);
    Notes model = new();
    EditContext editContext = new(model);
    ValidationMessageStore messages = new(editContext);
    List<string> changedFields = [];
    editContext.OnFieldChanged += (_, e) => changedFields.Add(e.FieldIdentifier.FieldName);
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>(p => p
      .AddCascadingValue(editContext)
      .Add(c => c.Value, model.Text)
      .Add(c => c.ValueChanged, (string v) => model.Text = v)
      .Add(c => c.ValueExpression, () => model.Text));
    Assert.IsFalse(cut.Find(".pixata-blazor-editor").ClassList.Contains("pixata-blazor-invalid"));
    await cut.InvokeAsync(() => cut.Instance.OnEditorValueChanged("<p>Hello</p>"));
    CollectionAssert.AreEqual(new[] { nameof(Notes.Text) }, changedFields);
    await cut.InvokeAsync(() => {
      messages.Add(editContext.Field(nameof(Notes.Text)), "Required");
      editContext.NotifyValidationStateChanged();
    });
    Assert.IsTrue(cut.Find(".pixata-blazor-editor").ClassList.Contains("pixata-blazor-invalid"));
    Assert.IsTrue(Json(Calls(module, "update").Last().Arguments[1]).GetProperty("invalid").GetBoolean());
  }

  // Read-only and disabled

  [TestMethod]
  public void ReadOnly_DisablesTheToolsAndTheDocument() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = SetupModule(context);
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>(p => p.Add(c => c.ReadOnly, true).Add(c => c.Tools, PixataEditorToolSets.All));
    Assert.IsTrue(cut.FindAll("[data-pixata-editor-tool]").All(t => t.HasAttribute("disabled")));
    Assert.IsTrue(Json(Calls(module, "create").Single().Arguments[4]).GetProperty("readOnly").GetBoolean());
    Assert.IsTrue(cut.Find(".pixata-blazor-editor").ClassList.Contains("pixata-blazor-editor-readonly"));
  }

  [TestMethod]
  public void Disabled_DisablesEverything_AndCanBeTurnedBackOn() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = SetupModule(context);
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>(p => p.Add(c => c.Enabled, false));
    Assert.IsTrue(cut.Find(".pixata-blazor-editor").ClassList.Contains("pixata-blazor-disabled"));
    Assert.IsTrue(cut.FindAll("[data-pixata-editor-tool]").All(t => t.HasAttribute("disabled")));
    cut.Render(p => p.Add(c => c.Enabled, true));
    Assert.IsFalse(cut.Find("button[aria-label=Bold]").HasAttribute("disabled"));
    Assert.IsFalse(Json(Calls(module, "update").Last().Arguments[1]).GetProperty("readOnly").GetBoolean());
  }

  // Selection state from the browser

  [TestMethod]
  public async Task SelectionState_UpdatesTheTools() {
    using BunitContext context = PixataTestContext.Create();
    SetupModule(context);
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>(p => p.Add(c => c.Tools, PixataEditorToolSets.All));
    string state = """{"active":["bold","alignCenter"],"disabled":["redo"],"format":"h2","fontFamily":"Georgia, Times, \"Times New Roman\", serif","fontSize":"x-large","foreColor":"rgb(230, 25, 25)","backColor":null}""";
    await cut.InvokeAsync(() => cut.Instance.OnEditorStateChanged(state));
    IElement bold = cut.Find("button[aria-label=Bold]");
    Assert.AreEqual("true", bold.GetAttribute("aria-pressed"));
    Assert.IsTrue(bold.ClassList.Contains("pixata-blazor-selected"));
    Assert.AreEqual("true", cut.Find("button[aria-label='Align centre']").GetAttribute("aria-pressed"));
    Assert.AreEqual("false", cut.Find("button[aria-label=Italic]").GetAttribute("aria-pressed"));
    Assert.IsFalse(cut.Find("button[aria-label=Undo]").HasAttribute("disabled"));
    Assert.IsTrue(cut.Find("button[aria-label=Redo]").HasAttribute("disabled"));
    Assert.AreEqual("Heading 2", Tool(cut, "format").TextContent.Trim());
    Assert.AreEqual("Georgia", Tool(cut, "font-family").TextContent.Trim());
    Assert.AreEqual("6 (24pt)", Tool(cut, "font-size").TextContent.Trim());
    StringAssert.Contains(Tool(cut, "fore-color").QuerySelector(".pixata-blazor-editor-colour-bar")!.GetAttribute("style"), "rgb(230, 25, 25)");
    Assert.IsTrue(Tool(cut, "back-color").QuerySelector(".pixata-blazor-editor-colour-bar")!.ClassList.Contains("pixata-blazor-editor-colour-none"));
  }

  // Commands

  [TestMethod]
  public async Task ExecuteAsync_SendsTheCommandToTheEditor() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = SetupModule(context);
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>();
    await cut.InvokeAsync(() => cut.Instance.ExecuteAsync(new PixataEditorHtmlCommandArgs(PixataEditorCommands.InsertHtml, "<p>Hi</p>", true)));
    JSRuntimeInvocation execute = Calls(module, "execute").Single();
    Assert.AreEqual(1, execute.Arguments[0]);
    Assert.AreEqual("insertHtml", execute.Arguments[1]);
    JsonElement args = Json(execute.Arguments[2]);
    Assert.AreEqual("<p>Hi</p>", args.GetProperty("value").GetString());
    Assert.IsTrue(args.GetProperty("inline").GetBoolean());
  }

  [TestMethod]
  public async Task ExecuteAsync_SetHtmlBeforeTheEditorLoads_SetsTheSanitisedValue() {
    using BunitContext context = PixataTestContext.Create();
    string? changed = null;
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>(p => p.Add(c => c.ValueChanged, (string v) => changed = v));
    await cut.InvokeAsync(() => cut.Instance.ExecuteAsync(new PixataEditorHtmlCommandArgs(PixataEditorCommands.ViewHtml, "<h1 style=\"color: red; behavior: url(x)\">Hi</h1>")));
    Assert.AreEqual("<h1 style=\"color: red;\">Hi</h1>", changed);
  }

  [TestMethod]
  public void ClickingAButton_RunsItsCommand() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = SetupModule(context);
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>();
    cut.Find("button[aria-label=Italic]").Click();
    Assert.AreEqual("italic", Calls(module, "execute").Single().Arguments[1]);
  }

  [TestMethod]
  public async Task FocusAsync_FocusesTheDocument() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = SetupModule(context);
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>();
    await cut.InvokeAsync(cut.Instance.FocusAsync);
    Assert.AreEqual(1, Calls(module, "focus").Single().Arguments[0]);
  }

  [TestMethod]
  public async Task DisposeAsync_DestroysTheEditor() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = SetupModule(context);
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>();
    await cut.InvokeAsync(async () => await cut.Instance.DisposeAsync());
    Assert.AreEqual(1, Calls(module, "destroy").Single().Arguments[0]);
  }

  [TestMethod]
  public async Task DisposeAsync_WhileTheEditorIsBeingCreated_DestroysItOnceItHasBeenCreated() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = context.JSInterop.SetupModule(ModulePath);
    JSRuntimeInvocationHandler<int> create = module.Setup<int>("create", _ => true);
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>();
    await cut.InvokeAsync(async () => await cut.Instance.DisposeAsync());
    Assert.AreEqual(0, module.Invocations["destroy"].Count);
    await cut.InvokeAsync(() => create.SetResult(3));
    cut.WaitForAssertion(() => Assert.AreEqual(3, Calls(module, "destroy").Single().Arguments[0]));
  }

  // Drop-downs, colour pickers and the table picker

  [TestMethod]
  public void FormatDropDown_OpensAndChoosesWithTheKeyboard() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = SetupModule(context);
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>();
    IElement format = Tool(cut, "format");
    Assert.AreEqual("false", format.GetAttribute("aria-expanded"));
    format.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
    format = Tool(cut, "format");
    Assert.AreEqual("true", format.GetAttribute("aria-expanded"));
    IReadOnlyList<IElement> options = cut.FindAll("[role=listbox] [role=option]");
    Assert.AreEqual(7, options.Count);
    Assert.AreEqual(options[0].Id, format.GetAttribute("aria-activedescendant"));
    format.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
    format.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
    Assert.AreEqual(cut.FindAll("[role=option]")[2].Id, Tool(cut, "format").GetAttribute("aria-activedescendant"));
    Tool(cut, "format").KeyDown(new KeyboardEventArgs { Key = "Enter" });
    Assert.AreEqual(0, cut.FindAll("[role=listbox]").Count);
    PixataEditorFormatCommandArgs args = LastArgs<PixataEditorFormatCommandArgs>(module);
    Assert.AreEqual(PixataEditorCommands.Format, args.CommandName);
    Assert.AreEqual("h2", args.Value);
  }

  [TestMethod]
  public void FormatDropDown_EscapeCloses_AndClickChooses() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = SetupModule(context);
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>(p => p.Add(c => c.Tools, PixataEditorToolSets.All));
    Tool(cut, "font-size").Click();
    Assert.AreEqual(7, cut.FindAll("[role=option]").Count);
    Tool(cut, "font-size").KeyDown(new KeyboardEventArgs { Key = "Escape" });
    Assert.AreEqual(0, cut.FindAll("[role=option]").Count);
    Tool(cut, "font-family").Click();
    cut.FindAll("[role=option]").Single(o => o.TextContent.Trim() == "Verdana").Click();
    Assert.AreEqual("Verdana, Geneva, sans-serif", LastArgs<PixataEditorFormatCommandArgs>(module).Value);
    Assert.AreEqual(PixataEditorCommands.FontFamily, LastArgs<PixataEditorFormatCommandArgs>(module).CommandName);
  }

  [TestMethod]
  public async Task ColourPicker_ShowsTheSelectionsColour_AndChoosesWithTheKeyboard() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = SetupModule(context);
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>(p => p.Add(c => c.Tools, PixataEditorToolSets.All));
    await cut.InvokeAsync(() => cut.Instance.OnEditorStateChanged("""{"active":[],"disabled":[],"foreColor":"rgb(230, 25, 25)"}"""));
    Tool(cut, "fore-color").Click();
    Assert.AreEqual(60, cut.FindAll("[role=gridcell]").Count);
    IElement selected = cut.Find("[role=gridcell][aria-selected=true]");
    Assert.AreEqual("#e61919", selected.GetAttribute("aria-label"));
    Assert.AreEqual(selected.Id, Tool(cut, "fore-color").GetAttribute("aria-activedescendant"));
    Tool(cut, "fore-color").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
    Tool(cut, "fore-color").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
    Tool(cut, "fore-color").KeyDown(new KeyboardEventArgs { Key = "Enter" });
    Assert.AreEqual(0, cut.FindAll("[role=gridcell]").Count);
    PixataEditorFormatCommandArgs args = LastArgs<PixataEditorFormatCommandArgs>(module);
    Assert.AreEqual(PixataEditorCommands.ForeColor, args.CommandName);
    Assert.AreEqual(PixataEditorColorTool.DefaultColors[42], args.Value);
  }

  [TestMethod]
  public void ColourPicker_NoColour_RemovesTheColour() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = SetupModule(context);
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>(p => p.Add(c => c.Tools, PixataEditorToolSets.All));
    Tool(cut, "back-color").Click();
    cut.Find(".pixata-blazor-editor-palette-clear").Click();
    PixataEditorFormatCommandArgs args = LastArgs<PixataEditorFormatCommandArgs>(module);
    Assert.AreEqual(PixataEditorCommands.BackgroundColor, args.CommandName);
    Assert.AreEqual("", args.Value);
  }

  [TestMethod]
  public void ColourPicker_UnsafeColoursAreLeftOut() {
    using BunitContext context = PixataTestContext.Create();
    List<PixataEditorTool> tools = [new PixataEditorForeColor { Colors = ["red", "url(javascript:steal())", "#00ff00"] }];
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>(p => p.Add(c => c.Tools, tools));
    Tool(cut, "fore-color").Click();
    CollectionAssert.AreEqual(new[] { "red", "#00ff00" }, cut.FindAll("[role=gridcell]").Select(c => c.GetAttribute("aria-label")).ToArray());
  }

  [TestMethod]
  public void TablePicker_InsertsATableOfTheChosenSize() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = SetupModule(context);
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>(p => p.Add(c => c.Tools, PixataEditorToolSets.All));
    Tool(cut, "insert-table").KeyDown(new KeyboardEventArgs { Key = "Enter" });
    Assert.AreEqual(64, cut.FindAll(".pixata-blazor-editor-table-cell").Count);
    Tool(cut, "insert-table").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
    Tool(cut, "insert-table").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
    Tool(cut, "insert-table").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
    Assert.AreEqual("2 × 3 table", cut.Find(".pixata-blazor-editor-table-size").TextContent);
    Assert.AreEqual(6, cut.FindAll(".pixata-blazor-editor-table-cell[aria-selected=true]").Count);
    Tool(cut, "insert-table").KeyDown(new KeyboardEventArgs { Key = "Enter" });
    PixataEditorTableCommandArgs args = LastArgs<PixataEditorTableCommandArgs>(module);
    Assert.AreEqual(2, args.Rows);
    Assert.AreEqual(3, args.Cols);
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-editor-table-cell").Count);
  }

  [TestMethod]
  public void TablePicker_ClickingACellInsertsThatSize() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = SetupModule(context);
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>(p => p.Add(c => c.Tools, PixataEditorToolSets.All));
    Tool(cut, "insert-table").Click();
    cut.FindAll(".pixata-blazor-editor-table-cell")[8 * 3 + 1].Click();
    PixataEditorTableCommandArgs args = LastArgs<PixataEditorTableCommandArgs>(module);
    Assert.AreEqual(4, args.Rows);
    Assert.AreEqual(2, args.Cols);
  }

  // Dialogs

  [TestMethod]
  public void LinkDialog_RejectsUnsafeAddresses_AndInsertsSafeOnes() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = SetupModule(context);
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>();
    cut.Find("button[aria-label='Insert link']").Click();
    IElement dialog = cut.Find("[role=dialog]");
    Assert.AreEqual("true", dialog.GetAttribute("aria-modal"));
    Assert.AreEqual("Insert link", cut.Find($"#{dialog.GetAttribute("aria-labelledby")}").TextContent);
    Assert.IsTrue(cut.Find(".pixata-blazor-editor-dialog-actions button[type=submit]").HasAttribute("disabled"));
    cut.FindAll(".pixata-blazor-editor-dialog input")[0].Input("JaVaScRiPt:steal()");
    cut.Find(".pixata-blazor-editor-dialog-form").Submit();
    Assert.IsNotNull(cut.Find("[role=alert]"));
    Assert.AreEqual(0, module.Invocations["execute"].Count);
    cut.FindAll(".pixata-blazor-editor-dialog input")[0].Input("https://example.com/");
    Assert.AreEqual(0, cut.FindAll("[role=alert]").Count);
    cut.FindAll(".pixata-blazor-editor-dialog input")[1].Input("Example");
    cut.Find(".pixata-blazor-editor-dialog input[type=checkbox]").Change(true);
    cut.Find(".pixata-blazor-editor-dialog-form").Submit();
    Assert.AreEqual(0, cut.FindAll("[role=dialog]").Count);
    PixataEditorLinkCommandArgs args = LastArgs<PixataEditorLinkCommandArgs>(module);
    Assert.AreEqual("https://example.com/", args.Href);
    Assert.AreEqual("Example", args.Text);
    Assert.AreEqual("_blank", args.Target);
  }

  [TestMethod]
  public void ImageDialog_InsertsTheImage() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = SetupModule(context);
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>();
    cut.Find("button[aria-label='Insert image']").Click();
    cut.FindAll(".pixata-blazor-editor-dialog input")[0].Input("/images/cat.png");
    cut.FindAll(".pixata-blazor-editor-dialog input")[1].Input("A cat");
    cut.FindAll(".pixata-blazor-editor-dialog input")[2].Input("200");
    cut.Find(".pixata-blazor-editor-dialog-form").Submit();
    PixataEditorImageCommandArgs args = LastArgs<PixataEditorImageCommandArgs>(module);
    Assert.AreEqual("/images/cat.png", args.Src);
    Assert.AreEqual("A cat", args.Alt);
    Assert.AreEqual("200", args.Width);
  }

  [TestMethod]
  public void HtmlDialog_ShowsAndUpdatesTheHtml() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = SetupModule(context);
    module.Setup<string>("getHtml", _ => true).SetResult("<p>\n  Formatted\n</p>");
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>(p => p.Add(c => c.Tools, PixataEditorToolSets.All).Add(c => c.Value, "<p>Formatted</p>"));
    cut.Find("button[aria-label='View HTML']").Click();
    IElement source = cut.Find("textarea.pixata-blazor-editor-html-source");
    Assert.AreEqual("<p>\n  Formatted\n</p>", source.GetAttribute("value") ?? source.TextContent);
    source.Change("<p>New</p>");
    cut.Find(".pixata-blazor-editor-dialog-form").Submit();
    PixataEditorHtmlCommandArgs args = LastArgs<PixataEditorHtmlCommandArgs>(module);
    Assert.AreEqual(PixataEditorCommands.ViewHtml, args.CommandName);
    Assert.AreEqual("<p>New</p>", args.Value);
  }

  [TestMethod]
  public void Dialog_EscapeAndCancelClose() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = SetupModule(context);
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>();
    cut.Find("button[aria-label='Insert image']").Click();
    cut.Find("[role=dialog]").KeyDown(new KeyboardEventArgs { Key = "Escape" });
    Assert.AreEqual(0, cut.FindAll("[role=dialog]").Count);
    cut.Find("button[aria-label='Insert link']").Click();
    cut.FindAll(".pixata-blazor-editor-dialog-actions button").Single(b => b.TextContent.Trim() == "Cancel").Click();
    Assert.AreEqual(0, cut.FindAll("[role=dialog]").Count);
    Assert.AreEqual(0, module.Invocations["execute"].Count);
    Assert.IsTrue(module.Invocations["focus"].Count > 0);
  }

  [TestMethod]
  public async Task Dialog_Escape_PutsTheFocusBackInTheDocument_OnceTheDialogHasReleasedIt() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = SetupModule(context);
    module.Setup<int>("trapFocus", _ => true).SetResult(7);
    JSRuntimeInvocationHandler release = module.SetupVoid("releaseFocus", _ => true);
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>();
    cut.Find("button[aria-label='Insert link']").Click();
    cut.Find("[role=dialog]").KeyDown(new KeyboardEventArgs { Key = "Escape" });
    Assert.AreEqual(0, cut.FindAll("[role=dialog]").Count);
    Assert.AreEqual(7, Calls(module, "releaseFocus").Single().Arguments[0]);
    Assert.AreEqual(0, module.Invocations["focus"].Count);
    // Before, the focus only went back at the editor's next render, whenever that was, taking it from wherever it had gone
    await cut.InvokeAsync(release.SetVoidResult);
    cut.WaitForAssertion(() => Assert.AreEqual(1, module.Invocations["focus"].Count));
  }

  // Custom tools and settings

  [TestMethod]
  public void CustomTools_RenderTheirTemplateInTheirPlace() {
    using BunitContext context = PixataTestContext.Create();
    List<PixataEditorTool> tools = [new PixataEditorBold(), new PixataEditorCustomTool("Mine"), new PixataEditorItalic()];
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>(p => p
      .Add(c => c.Tools, tools)
      .Add(c => c.EditorCustomTools, builder => {
        builder.OpenComponent<PixataEditorCustomToolTemplate>(0);
        builder.AddAttribute(1, nameof(PixataEditorCustomToolTemplate.Name), "Mine");
        builder.AddAttribute(2, nameof(PixataEditorCustomToolTemplate.ChildContent), (RenderFragment)(b => b.AddMarkupContent(0, "<button id=\"mine\">Mine</button>")));
        builder.CloseComponent();
        builder.OpenComponent<PixataEditorCustomToolTemplate>(3);
        builder.AddAttribute(4, nameof(PixataEditorCustomToolTemplate.Name), "Unused");
        builder.AddAttribute(5, nameof(PixataEditorCustomToolTemplate.ChildContent), (RenderFragment)(b => b.AddMarkupContent(0, "<button id=\"unused\">Unused</button>")));
        builder.CloseComponent();
      }));
    IReadOnlyList<IElement> items = cut.FindAll(".pixata-blazor-editor-toolbar > .pixata-blazor-editor-toolbar-item");
    Assert.AreEqual(3, items.Count);
    Assert.IsNotNull(items[1].QuerySelector(".pixata-blazor-editor-custom-tool #mine"));
    Assert.AreEqual(1, cut.FindAll("#mine").Count);
    Assert.AreEqual(0, cut.FindAll("#unused").Count);
  }

  [TestMethod]
  public void CustomToolTemplate_OutsideAnEditor_Throws() {
    using BunitContext context = PixataTestContext.Create();
    Assert.ThrowsException<InvalidOperationException>(() => context.Render<PixataEditorCustomToolTemplate>(p => p.Add(c => c.Name, "Mine")));
  }

  [TestMethod]
  public void PasteSettings_AreSentToTheEditor() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = SetupModule(context);
    context.Render<PixataEditor>(p => p.Add(c => c.EditorSettings, builder => {
      builder.OpenComponent<PixataEditorPasteSettings>(0);
      builder.AddAttribute(1, nameof(PixataEditorPasteSettings.ConvertMsLists), false);
      builder.AddAttribute(2, nameof(PixataEditorPasteSettings.RemoveAttributes), new List<string> { "style" });
      builder.AddAttribute(3, nameof(PixataEditorPasteSettings.StripTags), new List<string> { "span" });
      builder.CloseComponent();
    }));
    JsonElement paste = Json(Calls(module, "create").Single().Arguments[4]).GetProperty("pasteSettings");
    Assert.IsFalse(paste.GetProperty("convertMsLists").GetBoolean());
    Assert.IsTrue(paste.GetProperty("removeMsClasses").GetBoolean());
    Assert.AreEqual("style", paste.GetProperty("removeAttributes")[0].GetString());
    Assert.AreEqual("span", paste.GetProperty("stripTags")[0].GetString());
  }

  [TestMethod]
  public void PasteSettings_Default() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = SetupModule(context);
    context.Render<PixataEditor>();
    JsonElement paste = Json(Calls(module, "create").Single().Arguments[4]).GetProperty("pasteSettings");
    Assert.IsTrue(paste.GetProperty("convertMsLists").GetBoolean());
    Assert.IsTrue(paste.GetProperty("removeMsStyles").GetBoolean());
    Assert.IsFalse(paste.GetProperty("removeAllAttributes").GetBoolean());
    Assert.AreEqual(0, paste.GetProperty("stripTags").GetArrayLength());
  }

  // Adaptive toolbar

  [TestMethod]
  public void NotAdaptive_HasNoOverflowMenu() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>();
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-editor-toolbar-overflow").Count);
  }

  [TestMethod]
  public async Task Adaptive_ToolsThatDontFit_GoIntoTheOverflowMenu() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = SetupModule(context);
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>(p => p.Add(c => c.Adaptive, true));
    IElement overflowButton = cut.Find(".pixata-blazor-editor-toolbar-overflow-button");
    Assert.IsTrue(overflowButton.HasAttribute("disabled"));
    await cut.InvokeAsync(() => cut.Instance.OnToolbarOverflow(2));
    IReadOnlyList<IElement> items = cut.FindAll(".pixata-blazor-editor-toolbar > .pixata-blazor-editor-toolbar-item");
    Assert.AreEqual(2, items.Count(i => !i.ClassList.Contains("pixata-blazor-editor-toolbar-item-hidden")));
    Assert.IsTrue(items.Skip(2).All(i => i.GetAttribute("aria-hidden") == "true" && i.HasAttribute("inert")));
    overflowButton = cut.Find(".pixata-blazor-editor-toolbar-overflow-button");
    Assert.IsFalse(overflowButton.HasAttribute("disabled"));
    overflowButton.Click();
    Assert.AreEqual("true", cut.Find(".pixata-blazor-editor-toolbar-overflow-button").GetAttribute("aria-expanded"));
    IElement popup = cut.Find(".pixata-blazor-editor-overflow-popup");
    Assert.AreEqual(4, popup.QuerySelectorAll(".pixata-blazor-editor-toolbar-item").Length);
    popup.QuerySelector("button[aria-label='Insert image']")!.Click();
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-editor-overflow-popup").Count);
    Assert.IsTrue(Json(Calls(module, "create").Single().Arguments[4]).GetProperty("adaptive").GetBoolean());
  }

  [TestMethod]
  public async Task Adaptive_EscapeClosesTheOverflowMenu() {
    using BunitContext context = PixataTestContext.Create();
    SetupModule(context);
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>(p => p.Add(c => c.Adaptive, true));
    await cut.InvokeAsync(() => cut.Instance.OnToolbarOverflow(1));
    cut.Find(".pixata-blazor-editor-toolbar-overflow-button").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
    Assert.AreEqual(1, cut.FindAll(".pixata-blazor-editor-overflow-popup").Count);
    cut.Find(".pixata-blazor-editor-overflow-tools").KeyDown(new KeyboardEventArgs { Key = "Escape" });
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-editor-overflow-popup").Count);
  }

  [TestMethod]
  public async Task Adaptive_EverythingFits_HidesTheOverflowButton() {
    using BunitContext context = PixataTestContext.Create();
    SetupModule(context);
    IRenderedComponent<PixataEditor> cut = context.Render<PixataEditor>(p => p.Add(c => c.Adaptive, true));
    await cut.InvokeAsync(() => cut.Instance.OnToolbarOverflow(6));
    Assert.IsTrue(cut.Find(".pixata-blazor-editor-toolbar-overflow").ClassList.Contains("pixata-blazor-editor-toolbar-overflow-hidden"));
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-editor-toolbar-item-hidden").Count);
  }

  // The sanitiser

  [TestMethod]
  [DataRow("<p>Hi</p><script>steal()</script>", "<p>Hi</p>")]
  [DataRow("<p>Hi</p><SCRIPT type=\"text/javascript\">steal()</SCRIPT >", "<p>Hi</p>")]
  [DataRow("<p>Hi</p><script>document.write('</scr' + 'ipt>')</script>", "<p>Hi</p>")]
  [DataRow("<p onclick=\"steal()\" ONMOUSEOVER='steal()'>Hi</p>", "<p>Hi</p>")]
  [DataRow("<img src=x onerror=steal()>", "<img src=\"x\">")]
  [DataRow("<a href=\"javascript:steal()\">Hi</a>", "<a>Hi</a>")]
  [DataRow("<a href=\" JaVaScRiPt:steal()\">Hi</a>", "<a>Hi</a>")]
  [DataRow("<a href=\"java\tscript:steal()\">Hi</a>", "<a>Hi</a>")]
  [DataRow("<a href=\"&#106;avascript:steal()\">Hi</a>", "<a>Hi</a>")]
  [DataRow("<a href=\"vbscript:steal()\">Hi</a>", "<a>Hi</a>")]
  [DataRow("<a href=\"data:text/html,<script>steal()</script>\">Hi</a>", "<a>Hi</a>")]
  [DataRow("<img src=\"data:image/svg+xml;base64,PHN2Zz4=\">", "<img>")]
  [DataRow("<img src=\"data:image/png;base64,iVBORw0KGgo=\">", "<img src=\"data:image/png;base64,iVBORw0KGgo=\">")]
  [DataRow("<iframe src=\"https://example.com\">Hi</iframe><p>There</p>", "<p>There</p>")]
  [DataRow("<object data=\"x\"><embed src=\"x\"></object><p>There</p>", "<p>There</p>")]
  [DataRow("<svg><script>steal()</script></svg><p>There</p>", "<p>There</p>")]
  [DataRow("<math><mi xlink:href=\"javascript:steal()\">x</mi></math>", "")]
  [DataRow("<style>p { background: url(javascript:steal()) }</style><p>Hi</p>", "<p>Hi</p>")]
  [DataRow("<form action=\"https://example.com\"><input name=\"a\"><p>Hi</p></form>", "<p>Hi</p>")]
  [DataRow("<p>Hi<!-- <script>steal()</script> --></p>", "<p>Hi</p>")]
  [DataRow("<p id=\"x\" class=\"y\" data-x=\"z\">Hi</p>", "<p>Hi</p>")]
  [DataRow("<custom-element>Hi</custom-element>", "Hi")]
  [DataRow("<p>1 &lt; 2 &amp; 3 &gt; 2</p>", "<p>1 &lt; 2 &amp; 3 &gt; 2</p>")]
  [DataRow("<p>\"quoted\" 'text'</p>", "<p>\"quoted\" 'text'</p>")]
  [DataRow("<p title='a\"&lt;b'>Hi</p>", "<p title=\"a&quot;&lt;b\">Hi</p>")]
  [DataRow("<p>Unclosed <strong>bold", "<p>Unclosed <strong>bold</strong></p>")]
  [DataRow("</p>Stray end tag", "Stray end tag")]
  [DataRow("<p>1 < 2</p>", "<p>1 &lt; 2</p>")]
  [DataRow("<a href=\"https://example.com\" target=\"_blank\">Hi</a>", "<a href=\"https://example.com\" target=\"_blank\" rel=\"noopener noreferrer\">Hi</a>")]
  [DataRow("<a href=\"/relative?a=1&amp;b=2\" target=\"evil\">Hi</a>", "<a href=\"/relative?a=1&amp;b=2\">Hi</a>")]
  [DataRow("<a href=\"mailto:fred@example.com\">Mail</a>", "<a href=\"mailto:fred@example.com\">Mail</a>")]
  [DataRow("<p style=\"color: red; background-image: url(javascript:steal()); position: fixed\">Hi</p>", "<p style=\"color: red;\">Hi</p>")]
  [DataRow("<p style=\"width: expression(steal())\">Hi</p>", "<p>Hi</p>")]
  [DataRow("<p style=\"color: rgb(1, 2, 3); font-family: &quot;Times New Roman&quot;, serif\">Hi</p>", "<p style=\"color: rgb(1, 2, 3); font-family: &quot;Times New Roman&quot;, serif;\">Hi</p>")]
  [DataRow("<p style=\"color: red\\3b background: url(x)\">Hi</p>", "<p>Hi</p>")]
  [DataRow("<td colspan=\"2\" rowspan=\"x\" data-colwidth=\"100\">Hi</td>", "<td colspan=\"2\" data-colwidth=\"100\">Hi</td>")]
  [DataRow("<ol start=\"3\" type=\"a\"><li>Hi</li></ol>", "<ol start=\"3\" type=\"a\"><li>Hi</li></ol>")]
  [DataRow("<p>Non breaking</p>", "<p>Non&nbsp;breaking</p>")]
  public void Sanitise_KeepsFormattingAndRemovesAnythingUnsafe(string html, string expected) =>
    Assert.AreEqual(expected, PixataHtmlSanitiser.Sanitise(html));

  [TestMethod]
  public void Sanitise_TakesLinearTime_WithManyAttributesOrStrayEndTags() {
    // Before, each attribute was checked against all those before it, and each end tag searched the open elements, so
    // inputs like these took tens of seconds
    string attributes = $"<p {string.Join(" ", Enumerable.Range(0, 50_000).Select(i => $"a{i}"))}>Hi</p>";
    string strayEndTags = $"{string.Concat(Enumerable.Repeat("<b>", 50_000))}Hi{string.Concat(Enumerable.Repeat("</i>", 50_000))}";
    Stopwatch watch = Stopwatch.StartNew();
    string sanitisedAttributes = PixataHtmlSanitiser.Sanitise(attributes);
    string sanitisedEndTags = PixataHtmlSanitiser.Sanitise(strayEndTags);
    watch.Stop();
    Assert.AreEqual("<p>Hi</p>", sanitisedAttributes);
    Assert.AreEqual($"{string.Concat(Enumerable.Repeat("<b>", 50_000))}Hi{string.Concat(Enumerable.Repeat("</b>", 50_000))}", sanitisedEndTags);
    Assert.IsTrue(watch.ElapsedMilliseconds < 2000, $"Sanitising took {watch.ElapsedMilliseconds}ms");
  }

  [TestMethod]
  [DataRow("<p><b>One</p>Two</b>", "<p><b>One</b></p>Two")]
  [DataRow("<b><i>One</b>Two</i>", "<b><i>One</i></b>Two")]
  [DataRow("<ul><li>One<li>Two</ul>", "<ul><li>One<li>Two</li></li></ul>")]
  public void Sanitise_MisnestedEndTags_CloseWhatIsOpen(string html, string expected) =>
    Assert.AreEqual(expected, PixataHtmlSanitiser.Sanitise(html));

  [TestMethod]
  public void Sanitise_NullOrEmpty_ReturnsEmpty() {
    Assert.AreEqual("", PixataHtmlSanitiser.Sanitise(null));
    Assert.AreEqual("", PixataHtmlSanitiser.Sanitise(""));
  }

  [TestMethod]
  public void Sanitise_IsStable() {
    string html = "<h2 style=\"text-align: center;\">Title</h2><p>Some <strong>bold</strong>, <em>italic</em> and <span style=\"color: rgb(230, 25, 25); font-size: x-large;\">red</span> text.</p><ul><li><p>One</p></li></ul><table><tbody><tr><td><p>Cell</p></td></tr></tbody></table>";
    Assert.AreEqual(html, PixataHtmlSanitiser.Sanitise(html));
    Assert.AreEqual(html, PixataHtmlSanitiser.Sanitise(PixataHtmlSanitiser.Sanitise(html)));
  }

  [TestMethod]
  [DataRow("https://example.com", "https://example.com")]
  [DataRow(" http://example.com ", "http://example.com")]
  [DataRow("/images/cat.png", "/images/cat.png")]
  [DataRow("cat.png", "cat.png")]
  [DataRow("#section", "#section")]
  [DataRow("tel:+441234567890", "tel:+441234567890")]
  [DataRow("javascript:steal()", null)]
  [DataRow("JAVASCRIPT:steal()", null)]
  [DataRow("\u0001javascript:steal()", null)]
  [DataRow("data:image/png;base64,AAAA", null)]
  [DataRow("ftp://example.com", null)]
  [DataRow("", null)]
  public void SafeUrl_AllowsOnlySafeSchemes(string url, string? expected) =>
    Assert.AreEqual(expected, PixataHtmlSanitiser.SafeUrl(url));

  [TestMethod]
  public void SafeUrl_DataImages_OnlyWhenAllowed() {
    Assert.AreEqual("data:image/png;base64,AAAA", PixataHtmlSanitiser.SafeUrl("data:image/png;base64,AAAA", true));
    Assert.IsNull(PixataHtmlSanitiser.SafeUrl("data:image/svg+xml;base64,AAAA", true));
    Assert.IsNull(PixataHtmlSanitiser.SafeUrl("data:text/html;base64,AAAA", true));
  }

  [TestMethod]
  [DataRow("red", "red")]
  [DataRow("#ff0000", "#ff0000")]
  [DataRow("rgb(255, 0, 0)", "rgb(255, 0, 0)")]
  [DataRow("hsla(0, 100%, 50%, .5)", "hsla(0, 100%, 50%, .5)")]
  [DataRow("Georgia, \"Times New Roman\", serif", "Georgia, \"Times New Roman\", serif")]
  [DataRow("12px", "12px")]
  [DataRow("url(x)", null)]
  [DataRow("expression(steal())", null)]
  [DataRow("red; background: url(x)", null)]
  [DataRow("red !important", null)]
  [DataRow("var(--x)", null)]
  [DataRow("calc(1px + 2px)", null)]
  [DataRow("red /* comment */", null)]
  [DataRow("\\72 ed", null)]
  [DataRow("", null)]
  public void SafeCssValue_AllowsOnlyPlainValues(string value, string? expected) =>
    Assert.AreEqual(expected, PixataHtmlSanitiser.SafeCssValue(value));
}
