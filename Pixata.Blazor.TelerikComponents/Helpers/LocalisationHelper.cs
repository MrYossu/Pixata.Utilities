using System.Collections.Generic;
using Telerik.Blazor.Services;

namespace Pixata.Blazor.TelerikComponents.Helpers;

public class LocalisationHelper : ITelerikStringLocalizer {
  private readonly ITelerikStringLocalizer _fallback = new TelerikStringLocalizer();

  /// <summary>
  /// The messages to override, keyed by the Telerik message name. This is static, so set any of your own once in <c>Program.cs</c>, and
  /// every instance the DI container creates will use them
  /// </summary>
  public static readonly Dictionary<string, string> Values = new() {
    { "Filter_SelectValue", "All" },
    { "Grid_NoRecords", "Sorry, nothing matched your filters. Please widen your search criteria" },
  };

  public string this[string name] =>
    Values.TryGetValue(name, out string? value)
      ? value
      : _fallback[name];
}