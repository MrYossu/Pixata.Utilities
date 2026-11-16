using System.Linq;
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
public class MessageView_Tests {
  private const string Script = "<img src=x onerror=alert(1)>";

  [TestMethod]
  public void MessageView_ShowsTheMessageAsText() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<MessageView> cut = context.Render<MessageView>(p => p.Add(c => c.Message, $"Couldn't save 'Smith & Sons {Script}'"));
    IElement alert = cut.Find(".pixata-alert");
    Assert.AreEqual(0, alert.QuerySelectorAll("img").Length);
    Assert.IsTrue(alert.TextContent.Contains($"Couldn't save 'Smith & Sons {Script}'"));
  }

  [TestMethod]
  public void MessageView_MessageIsHtml_RendersTheMarkup() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<MessageView> cut = context.Render<MessageView>(p => p
      .Add(c => c.Message, "Saved <b>Fred</b>")
      .Add(c => c.MessageIsHtml, true));
    Assert.AreEqual("Fred", cut.Find(".pixata-alert b").TextContent);
  }

  [DataRow(true, "pixata-alert-danger")]
  [DataRow(false, "pixata-alert-success")]
  [DataTestMethod]
  public void MessageView_Error_ChoosesBetweenDangerAndSuccess(bool error, string alertClass) {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<MessageView> cut = context.Render<MessageView>(p => p
      .Add(c => c.Message, "Hello")
      .Add(c => c.Error, error));
    Assert.IsTrue(cut.Find(".pixata-alert").ClassList.Contains(alertClass));
  }

  [DataRow(NotificationType.Error, "pixata-alert-danger")]
  [DataRow(NotificationType.Success, "pixata-alert-success")]
  [DataRow(NotificationType.Warning, "pixata-alert-warning")]
  [DataRow(NotificationType.Info, "pixata-alert-info")]
  [DataRow(NotificationType.General, "pixata-alert-secondary")]
  [DataTestMethod]
  public void MessageView_Type_SetsTheColourAndOverridesError(NotificationType type, string alertClass) {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<MessageView> cut = context.Render<MessageView>(p => p
      .Add(c => c.Message, "Hello")
      .Add(c => c.Error, type != NotificationType.Error)
      .Add(c => c.Type, type));
    Assert.IsTrue(cut.Find(".pixata-alert").ClassList.Contains(alertClass));
  }

  private static IRenderedComponent<ApiResponseView<string>> RenderView(BunitContext context, bool? messageIsHtml = null) {
    context.Services.AddSingleton(new PersistentComponentStateHarness().State);
    return context.Render<ApiResponseView<string>>(p => {
      p.Add(c => c.LoadApiResponse, () => Task.FromResult(new ApiResponse<string>(ApiResponseStates.Success, "Hello")))
        .Add(c => c.Success, data => b => b.AddContent(0, data));
      if (messageIsHtml is bool html) {
        p.Add(c => c.MessageIsHtml, html);
      }
    });
  }

  [DataRow(NotificationType.Success, "pixata-alert-success")]
  [DataRow(NotificationType.Warning, "pixata-alert-warning")]
  [DataRow(NotificationType.Error, "pixata-alert-danger")]
  [DataTestMethod]
  public void ApiResponseView_Notify_ShowsTheMessageInTheColourForItsType(NotificationType type, string alertClass) {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<ApiResponseView<string>> cut = RenderView(context);
    cut.InvokeAsync(() => cut.Instance.Notify(type, "Saved"));
    cut.Render();
    IElement alert = cut.Find(".pixata-alert");
    Assert.IsTrue(alert.ClassList.Contains(alertClass));
    Assert.IsTrue(alert.TextContent.Contains("Saved"));
  }

  [TestMethod]
  public void ApiResponseView_SettingMessage_ShowsItAsAnError() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<ApiResponseView<string>> cut = RenderView(context);
    cut.InvokeAsync(() => cut.Instance.Notify(NotificationType.Success, "Saved"));
    cut.InvokeAsync(() => cut.Instance.Message = "Oops");
    cut.Render();
    Assert.IsTrue(cut.Find(".pixata-alert").ClassList.Contains("pixata-alert-danger"));
  }

  [TestMethod]
  public void ApiResponseView_FailedDo_ShowsTheMessageAsText() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<ApiResponseView<string>> cut = RenderView(context);
    cut.InvokeAsync(() => cut.Instance.Do(() => Task.FromResult(new ApiResponse<string>(ApiResponseStates.Failure, Message: $"Bad name {Script}")), _ => { }));
    cut.Render();
    IElement alert = cut.Find(".pixata-alert");
    Assert.AreEqual(0, alert.QuerySelectorAll("img").Length);
    Assert.IsTrue(alert.TextContent.Contains($"Bad name {Script}"));
  }

  [TestMethod]
  public void ApiResponseView_MessageIsHtml_RendersTheMarkup() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<ApiResponseView<string>> cut = RenderView(context, messageIsHtml: true);
    cut.InvokeAsync(() => cut.Instance.Notify(NotificationType.Error, "Bad <b>name</b>"));
    cut.Render();
    Assert.AreEqual("name", cut.Find(".pixata-alert b").TextContent);
  }

  [TestMethod]
  public void ApiResponseViewConfig_MessageIsHtml_IsTheDefault() {
    bool was = ApiResponseViewConfig.MessageIsHtml;
    try {
      ApiResponseViewConfig.MessageIsHtml = true;
      using BunitContext context = PixataTestContext.Create();
      IRenderedComponent<ApiResponseView<string>> cut = RenderView(context);
      Assert.IsTrue(cut.Instance.MessageIsHtml);
    }
    finally {
      ApiResponseViewConfig.MessageIsHtml = was;
    }
  }

  [DataRow("Oops --> <img src=x onerror=alert(1)>")]
  [DataRow("Oops ---> <img src=x onerror=alert(1)>")]
  [DataRow("Oops --!> <img src=x onerror=alert(1)>")]
  [DataRow("Oops <!-- --> <img src=x onerror=alert(1)>")]
  [DataTestMethod]
  public void ApiResponseViewConfig_Failure_KeepsTheMessageInsideTheComment(string message) {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<ContainerFragment> cut = context.Render<ContainerFragment>(p => p.Add(c => c.Content, ApiResponseViewConfig.Failure(message)));
    Assert.AreEqual(0, cut.FindAll("img").Count);
    string comment = cut.Markup[(cut.Markup.IndexOf("<!--") + 4)..cut.Markup.LastIndexOf("-->")];
    Assert.IsFalse(comment.Contains("--"));
    Assert.IsTrue(comment.Contains("Oops"));
  }

  [DataRow("Timeout expired", "Timeout expired")]
  [DataRow("Oops --> here", "Oops - -> here")]
  [DataRow("Oops ---> here", "Oops - - -> here")]
  [DataTestMethod]
  public void ApiResponseViewConfig_Failure_PutsTheMessageInTheCommentWithASpaceBetweenHyphens(string message, string inComment) {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<ContainerFragment> cut = context.Render<ContainerFragment>(p => p.Add(c => c.Content, ApiResponseViewConfig.Failure(message)));
    Assert.IsTrue(cut.Markup.Contains($"<!--\nEx: {inComment}\n-->"));
  }

  // Renders a fragment on its own
  public class ContainerFragment : ComponentBase {
    [Parameter]
    public RenderFragment? Content { get; set; }

    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) =>
      builder.AddContent(0, Content);
  }
}
