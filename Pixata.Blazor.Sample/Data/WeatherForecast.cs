using System;

namespace Pixata.Blazor.Sample.Data {
  public class WeatherForecast {
    public DateTime Date { get; set; }
    public int TemperatureC { get; set; }
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    public bool Rain { get; set; }
    public string Summary { get; set; }
  }
}