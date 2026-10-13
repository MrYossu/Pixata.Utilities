using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests.Components;

[TestClass]
public class Dialogs_Tests {
  private const string ModulePath = "./_content/Pixata.Blazor/pixata-dialogs.js";

  private static readonly KeyboardEventArgs Escape = new() { Key = "Escape" };

  private static IRenderedComponent<PixataDialog> RenderDialog(BunitContext context, Action<ComponentParameterCollectionBuilder<PixataDialog>>? parameters = null) =>
    context.Render<PixataDialog>(p => {
      p.Add(c => c.Visible, true);
      p.Add(c => c.Title, "Delete customer");
      p.Add(c => c.DialogContent, "<p>Are you sure?</p>");
      parameters?.Invoke(p);
    });

  #region PixataDialog

  [TestMethod]
  public void PixataDialog_NotVisible_RendersOnlyThePlaceholder() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDialog> cut = context.Render<PixataDialog>(p => p.Add(c => c.Title, "Delete customer"));
    Assert.AreEqual(1, cut.FindAll(".pixata-blazor-layer-anchor").Count);
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-dialog").Count);
  }

  [TestMethod]
  public void PixataDialog_Visible_RendersAnAccessibleModalDialog() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDialog> cut = RenderDialog(context, p => p.Add(c => c.Id, "delete"));
    IElement dialog = cut.Find(".pixata-blazor-dialog-wrapper > .pixata-blazor-dialog");
    Assert.AreEqual("delete", dialog.Id);
    Assert.AreEqual("dialog", dialog.GetAttribute("role"));
    Assert.AreEqual("true", dialog.GetAttribute("aria-modal"));
    Assert.AreEqual("-1", dialog.GetAttribute("tabindex"));
    Assert.AreEqual("delete-title", dialog.GetAttribute("aria-labelledby"));
    Assert.AreEqual("Delete customer", cut.Find("#delete-title").TextContent.Trim());
    Assert.AreEqual("delete-content", dialog.GetAttribute("aria-describedby"));
    Assert.AreEqual("Are you sure?", cut.Find("#delete-content").TextContent.Trim());
    Assert.AreEqual(1, cut.FindAll(".pixata-blazor-dialog-wrapper > .pixata-blazor-overlay").Count);
  }

  [TestMethod]
  public void PixataDialog_DialogTitle_TakesPrecedenceOverTitle() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDialog> cut = RenderDialog(context, p => p.Add(c => c.DialogTitle, "<em>Edit</em> contact"));
    Assert.AreEqual("Edit contact", cut.Find(".pixata-blazor-dialog-title").TextContent.Trim());
    Assert.IsNotNull(cut.Find(".pixata-blazor-dialog-title em"));
  }

  [TestMethod]
  public void PixataDialog_NoTitleAndNoCloseButton_HasNoTitlebarOrLabel() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDialog> cut = context.Render<PixataDialog>(p => {
      p.Add(c => c.Visible, true);
      p.Add(c => c.ShowCloseButton, false);
    });
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-dialog-titlebar").Count);
    Assert.IsFalse(cut.Find(".pixata-blazor-dialog").HasAttribute("aria-labelledby"));
  }

  [TestMethod]
  public void PixataDialog_CloseButton_HasALabelAndRaisesVisibleChanged() {
    using BunitContext context = PixataTestContext.Create();
    List<bool> changes = [];
    IRenderedComponent<PixataDialog> cut = RenderDialog(context, p => p.Add(c => c.VisibleChanged, (bool v) => changes.Add(v)));
    IElement close = cut.Find(".pixata-blazor-dialog-titlebar-action");
    Assert.AreEqual("Close", close.GetAttribute("title"));
    Assert.AreEqual("Close", close.GetAttribute("aria-label"));
    close.Click();
    CollectionAssert.AreEqual(new[] { false }, changes);
  }

  [TestMethod]
  public void PixataDialog_VisibleChangedHandledButNotObeyed_StaysOpen() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDialog> cut = RenderDialog(context, p => p.Add(c => c.VisibleChanged, (bool _) => { }));
    cut.Find(".pixata-blazor-dialog-titlebar-action").Click();
    Assert.AreEqual(1, cut.FindAll(".pixata-blazor-dialog").Count);
  }

  [TestMethod]
  public void PixataDialog_VisibleChangedNotHandled_HidesItself() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDialog> cut = RenderDialog(context);
    cut.Find(".pixata-blazor-dialog-titlebar-action").Click();
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-dialog").Count);
    Assert.IsFalse(cut.Instance.Visible);
  }

  [TestMethod]
  public void PixataDialog_Escape_ClosesWhenThereIsACloseButton() {
    using BunitContext context = PixataTestContext.Create();
    List<bool> changes = [];
    IRenderedComponent<PixataDialog> cut = RenderDialog(context, p => p.Add(c => c.VisibleChanged, (bool v) => changes.Add(v)));
    cut.Find(".pixata-blazor-dialog").KeyDown(Escape);
    CollectionAssert.AreEqual(new[] { false }, changes);
  }

  [TestMethod]
  public void PixataDialog_Escape_DoesNotCloseWithoutACloseButton() {
    using BunitContext context = PixataTestContext.Create();
    List<bool> changes = [];
    IRenderedComponent<PixataDialog> cut = RenderDialog(context, p => {
      p.Add(c => c.ShowCloseButton, false);
      p.Add(c => c.VisibleChanged, (bool v) => changes.Add(v));
    });
    cut.Find(".pixata-blazor-dialog").KeyDown(Escape);
    Assert.AreEqual(0, changes.Count);
  }

  [TestMethod]
  public void PixataDialog_CloseOnEscape_OverridesTheCloseButton() {
    using BunitContext context = PixataTestContext.Create();
    List<bool> changes = [];
    IRenderedComponent<PixataDialog> withoutButton = RenderDialog(context, p => {
      p.Add(c => c.ShowCloseButton, false);
      p.Add(c => c.CloseOnEscape, true);
      p.Add(c => c.VisibleChanged, (bool v) => changes.Add(v));
    });
    withoutButton.Find(".pixata-blazor-dialog").KeyDown(Escape);
    Assert.AreEqual(1, changes.Count);
    IRenderedComponent<PixataDialog> withButton = RenderDialog(context, p => {
      p.Add(c => c.CloseOnEscape, false);
      p.Add(c => c.VisibleChanged, (bool v) => changes.Add(v));
    });
    withButton.Find(".pixata-blazor-dialog").KeyDown(Escape);
    Assert.AreEqual(1, changes.Count);
  }

  [TestMethod]
  public void PixataDialog_OtherKeys_DoNotClose() {
    using BunitContext context = PixataTestContext.Create();
    List<bool> changes = [];
    IRenderedComponent<PixataDialog> cut = RenderDialog(context, p => p.Add(c => c.VisibleChanged, (bool v) => changes.Add(v)));
    cut.Find(".pixata-blazor-dialog").KeyDown(new KeyboardEventArgs { Key = "Enter" });
    Assert.AreEqual(0, changes.Count);
  }

  [TestMethod]
  public async Task PixataDialog_EscapeFromTheScript_FollowsTheSameRules() {
    using BunitContext context = PixataTestContext.Create();
    List<bool> changes = [];
    IRenderedComponent<PixataDialog> cut = RenderDialog(context, p => p.Add(c => c.VisibleChanged, (bool v) => changes.Add(v)));
    await cut.InvokeAsync(() => cut.Instance.OnEscapeKey());
    Assert.AreEqual(1, changes.Count);
    cut.Render(p => p.Add(c => c.ShowCloseButton, false));
    await cut.InvokeAsync(() => cut.Instance.OnEscapeKey());
    Assert.AreEqual(1, changes.Count);
  }

  [TestMethod]
  public void PixataDialog_OverlayClick_ClosesOnlyWhenAskedTo() {
    using BunitContext context = PixataTestContext.Create();
    List<bool> changes = [];
    IRenderedComponent<PixataDialog> cut = RenderDialog(context, p => p.Add(c => c.VisibleChanged, (bool v) => changes.Add(v)));
    cut.Find(".pixata-blazor-overlay").Click();
    Assert.AreEqual(0, changes.Count);
    cut.Render(p => p.Add(c => c.CloseOnOverlayClick, true));
    cut.Find(".pixata-blazor-overlay").Click();
    CollectionAssert.AreEqual(new[] { false }, changes);
  }

  [TestMethod]
  public void PixataDialog_ButtonsLayout_SetsTheActionsClass() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDialog> cut = RenderDialog(context, p => p.Add(c => c.DialogButtons, "<button>OK</button>"));
    Assert.AreEqual("pixata-blazor-dialog-actions pixata-blazor-dialog-actions-stretch", cut.Find(".pixata-blazor-dialog-actions").ClassName);
    cut.Render(p => p.Add(c => c.ButtonsLayout, PixataDialogButtonsLayout.End));
    Assert.AreEqual("pixata-blazor-dialog-actions pixata-blazor-dialog-actions-end", cut.Find(".pixata-blazor-dialog-actions").ClassName);
  }

  [TestMethod]
  public void PixataDialog_NoButtons_HasNoActionsRow() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDialog> cut = RenderDialog(context);
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-dialog-actions").Count);
  }

  [TestMethod]
  public void PixataDialog_WidthAndHeight_SetTheCustomProperties() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDialog> cut = RenderDialog(context);
    Assert.IsFalse(cut.Find(".pixata-blazor-dialog").HasAttribute("style"));
    cut.Render(p => {
      p.Add(c => c.Width, "400px");
      p.Add(c => c.Height, "50vh");
    });
    Assert.AreEqual("--pixata-blazor-dialog-width:400px;--pixata-blazor-dialog-height:50vh;", cut.Find(".pixata-blazor-dialog").GetAttribute("style"));
  }

  [TestMethod]
  public void PixataDialog_ThemeColorClassAndAttributes_AreRendered() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDialog> cut = RenderDialog(context, p => {
      p.Add(c => c.ThemeColor, PixataThemeConstants.ThemeColor.Error);
      p.Add(c => c.Class, "my-dialog");
      p.AddUnmatched("data-test", "x");
      p.AddUnmatched("role", "alertdialog");
    });
    IElement dialog = cut.Find(".pixata-blazor-dialog");
    Assert.AreEqual("pixata-blazor-dialog pixata-blazor-dialog-error my-dialog", dialog.ClassName);
    Assert.AreEqual("x", dialog.GetAttribute("data-test"));
    Assert.AreEqual("alertdialog", dialog.GetAttribute("role"));
  }

  [TestMethod]
  public void PixataDialog_AppWideTexts_LabelTheCloseButton() {
    using BunitContext context = PixataTestContext.Create();
    context.Services.AddScoped(_ => new PixataDialogTexts { Close = "Fermer" });
    IRenderedComponent<PixataDialog> cut = RenderDialog(context);
    Assert.AreEqual("Fermer", cut.Find(".pixata-blazor-dialog-titlebar-action").GetAttribute("aria-label"));
  }

  [TestMethod]
  public void PixataDialog_OpensAndClosesItsLayer() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = context.JSInterop.SetupModule(ModulePath);
    module.Setup<int>("openLayer", _ => true).SetResult(5);
    module.SetupVoid("closeLayer", _ => true);
    IRenderedComponent<PixataDialog> cut = RenderDialog(context, p => p.Add(c => c.FocusedElementSelector, "#email"));
    JSRuntimeInvocation open = module.VerifyInvoke("openLayer");
    string options = open.Arguments[4]?.ToString() ?? "";
    StringAssert.Contains(options, "Kind = dialog");
    StringAssert.Contains(options, "Modal = True");
    StringAssert.Contains(options, "FocusSelector = #email");
    cut.Render(p => p.Add(c => c.Visible, false));
    JSRuntimeInvocation close = module.VerifyInvoke("closeLayer");
    Assert.AreEqual(5, close.Arguments[0]);
  }

  [TestMethod]
  public async Task PixataDialog_Disposed_ClosesItsLayer() {
    BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = context.JSInterop.SetupModule(ModulePath);
    module.Setup<int>("openLayer", _ => true).SetResult(3);
    module.SetupVoid("closeLayer", _ => true);
    RenderDialog(context);
    await context.DisposeComponentsAsync();
    Assert.AreEqual(3, module.VerifyInvoke("closeLayer").Arguments[0]);
    await context.DisposeAsync();
  }

  #endregion

  #region Predefined dialogs

  private static (IRenderedComponent<PixataDialogHost> Host, PixataDialogFactory Factory) RenderHost(BunitContext context) =>
    (context.Render<PixataDialogHost>(), context.Services.GetRequiredService<PixataDialogFactory>());

  [TestMethod]
  public async Task PixataDialogFactory_NoHost_ThrowsAClearException() {
    using BunitContext context = PixataTestContext.Create();
    PixataDialogFactory factory = context.Services.GetRequiredService<PixataDialogFactory>();
    InvalidOperationException ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => factory.AlertAsync("Saved"));
    StringAssert.Contains(ex.Message, "<PixataDialogHost />");
  }

  [TestMethod]
  public async Task PixataDialogFactory_HostDisposed_ThrowsAgain() {
    BunitContext context = PixataTestContext.Create();
    (IRenderedComponent<PixataDialogHost> _, PixataDialogFactory factory) = RenderHost(context);
    await context.DisposeComponentsAsync();
    await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => factory.ConfirmAsync("Sure?"));
    await context.DisposeAsync();
  }

  [TestMethod]
  public async Task PixataDialogFactory_Alert_ShowsTheMessageAndCompletesOnOk() {
    using BunitContext context = PixataTestContext.Create();
    (IRenderedComponent<PixataDialogHost> host, PixataDialogFactory factory) = RenderHost(context);
    Task alert = factory.AlertAsync("Your changes have been saved.", "Saved");
    host.WaitForAssertion(() => Assert.AreEqual(1, host.FindAll(".pixata-blazor-dialog").Count));
    IElement dialog = host.Find(".pixata-blazor-dialog");
    Assert.AreEqual("alertdialog", dialog.GetAttribute("role"));
    StringAssert.Contains(dialog.ClassName, "pixata-blazor-dialog-alert");
    Assert.AreEqual("Saved", host.Find(".pixata-blazor-dialog-title").TextContent.Trim());
    Assert.AreEqual("Your changes have been saved.", host.Find(".pixata-blazor-dialog-message").TextContent);
    Assert.AreEqual(0, host.FindAll(".pixata-blazor-dialog-cancel").Count);
    Assert.AreEqual(0, host.FindAll(".pixata-blazor-dialog-titlebar-action").Count);
    Assert.AreEqual("OK", host.Find(".pixata-blazor-dialog-ok").TextContent.Trim());
    Assert.IsFalse(alert.IsCompleted);
    host.Find(".pixata-blazor-dialog-ok").Click();
    await alert;
    Assert.AreEqual(0, host.FindAll(".pixata-blazor-dialog").Count);
  }

  [TestMethod]
  public async Task PixataDialogFactory_NoTitle_HasNoTitlebar() {
    using BunitContext context = PixataTestContext.Create();
    (IRenderedComponent<PixataDialogHost> host, PixataDialogFactory factory) = RenderHost(context);
    _ = factory.AlertAsync("Saved");
    host.WaitForAssertion(() => Assert.AreEqual(1, host.FindAll(".pixata-blazor-dialog").Count));
    Assert.AreEqual(0, host.FindAll(".pixata-blazor-dialog-titlebar").Count);
  }

  [TestMethod]
  public async Task PixataDialogFactory_Confirm_ReturnsTrueForOk() {
    using BunitContext context = PixataTestContext.Create();
    (IRenderedComponent<PixataDialogHost> host, PixataDialogFactory factory) = RenderHost(context);
    Task<bool> confirm = factory.ConfirmAsync("Delete Fred?", "Delete");
    host.WaitForAssertion(() => Assert.AreEqual(1, host.FindAll(".pixata-blazor-dialog-confirm").Count));
    Assert.AreEqual("Cancel", host.Find(".pixata-blazor-dialog-cancel").TextContent.Trim());
    host.Find(".pixata-blazor-dialog-ok").Click();
    Assert.IsTrue(await confirm);
  }

  [TestMethod]
  public async Task PixataDialogFactory_Confirm_ReturnsFalseForCancelOrEscape() {
    using BunitContext context = PixataTestContext.Create();
    (IRenderedComponent<PixataDialogHost> host, PixataDialogFactory factory) = RenderHost(context);
    Task<bool> cancelled = factory.ConfirmAsync("Delete Fred?");
    host.WaitForAssertion(() => Assert.AreEqual(1, host.FindAll(".pixata-blazor-dialog").Count));
    host.Find(".pixata-blazor-dialog-cancel").Click();
    Assert.IsFalse(await cancelled);
    Task<bool> escaped = factory.ConfirmAsync("Delete Fred?");
    host.WaitForAssertion(() => Assert.AreEqual(1, host.FindAll(".pixata-blazor-dialog").Count));
    host.Find(".pixata-blazor-dialog").KeyDown(Escape);
    Assert.IsFalse(await escaped);
    Assert.AreEqual(0, host.FindAll(".pixata-blazor-dialog").Count);
  }

  [TestMethod]
  public async Task PixataDialogFactory_PerCallText_ReplacesTheButtonText() {
    using BunitContext context = PixataTestContext.Create();
    (IRenderedComponent<PixataDialogHost> host, PixataDialogFactory factory) = RenderHost(context);
    _ = factory.ConfirmAsync("Save the changes?", null, "Yes", "No");
    host.WaitForAssertion(() => Assert.AreEqual(1, host.FindAll(".pixata-blazor-dialog").Count));
    Assert.AreEqual("Yes", host.Find(".pixata-blazor-dialog-ok").TextContent.Trim());
    Assert.AreEqual("No", host.Find(".pixata-blazor-dialog-cancel").TextContent.Trim());
  }

  [TestMethod]
  public async Task PixataDialogFactory_AppWideTexts_SetTheDefaults() {
    using BunitContext context = PixataTestContext.Create();
    context.Services.AddScoped(_ => new PixataDialogTexts { Ok = "D'accord", Cancel = "Annuler", ConfirmTitle = "Confirmer" });
    (IRenderedComponent<PixataDialogHost> host, PixataDialogFactory factory) = RenderHost(context);
    _ = factory.ConfirmAsync("Supprimer ?");
    host.WaitForAssertion(() => Assert.AreEqual(1, host.FindAll(".pixata-blazor-dialog").Count));
    Assert.AreEqual("D'accord", host.Find(".pixata-blazor-dialog-ok").TextContent.Trim());
    Assert.AreEqual("Annuler", host.Find(".pixata-blazor-dialog-cancel").TextContent.Trim());
    Assert.AreEqual("Confirmer", host.Find(".pixata-blazor-dialog-title").TextContent.Trim());
  }

  [TestMethod]
  public async Task PixataDialogFactory_Prompt_ReturnsWhatWasTyped() {
    using BunitContext context = PixataTestContext.Create();
    (IRenderedComponent<PixataDialogHost> host, PixataDialogFactory factory) = RenderHost(context);
    Task<string?> prompt = factory.PromptAsync("What would you like to call the report?", "Save report", "Monthly sales");
    host.WaitForAssertion(() => Assert.AreEqual(1, host.FindAll(".pixata-blazor-dialog-prompt").Count));
    IElement input = host.Find(".pixata-blazor-dialog-prompt-input");
    Assert.AreEqual("Monthly sales", input.GetAttribute("value"));
    Assert.AreEqual("What would you like to call the report?", input.GetAttribute("aria-label"));
    input.Change("Weekly sales");
    host.Find(".pixata-blazor-dialog-ok").Click();
    Assert.AreEqual("Weekly sales", await prompt);
  }

  [TestMethod]
  public async Task PixataDialogFactory_Prompt_EnterSubmits() {
    using BunitContext context = PixataTestContext.Create();
    (IRenderedComponent<PixataDialogHost> host, PixataDialogFactory factory) = RenderHost(context);
    Task<string?> prompt = factory.PromptAsync("Name?");
    host.WaitForAssertion(() => Assert.AreEqual(1, host.FindAll(".pixata-blazor-dialog").Count));
    Assert.AreEqual("", host.Find(".pixata-blazor-dialog-prompt-input").GetAttribute("value"));
    host.Find(".pixata-blazor-dialog-prompt-input").Change("Fred");
    host.Find(".pixata-blazor-dialog-prompt-form").Submit();
    Assert.AreEqual("Fred", await prompt);
  }

  [TestMethod]
  public async Task PixataDialogFactory_Prompt_ReturnsNullWhenCancelled() {
    using BunitContext context = PixataTestContext.Create();
    (IRenderedComponent<PixataDialogHost> host, PixataDialogFactory factory) = RenderHost(context);
    Task<string?> prompt = factory.PromptAsync("Name?", null, "Fred");
    host.WaitForAssertion(() => Assert.AreEqual(1, host.FindAll(".pixata-blazor-dialog").Count));
    host.Find(".pixata-blazor-dialog-cancel").Click();
    Assert.IsNull(await prompt);
  }

  [TestMethod]
  public async Task PixataDialogFactory_SeveralDialogs_AreShownTogetherAndAnsweredSeparately() {
    using BunitContext context = PixataTestContext.Create();
    (IRenderedComponent<PixataDialogHost> host, PixataDialogFactory factory) = RenderHost(context);
    Task<bool> first = factory.ConfirmAsync("First?");
    Task<bool> second = factory.ConfirmAsync("Second?");
    host.WaitForAssertion(() => Assert.AreEqual(2, host.FindAll(".pixata-blazor-dialog").Count));
    host.FindAll(".pixata-blazor-dialog-ok")[1].Click();
    Assert.IsTrue(await second);
    Assert.IsFalse(first.IsCompleted);
    Assert.AreEqual("First?", host.Find(".pixata-blazor-dialog-message").TextContent);
    host.Find(".pixata-blazor-dialog-cancel").Click();
    Assert.IsFalse(await first);
  }

  [TestMethod]
  public async Task PixataDialogFactory_HostDisposedWhileWaiting_CancelsTheDialog() {
    BunitContext context = PixataTestContext.Create();
    (IRenderedComponent<PixataDialogHost> host, PixataDialogFactory factory) = RenderHost(context);
    Task<string?> prompt = factory.PromptAsync("Name?");
    host.WaitForAssertion(() => Assert.AreEqual(1, host.FindAll(".pixata-blazor-dialog").Count));
    await context.DisposeComponentsAsync();
    Assert.IsNull(await prompt);
    await context.DisposeAsync();
  }

  #endregion
}
