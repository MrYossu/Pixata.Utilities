using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests.Components;

[TestClass]
public class MediaQuery_Tests {
  private const string Phone = "(max-width: 720px)";

  private BunitContext _ctx = null!;
  private BunitJSModuleInterop _module = null!;
  private readonly List<bool> _changes = [];

  [TestInitialize]
  public void Initialise() {
    _ctx = new();
    _module = _ctx.JSInterop.SetupModule(PixataBlazorScript.Path);
    _module.SetupVoid("disposeMediaQuery", _ => true).SetVoidResult();
    _changes.Clear();
  }

  [TestCleanup]
  public void Cleanup() =>
    _ctx.Dispose();

  private IRenderedComponent<PixataMediaQuery> Render(string media = Phone) =>
    _ctx.Render<PixataMediaQuery>(p => p
      .Add(c => c.Media, media)
      .Add(c => c.OnChange, EventCallback.Factory.Create<bool>(this, matches => _changes.Add(matches))));

  private List<object?> Disposed =>
    [.. _module.Invocations.Where(i => i.Identifier == "disposeMediaQuery").Select(i => i.Arguments[0])];

  [TestMethod]
  public void RendersNothing_AndSaysWhetherTheQueryMatchesOnceItIsInteractive() {
    _module.Setup<int[]?>("initMediaQuery", i => (string?)i.Arguments[0] == Phone).SetResult([1, 1]);

    IRenderedComponent<PixataMediaQuery> cut = Render();

    Assert.AreEqual("", cut.Markup);
    CollectionAssert.AreEqual(new[] { true }, _changes);
  }

  [TestMethod]
  public void RaisesOnChange_WhenTheQueryStartsOrStopsMatching() {
    _module.Setup<int[]?>("initMediaQuery", _ => true).SetResult([1, 0]);
    IRenderedComponent<PixataMediaQuery> cut = Render();

    cut.InvokeAsync(() => cut.Instance.MediaChanged(1, true));
    cut.InvokeAsync(() => cut.Instance.MediaChanged(1, false));

    CollectionAssert.AreEqual(new[] { false, true, false }, _changes);
  }

  [TestMethod]
  public void ChangingTheQuery_StopsWatchingTheOldOne_AndWatchesTheNewOne() {
    _module.Setup<int[]?>("initMediaQuery", i => (string?)i.Arguments[0] == Phone).SetResult([1, 1]);
    _module.Setup<int[]?>("initMediaQuery", i => (string?)i.Arguments[0] == "(prefers-color-scheme: dark)").SetResult([2, 0]);
    IRenderedComponent<PixataMediaQuery> cut = Render();

    cut.Render(p => p.Add(c => c.Media, "(prefers-color-scheme: dark)"));

    CollectionAssert.AreEqual(new object?[] { 1 }, Disposed);
    CollectionAssert.AreEqual(new[] { true, false }, _changes);
    // A change to the old query that was already on its way is ignored
    cut.InvokeAsync(() => cut.Instance.MediaChanged(1, false));
    Assert.AreEqual(2, _changes.Count);
  }

  [TestMethod]
  public async Task Disposing_StopsWatching() {
    _module.Setup<int[]?>("initMediaQuery", _ => true).SetResult([7, 1]);
    IRenderedComponent<PixataMediaQuery> cut = Render();

    await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());

    CollectionAssert.AreEqual(new object?[] { 7 }, Disposed);
    await cut.InvokeAsync(() => cut.Instance.MediaChanged(7, false));
    Assert.AreEqual(1, _changes.Count);
  }

  [TestMethod]
  public void WhenTheCircuitHasGone_NothingIsThrown_AndOnChangeIsNotRaised() {
    _module.Setup<int[]?>("initMediaQuery", _ => true).SetException(new JSDisconnectedException("The circuit has gone"));

    IRenderedComponent<PixataMediaQuery> cut = Render();

    Assert.AreEqual(0, _changes.Count);
    Assert.IsFalse(_ctx.Renderer.UnhandledException.IsCompleted);
  }

  [TestMethod]
  public void NoQuery_WatchesNothing() {
    Render("");

    Assert.AreEqual(0, _module.Invocations.Count(i => i.Identifier == "initMediaQuery"));
    Assert.AreEqual(0, _changes.Count);
  }
}
