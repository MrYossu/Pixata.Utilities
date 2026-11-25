using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Containers;

namespace Pixata.Blazor.Tests.Components;

[TestClass]
public class Expander_Tests {
  [DataRow(false)]
  [DataRow(true)]
  [DataTestMethod]
  public async Task Expander_Toggle_ShowsOrHidesTheContentWithoutThePageRedrawing(bool expanded) {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<Expander> cut = context.Render<Expander>(p => p
      .Add(c => c.Expanded, expanded)
      .AddChildContent("<p>Content</p>"));
    Assert.AreEqual($"display: {(expanded ? "block" : "none")}", cut.Find("div").GetAttribute("style"));
    await cut.InvokeAsync(cut.Instance.Toggle);
    Assert.AreEqual($"display: {(expanded ? "none" : "block")}", cut.Find("div").GetAttribute("style"));
    await cut.InvokeAsync(cut.Instance.Toggle);
    Assert.AreEqual($"display: {(expanded ? "block" : "none")}", cut.Find("div").GetAttribute("style"));
  }
}
