using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Containers;

namespace Pixata.Blazor.Tests.Components;

[TestClass]
public class Confirm_Tests {
  private static bool IsOpen(IRenderedComponent<ConfirmPage> cut) =>
    cut.Find(".pixata-blazor-confirm-overlay").GetAttribute("style")!.Contains("display: block");

  [DataRow(false)]
  [DataRow(true)]
  [DataTestMethod]
  public void Confirm_Display_OpensWithOrWithoutContent(bool hasContent) {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<ConfirmPage> cut = context.Render<ConfirmPage>(p => p.Add(c => c.HasContent, hasContent));
    Assert.IsFalse(IsOpen(cut));
    cut.Find("#delete").Click();
    Assert.IsTrue(IsOpen(cut));
    Assert.AreEqual("Delete this?", cut.Find(".pixata-blazor-confirm-caption").TextContent);
  }

  [DataRow(false)]
  [DataRow(true)]
  [DataTestMethod]
  public void Confirm_ButtonClicked_ClosesAndRaisesOnClicked(bool yes) {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<ConfirmPage> cut = context.Render<ConfirmPage>();
    cut.Find("#delete").Click();
    Assert.IsTrue(IsOpen(cut));
    cut.Find(yes ? ".pixata-btn-primary" : ".pixata-btn-secondary").Click();
    Assert.IsFalse(IsOpen(cut));
    CollectionAssert.AreEqual(new[] { yes }, cut.Instance.Answers);
  }

  [TestMethod]
  public async Task Confirm_Close_ClosesAConfirmThatStayedOpenAndCanBeOpenedAgain() {
    using BunitContext context = PixataTestContext.Create();
    TaskCompletionSource deleting = new();
    IRenderedComponent<ConfirmPage> cut = context.Render<ConfirmPage>(p => p
      .Add(c => c.CloseOnClick, false)
      .Add(c => c.Deleting, deleting.Task));
    cut.Find("#delete").Click();
    cut.Find(".pixata-btn-primary").Click();
    Assert.IsTrue(IsOpen(cut));
    Assert.IsTrue(cut.Find(".pixata-btn-primary").HasAttribute("disabled"));
    await cut.InvokeAsync(deleting.SetResult);
    cut.WaitForAssertion(() => Assert.IsFalse(IsOpen(cut)));
    cut.Find("#delete").Click();
    Assert.IsTrue(IsOpen(cut));
    Assert.IsFalse(cut.Find(".pixata-btn-primary").HasAttribute("disabled"));
  }

  /// <summary>
  /// A page with a button that asks before deleting something. Unless the Confirm has content, its parameters are an enum, a
  /// string and OnClicked, none of which Blazor sees as changed when the page redraws, so that doesn't redraw the Confirm
  /// </summary>
  private class ConfirmPage : ComponentBase {
    private Confirm _confirm = null!;

    // Made once, as .NET 8 sees an EventCallback made again from the same method as changed, so redraws the Confirm. .NET 10
    // (which these tests don't run on) doesn't, so a page there with OnClicked="Clicked" in its markup is like this one
    private EventCallback<bool> _clicked;

    [Parameter]
    public bool HasContent { get; set; }

    [Parameter]
    public bool CloseOnClick { get; set; } = true;

    /// <summary>
    /// Awaited before the page calls Close, as when it deletes something
    /// </summary>
    [Parameter]
    public Task Deleting { get; set; } = Task.CompletedTask;

    public List<bool> Answers { get; } = [];

    protected override void OnInitialized() =>
      _clicked = EventCallback.Factory.Create<bool>(this, Clicked);

    private void Delete() =>
      _confirm.Display("Delete this?", CloseOnClick);

    private async Task Clicked(bool yes) {
      Answers.Add(yes);
      if (!CloseOnClick) {
        await Deleting;
        _confirm.Close();
      }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder) {
      builder.OpenElement(0, "button");
      builder.AddAttribute(1, "id", "delete");
      builder.AddAttribute(2, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, Delete));
      builder.CloseElement();
      builder.OpenComponent<Confirm>(3);
      builder.AddAttribute(4, nameof(Confirm.PopUpDisplayOption), PopUpDisplayOptions.Window);
      builder.AddAttribute(5, nameof(Confirm.YesText), "Delete");
      builder.AddAttribute(6, nameof(Confirm.OnClicked), _clicked);
      if (HasContent) {
        builder.AddAttribute(7, nameof(Confirm.ChildContent), (RenderFragment)(b => b.AddMarkupContent(0, "<p>Content</p>")));
      }
      builder.AddComponentReferenceCapture(8, c => _confirm = (Confirm)c);
      builder.CloseComponent();
    }
  }
}
