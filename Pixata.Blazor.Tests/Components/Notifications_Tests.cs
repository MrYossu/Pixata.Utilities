using System;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Containers;
using Pixata.Blazor.Notifications;
using Pixata.Extensions;

namespace Pixata.Blazor.Tests.Components;

[TestClass]
public class Notifications_Tests {
  private static IRenderedComponent<NotificationArea> RenderArea(BunitContext context, NotificationHelper helper) =>
    context.Render<NotificationArea>(p => p.Add(c => c.NotificationHelper, helper));

  // The icon classes are static, so the tests that set them put them back
  private static void WithIconClasses(Action test) {
    string[] icons = [NotificationHelper.GeneralIcon, NotificationHelper.InfoIcon, NotificationHelper.SuccessIcon, NotificationHelper.WarningIcon, NotificationHelper.ErrorIcon];
    try {
      test();
    }
    finally {
      (NotificationHelper.GeneralIcon, NotificationHelper.InfoIcon, NotificationHelper.SuccessIcon, NotificationHelper.WarningIcon, NotificationHelper.ErrorIcon) = (icons[0], icons[1], icons[2], icons[3], icons[4]);
    }
  }

  [DataRow(NotificationType.General, "pixata-blazor-notification-general", "bell")]
  [DataRow(NotificationType.Info, "pixata-blazor-notification-info", "info")]
  [DataRow(NotificationType.Success, "pixata-blazor-notification-success", "check")]
  [DataRow(NotificationType.Warning, "pixata-blazor-notification-warning", "warning")]
  [DataRow(NotificationType.Error, "pixata-blazor-notification-error", "exclamation")]
  [DataTestMethod]
  public void NotificationArea_Type_SetsTheColourClassAndBuiltInIcon(NotificationType type, string colourClass, string iconName) {
    using BunitContext context = PixataTestContext.Create();
    NotificationHelper helper = new();
    IRenderedComponent<NotificationArea> cut = RenderArea(context, helper);
    helper.Send(type, "Saved", DateTime.Now);
    cut.WaitForAssertion(() => {
      IElement icon = cut.Find(".pixata-blazor-notification .pixata-blazor-notification-icon");
      Assert.IsTrue(icon.ClassList.Contains(colourClass));
      Assert.IsFalse(icon.HasAttribute("style"));
      Assert.IsNotNull(icon.QuerySelector($".pixata-blazor-svg-icon-{iconName} svg"));
      Assert.IsNull(icon.QuerySelector("i"));
    });
  }

  [TestMethod]
  public void NotificationArea_IconClass_IsUsedInsteadOfTheBuiltInIcon() =>
    WithIconClasses(() => {
      NotificationHelper.SuccessIcon = "my-tick";
      using BunitContext context = PixataTestContext.Create();
      NotificationHelper helper = new();
      IRenderedComponent<NotificationArea> cut = RenderArea(context, helper);
      helper.Send(NotificationType.Success, "Saved", DateTime.Now);
      cut.WaitForAssertion(() => {
        IElement icon = cut.Find(".pixata-blazor-notification-icon .pixata-blazor-font-icon");
        Assert.IsTrue(icon.ClassList.Contains("my-tick"));
        Assert.IsNull(cut.Find(".pixata-blazor-notification-icon").QuerySelector("svg"));
      });
    });

  [TestMethod]
  public void NotificationHelper_UseFontAwesomeIcons_SetsTheIconClasses() =>
    WithIconClasses(() => {
      Assert.AreEqual("", NotificationHelper.GetIconClass(NotificationType.Warning));
      NotificationHelper.UseFontAwesomeIcons();
      Assert.AreEqual("fas fa-exclamation-triangle", NotificationHelper.GetIconClass(NotificationType.Warning));
      Assert.AreEqual("fas fa-bell", NotificationHelper.GetDisplayIcon(NotificationType.General));
    });

  [TestMethod]
  public void NotificationArea_InnerElements_HavePrefixedClasses() {
    using BunitContext context = PixataTestContext.Create();
    NotificationHelper helper = new();
    IRenderedComponent<NotificationArea> cut = RenderArea(context, helper);
    helper.Send(NotificationType.Info, "Hello", DateTime.Now);
    cut.WaitForAssertion(() => {
      Assert.AreEqual(1, cut.FindAll(".pixata-blazor-notification-area > .pixata-blazor-notification.pixata-blazor-notification-new").Count);
      Assert.AreEqual("Hello", cut.Find(".pixata-blazor-notification-message").TextContent.Trim());
      Assert.AreEqual(1, cut.FindAll(".pixata-blazor-notification-dismiss button.pixata-btn-close").Count);
      Assert.AreEqual(0, cut.FindAll(".notification, .message, .icon, .dismiss").Count);
    });
  }

  [TestMethod]
  public void NotificationArea_RenderFragment_IsRendered() {
    using BunitContext context = PixataTestContext.Create();
    NotificationHelper helper = new();
    IRenderedComponent<NotificationArea> cut = RenderArea(context, helper);
    helper.Send(NotificationType.Error, builder => {
      builder.OpenElement(0, "ul");
      builder.OpenElement(1, "li");
      builder.AddContent(2, "Name is required");
      builder.CloseElement();
      builder.CloseElement();
    }, DateTime.Now);
    cut.WaitForAssertion(() => Assert.AreEqual("Name is required", cut.Find(".pixata-blazor-notification-message ul li").TextContent));
  }

  [TestMethod]
  public void NotificationArea_MarkupString_IsRenderedAsHtml() {
    using BunitContext context = PixataTestContext.Create();
    NotificationHelper helper = new();
    IRenderedComponent<NotificationArea> cut = RenderArea(context, helper);
    helper.Send(NotificationType.Info, (MarkupString)"Invoice <strong>INV-001</strong> sent", DateTime.Now);
    cut.WaitForAssertion(() => Assert.AreEqual("INV-001", cut.Find(".pixata-blazor-notification-message strong").TextContent));
  }

  [TestMethod]
  public void NotificationArea_RenderFragments_AreNotTreatedAsDuplicates() {
    using BunitContext context = PixataTestContext.Create();
    NotificationHelper helper = new();
    IRenderedComponent<NotificationArea> cut = RenderArea(context, helper);
    helper.Send(NotificationType.Error, builder => builder.AddContent(0, "First"), DateTime.Now);
    helper.Send(NotificationType.Error, builder => builder.AddContent(0, "Second"), DateTime.Now);
    cut.WaitForAssertion(() => Assert.AreEqual(2, cut.FindAll(".pixata-blazor-notification-new").Count));
  }

  private static IRenderedComponent<NotificationArea> RenderFallbackArea(BunitContext context, NotificationHelper helper) =>
    context.Render<NotificationArea>(p => p.Add(c => c.NotificationHelper, helper).Add(c => c.Fallback, true));

  [TestMethod]
  public void FallbackAreas_OnlyTheFirstShowsANotification() {
    // As each ApiResponseView in Notifications mode has one, so a page with three views showed every message three times
    using BunitContext context = PixataTestContext.Create();
    NotificationHelper helper = new();
    IRenderedComponent<NotificationArea>[] areas = [RenderFallbackArea(context, helper), RenderFallbackArea(context, helper), RenderFallbackArea(context, helper)];
    helper.Send(NotificationType.Error, "Couldn't save", DateTime.Now);
    areas[0].WaitForAssertion(() => Assert.AreEqual(1, areas[0].FindAll(".pixata-blazor-notification").Count));
    Assert.AreEqual(0, areas[1].FindAll(".pixata-blazor-notification").Count);
    Assert.AreEqual(0, areas[2].FindAll(".pixata-blazor-notification").Count);
  }

  [TestMethod]
  public void FallbackAreas_ShowNothingWhenThereIsAnAreaInTheLayout() {
    using BunitContext context = PixataTestContext.Create();
    NotificationHelper helper = new();
    IRenderedComponent<NotificationArea> fallback = RenderFallbackArea(context, helper);
    IRenderedComponent<NotificationArea> layout = RenderArea(context, helper);
    helper.Send(NotificationType.Error, "Couldn't save", DateTime.Now);
    layout.WaitForAssertion(() => Assert.AreEqual(1, layout.FindAll(".pixata-blazor-notification").Count));
    Assert.AreEqual(0, fallback.FindAll(".pixata-blazor-notification").Count);
  }

  [TestMethod]
  public void FallbackArea_TakesOverWhenTheOneBeforeItGoes() {
    using BunitContext context = PixataTestContext.Create();
    NotificationHelper helper = new();
    IRenderedComponent<NotificationArea> first = RenderFallbackArea(context, helper);
    IRenderedComponent<NotificationArea> second = RenderFallbackArea(context, helper);
    first.Instance.Dispose();
    helper.Send(NotificationType.Error, "Couldn't save", DateTime.Now);
    second.WaitForAssertion(() => Assert.AreEqual(1, second.FindAll(".pixata-blazor-notification").Count));
  }

  [TestMethod]
  public void AreasThatAreNotFallbacks_AllShowANotification_AndReceiveIsStillRaised() {
    using BunitContext context = PixataTestContext.Create();
    NotificationHelper helper = new();
    int received = 0;
    // Assigning (rather than adding to) Receive used to unhook the areas
    helper.Receive = _ => received++;
    IRenderedComponent<NotificationArea> first = RenderArea(context, helper);
    IRenderedComponent<NotificationArea> second = RenderArea(context, helper);
    helper.Send(NotificationType.Info, "Saved", DateTime.Now);
    first.WaitForAssertion(() => Assert.AreEqual(1, first.FindAll(".pixata-blazor-notification").Count));
    second.WaitForAssertion(() => Assert.AreEqual(1, second.FindAll(".pixata-blazor-notification").Count));
    Assert.AreEqual(1, received);
  }

  [TestMethod]
  public void ApiResponseView_InNotificationsMode_HasAFallbackArea() {
    using BunitContext context = PixataTestContext.Create();
    context.Services.AddSingleton(new PersistentComponentStateHarness().State);
    IRenderedComponent<ApiResponseView<string>> cut = context.Render<ApiResponseView<string>>(p => p
      .Add(c => c.FeedbackType, ApiResponseViewFeedbackType.Notifications)
      .Add(c => c.LoadApiResponse, () => Task.FromResult(new ApiResponse<string>(ApiResponseStates.Success, "Hello"))));
    Assert.IsTrue(cut.FindComponent<NotificationArea>().Instance.Fallback);
  }

  [TestMethod]
  public void Confirm_UsesClassesNotIds_SoSeveralCanBeOnOnePage() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<Confirm> cut = context.Render<Confirm>(p => p.Add(c => c.Caption, "Delete <b>Fred</b>?"));
    Assert.AreEqual(0, cut.FindAll("[id]").Count);
    Assert.AreEqual(1, cut.FindAll(".pixata-blazor-confirm-overlay > .pixata-blazor-confirm-content > .pixata-blazor-confirm-caption").Count);
    Assert.AreEqual("Fred", cut.Find(".pixata-blazor-confirm-caption b").TextContent);
  }

  [TestMethod]
  public void Inform_UsesClassesNotIds_SoSeveralCanBeOnOnePage() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<Inform> cut = context.Render<Inform>(p => p.Add(c => c.Caption, "Saved"));
    Assert.AreEqual(0, cut.FindAll("[id]").Count);
    Assert.AreEqual(1, cut.FindAll(".pixata-blazor-inform-overlay > .pixata-blazor-inform-content").Count);
  }
}
