using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Pixata.Extensions;
using Pixata.Extensions.Grid;

namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// What the grid's <c>OnRead</c> handler is given. Read <see cref="Request"/>, then set <see cref="Data"/> and
/// <see cref="Total"/> (or call one of the <c>SetResult</c> methods)
/// </summary>
public class PixataGridReadEventArgs<TItem>(GridRequest request, CancellationToken cancellationToken) {
  /// <summary>
  /// The page, sorting and filtering the grid wants. Pass it to <c>ToGridResult</c> or <c>ToGridResultAsync</c>, or send it
  /// to an API that does
  /// </summary>
  public GridRequest Request { get; } = request;

  /// <summary>
  /// Cancelled when the grid asks for different data before this request has finished, such as when the user types quickly
  /// in a filter. Pass it on to any database or API calls
  /// </summary>
  public CancellationToken CancellationToken { get; } = cancellationToken;

  /// <summary>
  /// The items to show, which should only be the requested page
  /// </summary>
  public IEnumerable<TItem>? Data { get; set; }

  /// <summary>
  /// The number of items before paging, which the pager needs
  /// </summary>
  public int Total { get; set; }

  /// <summary>
  /// If set, the grid shows this message instead of any rows, styled as an error
  /// </summary>
  public string? ErrorMessage { get; set; }

  // The grid's messages, for the error message when an API call fails without one
  internal PixataGridMessages Messages { get; set; } = PixataGridMessages.Default;

  /// <summary>
  /// The groups the page's items are in, if the request asked for grouping. If not set, the grid groups the page itself, but
  /// then the group counts and aggregates only cover the page
  /// </summary>
  public List<GridGroup<TItem>>? Groups { get; set; }

  /// <summary>
  /// The aggregates over all the data, if the request asked for any
  /// </summary>
  public Dictionary<string, GridFieldAggregates>? Aggregates { get; set; }

  public void SetResult(GridResult<TItem> result) {
    Data = result.Data;
    Total = result.Total;
    Groups = result.Groups;
    Aggregates = result.Aggregates;
  }

  /// <summary>
  /// Takes the result of an API call. If the call failed, its message is shown in the grid
  /// </summary>
  public void SetResult(ApiResponse<GridResult<TItem>> response) {
    if (response.State == ApiResponseStates.Success && response.Data is GridResult<TItem> result) {
      SetResult(result);
    } else {
      ErrorMessage = string.IsNullOrWhiteSpace(response.Message) ? Messages.ReadFailed : response.Message;
    }
  }
}

public partial class PixataGrid<TItem> {
  /// <summary>
  /// Supplies the data yourself, rather than through <see cref="Data"/>. The grid calls it when it first renders, and
  /// whenever the page, sorting or filtering changes, with a request describing what it wants. Use this for large data, or
  /// data from an API. Call <see cref="Rebind"/> to make the grid ask again
  /// </summary>
  [Parameter]
  public EventCallback<PixataGridReadEventArgs<TItem>> OnRead { get; set; }

  /// <summary>
  /// Whether to show a loading indicator over the grid when <see cref="OnRead"/> takes a while. It appears after 600ms, so
  /// quick reads don't flash. Defaults to true. As in Telerik, it isn't shown for the first read
  /// </summary>
  [Parameter]
  public bool EnableLoaderContainer { get; set; } = true;

  private bool UsesOnRead =>
    OnRead.HasDelegate;

  private int _readVersion;
  private CancellationTokenSource? _readCancel;
  private bool _loading;
  private bool _showLoader;
  private bool _firstReadDone;
  private string? _readError;

  private GridRequest CurrentRequest() =>
    new() {
      Page = Pageable && !VirtualRows ? _page : 1,
      PageSize = VirtualRows ? VirtualRowCount : Pageable ? _pageSize : 0,
      Skip = VirtualRows ? _skip : null,
      Sorts = [.. _sorts],
      Filters = CurrentFilters,
      Search = _search,
      Groups = [.. _groups],
      Aggregates = AggregateDescriptors
    };

  // Asks for the data. A newer request makes any older one irrelevant, so its result is ignored if it arrives later
  private async Task ReadData() {
    int version = ++_readVersion;
    if (_readCancel is not null) {
      await _readCancel.CancelAsync();
    }
    _readCancel = new();
    GridRequest request = CurrentRequest();
    _requestedAggregates = request.Aggregates;
    PixataGridReadEventArgs<TItem> args = new(request, _readCancel.Token) { Messages = Text };
    _loading = true;
    // Virtual scrolling shows placeholder rows instead
    if (EnableLoaderContainer && _firstReadDone && !_virtualLoading) {
      _ = ShowLoaderAfterDelay(version);
    }
    try {
      await OnRead.InvokeAsync(args);
    } catch (OperationCanceledException) when (args.CancellationToken.IsCancellationRequested) {
      return;
    } finally {
      if (version == _readVersion) {
        _loading = false;
        _showLoader = false;
      }
    }
    _firstReadDone = true;
    if (version != _readVersion) {
      return;
    }
    _items = [.. args.Data ?? []];
    _total = args.Total;
    _virtualLoading = false;
    _resultGroups = request.Groups.Count == 0 ? null : args.Groups ?? GridGroupingExtensions.GroupPage(_items.AsQueryable(), _items, request.Groups, request.Aggregates);
    _resultAggregates = args.Aggregates;
    _readError = args.ErrorMessage;
    // If the page is now past the end (because the data has shrunk), go to the last page and ask again
    if (Pageable && !VirtualRows && _pageSize > 0 && _readError is null && _total > 0) {
      int pageCount = (_total + _pageSize - 1) / _pageSize;
      if (_page > pageCount) {
        _page = pageCount;
        await PageChanged.InvokeAsync(_page);
        await ReadData();
        return;
      }
    }
    StateHasChanged();
  }

  private async Task ShowLoaderAfterDelay(int version) {
    await Task.Delay(600);
    if (version == _readVersion && _loading) {
      _showLoader = true;
      await InvokeAsync(StateHasChanged);
    }
  }
}
