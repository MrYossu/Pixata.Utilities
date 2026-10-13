using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Pixata.Blazor.Sample.Models;

/// <summary>
/// Data for the drop-downs sample page
/// </summary>
public static class DropDownSampleData {
  public record Country(int Id, string Name, string Continent);

  public record Product(int Id, string Name, string Category, decimal Price);

  public static readonly List<string> Fruit = ["Apple", "Apricot", "Banana", "Blackberry", "Blueberry", "Cherry", "Damson", "Elderberry", "Fig", "Gooseberry", "Grape", "Kiwi", "Lemon", "Lime", "Mango", "Orange", "Peach", "Pear", "Plum", "Raspberry", "Strawberry"];

  public static readonly List<Country> Countries = [
    new(1, "Algeria", "Africa"), new(2, "Egypt", "Africa"), new(3, "Ghana", "Africa"), new(4, "Kenya", "Africa"), new(5, "Nigeria", "Africa"),
    new(6, "China", "Asia"), new(7, "India", "Asia"), new(8, "Japan", "Asia"), new(9, "Singapore", "Asia"), new(10, "Thailand", "Asia"),
    new(11, "France", "Europe"), new(12, "Germany", "Europe"), new(13, "Ireland", "Europe"), new(14, "Italy", "Europe"), new(15, "Spain", "Europe"), new(16, "United Kingdom", "Europe"),
    new(17, "Canada", "North America"), new(18, "Mexico", "North America"), new(19, "United States", "North America"),
    new(20, "Australia", "Oceania"), new(21, "Fiji", "Oceania"), new(22, "New Zealand", "Oceania"),
    new(23, "Argentina", "South America"), new(24, "Brazil", "South America"), new(25, "Chile", "South America"), new(26, "Peru", "South America")
  ];

  private static readonly string[] Adjectives = ["Ancient", "Bright", "Clever", "Daring", "Eager", "Fancy", "Gentle", "Happy", "Jolly", "Lucky", "Mighty", "Noble", "Proud", "Quiet", "Rapid", "Silent", "Tidy", "Useful", "Vivid", "Wise"];

  private static readonly string[] Nouns = ["Anchor", "Badger", "Candle", "Dragon", "Engine", "Falcon", "Garden", "Hammer", "Island", "Jacket", "Kettle", "Lantern", "Magnet", "Needle", "Otter", "Pencil", "Rocket", "Saddle", "Teapot", "Wagon"];

  private static readonly string[] Categories = ["Hardware", "Garden", "Kitchen", "Outdoors", "Toys"];

  /// <summary>
  /// 5,000 products, for showing virtual scrolling
  /// </summary>
  public static readonly List<Product> Products = Enumerable.Range(1, 5000)
    .Select(i => new Product(i, $"{Adjectives[i % Adjectives.Length]} {Nouns[i / Adjectives.Length % Nouns.Length]} {i}", Categories[i % Categories.Length], Math.Round(1 + i % 97 * 1.37m, 2)))
    .ToList();

  /// <summary>
  /// The model for the validation example
  /// </summary>
  public class FormModel {
    [Required(ErrorMessage = "Choose a fruit")]
    public string? Fruit { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Choose a country")]
    public int CountryId { get; set; }

    [MinLength(2, ErrorMessage = "Choose at least two countries")]
    public List<int> CountryIds { get; set; } = [];
  }
}
