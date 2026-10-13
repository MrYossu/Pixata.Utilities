using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Pixata.Extensions.Grid;

namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// What a column's <c>FooterTemplate</c> is given: the column's aggregates over all the data (not just the current page)
/// </summary>
public class PixataGridAggregateTemplateContext {
  /// <summary>
  /// The column's field
  /// </summary>
  public string? Field { get; init; }

  public object? Count { get; init; }
  public object? Sum { get; init; }
  public object? Average { get; init; }
  public object? Min { get; init; }
  public object? Max { get; init; }

  /// <summary>
  /// The aggregates for every field, for showing one column's aggregates in another's template
  /// </summary>
  public Dictionary<string, GridFieldAggregates> AggregateResults { get; init; } = [];

  internal static T For<T>(string? field, Dictionary<string, GridFieldAggregates>? aggregates) where T : PixataGridAggregateTemplateContext, new() {
    GridFieldAggregates? own = field is not null && aggregates?.TryGetValue(field, out GridFieldAggregates? found) == true ? found : null;
    return new() {
      Field = field,
      Count = own?.Count,
      Sum = own?.Sum,
      Average = own?.Average,
      Min = own?.Min,
      Max = own?.Max,
      AggregateResults = aggregates ?? []
    };
  }
}

/// <summary>
/// What a column's <c>GroupHeaderTemplate</c> and <c>GroupFooterTemplate</c> are given: the group's value, and the column's
/// aggregates over the whole group
/// </summary>
public class PixataGridGroupTemplateContext : PixataGridAggregateTemplateContext {
  /// <summary>
  /// The value the items in the group share
  /// </summary>
  public object? Value { get; init; }

  /// <summary>
  /// The field the data is grouped by
  /// </summary>
  public string? GroupField { get; init; }

  /// <summary>
  /// The number of items in the whole group
  /// </summary>
  public int ItemsCount { get; init; }
}

/// <summary>
/// Asks the grid to calculate an aggregate of a field, for the column templates. Goes inside the grid's
/// <c>GridAggregates</c>
/// </summary>
public class PixataGridAggregate<TItem> : PixataGridSettingsBase<TItem> {
  [Parameter]
  public string Field { get; set; } = "";

  [Parameter]
  public GridAggregateType Aggregate { get; set; }

  internal override void Register(PixataGrid<TItem> grid, bool add) {
    if (add) {
      grid.AddAggregate(this);
    } else {
      grid.RemoveAggregate(this);
    }
  }
}

public partial class PixataGrid<TItem> {
  /// <summary>
  /// Shows a panel above the grid, into which the user can drag column headers to group the rows
  /// </summary>
  [Parameter]
  public bool Groupable { get; set; }

  /// <summary>
  /// The aggregates to calculate, as <see cref="PixataGridAggregate{TItem}"/>s, for use in the columns' footer and group
  /// templates
  /// </summary>
  [Parameter]
  public RenderFragment? GridAggregates { get; set; }

  private readonly List<GridGroupDescriptor> _groups = [];
  private readonly List<PixataGridAggregate<TItem>> _aggregateComponents = [];
  private readonly HashSet<string> _collapsedGroups = [];
  private List<GridGroup<TItem>>? _resultGroups;
  private Dictionary<string, GridFieldAggregates>? _resultAggregates;
  private int _rowIndex;
  private List<GridAggregateDescriptor> _requestedAggregates = [];
  private bool _aggregatesCheckQueued;

  internal void AddAggregate(PixataGridAggregate<TItem> aggregate) {
    _aggregateComponents.Add(aggregate);
    QueueAggregatesCheck();
  }

  internal void RemoveAggregate(PixataGridAggregate<TItem> aggregate) {
    _aggregateComponents.Remove(aggregate);
    QueueAggregatesCheck();
  }

  // The aggregates register after the data has first been read, so it's read again if they aren't what it was read with.
  // This is queued so that several registering at once only cause one read
  private void QueueAggregatesCheck() {
    if (_aggregatesCheckQueued || _disposed) {
      return;
    }
    _aggregatesCheckQueued = true;
    _ = InvokeAsync(() => {
      _aggregatesCheckQueued = false;
      if (!_disposed && !AggregateDescriptors.SequenceEqual(_requestedAggregates)) {
        ProcessData();
        StateHasChanged();
      }
    });
  }

  // The number of group columns down the left of the table
  private int GroupLevels =>
    _groups.Count;

  private List<GridAggregateDescriptor> AggregateDescriptors =>
    [.. _aggregateComponents.Where(a => !string.IsNullOrWhiteSpace(a.Field)).Select(a => new GridAggregateDescriptor(a.Field, a.Aggregate)).Distinct()];

  private bool CanGroup(PixataGridColumn<TItem> column) =>
    Groupable && column.Groupable && !string.IsNullOrWhiteSpace(column.Field)
    && column is not PixataGridCommandColumn<TItem> and not PixataGridCheckboxColumn<TItem>;

  private bool HasGroupFooters(List<PixataGridColumn<TItem>> columns) =>
    columns.Any(c => c.GroupFooterTemplate is not null);

  private bool HasFooter(List<PixataGridColumn<TItem>> columns) =>
    columns.Any(c => c.FooterTemplate is not null);

  /// <summary>
  /// Groups the rows by a field, as dragging its header into the group panel does
  /// </summary>
  public void GroupBy(string field) {
    if (_groups.Any(g => g.Member == field)) {
      return;
    }
    _groups.Add(new(field));
    GroupsChanged();
  }

  /// <summary>
  /// Stops grouping by a field
  /// </summary>
  public void Ungroup(string field) {
    if (_groups.RemoveAll(g => g.Member == field) > 0) {
      GroupsChanged();
    }
  }

  private void GroupsChanged() {
    _collapsedGroups.Clear();
    if (_page != 1) {
      _page = 1;
      _ = PageChanged.InvokeAsync(1);
    }
    ProcessData();
    StateHasChanged();
  }

  // ---------- Dragging headers into the group panel ----------

  private bool _groupDragReady;

  // The dragging is done by the JavaScript, which calls GroupDrop at the end
  private async Task InitGroupDrag() {
    if (!Groupable || _groupDragReady || _disposed) {
      return;
    }
    _groupDragReady = true;
    try {
      _module ??= await Js.InvokeAsync<IJSObjectReference>("import", PixataBlazorScript.Path);
      _self ??= DotNetObjectReference.Create(this);
      await _module.InvokeVoidAsync("initGroupDrag", _gridId, _self);
    } catch (JSDisconnectedException) {
      // The circuit has gone
    }
  }

  /// <summary>
  /// Called by the JavaScript when the user drops a header or group chip. A header dropped on the group panel groups by its
  /// field, before the chip it was dropped on, if any. A chip dropped on another chip moves there, and a chip dropped outside
  /// the panel is removed
  /// </summary>
  [JSInvokable]
  [EditorBrowsable(EditorBrowsableState.Never)]
  public void GroupDrop(string? field, int from, int to, bool onPanel) {
    if (field is not null) {
      if (onPanel && _groups.All(g => g.Member != field)) {
        _groups.Insert(to >= 0 && to < _groups.Count ? to : _groups.Count, new(field));
        GroupsChanged();
      }
    } else if (from >= 0 && from < _groups.Count) {
      if (!onPanel) {
        _groups.RemoveAt(from);
        GroupsChanged();
      } else if (to >= 0 && to < _groups.Count && to != from) {
        GridGroupDescriptor moved = _groups[from];
        _groups.RemoveAt(from);
        _groups.Insert(to, moved);
        GroupsChanged();
      }
    }
  }

  // ---------- Expanding and collapsing ----------

  private static string GroupPath(string parent, GridGroup<TItem> group) =>
    $"{parent}|{group.Member}={group.Key}";

  private void ToggleGroup(string path) {
    if (!_collapsedGroups.Remove(path)) {
      _collapsedGroups.Add(path);
    }
  }

  private void GroupToggleKeyDown(KeyboardEventArgs e, string path) {
    if (e.Key is "Enter" or " ") {
      ToggleGroup(path);
    }
  }

  private PixataGridColumn<TItem>? ColumnFor(string member) =>
    RenderedColumns().FirstOrDefault(c => c.Field == member);

  private string GroupTitle(GridGroup<TItem> group) {
    PixataGridColumn<TItem>? column = ColumnFor(group.Member);
    string title = column?.HeaderText ?? group.Member;
    return $"{title}: {column?.FormatValue(group.Key) ?? Convert.ToString(group.Key)}";
  }

  private static PixataGridGroupTemplateContext GroupContext(GridGroup<TItem> group, string? field) {
    PixataGridAggregateTemplateContext aggregates = PixataGridAggregateTemplateContext.For<PixataGridGroupTemplateContext>(field, group.Aggregates);
    return new() {
      Field = field,
      Count = aggregates.Count,
      Sum = aggregates.Sum,
      Average = aggregates.Average,
      Min = aggregates.Min,
      Max = aggregates.Max,
      AggregateResults = group.Aggregates,
      Value = group.Key,
      GroupField = group.Member,
      ItemsCount = group.Count
    };
  }
}
