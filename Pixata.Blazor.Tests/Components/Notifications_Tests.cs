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

  [DataRow(NotificationType.General, "pixata-blazor-notification-general", "fa-bell")]
  [DataRow(NotificationType.Info, "pixata-blazor-notification-info", "fa-info-circle")]
  [DataRow(NotificationType.Success, "pixata-blazor-notification-success", "fa-check")]
  [DataRow(NotificationType.Warning, "pixata-blazor-notification-warning", "fa-exclamation-triangle")]
  [DataRow(NotificationType.Error, "pixata-blazor-notification-error", "fa-exclamation")]
  [DataTestMethod]
  public void NotificationArea_Type_SetsTheIconAndColourClasses(NotificationType type, string colourClass, string iconClass) {
    using BunitContext context = PixataTestContext.Create();
    NotificationHelper helper = new();
    IRenderedComponent<NotificationArea> cut = RenderArea(context, helper);
    helper.Send(type, "Saved", DateTime.Now);
    cut.WaitForAssertion(() => {
      IElement icon = cut.Find(".notification .icon");
      Assert.IsTrue(icon.ClassList.Contains(colourClass));
      Assert.IsFalse(icon.HasAttribute("style"));
      Assert.IsTrue(icon.QuerySelector("i")!.ClassList.Contains(iconClass));
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
    cut.WaitForAssertion(() => Assert.AreEqual("Name is required", cut.Find(".notification .message ul li").TextContent));
  }

  [TestMethod]
  public void NotificationArea_MarkupString_IsRenderedAsHtml() {
    using BunitContext context = PixataTestContext.Create();
    NotificationHelper helper = new();
    IRenderedComponent<NotificationArea> cut = RenderArea(context, helper);
    helper.Send(NotificationType.Info, (MarkupString)"Invoice <strong>INV-001</strong> sent", DateTime.Now);
    cut.WaitForAssertion(() => Assert.AreEqual("INV-001", cut.Find(".notification .message strong").TextContent));
  }

  [TestMethod]
  public void NotificationArea_RenderFragments_AreNotTreatedAsDuplicates() {
    using BunitContext context = PixataTestContext.Create();
    NotificationHelper helper = new();
    IRenderedComponent<NotificationArea> cut = RenderArea(context, helper);
    helper.Send(NotificationType.Error, builder => builder.AddContent(0, "First"), DateTime.Now);
    helper.Send(NotificationType.Error, builder => builder.AddContent(0, "Second"), DateTime.Now);
    cut.WaitForAssertion(() => Assert.AreEqual(2, cut.FindAll(".notification.new").Count));
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
