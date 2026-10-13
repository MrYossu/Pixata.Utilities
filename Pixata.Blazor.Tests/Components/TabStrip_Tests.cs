using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests.Components;

[TestClass]
public class TabStrip_Tests {
  private const string ModulePath = "./_content/Pixata.Blazor/Components/TabStrip/PixataTabStrip.razor.js";

  private record TabSpec(string Id, bool Disabled = false, bool Visible = true, bool Closeable = false);

  /// <summary>
  /// The tabs as a render fragment, the way a keyed @foreach in a page would declare them
  /// </summary>
  private static RenderFragment Tabs(params TabSpec[] tabs) =>
    Tabs(true, tabs);

  /// <summary>
  /// The tabs as a render fragment, the way a @foreach in a page would declare them, with or without a @key
  /// </summary>
  private static RenderFragment Tabs(bool keyed, params TabSpec[] tabs) =>
    builder => {
      foreach (TabSpec tab in tabs) {
        builder.OpenComponent<PixataTab>(0);
        if (keyed) {
          builder.SetKey(tab.Id);
        }
        builder.AddAttribute(1, nameof(PixataTab.Id), tab.Id);
        builder.AddAttribute(2, nameof(PixataTab.Title), $"Title {tab.Id}");
        builder.AddAttribute(3, nameof(PixataTab.Disabled), tab.Disabled);
        builder.AddAttribute(4, nameof(PixataTab.Visible), tab.Visible);
        builder.AddAttribute(5, nameof(PixataTab.Closeable), tab.Closeable);
        builder.AddAttribute(6, nameof(PixataTab.ChildContent), (RenderFragment)(content => content.AddContent(0, $"Content {tab.Id}")));
        builder.CloseComponent();
      }
    };

  private static TabSpec[] Specs(params string[] ids) =>
    [.. ids.Select(id => new TabSpec(id))];

  private static IRenderedComponent<PixataTabStrip> Render(BunitContext context, RenderFragment tabs, string? activeTabId = null, System.Action<ComponentParameterCollectionBuilder<PixataTabStrip>>? more = null) =>
    context.Render<PixataTabStrip>(p => {
      p.Add(c => c.ChildContent, tabs);
      if (activeTabId is not null) {
        p.Add(c => c.ActiveTabId, activeTabId);
      }
      more?.Invoke(p);
    });

  private static string[] HeaderTitles(IRenderedComponent<PixataTabStrip> cut) =>
    [.. cut.FindAll("[role=tab]").Select(t => t.TextContent.Trim())];

  private static IElement ActiveHeader(IRenderedComponent<PixataTabStrip> cut) =>
    cut.Find("[role=tab][aria-selected=true]");

  private static string VisiblePanelText(IRenderedComponent<PixataTabStrip> cut) =>
    cut.Find("[role=tabpanel]:not([hidden])").TextContent.Trim();

  private static void Press(IRenderedComponent<PixataTabStrip> cut, string key) =>
    cut.Find("[role=tablist]").KeyDown(new KeyboardEventArgs { Key = key });

  [TestMethod]
  public void Renders_TheTabsWithTheirRoles() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(Specs("a", "b", "c")));
    Assert.AreEqual("horizontal", cut.Find("ul[role=tablist]").GetAttribute("aria-orientation"));
    CollectionAssert.AreEqual(new[] { "Title a", "Title b", "Title c" }, HeaderTitles(cut));
    IReadOnlyList<IElement> headers = cut.FindAll("[role=tab]");
    IReadOnlyList<IElement> panels = cut.FindAll("[role=tabpanel]");
    for (int i = 0; i < headers.Count; i++) {
      Assert.AreEqual(panels[i].Id, headers[i].GetAttribute("aria-controls"));
      Assert.AreEqual(headers[i].Id, panels[i].GetAttribute("aria-labelledby"));
    }
  }

  [TestMethod]
  public void NoActiveTabId_SelectsTheFirstTab() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(Specs("a", "b")));
    Assert.AreEqual("Title a", ActiveHeader(cut).TextContent.Trim());
    Assert.AreEqual("0", ActiveHeader(cut).GetAttribute("tabindex"));
    Assert.AreEqual("-1", cut.FindAll("[role=tab]")[1].GetAttribute("tabindex"));
    Assert.AreEqual("Content a", VisiblePanelText(cut));
    Assert.AreEqual(1, cut.FindAll("[role=tabpanel]:not([hidden])").Count);
  }

  [TestMethod]
  public void NoActiveTabId_SkipsDisabledAndHiddenTabs() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(new TabSpec("a", Visible: false), new TabSpec("b", Disabled: true), new TabSpec("c")));
    Assert.AreEqual("Title c", ActiveHeader(cut).TextContent.Trim());
  }

  [TestMethod]
  public void ActiveTabId_SelectsThatTab() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(Specs("a", "b", "c")), "b");
    Assert.AreEqual("Title b", ActiveHeader(cut).TextContent.Trim());
    Assert.IsTrue(ActiveHeader(cut).ClassList.Contains("pixata-blazor-active"));
    Assert.AreEqual("Content b", VisiblePanelText(cut));
  }

  [TestMethod]
  public void ActiveTabId_ChangedByTheParent_SelectsThatTab() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(Specs("a", "b", "c")), "a");
    cut.Render(p => p.Add(c => c.ActiveTabId, "c"));
    Assert.AreEqual("Content c", VisiblePanelText(cut));
  }

  [TestMethod]
  public void ClickingATab_SelectsItAndRaisesActiveTabIdChanged() {
    using BunitContext context = PixataTestContext.Create();
    string? changed = null;
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(Specs("a", "b", "c")), "a", p => p.Add(c => c.ActiveTabIdChanged, (string id) => changed = id));
    cut.FindAll("[role=tab]")[2].Click();
    Assert.AreEqual("c", changed);
    Assert.AreEqual("Content c", VisiblePanelText(cut));
  }

  [TestMethod]
  public void ClickingADisabledTab_DoesNothing() {
    using BunitContext context = PixataTestContext.Create();
    string? changed = null;
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(new TabSpec("a"), new TabSpec("b", Disabled: true)), "a", p => p.Add(c => c.ActiveTabIdChanged, (string id) => changed = id));
    IElement disabled = cut.FindAll("[role=tab]")[1];
    Assert.AreEqual("true", disabled.GetAttribute("aria-disabled"));
    Assert.IsTrue(disabled.ClassList.Contains("pixata-blazor-disabled"));
    disabled.Click();
    Assert.IsNull(changed);
    Assert.AreEqual("Content a", VisiblePanelText(cut));
  }

  [TestMethod]
  public void HiddenTab_IsNotShown() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(new TabSpec("a"), new TabSpec("b", Visible: false), new TabSpec("c")), "b");
    CollectionAssert.AreEqual(new[] { "Title a", "Title c" }, HeaderTitles(cut));
    Assert.AreEqual("Content a", VisiblePanelText(cut));
  }

  [TestMethod]
  public void ArrowKeys_MoveThroughTheTabsSkippingDisabledOnesAndWrapping() {
    using BunitContext context = PixataTestContext.Create();
    List<string> changes = [];
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(new TabSpec("a"), new TabSpec("b", Disabled: true), new TabSpec("c")), "a", p => p.Add(c => c.ActiveTabIdChanged, (string id) => changes.Add(id)));
    Press(cut, "ArrowRight");
    Assert.AreEqual("Content c", VisiblePanelText(cut));
    Press(cut, "ArrowRight");
    Assert.AreEqual("Content a", VisiblePanelText(cut));
    Press(cut, "ArrowLeft");
    Assert.AreEqual("Content c", VisiblePanelText(cut));
    Press(cut, "ArrowUp");
    Assert.AreEqual("Content a", VisiblePanelText(cut));
    Press(cut, "ArrowDown");
    Assert.AreEqual("Content c", VisiblePanelText(cut));
    CollectionAssert.AreEqual(new[] { "c", "a", "c", "a", "c" }, changes);
  }

  [TestMethod]
  public void HomeAndEnd_SelectTheFirstAndLastTabs() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(Specs("a", "b", "c", "d")), "b");
    Press(cut, "End");
    Assert.AreEqual("Content d", VisiblePanelText(cut));
    Press(cut, "Home");
    Assert.AreEqual("Content a", VisiblePanelText(cut));
  }

  [TestMethod]
  public void KeyboardNavigation_FocusesTheNewTab() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(Specs("a", "b")), "a");
    Press(cut, "ArrowRight");
    Assert.IsTrue(context.JSInterop.Invocations.Any(i => i.Identifier == "focusElement"));
  }

  [TestMethod]
  public void HeaderTemplate_ReplacesTheTitle() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataTabStrip> cut = context.Render<PixataTabStrip>(p => p
      .AddChildContent<PixataTab>(t => t
        .Add(c => c.Title, "Ignored")
        .Add(c => c.HeaderTemplate, "<b class=\"custom\">Custom</b>")
        .Add(c => c.Content, "Templated content")));
    Assert.AreEqual("Custom", cut.Find("[role=tab] b.custom").TextContent);
    Assert.IsFalse(cut.Find("[role=tab]").TextContent.Contains("Ignored"));
    Assert.AreEqual("Templated content", VisiblePanelText(cut));
  }

  [TestMethod]
  public void Tab_OutsideATabStrip_Throws() {
    using BunitContext context = PixataTestContext.Create();
    Assert.ThrowsException<System.InvalidOperationException>(() => context.Render<PixataTab>(p => p.Add(c => c.Title, "Lost")));
  }

  [TestMethod]
  public void CloseButton_RaisesOnTabCloseHidesTheTabAndSelectsTheNextOne() {
    using BunitContext context = PixataTestContext.Create();
    PixataTabStripTabCloseEventArgs? closed = null;
    string? active = null;
    bool? visible = null;
    IRenderedComponent<PixataTabStrip> cut = context.Render<PixataTabStrip>(p => p
      .Add(c => c.ActiveTabId, "b")
      .Add(c => c.OnTabClose, (PixataTabStripTabCloseEventArgs e) => closed = e)
      .Add(c => c.ActiveTabIdChanged, (string id) => active = id)
      .AddChildContent<PixataTab>(t => t.Add(c => c.Id, "a").Add(c => c.Title, "A"))
      .AddChildContent<PixataTab>(t => t.Add(c => c.Id, "b").Add(c => c.Title, "B").Add(c => c.Closeable, true).Add(c => c.VisibleChanged, (bool v) => visible = v))
      .AddChildContent<PixataTab>(t => t.Add(c => c.Id, "c").Add(c => c.Title, "C").Add(c => c.ChildContent, "Content c")));
    Assert.AreEqual(1, cut.FindAll(".pixata-blazor-tabstrip-close").Count);
    cut.Find(".pixata-blazor-tabstrip-close").Click();
    Assert.AreEqual("b", closed?.TabId);
    Assert.IsFalse(visible);
    Assert.AreEqual("c", active);
    CollectionAssert.AreEqual(new[] { "A", "C" }, HeaderTitles(cut));
    Assert.AreEqual("Content c", VisiblePanelText(cut));
  }

  [TestMethod]
  public void ClosingTheLastTab_SelectsThePreviousOne() {
    using BunitContext context = PixataTestContext.Create();
    string? active = null;
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(new TabSpec("a"), new TabSpec("b", Closeable: true)), "b", p => p.Add(c => c.ActiveTabIdChanged, (string id) => active = id));
    cut.Find(".pixata-blazor-tabstrip-close").Click();
    Assert.AreEqual("a", active);
    Assert.AreEqual("Content a", VisiblePanelText(cut));
  }

  [TestMethod]
  public void ClosingAnInactiveTab_LeavesTheActiveTabAlone() {
    using BunitContext context = PixataTestContext.Create();
    string? active = null;
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(new TabSpec("a"), new TabSpec("b", Closeable: true)), "a", p => p.Add(c => c.ActiveTabIdChanged, (string id) => active = id));
    cut.Find(".pixata-blazor-tabstrip-close").Click();
    Assert.IsNull(active);
    CollectionAssert.AreEqual(new[] { "Title a" }, HeaderTitles(cut));
  }

  [TestMethod]
  public void ClosedTab_StaysClosedUntilVisibleChanges() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(new TabSpec("a"), new TabSpec("b", Closeable: true)), "a");
    cut.Find(".pixata-blazor-tabstrip-close").Click();
    // Re-rendering with the same (true) Visible doesn't bring it back...
    cut.Render(p => p.Add(c => c.ChildContent, Tabs(new TabSpec("a"), new TabSpec("b", Closeable: true))));
    CollectionAssert.AreEqual(new[] { "Title a" }, HeaderTitles(cut));
    // ...but a bound Visible going false and then true does
    cut.Render(p => p.Add(c => c.ChildContent, Tabs(new TabSpec("a"), new TabSpec("b", Visible: false, Closeable: true))));
    cut.Render(p => p.Add(c => c.ChildContent, Tabs(new TabSpec("a"), new TabSpec("b", Closeable: true))));
    CollectionAssert.AreEqual(new[] { "Title a", "Title b" }, HeaderTitles(cut));
  }

  [TestMethod]
  public void OnTabClose_Cancelled_KeepsTheTab() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(new TabSpec("a", Closeable: true), new TabSpec("b")), "a", p => p.Add(c => c.OnTabClose, (PixataTabStripTabCloseEventArgs e) => e.IsCancelled = true));
    cut.Find(".pixata-blazor-tabstrip-close").Click();
    CollectionAssert.AreEqual(new[] { "Title a", "Title b" }, HeaderTitles(cut));
    Assert.AreEqual("Content a", VisiblePanelText(cut));
  }

  [TestMethod]
  public void DeleteKey_ClosesTheActiveCloseableTab() {
    using BunitContext context = PixataTestContext.Create();
    string? closed = null;
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(new TabSpec("a", Closeable: true), new TabSpec("b")), "a", p => p.Add(c => c.OnTabClose, (PixataTabStripTabCloseEventArgs e) => closed = e.TabId));
    Press(cut, "Delete");
    Assert.AreEqual("a", closed);
    CollectionAssert.AreEqual(new[] { "Title b" }, HeaderTitles(cut));
  }

  [TestMethod]
  public void DeleteKey_LeavesATabThatIsNotCloseable() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(Specs("a", "b")), "a");
    Press(cut, "Delete");
    CollectionAssert.AreEqual(new[] { "Title a", "Title b" }, HeaderTitles(cut));
  }

  [TestMethod]
  public void WithoutPersistTabContent_OnlyTheActiveTabsContentIsRendered() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(Specs("a", "b")), "a");
    cut.FindAll("[role=tab]")[1].Click();
    Assert.IsFalse(cut.Markup.Contains("Content a"));
    Assert.IsTrue(cut.Markup.Contains("Content b"));
  }

  [TestMethod]
  public void WithPersistTabContent_VisitedTabsKeepTheirContent() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(Specs("a", "b", "c")), "a", p => p.Add(c => c.PersistTabContent, true));
    Assert.IsFalse(cut.Markup.Contains("Content b"));
    cut.FindAll("[role=tab]")[1].Click();
    Assert.IsTrue(cut.Markup.Contains("Content a"));
    Assert.IsTrue(cut.Markup.Contains("Content b"));
    Assert.IsFalse(cut.Markup.Contains("Content c"));
    Assert.AreEqual("Content b", VisiblePanelText(cut));
  }

  [TestMethod]
  public void TabPosition_SetsTheClassAndOrientation() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(Specs("a")), more: p => p.Add(c => c.TabPosition, PixataTabPosition.Left));
    Assert.IsTrue(cut.Find(".pixata-blazor-tabstrip").ClassList.Contains("pixata-blazor-tabstrip-left"));
    Assert.AreEqual("vertical", cut.Find("[role=tablist]").GetAttribute("aria-orientation"));
    cut.Render(p => p.Add(c => c.TabPosition, PixataTabPosition.Bottom));
    Assert.IsTrue(cut.Find(".pixata-blazor-tabstrip").ClassList.Contains("pixata-blazor-tabstrip-bottom"));
    Assert.AreEqual("horizontal", cut.Find("[role=tablist]").GetAttribute("aria-orientation"));
  }

  [TestMethod]
  public void TabAlignmentSizeWidthAndHeight_AreRendered() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(Specs("a")), more: p => p
      .Add(c => c.TabAlignment, PixataTabStripTabAlignment.Stretched)
      .Add(c => c.Size, PixataThemeConstants.Size.Large)
      .Add(c => c.Width, "300px")
      .Add(c => c.Height, "200px")
      .Add(c => c.Class, "mine"));
    IElement root = cut.Find(".pixata-blazor-tabstrip");
    Assert.IsTrue(root.ClassList.Contains("pixata-blazor-tabstrip-lg"));
    Assert.IsTrue(root.ClassList.Contains("mine"));
    Assert.AreEqual("width:300px;height:200px;", root.GetAttribute("style"));
    Assert.IsTrue(cut.Find("[role=tablist]").ClassList.Contains("pixata-blazor-tabstrip-items-stretched"));
  }

  [TestMethod]
  public void NotScrollable_HasNoScrollButtons() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(Specs("a")), more: p => p.Add(c => c.ScrollButtonsVisibility, PixataTabStripScrollButtonsVisibility.Visible));
    Assert.IsFalse(cut.Find(".pixata-blazor-tabstrip").ClassList.Contains("pixata-blazor-tabstrip-scrollable"));
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-tabstrip-scroll-button").Count);
  }

  [TestMethod]
  public void Scrollable_AutoButtons_AppearWhenTheTabsOverflow() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(Specs("a", "b")), more: p => p.Add(c => c.Scrollable, true));
    Assert.IsTrue(cut.Find(".pixata-blazor-tabstrip").ClassList.Contains("pixata-blazor-tabstrip-scrollable"));
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-tabstrip-scroll-button").Count);
    cut.InvokeAsync(() => cut.Instance.OnScrollStateChanged(true, false, true));
    Assert.IsTrue(cut.Find(".pixata-blazor-tabstrip-prev").HasAttribute("disabled"));
    Assert.IsFalse(cut.Find(".pixata-blazor-tabstrip-next").HasAttribute("disabled"));
    cut.InvokeAsync(() => cut.Instance.OnScrollStateChanged(false, false, false));
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-tabstrip-scroll-button").Count);
  }

  [TestMethod]
  public void OverflowModeScroll_IsTheSameAsScrollable() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(Specs("a")), more: p => p.Add(c => c.OverflowMode, PixataTabStripOverflowMode.Scroll));
    Assert.IsTrue(cut.Find(".pixata-blazor-tabstrip").ClassList.Contains("pixata-blazor-tabstrip-scrollable"));
  }

  [TestMethod]
  public void Scrollable_ButtonPositionsAndVisibility() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(Specs("a")), more: p => p
      .Add(c => c.Scrollable, true)
      .Add(c => c.ScrollButtonsVisibility, PixataTabStripScrollButtonsVisibility.Visible));
    string[] Layout() =>
      [.. cut.Find(".pixata-blazor-tabstrip-items-wrapper").Children.Select(e => e.ClassList.Contains("pixata-blazor-tabstrip-prev") ? "prev" : e.ClassList.Contains("pixata-blazor-tabstrip-next") ? "next" : "list")];
    CollectionAssert.AreEqual(new[] { "prev", "list", "next" }, Layout());
    cut.Render(p => p.Add(c => c.ScrollButtonsPosition, PixataTabStripScrollButtonsPosition.Start));
    CollectionAssert.AreEqual(new[] { "prev", "next", "list" }, Layout());
    cut.Render(p => p.Add(c => c.ScrollButtonsPosition, PixataTabStripScrollButtonsPosition.End));
    CollectionAssert.AreEqual(new[] { "list", "prev", "next" }, Layout());
    cut.Render(p => p.Add(c => c.ScrollButtonsVisibility, PixataTabStripScrollButtonsVisibility.Hidden));
    CollectionAssert.AreEqual(new[] { "list" }, Layout());
  }

  [TestMethod]
  public void Scrollable_ButtonsScrollTheTabs() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = context.JSInterop.SetupModule(ModulePath);
    module.Setup<int>("attach", _ => true).SetResult(7);
    module.SetupVoid("scrollTabs", _ => true).SetVoidResult();
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(Specs("a")), more: p => p
      .Add(c => c.Scrollable, true)
      .Add(c => c.ScrollButtonsVisibility, PixataTabStripScrollButtonsVisibility.Visible));
    cut.InvokeAsync(() => cut.Instance.OnScrollStateChanged(true, true, true));
    cut.Find(".pixata-blazor-tabstrip-next").Click();
    JSRuntimeInvocation scroll = module.Invocations.Last(i => i.Identifier == "scrollTabs");
    Assert.AreEqual(7, scroll.Arguments[0]);
    Assert.AreEqual(1, scroll.Arguments[1]);
  }

  [TestMethod]
  public void TabsAddedAndRemoved_AreShown() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(Specs("a", "b")), "a");
    cut.Render(p => p.Add(c => c.ChildContent, Tabs(Specs("a", "b", "c"))));
    CollectionAssert.AreEqual(new[] { "Title a", "Title b", "Title c" }, HeaderTitles(cut));
    cut.Render(p => p.Add(c => c.ChildContent, Tabs(Specs("a", "c"))));
    CollectionAssert.AreEqual(new[] { "Title a", "Title c" }, HeaderTitles(cut));
  }

  [TestMethod]
  public void RemovingTheActiveTab_SelectsItsNeighbourAndRaisesActiveTabIdChanged() {
    using BunitContext context = PixataTestContext.Create();
    string? active = null;
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(Specs("a", "b", "c")), "b", p => p.Add(c => c.ActiveTabIdChanged, (string id) => active = id));
    cut.Render(p => p.Add(c => c.ChildContent, Tabs(Specs("a", "c"))));
    Assert.AreEqual("c", active);
    Assert.AreEqual("Content c", VisiblePanelText(cut));
  }

  [TestMethod]
  public void TabInsertedInTheMiddle_IsPutInPageOrder() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = context.JSInterop.SetupModule(ModulePath);
    JSRuntimeInvocationHandler<string[]?> tabOrder = module.Setup<string[]?>("tabOrder", _ => true);
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(Specs("a", "c")), "a");
    cut.Render(p => p.Add(c => c.ChildContent, Tabs(Specs("a", "b", "c"))));
    // The new tab registers last, so is shown last until the page tells the tab strip where its panel is
    CollectionAssert.AreEqual(new[] { "Title a", "Title c", "Title b" }, HeaderTitles(cut));
    string[] panelOrder = [.. cut.FindAll("[data-pixata-tab-panel]").Select(e => e.GetAttribute("data-pixata-tab-panel")!)];
    cut.InvokeAsync(() => tabOrder.SetResult(panelOrder));
    cut.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "Title a", "Title b", "Title c" }, HeaderTitles(cut)));
  }

  [TestMethod]
  public void TabsMovedInAKeyedForeach_AreShownInTheirNewOrder() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = context.JSInterop.SetupModule(ModulePath);
    JSRuntimeInvocationHandler<string[]?> tabOrder = module.Setup<string[]?>("tabOrder", _ => true);
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(Specs("a", "b", "c")), "b");
    cut.Render(p => p.Add(c => c.ChildContent, Tabs(Specs("c", "a", "b"))));
    // The moved tabs aren't registered again, so are shown in their old order until the page tells the tab strip their new one
    string[] panelOrder = [.. cut.FindAll("[data-pixata-tab-panel]").Select(e => e.GetAttribute("data-pixata-tab-panel")!)];
    cut.InvokeAsync(() => tabOrder.SetResult(panelOrder));
    cut.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "Title c", "Title a", "Title b" }, HeaderTitles(cut)));
    Assert.AreEqual("Title b", ActiveHeader(cut).TextContent.Trim());
    Assert.AreEqual("Content b", VisiblePanelText(cut));
  }

  [TestMethod]
  public void HidingTheActiveTab_SelectsItsNeighbourAndRaisesActiveTabIdChanged() {
    using BunitContext context = PixataTestContext.Create();
    List<string> changes = [];
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(Specs("a", "b", "c")), "b", p => p.Add(c => c.ActiveTabIdChanged, (string id) => changes.Add(id)));
    cut.Render(p => p.Add(c => c.ChildContent, Tabs(new TabSpec("a"), new TabSpec("b", Visible: false), new TabSpec("c"))));
    Assert.AreEqual("Content c", VisiblePanelText(cut));
    CollectionAssert.AreEqual(new[] { "c" }, changes);
    // Showing the tab again doesn't select it
    cut.Render(p => p.Add(c => c.ChildContent, Tabs(Specs("a", "b", "c"))));
    Assert.AreEqual("Content c", VisiblePanelText(cut));
    CollectionAssert.AreEqual(new[] { "c" }, changes);
  }

  [TestMethod]
  public void RemovingTheActiveTabFromAForeachWithoutAKey_SelectsTheNextTabAndRaisesActiveTabIdChanged() {
    using BunitContext context = PixataTestContext.Create();
    List<string> changes = [];
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(false, Specs("a", "b", "c")), "b", p => p.Add(c => c.ActiveTabIdChanged, (string id) => changes.Add(id)));
    cut.Render(p => p.Add(c => c.ChildContent, Tabs(false, Specs("a", "c"))));
    CollectionAssert.AreEqual(new[] { "Title a", "Title c" }, HeaderTitles(cut));
    Assert.AreEqual("Title c", ActiveHeader(cut).TextContent.Trim());
    Assert.AreEqual("Content c", VisiblePanelText(cut));
    CollectionAssert.AreEqual(new[] { "c" }, changes);
  }

  [TestMethod]
  public void RemovingAnInactiveTabFromAForeachWithoutAKey_LeavesTheActiveTabAlone() {
    using BunitContext context = PixataTestContext.Create();
    List<string> changes = [];
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(false, Specs("a", "b", "c")), "b", p => p.Add(c => c.ActiveTabIdChanged, (string id) => changes.Add(id)));
    cut.Render(p => p.Add(c => c.ChildContent, Tabs(false, Specs("b", "c"))));
    Assert.AreEqual("Content b", VisiblePanelText(cut));
    Assert.AreEqual(0, changes.Count);
  }

  /// <summary>
  /// Declares a tab whose header and content show a value worked out when the host renders, as a page might in a @foreach
  /// </summary>
  private class HeaderTemplateHost : ComponentBase {
    public int Count { get; private set; }

    public void Increment() {
      Count++;
      StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder) {
      string text = $"Count {Count}";
      builder.OpenComponent<PixataTabStrip>(0);
      builder.AddAttribute(1, nameof(PixataTabStrip.ChildContent), (RenderFragment)(tabs => {
        tabs.OpenComponent<PixataTab>(0);
        tabs.AddAttribute(1, nameof(PixataTab.HeaderTemplate), (RenderFragment)(header => header.AddContent(0, text)));
        tabs.AddAttribute(2, nameof(PixataTab.Content), (RenderFragment)(content => content.AddContent(0, $"Content {text}")));
        tabs.CloseComponent();
      }));
      builder.CloseComponent();
    }
  }

  [TestMethod]
  public void HeaderTemplate_ShowsWhatTheParentRenderedLast() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<HeaderTemplateHost> cut = context.Render<HeaderTemplateHost>();
    Assert.AreEqual("Count 0", cut.Find("[role=tab]").TextContent.Trim());
    cut.InvokeAsync(cut.Instance.Increment);
    Assert.AreEqual("Count 1", cut.Find("[role=tab]").TextContent.Trim());
    Assert.AreEqual("Content Count 1", cut.Find("[role=tabpanel]").TextContent.Trim());
  }

  /// <summary>
  /// A component with state of its own, to show whether a tab's content is kept or created again
  /// </summary>
  private class Clicker : ComponentBase {
    private int _clicks;

    protected override void BuildRenderTree(RenderTreeBuilder builder) {
      builder.OpenElement(0, "button");
      builder.AddAttribute(1, "class", "clicker");
      builder.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, () => _clicks++));
      builder.AddContent(3, _clicks);
      builder.CloseElement();
    }
  }

  private static RenderFragment ClickerTabs(params string[] ids) =>
    builder => {
      foreach (string id in ids) {
        builder.OpenComponent<PixataTab>(0);
        builder.SetKey(id);
        builder.AddAttribute(1, nameof(PixataTab.Id), id);
        builder.AddAttribute(2, nameof(PixataTab.Title), id);
        builder.AddAttribute(3, nameof(PixataTab.ChildContent), (RenderFragment)(content => {
          content.OpenComponent<Clicker>(0);
          content.CloseComponent();
        }));
        builder.CloseComponent();
      }
    };

  private static string ActiveClicks(IRenderedComponent<PixataTabStrip> cut) =>
    cut.Find("[role=tabpanel]:not([hidden]) .clicker").TextContent;

  [TestMethod]
  public void PersistTabContent_KeepsTheStateOfTheComponentsInATab() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataTabStrip> cut = Render(context, ClickerTabs("a", "b"), "a", p => p.Add(c => c.PersistTabContent, true));
    cut.Find("[role=tabpanel]:not([hidden]) .clicker").Click();
    cut.Find("[role=tabpanel]:not([hidden]) .clicker").Click();
    Assert.AreEqual("2", ActiveClicks(cut));
    cut.FindAll("[role=tab]")[1].Click();
    Assert.AreEqual("0", ActiveClicks(cut));
    cut.FindAll("[role=tab]")[0].Click();
    Assert.AreEqual("2", ActiveClicks(cut));
    // The state stays with its tab when tabs are added before it and moved
    cut.Render(p => p.Add(c => c.ChildContent, ClickerTabs("new", "b", "a")));
    Assert.AreEqual("2", ActiveClicks(cut));
  }

  [TestMethod]
  public void WithoutPersistTabContent_ATabsComponentsStartAfreshEachTimeItIsSelected() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataTabStrip> cut = Render(context, ClickerTabs("a", "b"), "a");
    cut.Find("[role=tabpanel]:not([hidden]) .clicker").Click();
    Assert.AreEqual("1", ActiveClicks(cut));
    cut.FindAll("[role=tab]")[1].Click();
    cut.FindAll("[role=tab]")[0].Click();
    Assert.AreEqual("0", ActiveClicks(cut));
  }

  [TestMethod]
  public void ChangingTabPosition_ReattachesTheScrollScriptForTheNewDirection() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = context.JSInterop.SetupModule(ModulePath);
    module.Setup<int>("attach", _ => true).SetResult(5);
    module.SetupVoid("detach", _ => true).SetVoidResult();
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(Specs("a")), more: p => p.Add(c => c.Scrollable, true));
    Assert.AreEqual(false, module.Invocations.Single(i => i.Identifier == "attach").Arguments[2]);
    cut.Render(p => p.Add(c => c.TabPosition, PixataTabPosition.Left));
    Assert.IsTrue(module.Invocations.Any(i => i.Identifier == "detach" && (int)i.Arguments[0]! == 5));
    Assert.AreEqual(true, module.Invocations.Last(i => i.Identifier == "attach").Arguments[2]);
  }

  [TestMethod]
  public void Refresh_RedrawsTheTabs() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(Specs("a")));
    cut.InvokeAsync(cut.Instance.Refresh);
    Assert.AreEqual("Content a", VisiblePanelText(cut));
  }

  [TestMethod]
  public void AdditionalAttributes_AreRenderedOnTheOuterElement() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataTabStrip> cut = Render(context, Tabs(Specs("a")), more: p => p.AddUnmatched("data-test", "strip").Add(c => c.Id, "my-tabs"));
    IElement root = cut.Find(".pixata-blazor-tabstrip");
    Assert.AreEqual("strip", root.GetAttribute("data-test"));
    Assert.AreEqual("my-tabs", root.Id);
  }

  [TestMethod]
  public async Task Dispose_DetachesTheScrollScript() {
    BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = context.JSInterop.SetupModule(ModulePath);
    module.Setup<int>("attach", _ => true).SetResult(3);
    module.SetupVoid("detach", _ => true).SetVoidResult();
    Render(context, Tabs(Specs("a")), more: p => p.Add(c => c.Scrollable, true));
    await context.DisposeAsync();
    Assert.IsTrue(module.Invocations.Any(i => i.Identifier == "detach" && (int)i.Arguments[0]! == 3));
  }
}
