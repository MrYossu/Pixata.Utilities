using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests.Components;

[TestClass]
public class TreeView_Tests {
  private class FlatItem {
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string Text { get; set; } = "";
    public bool Disabled { get; set; }
    public string? Url { get; set; }
  }

  // Has a HasChildren property, so children can be loaded on demand
  private class LazyItem {
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string Text { get; set; } = "";
    public bool HasChildren { get; set; }
  }

  private class Folder {
    public string Name { get; set; } = "";
    public List<Document> Documents { get; set; } = [];
  }

  private class Document {
    public string Title { get; set; } = "";
  }

  private class HierarchicalItem {
    public string Text { get; set; } = "";
    public List<HierarchicalItem>? Items { get; set; }
  }

  // 1 Animals (2 Mammals (4 Cat, 5 Dog), 3 Birds (6 Owl)), 7 Plants
  private static List<FlatItem> FlatData() => [
    new() { Id = 1, Text = "Animals" },
    new() { Id = 2, ParentId = 1, Text = "Mammals" },
    new() { Id = 3, ParentId = 1, Text = "Birds" },
    new() { Id = 4, ParentId = 2, Text = "Cat" },
    new() { Id = 5, ParentId = 2, Text = "Dog" },
    new() { Id = 6, ParentId = 3, Text = "Owl" },
    new() { Id = 7, Text = "Plants" }
  ];

  private static IRenderedComponent<PixataTreeView> Render(BunitContext context, IEnumerable<object>? data, Action<ComponentParameterCollectionBuilder<PixataTreeView>>? more = null) =>
    context.Render<PixataTreeView>(p => {
      p.Add(c => c.Data, data);
      more?.Invoke(p);
    });

  private static string[] Texts(IRenderedComponent<PixataTreeView> cut) =>
    [.. cut.FindAll("[role=treeitem]").Select(li => li.QuerySelector(".pixata-blazor-treeview-leaf")!.TextContent.Trim())];

  private static IElement Item(IRenderedComponent<PixataTreeView> cut, string text) =>
    cut.FindAll("[role=treeitem]").First(li => li.QuerySelector(".pixata-blazor-treeview-leaf")!.TextContent.Trim() == text);

  private static IElement Leaf(IRenderedComponent<PixataTreeView> cut, string text) =>
    Item(cut, text).QuerySelector(".pixata-blazor-treeview-leaf")!;

  private static void Toggle(IRenderedComponent<PixataTreeView> cut, string text) =>
    Item(cut, text).QuerySelector(".pixata-blazor-treeview-toggle")!.Click();

  private static void Press(IRenderedComponent<PixataTreeView> cut, string key, bool shift = false, bool ctrl = false) =>
    cut.Find("[role=tree]").KeyDown(new KeyboardEventArgs { Key = key, ShiftKey = shift, CtrlKey = ctrl });

  // Each binding is given as its parameters' names and values
  private static RenderFragment Bindings(params Dictionary<string, object?>[] bindings) =>
    builder => {
      foreach (Dictionary<string, object?> binding in bindings) {
        builder.OpenComponent<PixataTreeViewBinding>(0);
        builder.AddMultipleAttributes(1, binding!);
        builder.CloseComponent();
      }
    };

  // ---------- Data ----------

  [TestMethod]
  public void FlatData_ShowsTheRootItemsWithTheirRoles() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataTreeView> cut = Render(context, FlatData());
    CollectionAssert.AreEqual(new[] { "Animals", "Plants" }, Texts(cut));
    IElement animals = Item(cut, "Animals");
    Assert.AreEqual("1", animals.GetAttribute("aria-level"));
    Assert.AreEqual("1", animals.GetAttribute("aria-posinset"));
    Assert.AreEqual("2", animals.GetAttribute("aria-setsize"));
    Assert.AreEqual("false", animals.GetAttribute("aria-expanded"));
    Assert.IsNotNull(animals.QuerySelector(".pixata-blazor-treeview-toggle"));
    IElement plants = Item(cut, "Plants");
    Assert.IsNull(plants.GetAttribute("aria-expanded"));
    Assert.IsNull(plants.QuerySelector(".pixata-blazor-treeview-toggle"));
    Assert.IsTrue(cut.Find(".pixata-blazor-treeview").ClassList.Contains("pixata-blazor-treeview-md"));
  }

  [TestMethod]
  public void ClickingTheArrow_ExpandsAndCollapses() {
    using BunitContext context = PixataTestContext.Create();
    IEnumerable<object>? expanded = null;
    List<PixataTreeViewExpandEventArgs> expands = [];
    List<FlatItem> data = FlatData();
    IRenderedComponent<PixataTreeView> cut = Render(context, data, p => p
      .Add(c => c.ExpandedItemsChanged, (IEnumerable<object> items) => expanded = items)
      .Add(c => c.OnExpand, (PixataTreeViewExpandEventArgs e) => expands.Add(e)));
    Toggle(cut, "Animals");
    CollectionAssert.AreEqual(new[] { "Animals", "Mammals", "Birds", "Plants" }, Texts(cut));
    Assert.AreEqual("true", Item(cut, "Animals").GetAttribute("aria-expanded"));
    Assert.AreEqual("2", Item(cut, "Mammals").GetAttribute("aria-level"));
    CollectionAssert.AreEqual(new object[] { data[0] }, expanded!.ToList());
    Assert.IsTrue(expands[0].Expanded);
    Assert.AreSame(data[0], expands[0].Item);
    Toggle(cut, "Animals");
    CollectionAssert.AreEqual(new[] { "Animals", "Plants" }, Texts(cut));
    Assert.IsFalse(expands[1].Expanded);
    Assert.AreEqual(0, expanded!.Count());
  }

  [TestMethod]
  public void ExpandedItems_ExpandsThoseItems() {
    using BunitContext context = PixataTestContext.Create();
    List<FlatItem> data = FlatData();
    IRenderedComponent<PixataTreeView> cut = Render(context, data, p => p.Add(c => c.ExpandedItems, new List<object> { data[0], data[1] }));
    CollectionAssert.AreEqual(new[] { "Animals", "Mammals", "Cat", "Dog", "Birds", "Plants" }, Texts(cut));
  }

  [TestMethod]
  public void HierarchicalData_ReadsTheChildrenFromItems() {
    using BunitContext context = PixataTestContext.Create();
    List<HierarchicalItem> data = [
      new() { Text = "Fruit", Items = [new() { Text = "Apple" }, new() { Text = "Pear" }] },
      new() { Text = "Empty", Items = [] }
    ];
    IRenderedComponent<PixataTreeView> cut = Render(context, data, p => p.Add(c => c.ExpandedItems, new List<object> { data[0] }));
    CollectionAssert.AreEqual(new[] { "Fruit", "Apple", "Pear", "Empty" }, Texts(cut));
    Assert.IsNull(Item(cut, "Empty").QuerySelector(".pixata-blazor-treeview-toggle"));
  }

  [TestMethod]
  public void Bindings_ReadOtherFieldsForEachLevelAndUseTheirTemplates() {
    using BunitContext context = PixataTestContext.Create();
    List<Folder> data = [new() { Name = "Reports", Documents = [new() { Title = "Q1" }, new() { Title = "Q2" }] }];
    RenderFragment<object> template = item => builder => builder.AddContent(0, $"[{((Document)item).Title}]");
    IRenderedComponent<PixataTreeView> cut = Render(context, data, p => p
      .Add(c => c.ExpandedItems, new List<object> { data[0] })
      .Add(c => c.TreeViewBindings, Bindings(
        new() { [nameof(PixataTreeViewBinding.Level)] = 0, [nameof(PixataTreeViewBinding.TextField)] = nameof(Folder.Name), [nameof(PixataTreeViewBinding.ItemsField)] = nameof(Folder.Documents) },
        new() { [nameof(PixataTreeViewBinding.Level)] = 1, [nameof(PixataTreeViewBinding.ItemTemplate)] = template })));
    CollectionAssert.AreEqual(new[] { "Reports", "[Q1]", "[Q2]" }, Texts(cut));
  }

  [TestMethod]
  public void ABindingWithoutALevel_AppliesToEveryLevel() {
    using BunitContext context = PixataTestContext.Create();
    var data = new[] { new { Key = 1, Up = (int?)null, Name = "Top" }, new { Key = 2, Up = (int?)1, Name = "Under" } };
    IRenderedComponent<PixataTreeView> cut = Render(context, data, p => p
      .Add(c => c.ExpandedItems, new List<object> { data[0] })
      .Add(c => c.TreeViewBindings, Bindings(new Dictionary<string, object?> { [nameof(PixataTreeViewBinding.IdField)] = "Key", [nameof(PixataTreeViewBinding.ParentIdField)] = "Up", [nameof(PixataTreeViewBinding.TextField)] = "Name" })));
    CollectionAssert.AreEqual(new[] { "Top", "Under" }, Texts(cut));
  }

  [TestMethod]
  public void ObservableData_RedrawsWhenItChanges() {
    using BunitContext context = PixataTestContext.Create();
    ObservableCollection<FlatItem> data = [new() { Id = 1, Text = "One" }];
    IRenderedComponent<PixataTreeView> cut = Render(context, data);
    cut.InvokeAsync(() => data.Add(new() { Id = 2, Text = "Two" }));
    cut.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "One", "Two" }, Texts(cut)));
  }

  [TestMethod]
  public async Task LoadOnDemand_ShowsLoadingUntilTheChildrenArrive() {
    using BunitContext context = PixataTestContext.Create();
    List<LazyItem> data = [new() { Id = 1, Text = "Server", HasChildren = true }];
    TaskCompletionSource loaded = new();
    IRenderedComponent<PixataTreeView> cut = Render(context, data, p => p.Add(c => c.OnExpand, async (PixataTreeViewExpandEventArgs e) => {
      await loaded.Task;
      data.Add(new() { Id = 2, ParentId = 1, Text = "Disk" });
    }));
    Assert.IsNotNull(Item(cut, "Server").QuerySelector(".pixata-blazor-treeview-toggle"));
    Toggle(cut, "Server");
    Assert.IsNotNull(cut.Find(".pixata-blazor-treeview-loading"));
    Assert.AreEqual("true", Item(cut, "Server").GetAttribute("aria-busy"));
    await cut.InvokeAsync(loaded.SetResult);
    cut.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "Server", "Disk" }, Texts(cut)));
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-treeview-loading").Count);
  }

  [TestMethod]
  public void Rebind_ShowsChangesMadeElsewhere() {
    using BunitContext context = PixataTestContext.Create();
    List<FlatItem> data = [new() { Id = 1, Text = "One" }];
    IRenderedComponent<PixataTreeView> cut = Render(context, data);
    data.Add(new() { Id = 2, Text = "Two" });
    cut.InvokeAsync(cut.Instance.Rebind);
    CollectionAssert.AreEqual(new[] { "One", "Two" }, Texts(cut));
  }

  [TestMethod]
  public void OnItemRender_AddsClasses() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataTreeView> cut = Render(context, FlatData(), p => p.Add(c => c.OnItemRender, (Action<PixataTreeViewItemRenderEventArgs>)(e => e.Class = ((FlatItem)e.Item).Id == 7 ? "green" : null)));
    Assert.IsTrue(Item(cut, "Plants").QuerySelector(".pixata-blazor-treeview-content")!.ClassList.Contains("green"));
    Assert.IsFalse(Item(cut, "Animals").QuerySelector(".pixata-blazor-treeview-content")!.ClassList.Contains("green"));
  }

  // ---------- Selection ----------

  [TestMethod]
  public void SingleSelection_SelectsTheClickedItem() {
    using BunitContext context = PixataTestContext.Create();
    List<FlatItem> data = FlatData();
    IEnumerable<object>? selected = null;
    PixataTreeViewItemClickEventArgs? clicked = null;
    IRenderedComponent<PixataTreeView> cut = Render(context, data, p => p
      .Add(c => c.SelectionMode, PixataTreeViewSelectionMode.Single)
      .Add(c => c.SelectedItemsChanged, (IEnumerable<object> items) => selected = items)
      .Add(c => c.OnItemClick, (PixataTreeViewItemClickEventArgs e) => clicked = e));
    Leaf(cut, "Plants").Click();
    Assert.AreSame(data[6], clicked!.Item);
    CollectionAssert.AreEqual(new object[] { data[6] }, selected!.ToList());
    Assert.AreEqual("true", Item(cut, "Plants").GetAttribute("aria-selected"));
    Assert.IsTrue(Leaf(cut, "Plants").ClassList.Contains("pixata-blazor-selected"));
    Leaf(cut, "Animals").Click();
    CollectionAssert.AreEqual(new object[] { data[0] }, selected!.ToList());
    Assert.AreEqual("false", Item(cut, "Plants").GetAttribute("aria-selected"));
  }

  [TestMethod]
  public void MultipleSelection_UsesCtrlAndShift() {
    using BunitContext context = PixataTestContext.Create();
    List<FlatItem> data = FlatData();
    IEnumerable<object> selected = [];
    IRenderedComponent<PixataTreeView> cut = Render(context, data, p => p
      .Add(c => c.SelectionMode, PixataTreeViewSelectionMode.Multiple)
      .Add(c => c.ExpandedItems, new List<object> { data[0] })
      .Add(c => c.SelectedItemsChanged, (IEnumerable<object> items) => selected = items));
    Assert.AreEqual("true", cut.Find("[role=tree]").GetAttribute("aria-multiselectable"));
    Leaf(cut, "Animals").Click();
    Leaf(cut, "Birds").Click(new MouseEventArgs { CtrlKey = true });
    CollectionAssert.AreEquivalent(new object[] { data[0], data[2] }, selected.ToList());
    Leaf(cut, "Plants").Click(new MouseEventArgs { ShiftKey = true });
    // From Birds, the last one picked, to Plants
    CollectionAssert.AreEquivalent(new object[] { data[2], data[6] }, selected.ToList());
    Leaf(cut, "Birds").Click(new MouseEventArgs { CtrlKey = true });
    CollectionAssert.AreEquivalent(new object[] { data[6] }, selected.ToList());
  }

  [TestMethod]
  public void SelectedItems_OneWayWithAHandler_OnlyChangesWhenTheParentSetsIt() {
    using BunitContext context = PixataTestContext.Create();
    List<FlatItem> data = FlatData();
    int raised = 0;
    IRenderedComponent<PixataTreeView> cut = Render(context, data, p => p
      .Add(c => c.SelectionMode, PixataTreeViewSelectionMode.Single)
      .Add(c => c.SelectedItems, new List<object> { data[0] })
      .Add(c => c.SelectedItemsChanged, (IEnumerable<object> _) => raised++));
    Leaf(cut, "Plants").Click();
    Assert.AreEqual(1, raised);
    // The handler's parent redraws without setting SelectedItems, so the old selection comes back
    cut.Render(p => p.Add(c => c.SelectedItems, cut.Instance.SelectedItems));
    Assert.AreEqual("true", Item(cut, "Animals").GetAttribute("aria-selected"));
  }

  [TestMethod]
  public void DisabledItems_CantBeSelectedOrClicked() {
    using BunitContext context = PixataTestContext.Create();
    List<FlatItem> data = [new() { Id = 1, Text = "Off", Disabled = true }];
    bool clicked = false;
    IRenderedComponent<PixataTreeView> cut = Render(context, data, p => p
      .Add(c => c.SelectionMode, PixataTreeViewSelectionMode.Single)
      .Add(c => c.OnItemClick, (PixataTreeViewItemClickEventArgs _) => clicked = true));
    Leaf(cut, "Off").Click();
    Assert.IsFalse(clicked);
    Assert.AreEqual("false", Item(cut, "Off").GetAttribute("aria-selected"));
    Assert.AreEqual("true", Item(cut, "Off").GetAttribute("aria-disabled"));
  }

  // ---------- Checkboxes ----------

  private static void Check(IRenderedComponent<PixataTreeView> cut, string text) =>
    Item(cut, text).QuerySelector(".pixata-blazor-treeview-checkbox")!.Click();

  private static string? Checked(IRenderedComponent<PixataTreeView> cut, string text) =>
    Item(cut, text).GetAttribute("aria-checked");

  [TestMethod]
  public void CheckChildrenAndCheckParents_KeepTheTreeConsistent() {
    using BunitContext context = PixataTestContext.Create();
    List<FlatItem> data = FlatData();
    IEnumerable<object> checkedItems = [];
    IRenderedComponent<PixataTreeView> cut = Render(context, data, p => p
      .Add(c => c.CheckBoxMode, PixataTreeViewCheckBoxMode.Multiple)
      .Add(c => c.CheckChildren, true)
      .Add(c => c.CheckParents, true)
      .Add(c => c.ExpandedItems, new List<object> { data[0], data[1] })
      .Add(c => c.CheckedItemsChanged, (IEnumerable<object> items) => checkedItems = items));
    Check(cut, "Mammals");
    CollectionAssert.AreEquivalent(new object[] { data[1], data[3], data[4] }, checkedItems.ToList());
    Assert.AreEqual("true", Checked(cut, "Cat"));
    Assert.AreEqual("mixed", Checked(cut, "Animals"));
    Assert.IsNotNull(Item(cut, "Animals").QuerySelector(".pixata-blazor-checkbox-indeterminate"));
    // Checking Birds checks its child Owl, and now all of Animals' children are checked, so it is too
    Check(cut, "Birds");
    Assert.AreEqual("true", Checked(cut, "Animals"));
    CollectionAssert.Contains(checkedItems.ToList(), data[5]);
    Check(cut, "Cat");
    Assert.AreEqual("false", Checked(cut, "Cat"));
    Assert.AreEqual("mixed", Checked(cut, "Mammals"));
    Assert.AreEqual("mixed", Checked(cut, "Animals"));
    CollectionAssert.DoesNotContain(checkedItems.ToList(), data[0]);
  }

  [TestMethod]
  public void SingleCheckBoxMode_ChecksOneItemAtATime() {
    using BunitContext context = PixataTestContext.Create();
    List<FlatItem> data = FlatData();
    IEnumerable<object> checkedItems = [];
    IRenderedComponent<PixataTreeView> cut = Render(context, data, p => p
      .Add(c => c.CheckBoxMode, PixataTreeViewCheckBoxMode.Single)
      .Add(c => c.CheckedItemsChanged, (IEnumerable<object> items) => checkedItems = items));
    Check(cut, "Animals");
    Check(cut, "Plants");
    CollectionAssert.AreEqual(new object[] { data[6] }, checkedItems.ToList());
    Assert.AreEqual("false", Checked(cut, "Animals"));
  }

  [TestMethod]
  public void CheckOnClick_ChecksTheClickedItem() {
    using BunitContext context = PixataTestContext.Create();
    List<FlatItem> data = FlatData();
    IRenderedComponent<PixataTreeView> cut = Render(context, data, p => p
      .Add(c => c.CheckBoxMode, PixataTreeViewCheckBoxMode.Multiple)
      .Add(c => c.CheckOnClick, true));
    Leaf(cut, "Plants").Click();
    Assert.AreEqual("true", Checked(cut, "Plants"));
  }

  // ---------- Keyboard ----------

  [TestMethod]
  public void Keyboard_MovesExpandsAndCollapses() {
    using BunitContext context = PixataTestContext.Create();
    List<FlatItem> data = FlatData();
    IRenderedComponent<PixataTreeView> cut = Render(context, data);
    Assert.AreEqual("0", Item(cut, "Animals").GetAttribute("tabindex"));
    Assert.AreEqual("-1", Item(cut, "Plants").GetAttribute("tabindex"));
    Item(cut, "Animals").Focus();
    Press(cut, "ArrowRight");
    CollectionAssert.AreEqual(new[] { "Animals", "Mammals", "Birds", "Plants" }, Texts(cut));
    Press(cut, "ArrowRight");
    Assert.AreEqual("0", Item(cut, "Mammals").GetAttribute("tabindex"));
    Assert.IsTrue(context.JSInterop.Invocations.Any(i => i.Identifier == "focusItem" && Equals(i.Arguments[1], "0_0")));
    Press(cut, "ArrowDown");
    Assert.AreEqual("0", Item(cut, "Birds").GetAttribute("tabindex"));
    Press(cut, "ArrowLeft");
    Assert.AreEqual("0", Item(cut, "Animals").GetAttribute("tabindex"));
    Press(cut, "ArrowLeft");
    CollectionAssert.AreEqual(new[] { "Animals", "Plants" }, Texts(cut));
    Press(cut, "End");
    Assert.AreEqual("0", Item(cut, "Plants").GetAttribute("tabindex"));
    Press(cut, "Home");
    Assert.AreEqual("0", Item(cut, "Animals").GetAttribute("tabindex"));
  }

  [TestMethod]
  public void EnterAndSpace_SelectClickAndCheck() {
    using BunitContext context = PixataTestContext.Create();
    List<FlatItem> data = FlatData();
    IEnumerable<object> selected = [];
    IEnumerable<object> checkedItems = [];
    PixataTreeViewItemClickEventArgs? clicked = null;
    IRenderedComponent<PixataTreeView> cut = Render(context, data, p => p
      .Add(c => c.SelectionMode, PixataTreeViewSelectionMode.Single)
      .Add(c => c.CheckBoxMode, PixataTreeViewCheckBoxMode.Multiple)
      .Add(c => c.SelectedItemsChanged, (IEnumerable<object> items) => selected = items)
      .Add(c => c.CheckedItemsChanged, (IEnumerable<object> items) => checkedItems = items)
      .Add(c => c.OnItemClick, (PixataTreeViewItemClickEventArgs e) => clicked = e));
    Press(cut, "ArrowDown");
    Press(cut, "Enter");
    CollectionAssert.AreEqual(new object[] { data[6] }, selected.ToList());
    Assert.IsInstanceOfType<KeyboardEventArgs>(clicked!.EventArgs);
    Press(cut, " ");
    CollectionAssert.AreEqual(new object[] { data[6] }, checkedItems.ToList());
  }

  [TestMethod]
  public void Urls_RenderLinksAndEnterNavigates() {
    using BunitContext context = PixataTestContext.Create();
    List<FlatItem> data = [new() { Id = 1, Text = "Home", Url = "home" }];
    IRenderedComponent<PixataTreeView> cut = Render(context, data);
    IElement link = cut.Find("a.pixata-blazor-treeview-leaf");
    Assert.AreEqual("home", link.GetAttribute("href"));
    Press(cut, "Enter");
    Assert.AreEqual("http://localhost/home", context.Services.GetRequiredService<NavigationManager>().Uri);
  }

  // ---------- Dragging ----------

  [TestMethod]
  public async Task Drop_RaisesOnDropAndOnDragEndWithTheDestination() {
    using BunitContext context = PixataTestContext.Create();
    List<FlatItem> data = FlatData();
    PixataTreeViewDropEventArgs? dropped = null;
    PixataTreeViewDragEndEventArgs? ended = null;
    IRenderedComponent<PixataTreeView> cut = Render(context, data, p => p
      .Add(c => c.Draggable, true)
      .Add(c => c.ExpandedItems, new List<object> { data[0] })
      .Add(c => c.OnDrop, (PixataTreeViewDropEventArgs e) => dropped = e)
      .Add(c => c.OnDragEnd, (PixataTreeViewDragEndEventArgs e) => ended = e));
    Assert.IsTrue(cut.Find(".pixata-blazor-treeview").ClassList.Contains("pixata-blazor-treeview-draggable"));
    using DotNetObjectReference<PixataTreeView> self = DotNetObjectReference.Create(cut.Instance);
    // Plants (index 1) dropped before Birds (the second child of the first root)
    await cut.InvokeAsync(() => cut.Instance.Drop("1", self, "0_1", "tree", "Before", true));
    Assert.AreSame(data[6], dropped!.Item);
    Assert.AreSame(data[2], dropped.DestinationItem);
    Assert.AreSame(cut.Instance, dropped.DestinationTreeView);
    Assert.AreEqual(PixataTreeViewDropPosition.Before, dropped.DropPosition);
    Assert.AreEqual("0_1", dropped.DestinationIndex);
    Assert.AreSame(data[6], ended!.Item);
    Assert.AreSame(data[2], cut.Instance.GetItemFromDropIndex("0_1"));
    Assert.IsNull(cut.Instance.GetItemFromDropIndex("9"));
  }

  [TestMethod]
  public async Task ACancelledDrop_OnlyRaisesOnDragEnd() {
    using BunitContext context = PixataTestContext.Create();
    bool dropped = false;
    PixataTreeViewDragEndEventArgs? ended = null;
    IRenderedComponent<PixataTreeView> cut = Render(context, FlatData(), p => p
      .Add(c => c.Draggable, true)
      .Add(c => c.OnDrop, (PixataTreeViewDropEventArgs _) => dropped = true)
      .Add(c => c.OnDragEnd, (PixataTreeViewDragEndEventArgs e) => ended = e));
    await cut.InvokeAsync(() => cut.Instance.Drop("1", null, null, null, "Over", false));
    Assert.IsFalse(dropped);
    Assert.IsNotNull(ended);
    Assert.IsNull(ended.DestinationTreeView);
  }

  [TestMethod]
  public async Task OnDragStart_CanCancelTheDrag() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataTreeView> cut = Render(context, FlatData(), p => p
      .Add(c => c.Draggable, true)
      .Add(c => c.OnDragStart, (PixataTreeViewDragStartEventArgs e) => e.IsCancelled = ((FlatItem)e.Item).Id == 7));
    Assert.IsTrue(await cut.InvokeAsync(() => cut.Instance.DragStart("0")));
    Assert.IsFalse(await cut.InvokeAsync(() => cut.Instance.DragStart("1")));
  }

  // ---------- Field reader ----------

  [TestMethod]
  public void TryGetValue_SaysWhetherTheFieldExists() {
    FlatItem item = new() { Id = 3, Text = "Three" };
    Assert.IsTrue(PixataFieldReader.TryGetValue(item, "Text", out object? text));
    Assert.AreEqual("Three", text);
    Assert.IsTrue(PixataFieldReader.TryGetValue(item, "ParentId", out object? parentId));
    Assert.IsNull(parentId);
    Assert.IsFalse(PixataFieldReader.TryGetValue(item, "Missing", out _));
    Assert.IsFalse(PixataFieldReader.TryGetValue(new Dictionary<string, object?>(), "Missing", out _));
  }
}
