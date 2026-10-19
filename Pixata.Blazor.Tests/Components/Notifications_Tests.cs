using System;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Containers;
using Pixata.Blazor.Notifications;

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
