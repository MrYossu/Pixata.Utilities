using System.Collections;
using System.Collections.Generic;

namespace Pixata.Blazor.Components;

/// <summary>
/// Passed to the <c>OnRead</c> event of <see cref="PixataDropDownList{TItem, TValue}"/>, <see cref="PixataComboBox{TItem, TValue}"/>
/// and <see cref="PixataMultiSelect{TItem, TValue}"/>, which you handle to supply the data yourself (eg from a database or an
/// API) instead of setting <c>Data</c>. Read the request properties, fetch the matching items, and set <see cref="Data"/> (and
/// <see cref="Total"/> when virtual scrolling)
/// </summary>
public class PixataDropDownReadEventArgs {
  /// <summary>
  /// The text the user has typed to filter the list, or <c>""</c> if there is none. Only return the items whose text matches it
  /// using <see cref="FilterOperator"/>
  /// </summary>
  public string Filter { get; init; } = "";

  /// <summary>
  /// How <see cref="Filter"/> should be matched against the text of each item
  /// </summary>
  public PixataStringFilterOperator FilterOperator { get; init; }

  /// <summary>
  /// The number of matching items to skip. Always 0 unless the component is virtually scrolling
  /// </summary>
  public int Skip { get; init; }

  /// <summary>
  /// The number of items wanted, starting at <see cref="Skip"/>. 0 means all the matching items (the component isn't virtually
  /// scrolling); otherwise it is the component's <c>PageSize</c>
  /// </summary>
  public int Take { get; init; }

  /// <summary>
  /// Set this to the items the component should show
  /// </summary>
  public IEnumerable? Data { get; set; }

  /// <summary>
  /// Set this to the total number of items that match <see cref="Filter"/> (not just the ones in <see cref="Data"/>). Needed when
  /// virtually scrolling, so the component knows how long the list is; otherwise the number of items in <see cref="Data"/> is
  /// used if you leave it at 0
  /// </summary>
  public int Total { get; set; }
}

/// <summary>
/// Passed to the <c>OnOpen</c> event of the drop-downs. Set <see cref="IsCancelled"/> to stop the list opening
/// </summary>
public class PixataDropDownOpenEventArgs {
  public bool IsCancelled { get; set; }
}

/// <summary>
/// Passed to the <c>OnClose</c> event of the drop-downs. Set <see cref="IsCancelled"/> to keep the list open. The list closes
/// anyway when the component loses focus or is disabled
/// </summary>
public class PixataDropDownCloseEventArgs {
  public bool IsCancelled { get; set; }
}

/// <summary>
/// The context of the <c>SummaryTagTemplate</c> of <see cref="PixataMultiSelect{TItem, TValue}"/>
/// </summary>
public class PixataMultiSelectSummaryTagTemplateContext<TItem> {
  /// <summary>
  /// All the selected items (including any shown in their own tags)
  /// </summary>
  public List<TItem> Items { get; init; } = [];

  /// <summary>
  /// The number of selected items the summary tag stands for: all of them in <see cref="PixataMultiSelectTagMode.Single"/>
  /// mode, or the ones beyond <c>MaxAllowedTags</c>
  /// </summary>
  public int Count { get; init; }
}
