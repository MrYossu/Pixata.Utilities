using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests.Components;

[TestClass]
public class Window_Tests {
  private const string ModulePath = "./_content/Pixata.Blazor/pixata-dialogs.js";

  private static RenderFragment Actions(params string[] names) =>
    builder => {
      foreach (string name in names) {
        builder.OpenComponent<PixataWindowAction>(0);
        builder.AddAttribute(1, nameof(PixataWindowAction.Name), name);
        builder.CloseComponent();
      }
    };

  private static IRenderedComponent<PixataWindow> RenderWindow(BunitContext context, Action<ComponentParameterCollectionBuilder<PixataWindow>>? parameters = null) =>
    context.Render<PixataWindow>(p => {
      p.Add(c => c.Visible, true);
      p.Add(c => c.WindowTitle, "Customer details");
      p.Add(c => c.WindowContent, "<p class=\"window-text\">Fred Bloggs</p>");
      parameters?.Invoke(p);
    });

  private static string Style(IRenderedComponent<PixataWindow> cut) =>
    cut.Find(".pixata-blazor-window").GetAttribute("style") ?? "";

  [TestMethod]
  public void PixataWindow_NotVisible_RendersOnlyThePlaceholder() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWindow> cut = context.Render<PixataWindow>(p => p.Add(c => c.WindowTitle, "Customer details"));
    Assert.AreEqual(1, cut.FindAll(".pixata-blazor-layer-anchor").Count);
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-window").Count);
  }

  [TestMethod]
  public void PixataWindow_Visible_RendersAnAccessibleNonModalWindow() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWindow> cut = RenderWindow(context, p => p.Add(c => c.Id, "customer"));
    IElement window = cut.Find(".pixata-blazor-window-wrapper > .pixata-blazor-window");
    Assert.AreEqual("customer", window.Id);
    Assert.AreEqual("dialog", window.GetAttribute("role"));
    Assert.AreEqual("false", window.GetAttribute("aria-modal"));
    Assert.AreEqual("0", window.GetAttribute("tabindex"));
    Assert.AreEqual("customer-title", window.GetAttribute("aria-labelledby"));
    Assert.AreEqual("Customer details", cut.Find("#customer-title").TextContent);
    Assert.AreEqual("Fred Bloggs", cut.Find(".pixata-blazor-window-content .window-text").TextContent);
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-overlay").Count);
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-window-titlebar-actions").Count);
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-window-footer").Count);
  }

  [TestMethod]
  public void PixataWindow_Modal_HasAnOverlay() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWindow> cut = RenderWindow(context, p => p.Add(c => c.Modal, true));
    Assert.AreEqual("true", cut.Find(".pixata-blazor-window").GetAttribute("aria-modal"));
    Assert.AreEqual("pixata-blazor-window-wrapper pixata-blazor-window-wrapper-modal", cut.Find(".pixata-blazor-window-wrapper").ClassName);
    Assert.AreEqual(1, cut.FindAll(".pixata-blazor-window-wrapper > .pixata-blazor-overlay").Count);
  }

  [TestMethod]
  public void PixataWindow_OverlayClick_ClosesOnlyWhenAskedTo() {
    using BunitContext context = PixataTestContext.Create();
    List<bool> changes = [];
    IRenderedComponent<PixataWindow> cut = RenderWindow(context, p => {
      p.Add(c => c.Modal, true);
      p.Add(c => c.VisibleChanged, (bool v) => changes.Add(v));
    });
    cut.Find(".pixata-blazor-overlay").Click();
    Assert.AreEqual(0, changes.Count);
    cut.Render(p => p.Add(c => c.CloseOnOverlayClick, true));
    cut.Find(".pixata-blazor-overlay").Click();
    CollectionAssert.AreEqual(new[] { false }, changes);
  }

  [TestMethod]
  public void PixataWindow_Escape_ClosesUnlessTurnedOff() {
    using BunitContext context = PixataTestContext.Create();
    List<bool> changes = [];
    IRenderedComponent<PixataWindow> cut = RenderWindow(context, p => p.Add(c => c.VisibleChanged, (bool v) => changes.Add(v)));
    cut.Find(".pixata-blazor-window").KeyDown(new KeyboardEventArgs { Key = "Escape" });
    Assert.AreEqual(1, changes.Count);
    cut.Render(p => p.Add(c => c.CloseOnEscape, false));
    cut.Find(".pixata-blazor-window").KeyDown(new KeyboardEventArgs { Key = "Escape" });
    Assert.AreEqual(1, changes.Count);
  }

  [TestMethod]
  public void PixataWindow_BuiltInActions_HaveIconsAndLabels() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWindow> cut = RenderWindow(context, p => p.Add(c => c.WindowActions, Actions("Minimize", "Maximize", "Close")));
    IReadOnlyList<IElement> buttons = cut.FindAll(".pixata-blazor-window-titlebar-action");
    Assert.AreEqual(3, buttons.Count);
    Assert.AreEqual("Minimise", buttons[0].GetAttribute("aria-label"));
    Assert.AreEqual("Maximise", buttons[1].GetAttribute("title"));
    Assert.AreEqual("Close", buttons[2].GetAttribute("aria-label"));
    StringAssert.Contains(buttons[0].ClassName, "pixata-blazor-window-action-minimise");
    Assert.IsNotNull(buttons[1].QuerySelector("svg"));
  }

  [TestMethod]
  public void PixataWindow_UkSpelledActionNames_AreBuiltIn() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWindow> cut = RenderWindow(context, p => p.Add(c => c.WindowActions, Actions("minimise", "Maximise")));
    Assert.AreEqual(1, cut.FindAll(".pixata-blazor-window-action-minimise").Count);
    Assert.AreEqual(1, cut.FindAll(".pixata-blazor-window-action-maximise").Count);
  }

  [TestMethod]
  public void PixataWindow_MinimiseAction_MinimisesAndRestores() {
    using BunitContext context = PixataTestContext.Create();
    List<PixataWindowState> changes = [];
    IRenderedComponent<PixataWindow> cut = RenderWindow(context, p => {
      p.Add(c => c.WindowActions, Actions("Minimize", "Maximize", "Close"));
      p.Add(c => c.WindowFooter, "<button>Save</button>");
      p.Add(c => c.StateChanged, (PixataWindowState s) => changes.Add(s));
    });
    cut.Find(".pixata-blazor-window-action-minimise").Click();
    CollectionAssert.AreEqual(new[] { PixataWindowState.Minimized }, changes);
    StringAssert.Contains(cut.Find(".pixata-blazor-window").ClassName, "pixata-blazor-window-minimised");
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-window-content").Count);
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-window-footer").Count);
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-window-resize-handle").Count);
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-window-action-maximise").Count);
    IElement restore = cut.Find(".pixata-blazor-window-action-minimise");
    Assert.AreEqual("Restore", restore.GetAttribute("aria-label"));
    restore.Click();
    CollectionAssert.AreEqual(new[] { PixataWindowState.Minimized, PixataWindowState.Default }, changes);
    Assert.AreEqual(1, cut.FindAll(".pixata-blazor-window-content").Count);
    Assert.AreEqual(1, cut.FindAll(".pixata-blazor-window-footer").Count);
  }

  [TestMethod]
  public void PixataWindow_PersistContent_HidesTheContentRatherThanRemovingIt() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWindow> cut = RenderWindow(context, p => {
      p.Add(c => c.State, PixataWindowState.Minimized);
      p.Add(c => c.PersistContent, true);
    });
    Assert.IsTrue(cut.Find(".pixata-blazor-window-content").HasAttribute("hidden"));
  }

  [TestMethod]
  public void PixataWindow_MaximiseAction_MaximisesAndRestores() {
    using BunitContext context = PixataTestContext.Create();
    List<PixataWindowState> changes = [];
    IRenderedComponent<PixataWindow> cut = RenderWindow(context, p => {
      p.Add(c => c.WindowActions, Actions("Minimize", "Maximize"));
      p.Add(c => c.Top, "100px");
      p.Add(c => c.Left, "200px");
      p.Add(c => c.StateChanged, (PixataWindowState s) => changes.Add(s));
    });
    cut.Find(".pixata-blazor-window-action-maximise").Click();
    CollectionAssert.AreEqual(new[] { PixataWindowState.Maximized }, changes);
    IElement window = cut.Find(".pixata-blazor-window");
    StringAssert.Contains(window.ClassName, "pixata-blazor-window-maximised");
    Assert.IsFalse(window.HasAttribute("style"));
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-window-action-minimise").Count);
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-window-resize-handle").Count);
    cut.Find(".pixata-blazor-window-action-maximise").Click();
    Assert.AreEqual(PixataWindowState.Default, changes[^1]);
    StringAssert.Contains(Style(cut), "left:200px;top:100px");
  }

  [TestMethod]
  public void PixataWindow_CloseAction_RaisesVisibleChanged() {
    using BunitContext context = PixataTestContext.Create();
    List<bool> changes = [];
    IRenderedComponent<PixataWindow> cut = RenderWindow(context, p => {
      p.Add(c => c.WindowActions, Actions("Close"));
      p.Add(c => c.VisibleChanged, (bool v) => changes.Add(v));
    });
    cut.Find(".pixata-blazor-window-action-close").Click();
    CollectionAssert.AreEqual(new[] { false }, changes);
  }

  [TestMethod]
  public void PixataWindow_CloseActionWithoutBinding_HidesTheWindow() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWindow> cut = RenderWindow(context, p => p.Add(c => c.WindowActions, Actions("Close")));
    cut.Find(".pixata-blazor-window-action-close").Click();
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-window").Count);
  }

  [TestMethod]
  public void PixataWindow_CustomAction_RunsItsOnClick() {
    using BunitContext context = PixataTestContext.Create();
    int clicks = 0;
    IRenderedComponent<PixataWindow> cut = RenderWindow(context, p => p.Add(c => c.WindowActions, builder => {
      builder.OpenComponent<PixataWindowAction>(0);
      builder.AddAttribute(1, nameof(PixataWindowAction.Name), "Refresh");
      builder.AddAttribute(2, nameof(PixataWindowAction.Icon), PixataSvgIcons.Refresh);
      builder.AddAttribute(3, nameof(PixataWindowAction.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, () => clicks++));
      builder.CloseComponent();
    }));
    IElement button = cut.Find(".pixata-blazor-window-titlebar-action");
    Assert.AreEqual("Refresh", button.GetAttribute("aria-label"));
    button.Click();
    Assert.AreEqual(1, clicks);
  }

  [TestMethod]
  public void PixataWindow_OnClickOnABuiltInAction_ReplacesWhatItDoes() {
    using BunitContext context = PixataTestContext.Create();
    int clicks = 0;
    List<bool> changes = [];
    IRenderedComponent<PixataWindow> cut = RenderWindow(context, p => {
      p.Add(c => c.VisibleChanged, (bool v) => changes.Add(v));
      p.Add(c => c.WindowActions, builder => {
        builder.OpenComponent<PixataWindowAction>(0);
        builder.AddAttribute(1, nameof(PixataWindowAction.Name), "Close");
        builder.AddAttribute(2, nameof(PixataWindowAction.Title), "Close the customer");
        builder.AddAttribute(3, nameof(PixataWindowAction.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, () => clicks++));
        builder.CloseComponent();
      });
    });
    IElement button = cut.Find(".pixata-blazor-window-action-close");
    Assert.AreEqual("Close the customer", button.GetAttribute("title"));
    button.Click();
    Assert.AreEqual(1, clicks);
    Assert.AreEqual(0, changes.Count);
  }

  [TestMethod]
  public void PixataWindow_HiddenAction_IsNotRendered() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWindow> cut = RenderWindow(context, p => p.Add(c => c.WindowActions, builder => {
      builder.OpenComponent<PixataWindowAction>(0);
      builder.AddAttribute(1, nameof(PixataWindowAction.Name), "Close");
      builder.AddAttribute(2, nameof(PixataWindowAction.Hidden), true);
      builder.CloseComponent();
    }));
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-window-titlebar-action").Count);
  }

  [TestMethod]
  public void PixataWindow_AppWideTexts_LabelTheActions() {
    using BunitContext context = PixataTestContext.Create();
    context.Services.AddScoped(_ => new PixataDialogTexts { Minimise = "Réduire", Close = "Fermer" });
    IRenderedComponent<PixataWindow> cut = RenderWindow(context, p => p.Add(c => c.WindowActions, Actions("Minimize", "Close")));
    Assert.AreEqual("Réduire", cut.Find(".pixata-blazor-window-action-minimise").GetAttribute("title"));
    Assert.AreEqual("Fermer", cut.Find(".pixata-blazor-window-action-close").GetAttribute("title"));
  }

  [TestMethod]
  public void PixataWindow_NoTopOrLeft_IsCentred() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWindow> cut = RenderWindow(context, p => p.Add(c => c.Width, "400px"));
    StringAssert.Contains(cut.Find(".pixata-blazor-window").ClassName, "pixata-blazor-window-centred");
    Assert.IsFalse(Style(cut).Contains("left:"));
  }

  [TestMethod]
  public void PixataWindow_TopLeftWidthAndHeight_AreInTheStyle() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWindow> cut = RenderWindow(context, p => {
      p.Add(c => c.Top, "100px");
      p.Add(c => c.Left, "200px");
      p.Add(c => c.Width, "300px");
      p.Add(c => c.Height, "250px");
    });
    Assert.IsFalse(cut.Find(".pixata-blazor-window").ClassName!.Contains("centred"));
    Assert.AreEqual("left:200px;top:100px;width:300px;min-width:120px;height:250px;min-height:100px;", Style(cut));
  }

  [TestMethod]
  public void PixataWindow_Centered_IgnoresTopAndLeft() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWindow> cut = RenderWindow(context, p => {
      p.Add(c => c.Top, "100px");
      p.Add(c => c.Left, "200px");
      p.Add(c => c.Centered, true);
    });
    StringAssert.Contains(cut.Find(".pixata-blazor-window").ClassName, "pixata-blazor-window-centred");
    Assert.IsFalse(Style(cut).Contains("left:"));
  }

  [TestMethod]
  public void PixataWindow_SizeLimits_AreInTheStyle() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWindow> cut = RenderWindow(context, p => {
      p.Add(c => c.MinWidth, "240px");
      p.Add(c => c.MaxWidth, "560px");
      p.Add(c => c.MinHeight, "160px");
      p.Add(c => c.MaxHeight, "400px");
    });
    Assert.AreEqual("min-width:240px;max-width:min(100%, 560px);min-height:160px;max-height:min(100%, 400px);", Style(cut));
  }

  [TestMethod]
  public void PixataWindow_Resizable_HasEightHandles() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWindow> cut = RenderWindow(context);
    IReadOnlyList<IElement> handles = cut.FindAll(".pixata-blazor-window-resize-handle");
    Assert.AreEqual(8, handles.Count);
    Assert.AreEqual("se", cut.Find(".pixata-blazor-window-resize-se").GetAttribute("data-pixata-blazor-direction"));
  }

  [TestMethod]
  public void PixataWindow_NotResizable_HasNoHandlesOrDefaultMinimumSize() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWindow> cut = RenderWindow(context, p => p.Add(c => c.Resizable, false));
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-window-resize-handle").Count);
    Assert.AreEqual("", Style(cut));
  }

  [TestMethod]
  public void PixataWindow_SizeThemeColorAndClass_SetTheClasses() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWindow> cut = RenderWindow(context, p => {
      p.Add(c => c.Size, PixataThemeConstants.Window.Size.Medium);
      p.Add(c => c.ThemeColor, PixataThemeConstants.ThemeColor.Primary);
      p.Add(c => c.Class, "my-window");
      p.AddUnmatched("data-test", "x");
    });
    IElement window = cut.Find(".pixata-blazor-window");
    Assert.AreEqual("pixata-blazor-window pixata-blazor-window-md pixata-blazor-window-centred pixata-blazor-window-primary my-window", window.ClassName);
    Assert.AreEqual("x", window.GetAttribute("data-test"));
  }

  [TestMethod]
  public void PixataWindow_FooterLayoutAlign_SetsTheFooterClass() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWindow> cut = RenderWindow(context, p => p.Add(c => c.WindowFooter, "<button>Save</button>"));
    Assert.AreEqual("pixata-blazor-window-footer pixata-blazor-window-footer-stretch", cut.Find(".pixata-blazor-window-footer").ClassName);
    cut.Render(p => p.Add(c => c.FooterLayoutAlign, PixataWindowFooterLayoutAlign.End));
    Assert.AreEqual("pixata-blazor-window-footer pixata-blazor-window-footer-end", cut.Find(".pixata-blazor-window-footer").ClassName);
  }

  [TestMethod]
  public async Task PixataWindow_MovedByTheUser_ReportsTheNewPosition() {
    using BunitContext context = PixataTestContext.Create();
    List<string> tops = [];
    List<string> lefts = [];
    IRenderedComponent<PixataWindow> cut = RenderWindow(context, p => {
      p.Add(c => c.TopChanged, (string v) => tops.Add(v));
      p.Add(c => c.LeftChanged, (string v) => lefts.Add(v));
    });
    await cut.InvokeAsync(() => cut.Instance.OnWindowMoved(120.4, 80));
    CollectionAssert.AreEqual(new[] { "80px" }, tops);
    CollectionAssert.AreEqual(new[] { "120px" }, lefts);
    Assert.IsFalse(cut.Find(".pixata-blazor-window").ClassName!.Contains("centred"));
    StringAssert.StartsWith(Style(cut), "left:120px;top:80px;");
  }

  [TestMethod]
  public async Task PixataWindow_ResizedByTheUser_ReportsTheNewSize() {
    using BunitContext context = PixataTestContext.Create();
    List<string> widths = [];
    List<string> heights = [];
    List<string> tops = [];
    IRenderedComponent<PixataWindow> cut = RenderWindow(context, p => {
      p.Add(c => c.Top, "10px");
      p.Add(c => c.Left, "20px");
      p.Add(c => c.TopChanged, (string v) => tops.Add(v));
      p.Add(c => c.WidthChanged, (string v) => widths.Add(v));
      p.Add(c => c.HeightChanged, (string v) => heights.Add(v));
    });
    await cut.InvokeAsync(() => cut.Instance.OnWindowResized(20, 10, 400, 300));
    CollectionAssert.AreEqual(new[] { "400px" }, widths);
    CollectionAssert.AreEqual(new[] { "300px" }, heights);
    Assert.AreEqual(0, tops.Count);
    Assert.AreEqual("left:20px;top:10px;width:400px;min-width:120px;height:300px;min-height:100px;", Style(cut));
  }

  [TestMethod]
  public async Task PixataWindow_KeyboardShortcuts_MaximiseMinimiseAndRestore() {
    using BunitContext context = PixataTestContext.Create();
    List<PixataWindowState> changes = [];
    IRenderedComponent<PixataWindow> cut = RenderWindow(context, p => p.Add(c => c.StateChanged, (PixataWindowState s) => changes.Add(s)));
    await cut.InvokeAsync(() => cut.Instance.OnWindowShortcut("maximise"));
    await cut.InvokeAsync(() => cut.Instance.OnWindowShortcut("maximise"));
    await cut.InvokeAsync(() => cut.Instance.OnWindowShortcut("minimise"));
    await cut.InvokeAsync(() => cut.Instance.OnWindowShortcut("minimise"));
    await cut.InvokeAsync(() => cut.Instance.OnWindowShortcut("minimise"));
    await cut.InvokeAsync(() => cut.Instance.OnWindowShortcut("maximise"));
    CollectionAssert.AreEqual(new[] { PixataWindowState.Maximized, PixataWindowState.Default, PixataWindowState.Minimized, PixataWindowState.Default }, changes);
  }

  [TestMethod]
  public async Task PixataWindow_Methods_ChangeTheState() {
    using BunitContext context = PixataTestContext.Create();
    List<PixataWindowState> changes = [];
    IRenderedComponent<PixataWindow> cut = RenderWindow(context, p => p.Add(c => c.StateChanged, (PixataWindowState s) => changes.Add(s)));
    await cut.Instance.Minimize();
    StringAssert.Contains(cut.Find(".pixata-blazor-window").ClassName, "pixata-blazor-window-minimised");
    await cut.Instance.Maximize();
    StringAssert.Contains(cut.Find(".pixata-blazor-window").ClassName, "pixata-blazor-window-maximised");
    await cut.Instance.Restore();
    await cut.Instance.Restore();
    CollectionAssert.AreEqual(new[] { PixataWindowState.Minimized, PixataWindowState.Maximized, PixataWindowState.Default }, changes);
  }

  [TestMethod]
  public void PixataWindow_StateParameter_IsFollowed() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWindow> cut = RenderWindow(context, p => p.Add(c => c.State, PixataWindowState.Maximized));
    StringAssert.Contains(cut.Find(".pixata-blazor-window").ClassName, "pixata-blazor-window-maximised");
    cut.Render(p => p.Add(c => c.State, PixataWindowState.Default));
    Assert.IsFalse(cut.Find(".pixata-blazor-window").ClassName!.Contains("maximised"));
  }

  [TestMethod]
  public void PixataWindow_OpensItsLayerWithTheWindowOptions() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = context.JSInterop.SetupModule(ModulePath);
    module.Setup<int>("openLayer", _ => true).SetResult(9);
    module.SetupVoid("updateLayer", _ => true).SetVoidResult();
    module.SetupVoid("closeLayer", _ => true);
    IRenderedComponent<PixataWindow> cut = RenderWindow(context, p => {
      p.Add(c => c.ContainmentSelector, "#workspace");
      p.Add(c => c.Draggable, false);
    });
    string options = module.VerifyInvoke("openLayer").Arguments[4]?.ToString() ?? "";
    StringAssert.Contains(options, "Kind = window");
    StringAssert.Contains(options, "Modal = False");
    StringAssert.Contains(options, "Draggable = False");
    StringAssert.Contains(options, "Resizable = True");
    StringAssert.Contains(options, "ContainmentSelector = #workspace");
    cut.Render(p => p.Add(c => c.Resizable, false));
    JSRuntimeInvocation update = module.VerifyInvoke("updateLayer");
    Assert.AreEqual(9, update.Arguments[0]);
    StringAssert.Contains(update.Arguments[1]?.ToString(), "Resizable = False");
    cut.Render(p => p.Add(c => c.Visible, false));
    Assert.AreEqual(9, module.VerifyInvoke("closeLayer").Arguments[0]);
  }

  [TestMethod]
  public void PixataWindow_Modal_IgnoresTheContainment() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = context.JSInterop.SetupModule(ModulePath);
    module.Setup<int>("openLayer", _ => true).SetResult(1);
    RenderWindow(context, p => {
      p.Add(c => c.Modal, true);
      p.Add(c => c.ContainmentSelector, "#workspace");
    });
    string options = module.VerifyInvoke("openLayer").Arguments[4]?.ToString() ?? "";
    StringAssert.Contains(options, "Modal = True");
    Assert.IsFalse(options.Contains("#workspace"));
  }
}
