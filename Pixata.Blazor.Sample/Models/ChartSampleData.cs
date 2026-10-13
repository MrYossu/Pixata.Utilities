using System.Collections.Generic;

namespace Pixata.Blazor.Sample.Models;

/// <summary>
/// A month's sales figures, used by the chart sample page
/// </summary>
public class MonthlySales {
  public string Month { get; set; } = "";
  public double Hardware { get; set; }
  public double Software { get; set; }
  public double Services { get; set; }
  public double? Margin { get; set; }
  public double? Visitors { get; set; }
}

/// <summary>
/// A share of a whole, used by the pie charts on the chart sample page
/// </summary>
public class MarketShare {
  public string Browser { get; set; } = "";
  public double Share { get; set; }
  public bool Explode { get; set; }
  public string? Colour { get; set; }
}

public static class ChartSampleData {
  public static List<MonthlySales> Sales() => [
    new() { Month = "Jan", Hardware = 42000, Software = 28000, Services = 15000, Margin = 0.21, Visitors = 1200 },
    new() { Month = "Feb", Hardware = 38000, Software = 31000, Services = 17000, Margin = 0.24, Visitors = 1350 },
    new() { Month = "Mar", Hardware = 45000, Software = 30500, Services = 16500, Margin = 0.19, Visitors = null },
    new() { Month = "Apr", Hardware = 51000, Software = 34000, Services = 19000, Margin = 0.27, Visitors = 1610 },
    new() { Month = "May", Hardware = 47000, Software = 36500, Services = 21000, Margin = 0.29, Visitors = 1480 },
    new() { Month = "Jun", Hardware = 55000, Software = 39000, Services = 22500, Margin = 0.31, Visitors = 1720 },
    new() { Month = "Jul", Hardware = 49000, Software = 41000, Services = 20500, Margin = null, Visitors = 1690 },
    new() { Month = "Aug", Hardware = 53000, Software = 42500, Services = 24000, Margin = 0.33, Visitors = 1900 }
  ];

  public static List<MarketShare> Browsers() => [
    new() { Browser = "Chrome", Share = 0.62 },
    new() { Browser = "Safari", Share = 0.19, Explode = true },
    new() { Browser = "Edge", Share = 0.08 },
    new() { Browser = "Firefox", Share = 0.06 },
    new() { Browser = "Opera", Share = 0.03 },
    new() { Browser = "Other", Share = 0.02 }
  ];

  /// <summary>
  /// Category names long enough to need rotating or thinning on a narrow screen
  /// </summary>
  public static List<string> Regions => ["North East", "North West", "Yorkshire and the Humber", "East Midlands", "West Midlands", "East of England", "London", "South East", "South West", "Wales", "Scotland", "Northern Ireland"];

  public static List<object> RegionValues => [2.7, 7.4, 5.5, 4.9, 5.9, 6.3, 8.9, 9.3, 5.7, 3.1, 5.4, 1.9];

  public static List<object> TemperatureHigh => [7.0, 8.1, 11.0, 13.9, 17.2, 20.1, null!, 21.8, 18.9, 14.9, 10.4, 7.6];

  public static List<object> TemperatureLow => [1.2, 1.1, 2.8, 4.4, 7.3, 10.2, 12.4, 12.1, 9.8, 7.1, 3.9, 1.8];

  public static List<string> MonthNames => ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
}
