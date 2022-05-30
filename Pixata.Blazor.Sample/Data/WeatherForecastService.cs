using System;
using System.Linq;
using System.Threading.Tasks;

namespace Pixata.Blazor.Sample.Data {
  public class WeatherForecastService {
    private static readonly string[] Summaries = {
      "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    public Task<WeatherForecast[]> GetForecastAsync(DateTime startDate) {
      Random rng = new();
      return Task.FromResult(Enumerable.Range(1, 15).Select(index => new WeatherForecast {
        Date = startDate.AddDays(index),
        TemperatureC = rng.Next(-20, 55),
        Rain = rng.Next(100) < 30,
        Summary = Summaries[rng.Next(Summaries.Length)]
      }).ToArray());
    }
  }
}