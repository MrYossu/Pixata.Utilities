using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Extensions;

namespace Pixata.Blazor.Tests {
  [TestClass]
  public class TemplateHelper_Tests {
    private record Speaker(int Id, string Name) {
      public override string ToString() =>
        Name;
    }

    [TestMethod]
    public async Task TemplateHelper_Link_NoTitleGiven_UsesEachRowsOwnToString() {
      TemplateHelper helper = new();
      RenderFragment<object> template = helper.Link<Speaker>(s => s.Name, s => $"/speaker-{s.Id}");
      Speaker first = new(375, "Rabbis Y. Marmorstein and G. Miller");
      Speaker second = new(871, "Rabbis E.P. Levy and S.Z. Hoff");

      string firstHtml = await Render(template(first));
      string secondHtml = await Render(template(second));

      StringAssert.Contains(firstHtml, $"title=\"{first}\"");
      StringAssert.Contains(secondHtml, $"title=\"{second}\"");
    }

    [TestMethod]
    public async Task TemplateHelper_Link_TitleGiven_UsesItForEveryRow() {
      TemplateHelper helper = new();
      RenderFragment<object> template = helper.Link<Speaker>(s => s.Name, s => $"/speaker-{s.Id}", title: "Click to see the shiurim");

      string firstHtml = await Render(template(new Speaker(375, "Rabbi Y. Marmorstein")));
      string secondHtml = await Render(template(new Speaker(871, "Rabbi E.P. Levy")));

      StringAssert.Contains(firstHtml, "title=\"Click to see the shiurim\"");
      StringAssert.Contains(secondHtml, "title=\"Click to see the shiurim\"");
    }

    [TestMethod]
    public async Task TemplateHelper_Text_OnlyConvertGiven_RendersTheText() {
      TemplateHelper helper = new();
      RenderFragment<object> template = helper.Text<Speaker>(s => s.Name);

      string html = await Render(template(new Speaker(375, "Rabbi Y. Marmorstein")));

      Assert.AreEqual("<div class=\"\" style=\"\">Rabbi Y. Marmorstein</div>", html);
    }

    [TestMethod]
    public async Task TemplateHelper_Text_FuncOverloadWithoutCssFunc_HasNoCssClass() {
      TemplateHelper helper = new();
      RenderFragment<object> template = helper.Text<Speaker>(s => s.Name, styleFunc: _ => "text-align: right");

      string html = await Render(template(new Speaker(375, "Rabbi Y. Marmorstein")));

      StringAssert.Contains(html, "class=\"\"");
    }

    [TestMethod]
    public async Task TemplateHelper_Link_FuncOverloadWithoutCssFunc_HasNoCssClass() {
      TemplateHelper helper = new();
      RenderFragment<object> template = helper.Link<Speaker>(s => s.Name, s => $"/speaker-{s.Id}", styleFunc: _ => "text-align: right");

      string html = await Render(template(new Speaker(375, "Rabbi Y. Marmorstein")));

      StringAssert.Contains(html, "class=\"\"");
    }

    [TestMethod]
    public async Task TemplateHelper_Link_FuncOverloadWithoutTitleFunc_UsesEachRowsOwnToString() {
      TemplateHelper helper = new();
      RenderFragment<object> template = helper.Link<Speaker>(s => s.Name, s => $"/speaker-{s.Id}", styleFunc: _ => "text-align: right");
      Speaker first = new(375, "Rabbis Y. Marmorstein and G. Miller");
      Speaker second = new(871, "Rabbis E.P. Levy and S.Z. Hoff");

      string firstHtml = await Render(template(first));
      string secondHtml = await Render(template(second));

      StringAssert.Contains(firstHtml, $"title=\"{first}\"");
      StringAssert.Contains(secondHtml, $"title=\"{second}\"");
    }

    private static async Task<string> Render(RenderFragment fragment) {
      ServiceProvider services = new ServiceCollection().AddLogging().BuildServiceProvider();
      await using HtmlRenderer renderer = new(services, services.GetRequiredService<ILoggerFactory>());
      return await renderer.Dispatcher.InvokeAsync(async () => {
        HtmlRootComponent component = await renderer.RenderComponentAsync<FragmentHost>(ParameterView.FromDictionary(new Dictionary<string, object?> {
          [nameof(FragmentHost.Fragment)] = fragment
        }));
        return component.ToHtmlString();
      });
    }

    private class FragmentHost : ComponentBase {
      [Parameter]
      public RenderFragment Fragment { get; set; } = null!;

      protected override void BuildRenderTree(RenderTreeBuilder builder) =>
        builder.AddContent(0, Fragment);
    }
  }
}
