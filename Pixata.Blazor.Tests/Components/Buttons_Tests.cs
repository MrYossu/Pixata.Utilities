using System;
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
public class Buttons_Tests {
  private static readonly string ModulePath = PixataJs.ModulePath;

  // Items for a drop-down button: "One", "Two" (disabled when twoEnabled is false) and "Three", which add their text to clicked
  private static RenderFragment DropDownItems(List<string> clicked, bool twoEnabled = true) => builder => {
    AddItem<PixataDropDownButtonItem>(builder, 0, "One", clicked, true);
    AddItem<PixataDropDownButtonItem>(builder, 10, "Two", clicked, twoEnabled);
    AddItem<PixataDropDownButtonItem>(builder, 20, "Three", clicked, true);
  };

  private static RenderFragment SplitItems(List<string> clicked) => builder => {
    AddItem<PixataSplitButtonItem>(builder, 0, "Save as", clicked, true);
    AddItem<PixataSplitButtonItem>(builder, 10, "Save a copy", clicked, true);
  };

  private static void AddItem<TItem>(RenderTreeBuilder builder, int sequence, string text, List<string> clicked, bool enabled) where TItem : PixataMenuButtonItemBase {
    builder.OpenComponent<TItem>(sequence);
    builder.AddComponentParameter(sequence + 1, nameof(PixataMenuButtonItemBase.Enabled), enabled);
    builder.AddComponentParameter(sequence + 2, nameof(PixataMenuButtonItemBase.OnClick), EventCallback.Factory.Create<MouseEventArgs>(clicked, () => clicked.Add(text)));
    builder.AddComponentParameter(sequence + 3, nameof(PixataMenuButtonItemBase.ChildContent), (RenderFragment)(b => b.AddContent(0, text)));
    builder.CloseComponent();
  }

  private static IRenderedComponent<PixataDropDownButton> RenderDropDown(BunitContext context, List<string> clicked, Action<ComponentParameterCollectionBuilder<PixataDropDownButton>>? parameters = null, bool twoEnabled = true) =>
    context.Render<PixataDropDownButton>(p => {
      p.Add(c => c.DropDownButtonContent, "Actions");
      p.Add(c => c.DropDownButtonItems, DropDownItems(clicked, twoEnabled));
      parameters?.Invoke(p);
    });

  private static IElement MainButton(IRenderedComponent<PixataDropDownButton> cut) =>
    cut.Find("button.pixata-blazor-dropdownbutton-button");

  private static List<string> ItemTexts<TComponent>(IRenderedComponent<TComponent> cut) where TComponent : IComponent =>
    cut.FindAll("li[role=menuitem]").Select(li => li.TextContent.Trim()).ToList();

  [TestMethod]
  public void DropDownButton_RendersAClosedMenuButton() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownButton> cut = RenderDropDown(context, []);
    IElement button = MainButton(cut);
    Assert.AreEqual("Actions", button.TextContent.Trim());
    Assert.AreEqual("button", button.GetAttribute("type"));
    Assert.AreEqual("menu", button.GetAttribute("aria-haspopup"));
    Assert.AreEqual("false", button.GetAttribute("aria-expanded"));
    Assert.IsFalse(button.HasAttribute("aria-controls"));
    Assert.AreEqual(0, cut.FindAll("[role=menu]").Count);
  }

  [TestMethod]
  public void DropDownButton_Click_OpensTheMenuAndRaisesOnClick() {
    using BunitContext context = PixataTestContext.Create();
    bool clicked = false;
    IRenderedComponent<PixataDropDownButton> cut = RenderDropDown(context, [], p => p.Add(c => c.OnClick, () => clicked = true));
    MainButton(cut).Click();
    Assert.IsTrue(clicked);
    IElement menu = cut.Find("ul[role=menu]");
    Assert.AreEqual("true", MainButton(cut).GetAttribute("aria-expanded"));
    Assert.AreEqual(menu.Id, MainButton(cut).GetAttribute("aria-controls"));
    Assert.AreEqual(MainButton(cut).Id, menu.GetAttribute("aria-labelledby"));
    CollectionAssert.AreEqual(new[] { "One", "Two", "Three" }, ItemTexts(cut));
    Assert.IsTrue(cut.FindAll("li[role=menuitem]").All(li => li.GetAttribute("tabindex") == "-1"));
    MainButton(cut).Click();
    Assert.AreEqual(0, cut.FindAll("[role=menu]").Count);
  }

  [TestMethod]
  public void DropDownButton_ClickingAnItem_RaisesItsOnClickAndCloses() {
    using BunitContext context = PixataTestContext.Create();
    List<string> clicked = [];
    IRenderedComponent<PixataDropDownButton> cut = RenderDropDown(context, clicked);
    MainButton(cut).Click();
    cut.FindAll("li[role=menuitem]")[2].Click();
    CollectionAssert.AreEqual(new[] { "Three" }, clicked);
    Assert.AreEqual(0, cut.FindAll("[role=menu]").Count);
    Assert.AreEqual("false", MainButton(cut).GetAttribute("aria-expanded"));
  }

  [TestMethod]
  public void DropDownButton_DisabledItem_CannotBeChosen() {
    using BunitContext context = PixataTestContext.Create();
    List<string> clicked = [];
    IRenderedComponent<PixataDropDownButton> cut = RenderDropDown(context, clicked, twoEnabled: false);
    MainButton(cut).Click();
    IElement two = cut.FindAll("li[role=menuitem]")[1];
    Assert.AreEqual("true", two.GetAttribute("aria-disabled"));
    Assert.IsTrue(two.ClassList.Contains("pixata-blazor-disabled"));
    two.Click();
    Assert.AreEqual(0, clicked.Count);
    Assert.AreEqual(1, cut.FindAll("[role=menu]").Count);
  }

  [TestMethod]
  public void DropDownButton_AltDown_OpensTheMenuAndFocusesTheFirstItem() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = context.JSInterop.SetupModule(ModulePath);
    module.Mode = JSRuntimeMode.Loose;
    IRenderedComponent<PixataDropDownButton> cut = RenderDropDown(context, []);
    MainButton(cut).KeyDown(new KeyboardEventArgs { Key = "ArrowDown", AltKey = true });
    Assert.AreEqual(1, cut.FindAll("[role=menu]").Count);
    string firstId = cut.FindAll("li[role=menuitem]")[0].Id!;
    Assert.IsTrue(module.Invocations["focusSelector"].Any(i => (string?)i.Arguments[1] == $"#{firstId}"));
    Assert.IsTrue(module.Invocations["preventDefaultKeys"].Any());
  }

  [TestMethod]
  public void DropDownButton_ArrowKeysSkipDisabledItemsAndEnterChoosesTheFocusedOne() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = context.JSInterop.SetupModule(ModulePath);
    module.Mode = JSRuntimeMode.Loose;
    List<string> clicked = [];
    IRenderedComponent<PixataDropDownButton> cut = RenderDropDown(context, clicked, twoEnabled: false);
    MainButton(cut).KeyDown(new KeyboardEventArgs { Key = "ArrowDown", AltKey = true });
    // The first item is focused; ArrowDown skips the disabled second one
    cut.Find("ul[role=menu]").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
    string thirdId = cut.FindAll("li[role=menuitem]")[2].Id!;
    Assert.AreEqual($"#{thirdId}", module.Invocations["focusSelector"].Last().Arguments[1]);
    cut.Find("ul[role=menu]").KeyDown(new KeyboardEventArgs { Key = "Enter" });
    CollectionAssert.AreEqual(new[] { "Three" }, clicked);
    Assert.AreEqual(0, cut.FindAll("[role=menu]").Count);
    // The focus goes back to the button
    Assert.IsTrue(module.Invocations["focusElement"].Any());
  }

  [TestMethod]
  public void DropDownButton_ArrowKeysWrapRoundAndHomeEndJump() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = context.JSInterop.SetupModule(ModulePath);
    module.Mode = JSRuntimeMode.Loose;
    List<string> clicked = [];
    IRenderedComponent<PixataDropDownButton> cut = RenderDropDown(context, clicked);
    MainButton(cut).KeyDown(new KeyboardEventArgs { Key = "ArrowDown", AltKey = true });
    IElement menu = cut.Find("ul[role=menu]");
    List<string> ids = cut.FindAll("li[role=menuitem]").Select(li => li.Id!).ToList();
    menu.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
    Assert.AreEqual($"#{ids[2]}", module.Invocations["focusSelector"].Last().Arguments[1]);
    cut.Find("ul[role=menu]").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
    Assert.AreEqual($"#{ids[0]}", module.Invocations["focusSelector"].Last().Arguments[1]);
    cut.Find("ul[role=menu]").KeyDown(new KeyboardEventArgs { Key = "End" });
    Assert.AreEqual($"#{ids[2]}", module.Invocations["focusSelector"].Last().Arguments[1]);
    cut.Find("ul[role=menu]").KeyDown(new KeyboardEventArgs { Key = "Home" });
    Assert.AreEqual($"#{ids[0]}", module.Invocations["focusSelector"].Last().Arguments[1]);
    cut.Find("ul[role=menu]").KeyDown(new KeyboardEventArgs { Key = " " });
    CollectionAssert.AreEqual(new[] { "One" }, clicked);
  }

  [TestMethod]
  public void DropDownButton_FocusingAnItem_MakesItTheOneEnterChooses() {
    using BunitContext context = PixataTestContext.Create();
    List<string> clicked = [];
    IRenderedComponent<PixataDropDownButton> cut = RenderDropDown(context, clicked);
    MainButton(cut).Click();
    cut.FindAll("li[role=menuitem]")[1].Focus();
    cut.Find("ul[role=menu]").KeyDown(new KeyboardEventArgs { Key = "Enter" });
    CollectionAssert.AreEqual(new[] { "Two" }, clicked);
  }

  [TestMethod]
  [DataRow("Escape", false)]
  [DataRow("ArrowUp", true)]
  [DataRow("Tab", false)]
  public void DropDownButton_CloseKeys_CloseTheMenu(string key, bool alt) {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownButton> cut = RenderDropDown(context, []);
    MainButton(cut).Click();
    cut.Find("ul[role=menu]").KeyDown(new KeyboardEventArgs { Key = key, AltKey = alt });
    Assert.AreEqual(0, cut.FindAll("[role=menu]").Count);
  }

  [TestMethod]
  public void DropDownButton_StopsKeysPropagatingOnlyWhileOpen() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownButton> cut = RenderDropDown(context, []);
    Assert.IsFalse(cut.Markup.Contains("blazor:onkeydown:stoppropagation", StringComparison.OrdinalIgnoreCase));
    MainButton(cut).Click();
    Assert.IsTrue(cut.Markup.Contains("blazor:onkeydown:stoppropagation", StringComparison.OrdinalIgnoreCase));
  }

  [TestMethod]
  public async Task DropDownButton_OutsideClick_ClosesTheMenu() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownButton> cut = RenderDropDown(context, []);
    MainButton(cut).Click();
    IRenderedComponent<PixataPopup> popup = cut.FindComponent<PixataPopup>();
    await cut.InvokeAsync(popup.Instance.HandleOutsideClick);
    Assert.AreEqual(0, cut.FindAll("[role=menu]").Count);
  }

  [TestMethod]
  public void DropDownButton_OnOpenCancelled_KeepsItClosed() {
    using BunitContext context = PixataTestContext.Create();
    int opens = 0;
    IRenderedComponent<PixataDropDownButton> cut = RenderDropDown(context, [], p => p.Add(c => c.OnOpen, (PixataMenuButtonOpenEventArgs args) => {
      opens++;
      args.IsCancelled = true;
    }));
    MainButton(cut).Click();
    Assert.AreEqual(1, opens);
    Assert.AreEqual(0, cut.FindAll("[role=menu]").Count);
  }

  [TestMethod]
  public void DropDownButton_OnCloseCancelled_KeepsItOpen() {
    using BunitContext context = PixataTestContext.Create();
    int closes = 0;
    IRenderedComponent<PixataDropDownButton> cut = RenderDropDown(context, [], p => p.Add(c => c.OnClose, (PixataMenuButtonCloseEventArgs args) => {
      closes++;
      args.IsCancelled = true;
    }));
    MainButton(cut).Click();
    cut.Find("ul[role=menu]").KeyDown(new KeyboardEventArgs { Key = "Escape" });
    Assert.AreEqual(1, closes);
    Assert.AreEqual(1, cut.FindAll("[role=menu]").Count);
  }

  [TestMethod]
  public void DropDownButton_PopupSettings_SizeTheMenu() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownButton> cut = RenderDropDown(context, [], p => p.Add(c => c.PopupSettings, new PixataPopupSettings { Width = "220px", MaxHeight = "120px" }));
    MainButton(cut).Click();
    string style = cut.Find(".pixata-blazor-popup").GetAttribute("style")!;
    StringAssert.Contains(style, "width:220px");
    StringAssert.Contains(style, "--pixata-blazor-popup-max-height:120px");
    Assert.IsTrue(cut.Find(".pixata-blazor-popup").ClassList.Contains("pixata-blazor-button-menu-popup"));
  }

  [TestMethod]
  public void DropDownButton_SettingsTag_SizesTheMenu() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownButton> cut = RenderDropDown(context, [], p => p.Add(c => c.DropDownButtonSettings, (RenderFragment)(builder => {
      builder.OpenComponent<PixataDropDownButtonPopupSettings>(0);
      builder.AddComponentParameter(1, nameof(PixataDropDownButtonPopupSettings.Height), "150px");
      builder.AddComponentParameter(2, nameof(PixataDropDownButtonPopupSettings.Class), "mine");
      builder.CloseComponent();
    })));
    MainButton(cut).Click();
    IElement popup = cut.Find(".pixata-blazor-popup");
    StringAssert.Contains(popup.GetAttribute("style"), "--pixata-blazor-popup-height:150px");
    Assert.IsTrue(popup.ClassList.Contains("mine"));
  }

  [TestMethod]
  public void DropDownButton_Appearance_UsesTheButtonClasses() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownButton> cut = RenderDropDown(context, [], p => p
      .Add(c => c.ThemeColor, PixataThemeConstants.Button.ThemeColor.Primary)
      .Add(c => c.FillMode, PixataThemeConstants.Button.FillMode.Outline)
      .Add(c => c.Rounded, PixataThemeConstants.Button.Rounded.Full)
      .Add(c => c.Size, PixataThemeConstants.Button.Size.Large)
      .Add(c => c.Class, "mine")
      .Add(c => c.Id, "actions")
      .Add(c => c.Title, "Things you can do")
      .Add(c => c.ShowArrowButton, true));
    IElement button = MainButton(cut);
    foreach (string css in new[] { "pixata-blazor-button", "pixata-blazor-button-primary", "pixata-blazor-button-outline", "pixata-blazor-rounded-full", "pixata-blazor-button-lg" }) {
      Assert.IsTrue(button.ClassList.Contains(css), css);
    }
    Assert.AreEqual("actions", button.Id);
    Assert.AreEqual("Things you can do", button.GetAttribute("title"));
    Assert.IsTrue(cut.Find(".pixata-blazor-dropdownbutton").ClassList.Contains("mine"));
    Assert.IsNotNull(cut.Find(".pixata-blazor-dropdownbutton-arrow .pixata-blazor-svg-icon-chevron-down"));
    button.Click();
    Assert.IsTrue(cut.Find("ul[role=menu]").ClassList.Contains("pixata-blazor-button-menu-lg"));
  }

  [TestMethod]
  public void DropDownButton_IconOnly_IsSquare() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownButton> cut = context.Render<PixataDropDownButton>(p => p
      .Add(c => c.Icon, PixataSvgIcons.MoreVertical)
      .Add(c => c.AriaLabel, "More")
      .Add(c => c.DropDownButtonItems, DropDownItems([])));
    IElement button = MainButton(cut);
    Assert.IsTrue(button.ClassList.Contains("pixata-blazor-icon-button"));
    Assert.AreEqual("More", button.GetAttribute("aria-label"));
    Assert.IsNotNull(button.QuerySelector(".pixata-blazor-svg-icon-more-vertical"));
  }

  [TestMethod]
  public void DropDownButton_ItemIconsAndClasses_AreRendered() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownButton> cut = context.Render<PixataDropDownButton>(p => p
      .Add(c => c.DropDownButtonContent, "Edit")
      .Add(c => c.DropDownButtonItems, (RenderFragment)(builder => {
        builder.OpenComponent<PixataDropDownButtonItem>(0);
        builder.AddComponentParameter(1, nameof(PixataDropDownButtonItem.Icon), PixataSvgIcons.Trash);
        builder.AddComponentParameter(2, nameof(PixataDropDownButtonItem.Class), "danger");
        builder.AddComponentParameter(3, "title", "Delete it");
        builder.AddComponentParameter(4, nameof(PixataDropDownButtonItem.ChildContent), (RenderFragment)(b => b.AddContent(0, "Delete")));
        builder.CloseComponent();
      })));
    MainButton(cut).Click();
    IElement item = cut.Find("li[role=menuitem]");
    Assert.IsTrue(item.ClassList.Contains("danger"));
    Assert.AreEqual("Delete it", item.GetAttribute("title"));
    Assert.IsNotNull(item.QuerySelector(".pixata-blazor-svg-icon-trash"));
    Assert.AreEqual("Delete", item.QuerySelector(".pixata-blazor-button-menu-item-text")!.TextContent);
  }

  [TestMethod]
  public async Task DropDownButton_Disabled_CannotBeOpened() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownButton> cut = RenderDropDown(context, [], p => p.Add(c => c.Enabled, false));
    Assert.IsTrue(MainButton(cut).HasAttribute("disabled"));
    await cut.InvokeAsync(cut.Instance.OpenAsync);
    MainButton(cut).KeyDown(new KeyboardEventArgs { Key = "ArrowDown", AltKey = true });
    Assert.AreEqual(0, cut.FindAll("[role=menu]").Count);
  }

  [TestMethod]
  public async Task DropDownButton_OpenAsyncAndCloseAsync_OpenAndCloseTheMenu() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownButton> cut = RenderDropDown(context, []);
    await cut.InvokeAsync(cut.Instance.OpenAsync);
    Assert.AreEqual(1, cut.FindAll("[role=menu]").Count);
    await cut.InvokeAsync(cut.Instance.CloseAsync);
    Assert.AreEqual(0, cut.FindAll("[role=menu]").Count);
  }

  [TestMethod]
  public void Item_OutsideAButton_Throws() {
    using BunitContext context = PixataTestContext.Create();
    Assert.ThrowsException<InvalidOperationException>(() => context.Render<PixataDropDownButtonItem>());
  }

  [TestMethod]
  public void SplitButton_MainButton_RaisesOnClickWithoutOpeningTheMenu() {
    using BunitContext context = PixataTestContext.Create();
    int clicks = 0;
    IRenderedComponent<PixataSplitButton> cut = context.Render<PixataSplitButton>(p => p
      .Add(c => c.SplitButtonContent, "Save")
      .Add(c => c.SplitButtonItems, SplitItems([]))
      .Add(c => c.OnClick, () => clicks++));
    cut.Find(".pixata-blazor-splitbutton-main").Click();
    Assert.AreEqual(1, clicks);
    Assert.AreEqual(0, cut.FindAll("[role=menu]").Count);
    Assert.AreEqual("Save", cut.Find(".pixata-blazor-splitbutton-main").TextContent.Trim());
  }

  [TestMethod]
  public void SplitButton_Arrow_OpensTheMenuAndTheMainButtonClosesIt() {
    using BunitContext context = PixataTestContext.Create();
    List<string> clicked = [];
    int mainClicks = 0;
    IRenderedComponent<PixataSplitButton> cut = context.Render<PixataSplitButton>(p => p
      .Add(c => c.SplitButtonContent, "Save")
      .Add(c => c.SplitButtonItems, SplitItems(clicked))
      .Add(c => c.OnClick, () => mainClicks++));
    IElement arrow = cut.Find(".pixata-blazor-splitbutton-arrow");
    Assert.AreEqual("menu", arrow.GetAttribute("aria-haspopup"));
    Assert.AreEqual("More options", arrow.GetAttribute("aria-label"));
    Assert.AreEqual("false", arrow.GetAttribute("aria-expanded"));
    arrow.Click();
    Assert.AreEqual("true", cut.Find(".pixata-blazor-splitbutton-arrow").GetAttribute("aria-expanded"));
    CollectionAssert.AreEqual(new[] { "Save as", "Save a copy" }, ItemTexts(cut));
    cut.Find(".pixata-blazor-splitbutton-main").Click();
    Assert.AreEqual(1, mainClicks);
    Assert.AreEqual(0, cut.FindAll("[role=menu]").Count);
  }

  [TestMethod]
  public void SplitButton_ChoosingAnItem_RaisesItsOnClickNotTheMainOne() {
    using BunitContext context = PixataTestContext.Create();
    List<string> clicked = [];
    int mainClicks = 0;
    IRenderedComponent<PixataSplitButton> cut = context.Render<PixataSplitButton>(p => p
      .Add(c => c.SplitButtonContent, "Save")
      .Add(c => c.SplitButtonItems, SplitItems(clicked))
      .Add(c => c.OnClick, () => mainClicks++));
    cut.Find(".pixata-blazor-splitbutton-arrow").Click();
    cut.FindAll("li[role=menuitem]")[1].Click();
    CollectionAssert.AreEqual(new[] { "Save a copy" }, clicked);
    Assert.AreEqual(0, mainClicks);
    Assert.AreEqual(0, cut.FindAll("[role=menu]").Count);
  }

  [TestMethod]
  public void SplitButton_AltDownOnEitherButton_OpensTheMenu() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataSplitButton> cut = context.Render<PixataSplitButton>(p => p
      .Add(c => c.SplitButtonContent, "Save")
      .Add(c => c.SplitButtonItems, SplitItems([])));
    cut.Find(".pixata-blazor-splitbutton-main").KeyDown(new KeyboardEventArgs { Key = "ArrowDown", AltKey = true });
    Assert.AreEqual(1, cut.FindAll("[role=menu]").Count);
    cut.Find("ul[role=menu]").KeyDown(new KeyboardEventArgs { Key = "Escape" });
    Assert.AreEqual(0, cut.FindAll("[role=menu]").Count);
    cut.Find(".pixata-blazor-splitbutton-arrow").KeyDown(new KeyboardEventArgs { Key = "ArrowDown", AltKey = true });
    Assert.AreEqual(1, cut.FindAll("[role=menu]").Count);
  }

  [TestMethod]
  public void SplitButton_Appearance_AppliesToBothParts() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataSplitButton> cut = context.Render<PixataSplitButton>(p => p
      .Add(c => c.SplitButtonContent, "Save")
      .Add(c => c.SplitButtonItems, SplitItems([]))
      .Add(c => c.ThemeColor, PixataThemeConstants.Button.ThemeColor.Primary)
      .Add(c => c.Size, PixataThemeConstants.Button.Size.Small)
      .Add(c => c.Enabled, false)
      .Add(c => c.ArrowButtonAriaLabel, "Other ways to save")
      .Add(c => c.Class, "mine"));
    Assert.IsTrue(cut.Find(".pixata-blazor-splitbutton").ClassList.Contains("mine"));
    foreach (IElement button in cut.FindAll("button")) {
      Assert.IsTrue(button.ClassList.Contains("pixata-blazor-button-primary"));
      Assert.IsTrue(button.ClassList.Contains("pixata-blazor-button-sm"));
      Assert.IsTrue(button.HasAttribute("disabled"));
    }
    Assert.IsTrue(cut.Find(".pixata-blazor-splitbutton-arrow").ClassList.Contains("pixata-blazor-icon-button"));
    Assert.AreEqual("Other ways to save", cut.Find(".pixata-blazor-splitbutton-arrow").GetAttribute("aria-label"));
  }

  [TestMethod]
  public void SplitButton_SettingsTag_SizesTheMenu() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataSplitButton> cut = context.Render<PixataSplitButton>(p => p
      .Add(c => c.SplitButtonContent, "Save")
      .Add(c => c.SplitButtonItems, SplitItems([]))
      .Add(c => c.SplitButtonSettings, (RenderFragment)(builder => {
        builder.OpenComponent<PixataSplitButtonPopupSettings>(0);
        builder.AddComponentParameter(1, nameof(PixataSplitButtonPopupSettings.Width), "300px");
        builder.CloseComponent();
      })));
    cut.Find(".pixata-blazor-splitbutton-arrow").Click();
    StringAssert.Contains(cut.Find(".pixata-blazor-popup").GetAttribute("style"), "width:300px");
  }

  #region PixataButton

  [TestMethod]
  public void Button_RendersASubmitButtonWithTheDefaultClasses() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataButton> cut = context.Render<PixataButton>(p => p.AddChildContent("Save"));
    IElement button = cut.Find("button");
    Assert.AreEqual("submit", button.GetAttribute("type"));
    Assert.AreEqual("Save", button.TextContent.Trim());
    Assert.AreEqual("pixata-blazor-button pixata-blazor-button-md pixata-blazor-rounded-md pixata-blazor-button-solid pixata-blazor-button-base", button.ClassName);
    Assert.IsFalse(button.HasAttribute("disabled"));
  }

  [TestMethod]
  public void Button_Click_RaisesOnClick() {
    using BunitContext context = PixataTestContext.Create();
    bool clicked = false;
    IRenderedComponent<PixataButton> cut = context.Render<PixataButton>(p => p
      .Add(c => c.OnClick, () => clicked = true)
      .AddChildContent("Save"));
    cut.Find("button").Click();
    Assert.IsTrue(clicked);
  }

  [TestMethod]
  public void Button_IconOnly_IsSquare() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataButton> cut = context.Render<PixataButton>(p => p
      .Add(c => c.Icon, PixataSvgIcons.Refresh)
      .Add(c => c.AriaLabel, "Refresh"));
    IElement button = cut.Find("button");
    Assert.IsTrue(button.ClassList.Contains("pixata-blazor-icon-button"));
    Assert.AreEqual("Refresh", button.GetAttribute("aria-label"));
    Assert.AreEqual(1, cut.FindAll("svg").Count);
  }

  [TestMethod]
  public void Button_Busy_ShowsASpinnerInPlaceOfTheIconAndIsDisabled() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataButton> cut = context.Render<PixataButton>(p => p
      .Add(c => c.Icon, PixataSvgIcons.Refresh)
      .Add(c => c.IsBusy, true)
      .AddChildContent("Save"));
    IElement button = cut.Find("button");
    Assert.IsTrue(button.HasAttribute("disabled"));
    Assert.AreEqual("true", button.GetAttribute("aria-busy"));
    Assert.AreEqual(1, cut.FindAll(".pixata-blazor-button-spinner").Count);
    Assert.AreEqual(0, cut.FindAll("svg").Count);
    cut.Render(p => p.Add(c => c.IsBusy, false));
    Assert.IsFalse(cut.Find("button").HasAttribute("disabled"));
    Assert.IsFalse(cut.Find("button").HasAttribute("aria-busy"));
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-button-spinner").Count);
  }

  [TestMethod]
  public void Button_NotEnabled_IsDisabled() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataButton> cut = context.Render<PixataButton>(p => p.Add(c => c.Enabled, false).AddChildContent("Save"));
    Assert.IsTrue(cut.Find("button").HasAttribute("disabled"));
  }

  [TestMethod]
  public void Button_NotVisible_RendersNothing() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataButton> cut = context.Render<PixataButton>(p => p.Add(c => c.Visible, false).AddChildContent("Save"));
    Assert.AreEqual(0, cut.FindAll("button").Count);
  }

  [TestMethod]
  public void Button_ParametersAndExtraAttributes_AreRendered() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataButton> cut = context.Render<PixataButton>(p => p
      .Add(c => c.ButtonType, PixataButtonType.Reset)
      .Add(c => c.ThemeColor, PixataThemeConstants.Button.ThemeColor.Error)
      .Add(c => c.FillMode, PixataThemeConstants.Button.FillMode.Outline)
      .Add(c => c.Size, PixataThemeConstants.Button.Size.Large)
      .Add(c => c.Rounded, PixataThemeConstants.Button.Rounded.Full)
      .Add(c => c.Class, "mine")
      .Add(c => c.Title, "Clear the form")
      .AddUnmatched("data-test", "clear")
      .AddChildContent("Clear"));
    IElement button = cut.Find("button");
    Assert.AreEqual("reset", button.GetAttribute("type"));
    Assert.AreEqual("pixata-blazor-button pixata-blazor-button-lg pixata-blazor-rounded-full pixata-blazor-button-outline pixata-blazor-button-error mine", button.ClassName);
    Assert.AreEqual("Clear the form", button.GetAttribute("title"));
    Assert.AreEqual("clear", button.GetAttribute("data-test"));
  }

  [TestMethod]
  public void PrimaryButton_IsAPrimarySubmitButton() {
    using BunitContext context = PixataTestContext.Create();
    IElement button = context.Render<PixataPrimaryButton>(p => p.AddChildContent("Save")).Find("button");
    Assert.AreEqual("submit", button.GetAttribute("type"));
    Assert.IsTrue(button.ClassList.Contains("pixata-blazor-button-primary"));
  }

  [TestMethod]
  public void SecondaryButton_IsABaseButtonThatDoesNotSubmit() {
    using BunitContext context = PixataTestContext.Create();
    IElement button = context.Render<PixataSecondaryButton>(p => p.AddChildContent("Cancel")).Find("button");
    Assert.AreEqual("button", button.GetAttribute("type"));
    Assert.IsTrue(button.ClassList.Contains("pixata-blazor-button-base"));
  }

  [TestMethod]
  public void PrimaryButton_DefaultsCanBeOverridden() {
    using BunitContext context = PixataTestContext.Create();
    IElement button = context.Render<PixataPrimaryButton>(p => p
      .Add(c => c.ThemeColor, PixataThemeConstants.Button.ThemeColor.Success)
      .Add(c => c.ButtonType, PixataButtonType.Button)
      .AddChildContent("Go")).Find("button");
    Assert.AreEqual("button", button.GetAttribute("type"));
    Assert.IsTrue(button.ClassList.Contains("pixata-blazor-button-success"));
    Assert.IsFalse(button.ClassList.Contains("pixata-blazor-button-primary"));
  }

  #endregion

  #region The shared popup

  [TestMethod]
  public void Popup_ClosedBeforeTheScriptAttachedIt_IsDetachedAndAttachedAgainWhenReopened() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = context.JSInterop.SetupModule(ModulePath);
    JSRuntimeInvocationHandler<int> attach = module.Setup<int>("attachPopup", _ => true);
    IRenderedComponent<PixataPopup> cut = context.Render<PixataPopup>(p => p.Add(c => c.Visible, true));
    // Closed (eg by a double click) while the script is still attaching it
    cut.Render(p => p.Add(c => c.Visible, false));
    attach.SetResult(7);
    cut.WaitForAssertion(() => Assert.IsTrue(module.Invocations["detachPopup"].Any(i => Equals(i.Arguments[0], 7)), "The stale attachment is removed"));
    // Opened again, so the new popup element must be attached, rather than the old handle repositioned
    attach.SetResult(8);
    cut.Render(p => p.Add(c => c.Visible, true));
    cut.WaitForAssertion(() => Assert.AreEqual(2, module.Invocations["attachPopup"].Count));
    cut.Render(p => p.Add(c => c.Visible, false));
    cut.WaitForAssertion(() => Assert.IsTrue(module.Invocations["detachPopup"].Any(i => Equals(i.Arguments[0], 8))));
  }

  [TestMethod]
  public void Popup_RenderedAgainWhileAttaching_IsOnlyAttachedOnce() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = context.JSInterop.SetupModule(ModulePath);
    JSRuntimeInvocationHandler<int> attach = module.Setup<int>("attachPopup", _ => true);
    IRenderedComponent<PixataPopup> cut = context.Render<PixataPopup>(p => p.Add(c => c.Visible, true));
    cut.Render(p => p.Add(c => c.Class, "again"));
    cut.Render(p => p.Add(c => c.Class, "and again"));
    attach.SetResult(7);
    cut.WaitForAssertion(() => Assert.AreEqual(1, module.Invocations["attachPopup"].Count));
    Assert.IsFalse(module.Invocations["detachPopup"].Any());
  }

  #endregion
}
