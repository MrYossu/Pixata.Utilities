using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// Whether the grid pages and scrolls normally, or renders only the rows in view as the user scrolls
/// </summary>
public enum PixataGridScrollMode {
  Scrollable,
  Virtual
}

public partial class PixataGrid<TItem> {
  /// <summary>
  /// With <c>Virtual</c>, only the rows in view (and a few either side) are rendered, and more are rendered as the user
  /// scrolls, instead of paging. Needs a <see cref="Height"/> and a <see cref="RowHeight"/>. <see cref="PageSize"/> is the
  /// number of rows the user sees at once, and three times that are rendered
  /// </summary>
  [Parameter]
  public PixataGridScrollMode ScrollMode { get; set; } = PixataGridScrollMode.Scrollable;

  /// <summary>
  /// The height of each row, in pixels, for virtual scrolling. Rows don't wrap their text, so they are all this high
  /// </summary>
  [Parameter]
  public double? RowHeight { get; set; }

  /// <summary>
  /// Renders only the columns in view in each row, for grids with a lot of columns. Every column needs a width in pixels
  /// </summary>
  [Parameter]
  public bool ColumnVirtualization { get; set; }

  private ElementReference _contentElement;
  private bool _virtualReady;
  private int _skip;
  // The first row in view, which is what the state saves, so the grid comes back where the user left it
  private int _topRow;
  private CancellationTokenSource? _scrollStateCancel;
  private bool _virtualLoading;
  private double? _scrollTarget;
  private double _viewLeft;
  private double _viewWidth;
  private (int First, int Last)? _columnWindow;

  private bool VirtualRows =>
    ScrollMode == PixataGridScrollMode.Virtual;

  // The number of rows rendered at once
  private int VirtualRowCount =>
    Math.Max(_pageSize, 1) * 3;

  // Fail when the parameters are set, rather than with a confusing grid
  private void CheckVirtualParameters() {
    if (!VirtualRows) {
      return;
    }
    if (RowHeight is not > 0 || string.IsNullOrWhiteSpace(Height)) {
      throw new InvalidOperationException("A PixataGrid with ScrollMode=\"Virtual\" needs a Height and a RowHeight");
    }
    if (Groupable) {
      throw new InvalidOperationException("A PixataGrid can't be Groupable with ScrollMode=\"Virtual\"");
    }
  }

  private string? VirtualRowStyle =>
    VirtualRows ? FormattableString.Invariant($"height: {RowHeight}px;") : null;

  // The space above and below the rendered rows, so the scrollbar is as if they were all there
  private string SpacerStyle(int rows) =>
    FormattableString.Invariant($"height: {rows * RowHeight!.Value}px;");

  private int RowsAfter =>
    Math.Max(_total - _skip - _items.Count, 0);

  private async Task InitVirtualScroll() {
    if (!(VirtualRows || ColumnVirtualization) || _virtualReady || _disposed) {
      return;
    }
    _virtualReady = true;
    try {
      _module ??= await Js.InvokeAsync<IJSObjectReference>("import", PixataBlazorScript.Path);
      _self ??= DotNetObjectReference.Create(this);
      await _module.InvokeVoidAsync("initVirtualScroll", _contentElement, _self);
    } catch (Exception ex) when (ex is JSDisconnectedException or OperationCanceledException) {
      // The page has gone
    }
  }

  // The rows the scroll target refers to have to be there first, or the browser clamps the scroll to the content rendered
  // so far
  private bool ScrollTargetReady =>
    !_loading && !_virtualLoading && (_firstReadDone || !UsesOnRead);

  private async Task AfterVirtualRender() {
    if (_scrollTarget is not double top || _module is null || !ScrollTargetReady) {
      return;
    }
    _scrollTarget = null;
    try {
      await _module.InvokeVoidAsync("scrollContentTo", _contentElement, top);
    } catch (Exception ex) when (ex is JSDisconnectedException or OperationCanceledException) {
      // The page has gone
    }
  }

  // Filtering and searching start again at the top, as the rows are different
  private void ResetVirtualScroll() {
    if (VirtualRows && _skip != 0) {
      ScrollToRow(0);
    }
  }

  // Renders the rows from this one, and scrolls there once they have rendered
  private void ScrollToRow(int row) {
    _skip = Math.Max(row, 0);
    _topRow = _skip;
    _scrollTarget = _skip * (RowHeight ?? 0);
  }

  /// <summary>
  /// Called by the JavaScript as the user scrolls a grid with virtual rows or columns
  /// </summary>
  [JSInvokable]
  [EditorBrowsable(EditorBrowsableState.Never)]
  public void VirtualScrolled(double top, double left, double width, double height) {
    bool changed = false;
    // Until the grid has scrolled to where it's going (a saved position, say), the position reported is where it was, not
    // where the user wants to be. The size still counts
    if (VirtualRows && RowHeight is > 0 && _scrollTarget is null) {
      int first = (int)(top / RowHeight.Value);
      if (first != _topRow) {
        _topRow = first;
        CheckStateAfterScrolling();
      }
      int last = (int)Math.Ceiling((top + height) / RowHeight.Value);
      int margin = Math.Max(_pageSize / 2, 1);
      int end = _skip + VirtualRowCount;
      // A new set of rows is wanted when the rows in view get close to either end of the ones rendered
      bool nearStart = _skip > 0 && first - _skip < margin;
      bool nearEnd = end < _total && end - last < margin;
      if (nearStart || nearEnd) {
        int skip = Math.Clamp(first - _pageSize, 0, Math.Max(_total - VirtualRowCount, 0));
        if (skip != _skip) {
          _skip = skip;
          _virtualLoading = UsesOnRead;
          ProcessData();
          changed = true;
        }
      }
    }
    if (ColumnVirtualization && (left != _viewLeft || width != _viewWidth)) {
      _viewLeft = left;
      _viewWidth = width;
      (int, int)? window = ColumnWindow(RenderedColumns());
      if (window != _columnWindow) {
        changed = true;
      }
    }
    if (changed) {
      StateHasChanged();
    }
  }

  // Scrolling within the rows rendered doesn't render the grid, so the state isn't checked. It is once the user stops
  private void CheckStateAfterScrolling() {
    if (!OnStateChanged.HasDelegate && string.IsNullOrWhiteSpace(StorageKey)) {
      return;
    }
    _scrollStateCancel?.Cancel();
    _scrollStateCancel = new();
    CancellationToken token = _scrollStateCancel.Token;
    _ = Task.Delay(500, token).ContinueWith(_ => InvokeAsync(CheckStateChanged), token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
  }

  // ---------- Virtual columns ----------

  // The columns in view, with one either side, if they all have widths in pixels and the grid knows how wide it is
  private (int First, int Last)? ColumnWindow(List<PixataGridColumn<TItem>> columns) {
    if (!ColumnVirtualization || _viewWidth <= 0 || columns.Count == 0) {
      return null;
    }
    List<double?> widths = [.. columns.Select(c => Pixels(c.CurrentWidth))];
    if (widths.Any(w => w is null)) {
      return null;
    }
    double offset = LeadingColumns * 32;
    int first = -1;
    int last = columns.Count - 1;
    for (int i = 0; i < columns.Count; i++) {
      double right = offset + widths[i]!.Value;
      if (first < 0 && right > _viewLeft) {
        first = i;
      }
      if (offset >= _viewLeft + _viewWidth) {
        last = i - 1;
        break;
      }
      offset = right;
    }
    first = Math.Max((first < 0 ? columns.Count - 1 : first) - 1, 0);
    last = Math.Min(last + 1, columns.Count - 1);
    return (first, last);
  }

  // The columns to render cells for in a row, and how many are left out before and after them. Rows being edited a cell at
  // a time have all their cells, as the one being edited may be out of view
  private (List<PixataGridColumn<TItem>> Columns, int Before, int After) RowColumns(List<PixataGridColumn<TItem>> columns, TItem item) =>
    _columnWindow is (int first, int last) && !IsInCellRow(item) && last < columns.Count
      ? (columns.GetRange(first, last - first + 1), first, columns.Count - last - 1)
      : (columns, 0, 0);
}
