using System.Collections.Generic;
using Telerik.Blazor.Services;

namespace Pixata.Blazor.TelerikComponents.Helpers;

public class LocalisationHelper : ITelerikStringLocalizer {
  private readonly ITelerikStringLocalizer _fallback = new TelerikStringLocalizer();

  public readonly Dictionary<string, string> Values = new() {
    { "Filter_SelectValue", "All" },
    { "Grid_NoRecords", "Sorry, nothing matched your filters. Please widen your search criteria" },
  };

  public string this[string name] =>
    Values.ContainsKey(name)
      ? Values[name]
      : _fallback[name];
}