using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.PdfViewer;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataPdfViewer_Tests {
  private BunitContext _ctx = null!;
  private BunitJSModuleInterop _module = null!;

  [TestInitialize]
  public void Initialise() {
    _ctx = new();
    _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    _module = _ctx.JSInterop.SetupModule(PixataPdfViewer.ModulePath);
    _module.Mode = JSRuntimeMode.Loose;
  }

  [TestCleanup]
  public void Cleanup() =>
    _ctx.Dispose();

  private IRenderedComponent<PixataPdfViewer> Render(Action<ComponentParameterCollectionBuilder<PixataPdfViewer>>? parameters = null) =>
    _ctx.Render<PixataPdfViewer>(p => {
      p.Add(c => c.Url, "invoice.pdf");
      parameters?.Invoke(p);
    });

  private static IRenderedComponent<PixataPdfViewer> Loaded(IRenderedComponent<PixataPdfViewer> viewer, int pages = 3) {
    viewer.InvokeAsync(() => viewer.Instance.Loaded(pages));
    return viewer;
  }

  private static IElement Button(IRenderedComponent<PixataPdfViewer> viewer, string label) =>
    viewer.Find($"button[aria-label='{label}']");

  private static IElement PageBox(IRenderedComponent<PixataPdfViewer> viewer) =>
    viewer.Find(".pixata-blazor-pdfviewer-page-input input");

  private string Key() =>
    (string)_module.Invocations["create"].Single().Arguments[0]!;

  // ---------- Opening ----------

  [TestMethod]
  public void MoreThanOneSource_Throws() {
    InvalidOperationException ex = Assert.ThrowsException<InvalidOperationException>(() => Render(p => p.Add(c => c.Data, [1, 2, 3])));

    StringAssert.Contains(ex.Message, "one of Url, Data or Stream");
  }

  [TestMethod]
  public void NoSource_SaysThereIsNoDocument_AndTheToolsAreDisabled() {
    IRenderedComponent<PixataPdfViewer> viewer = _ctx.Render<PixataPdfViewer>();

    Assert.AreEqual("No document", viewer.Find(".pixata-blazor-pdfviewer-status").TextContent);
    Assert.IsTrue(Button(viewer, "Next page").HasAttribute("disabled"));
    Assert.IsTrue(PageBox(viewer).HasAttribute("disabled"));
    Assert.AreEqual(0, _module.Invocations["openUrl"].Count);
  }

  [TestMethod]
  public void Url_IsOpened_AndLoadingIsShownUntilItIs() {
    IRenderedComponent<PixataPdfViewer> viewer = Render();

    JSRuntimeInvocation create = _module.Invocations["create"].Single();
    Assert.AreEqual(viewer.Find(".pixata-blazor-pdfviewer-container").Id, create.Arguments[1]);
    CollectionAssert.AreEqual(new object[] { Key(), "invoice.pdf", 1 }, _module.Invocations["openUrl"].Single().Arguments.ToArray());
    Assert.AreEqual("Loading...", viewer.Find(".pixata-blazor-pdfviewer-status").TextContent);
  }

  [TestMethod]
  public void Data_IsSentAsAStream() {
    _ctx.Render<PixataPdfViewer>(p => p.Add(c => c.Data, [37, 80, 68, 70]));

    Assert.IsInstanceOfType<DotNetStreamReference>(_module.Invocations["openData"].Single().Arguments[1]);
  }

  [TestMethod]
  public void AStream_IsSentAndLeftOpen() {
    using MemoryStream stream = new([37, 80, 68, 70]);
    _ctx.Render<PixataPdfViewer>(p => p.Add(c => c.Stream, stream));

    DotNetStreamReference reference = (DotNetStreamReference)_module.Invocations["openData"].Single().Arguments[1]!;
    Assert.AreSame(stream, reference.Stream);
    Assert.IsTrue(reference.LeaveOpen);
  }

  [TestMethod]
  public void ANewUrl_IsOpened_ButTheSameOneIsnt() {
    IRenderedComponent<PixataPdfViewer> viewer = Render();

    viewer.Render(p => p.Add(c => c.Url, "invoice.pdf"));
    viewer.Render(p => p.Add(c => c.Url, "other.pdf"));

    CollectionAssert.AreEqual(new[] { "invoice.pdf", "other.pdf" }, _module.Invocations["openUrl"].Select(i => i.Arguments[1]).ToArray());
  }

  [TestMethod]
  public void Loaded_ShowsThePageCount_EnablesTheTools_AndRaisesOnLoaded() {
    int pages = 0;
    IRenderedComponent<PixataPdfViewer> viewer = Loaded(Render(p => p.Add(c => c.OnLoaded, EventCallback.Factory.Create<PixataPdfViewerLoadedEventArgs>(this, e => pages = e.PageCount))));

    Assert.AreEqual("of 3", viewer.Find(".pixata-blazor-pdfviewer-page-count").TextContent);
    Assert.AreEqual("1", PageBox(viewer).GetAttribute("value"));
    Assert.IsFalse(Button(viewer, "Next page").HasAttribute("disabled"));
    Assert.IsTrue(Button(viewer, "Previous page").HasAttribute("disabled"));
    Assert.AreEqual(0, viewer.FindAll(".pixata-blazor-pdfviewer-status").Count);
    Assert.AreEqual(3, pages);
    Assert.AreEqual(3, viewer.Instance.PageCount);
  }

  [TestMethod]
  [DataRow("password", PixataPdfViewerErrorKind.PasswordProtected, "This PDF is protected by a password, so it can't be shown")]
  [DataRow("invalid", PixataPdfViewerErrorKind.NotAPdf, "This file isn't a PDF, or it's damaged")]
  [DataRow("missing", PixataPdfViewerErrorKind.NotFound, "The PDF couldn't be found")]
  [DataRow("other", PixataPdfViewerErrorKind.Other, "The PDF couldn't be loaded")]
  public void Failed_SaysWhy_AndRaisesOnError(string kind, PixataPdfViewerErrorKind expected, string message) {
    PixataPdfViewerErrorEventArgs? error = null;
    IRenderedComponent<PixataPdfViewer> viewer = Render(p => p.Add(c => c.OnError, EventCallback.Factory.Create<PixataPdfViewerErrorEventArgs>(this, e => error = e)));

    viewer.InvokeAsync(() => viewer.Instance.Failed(kind, "Some detail"));

    Assert.AreEqual(message, viewer.Find(".pixata-blazor-pdfviewer-error").TextContent);
    Assert.AreEqual(expected, error!.Kind);
    Assert.AreEqual("Some detail", error.Detail);
    Assert.IsFalse(viewer.Instance.IsLoaded);
  }

  // ---------- Pages ----------

  [TestMethod]
  public void PageShown_UpdatesTheToolbar_AndRaisesPageChanged() {
    int page = 0;
    IRenderedComponent<PixataPdfViewer> viewer = Loaded(Render(p => p.Add(c => c.PageChanged, EventCallback.Factory.Create<int>(this, v => page = v))));

    viewer.InvokeAsync(() => viewer.Instance.PageShown(3));

    Assert.AreEqual("3", PageBox(viewer).GetAttribute("value"));
    Assert.IsTrue(Button(viewer, "Next page").HasAttribute("disabled"));
    Assert.AreEqual(3, page);
    Assert.AreEqual(3, viewer.Instance.CurrentPage);
  }

  [TestMethod]
  public void TheButtons_AndTypingAPage_GoToIt() {
    IRenderedComponent<PixataPdfViewer> viewer = Loaded(Render());

    Button(viewer, "Next page").Click();
    PageBox(viewer).Change("3");
    PageBox(viewer).Change("99");

    CollectionAssert.AreEqual(new object[] { 2, 3, 3 }, _module.Invocations["goToPage"].Select(i => i.Arguments[1]!).ToArray());
  }

  [TestMethod]
  public void TypingTheCurrentPageOrNotANumber_PutsTheNumberBack() {
    IRenderedComponent<PixataPdfViewer> viewer = Loaded(Render());

    PageBox(viewer).Change("abc");

    Assert.AreEqual(0, _module.Invocations["goToPage"].Count);
    Assert.AreEqual("1", PageBox(viewer).GetAttribute("value"));
  }

  [TestMethod]
  public void ChangingThePageParameter_GoesToIt_ButRenderingWithTheSameValueDoesnt() {
    IRenderedComponent<PixataPdfViewer> viewer = Loaded(Render(p => p.Add(c => c.Page, 1)));
    viewer.InvokeAsync(() => viewer.Instance.PageShown(2));

    viewer.Render(p => p.Add(c => c.Page, 1));
    Assert.AreEqual(0, _module.Invocations["goToPage"].Count);

    viewer.Render(p => p.Add(c => c.Page, 3));
    Assert.AreEqual(3, _module.Invocations["goToPage"].Single().Arguments[1]);
  }

  [TestMethod]
  public void ThePageParameter_IsThePageEachPdfIsOpenedAt() {
    IRenderedComponent<PixataPdfViewer> viewer = Render(p => p.Add(c => c.Page, 2));
    viewer.Render(p => p.Add(c => c.Data, [37, 80, 68, 70]).Add(c => c.Url, null));

    Assert.AreEqual(2, _module.Invocations["openUrl"].Single().Arguments[2]);
    Assert.AreEqual(2, _module.Invocations["openData"].Single().Arguments[2]);
  }

  [TestMethod]
  public void APageAfterTheLast_ShowsTheLast_AndSaysSo() {
    int changed = 0;
    IRenderedComponent<PixataPdfViewer> viewer = Render(p => p.Add(c => c.Page, 9).Add(c => c.PageChanged, EventCallback.Factory.Create<int>(this, page => changed = page)));

    viewer.InvokeAsync(() => viewer.Instance.Loaded(3));

    Assert.AreEqual(3, viewer.Instance.CurrentPage);
    Assert.AreEqual(3, changed);
  }

  // ---------- Zoom ----------

  [TestMethod]
  public void Zoom_IsSentToTheScript_AndShownInTheDropDown() {
    IRenderedComponent<PixataPdfViewer> viewer = Loaded(Render(p => p.Add(c => c.Zoom, PixataPdfViewerZoom.FitPage)));

    Assert.AreEqual("page-fit", JsonDocument.Parse((string)_module.Invocations["create"].Single().Arguments[3]!).RootElement.GetProperty("zoom").GetString());
    Assert.AreEqual("page-fit", viewer.Find("select").GetAttribute("value"));

    viewer.Render(p => p.Add(c => c.Zoom, 1.5m));
    Assert.AreEqual("1.5", _module.Invocations["setZoom"].Single().Arguments[1]);
  }

  [TestMethod]
  public void ChoosingAZoom_SetsIt() {
    IRenderedComponent<PixataPdfViewer> viewer = Loaded(Render());

    viewer.Find("select").Change("0.75");
    Button(viewer, "Zoom in").Click();

    Assert.AreEqual("0.75", _module.Invocations["setZoom"].Single().Arguments[1]);
    Assert.AreEqual(1, _module.Invocations["zoomBy"].Single().Arguments[1]);
  }

  [TestMethod]
  public void AScaleThatIsntInTheList_IsAddedToIt() {
    IRenderedComponent<PixataPdfViewer> viewer = Loaded(Render());

    viewer.InvokeAsync(() => viewer.Instance.ScaleChanged(1.234, null));

    Assert.AreEqual("1.23", viewer.Find("select").GetAttribute("value"));
    Assert.IsTrue(viewer.FindAll("option").Any(o => o.GetAttribute("value") == "1.23" && o.TextContent == (123m).ToString("0") + "%"));

    viewer.InvokeAsync(() => viewer.Instance.ScaleChanged(0.8, "page-width"));
    Assert.AreEqual("page-width", viewer.Find("select").GetAttribute("value"));
    Assert.IsFalse(viewer.FindAll("option").Any(o => o.GetAttribute("value") == "1.23"));
  }

  [TestMethod]
  public void ZoomValues_AreTidied() {
    Assert.AreEqual("1.5", PixataPdfViewerZoom.Scale(1.50m).Value);
    Assert.AreEqual("25", PixataPdfViewerZoom.Scale(100m).Value);
    Assert.AreEqual("0.1", ((PixataPdfViewerZoom)0.01).Value);
    Assert.AreEqual(PixataPdfViewerZoom.FitWidth, default);
    Assert.IsTrue(PixataPdfViewerZoom.FitPage.IsFit);
    Assert.AreEqual(2m, PixataPdfViewerZoom.Scale(2).ScaleValue);
  }

  // ---------- Rotate, search, download, print ----------

  [TestMethod]
  public void TheRotateButtons_TurnThePages() {
    IRenderedComponent<PixataPdfViewer> viewer = Loaded(Render());

    Button(viewer, "Rotate clockwise").Click();
    Button(viewer, "Rotate anticlockwise").Click();

    CollectionAssert.AreEqual(new object[] { 1, -1 }, _module.Invocations["rotate"].Select(i => i.Arguments[1]!).ToArray());
  }

  [TestMethod]
  public void Searching_FindsTheText_AndShowsTheMatches() {
    IRenderedComponent<PixataPdfViewer> viewer = Loaded(Render());
    IElement box = viewer.Find("input[type=search]");

    box.Input("payment");
    viewer.InvokeAsync(() => viewer.Instance.FindChanged(2, 5));
    Assert.AreEqual("2 of 5", viewer.Find(".pixata-blazor-pdfviewer-matches").TextContent);

    box.KeyDown("Enter");
    box.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter", ShiftKey = true });
    viewer.InvokeAsync(() => viewer.Instance.FindChanged(0, 0));
    Assert.AreEqual("No matches", viewer.Find(".pixata-blazor-pdfviewer-matches").TextContent);

    // The query, whether it's searching again, and whether it's going backwards
    string[] calls = [.. _module.Invocations["find"].Select(i => $"{i.Arguments[1]} {i.Arguments[2]} {i.Arguments[3]}")];
    CollectionAssert.AreEqual(new[] { "payment False False", "payment True False", "payment True True" }, calls);
  }

  [TestMethod]
  public void ClearingTheSearch_HidesTheMatches() {
    IRenderedComponent<PixataPdfViewer> viewer = Loaded(Render());
    viewer.Find("input[type=search]").Input("payment");

    viewer.Find("input[type=search]").Input("");

    Assert.AreEqual(0, viewer.FindAll(".pixata-blazor-pdfviewer-matches").Count);
    Assert.AreEqual("", _module.Invocations["find"].Last().Arguments[1]);
  }

  [TestMethod]
  public void Download_UsesTheFileName_WhichOnDownloadCanChange() {
    IRenderedComponent<PixataPdfViewer> viewer = Loaded(Render(p => p
      .Add(c => c.FileName, "INV-1042.pdf")
      .Add(c => c.OnDownload, EventCallback.Factory.Create<PixataPdfViewerDownloadEventArgs>(this, e => e.FileName = $"Copy of {e.FileName}"))));

    Button(viewer, "Download").Click();

    Assert.AreEqual("Copy of INV-1042.pdf", _module.Invocations["download"].Single().Arguments[1]);
  }

  [TestMethod]
  public void OnDownload_CanCancelIt() {
    IRenderedComponent<PixataPdfViewer> viewer = Loaded(Render(p => p
      .Add(c => c.OnDownload, EventCallback.Factory.Create<PixataPdfViewerDownloadEventArgs>(this, e => e.IsCancelled = true))));

    Button(viewer, "Download").Click();

    Assert.AreEqual(0, _module.Invocations["download"].Count);
  }

  [TestMethod]
  public void Print_AsksTheScriptToPrint() {
    IRenderedComponent<PixataPdfViewer> viewer = Loaded(Render());

    Button(viewer, "Print").Click();

    Assert.AreEqual(Key(), _module.Invocations["print"].Single().Arguments[0]);
  }

  // ---------- Appearance ----------

  [TestMethod]
  public void TheToolbarsParts_CanBeTurnedOff() {
    IRenderedComponent<PixataPdfViewer> viewer = Render(p => p
      .Add(c => c.ShowSearch, false)
      .Add(c => c.ShowRotate, false)
      .Add(c => c.ShowDownload, false)
      .Add(c => c.ShowPrint, false));

    Assert.AreEqual(0, viewer.FindAll("input[type=search]").Count);
    Assert.AreEqual(0, viewer.FindAll("button[aria-label^='Rotate'], button[aria-label='Download'], button[aria-label='Print']").Count);

    viewer.Render(p => p.Add(c => c.ShowToolbar, false));
    Assert.AreEqual(0, viewer.FindAll(".pixata-blazor-pdfviewer-toolbar").Count);
    Assert.AreEqual(1, viewer.FindAll(".pixata-blazor-pdfviewer-container").Count);
  }

  [TestMethod]
  public void SizeClassAndId_AreUsed() {
    IRenderedComponent<PixataPdfViewer> viewer = Render(p => p.Add(c => c.Height, "600px").Add(c => c.Width, "50%").Add(c => c.Class, "invoice").Add(c => c.Id, "viewer"));

    IElement outer = viewer.Find(".pixata-blazor-pdfviewer");
    Assert.AreEqual("height: 600px; width: 50%", outer.GetAttribute("style"));
    Assert.AreEqual("pixata-blazor-pdfviewer invoice", outer.ClassName);
    Assert.AreEqual("viewer", outer.Id);
  }

  [TestMethod]
  public void Messages_TranslateTheText() {
    IRenderedComponent<PixataPdfViewer> viewer = Loaded(Render(p => p.Add(c => c.Messages, new PixataPdfViewerMessages { NextPage = "Page suivante", PageCountFormat = "sur {0}" })));

    Assert.IsNotNull(Button(viewer, "Page suivante"));
    Assert.AreEqual("sur 3", viewer.Find(".pixata-blazor-pdfviewer-page-count").TextContent);
  }

  [TestMethod]
  public void Disposing_DestroysTheViewer() {
    IRenderedComponent<PixataPdfViewer> viewer = Render();

    viewer.InvokeAsync(() => viewer.Instance.DisposeAsync().AsTask());

    Assert.AreEqual(Key(), _module.Invocations["destroy"].Single().Arguments[0]);
  }
}
