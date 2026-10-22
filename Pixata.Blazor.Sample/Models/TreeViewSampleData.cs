using System.Collections.Generic;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Sample.Models;

/// <summary>
/// An item of flat tree view data, where each item says which is its parent
/// </summary>
public class TreeViewFlatItem {
  public int Id { get; set; }
  public int? ParentId { get; set; }
  public string Text { get; set; } = "";
  public object? Icon { get; set; }
  public bool Disabled { get; set; }
  public string? Url { get; set; }
}

/// <summary>
/// An item for loading children on demand: HasChildren says whether it has any before they have been loaded
/// </summary>
public class TreeViewLazyItem {
  public int Id { get; set; }
  public int? ParentId { get; set; }
  public string Text { get; set; } = "";
  public bool HasChildren { get; set; }
}

/// <summary>
/// Hierarchical data with a different type at each level
/// </summary>
public class TreeViewDepartment {
  public string Name { get; set; } = "";
  public List<TreeViewEmployee> Staff { get; set; } = [];
}

public class TreeViewEmployee {
  public string FullName { get; set; } = "";
  public string Role { get; set; } = "";
}

public static class TreeViewSampleData {
  public static List<TreeViewFlatItem> Files() => [
    new() { Id = 1, Text = "Documents", Icon = PixataSvgIcons.File },
    new() { Id = 2, ParentId = 1, Text = "Reports", Icon = PixataSvgIcons.File },
    new() { Id = 3, ParentId = 2, Text = "Q1 report.pdf" },
    new() { Id = 4, ParentId = 2, Text = "Q2 report.pdf" },
    new() { Id = 5, ParentId = 2, Text = "Q3 report.pdf (locked)", Disabled = true },
    new() { Id = 6, ParentId = 1, Text = "Letters", Icon = PixataSvgIcons.File },
    new() { Id = 7, ParentId = 6, Text = "To the bank.docx" },
    new() { Id = 8, ParentId = 6, Text = "To the council.docx" },
    new() { Id = 9, Text = "Pictures", Icon = PixataSvgIcons.File },
    new() { Id = 10, ParentId = 9, Text = "Holiday.jpg" },
    new() { Id = 11, ParentId = 9, Text = "Cat.png" },
    new() { Id = 12, Text = "Notes.txt" }
  ];

  public static List<TreeViewDepartment> Departments() => [
    new() {
      Name = "Sales",
      Staff = [new() { FullName = "Alice Archer", Role = "Manager" }, new() { FullName = "Bob Baker", Role = "Account executive" }]
    },
    new() {
      Name = "Development",
      Staff = [new() { FullName = "Carol Cooper", Role = "Lead developer" }, new() { FullName = "Dan Dyer", Role = "Developer" }, new() { FullName = "Eve Evans", Role = "Tester" }]
    },
    new() { Name = "Support", Staff = [new() { FullName = "Frank Fisher", Role = "Support engineer" }] }
  ];

  public static List<TreeViewFlatItem> Pages() => [
    new() { Id = 1, Text = "Components" },
    new() { Id = 2, ParentId = 1, Text = "Pixata grid", Url = "pixata-grid" },
    new() { Id = 3, ParentId = 1, Text = "Tab strip", Url = "TabStripSample" },
    new() { Id = 4, ParentId = 1, Text = "Switch", Url = "SwitchSample" },
    new() { Id = 5, Text = "Pixata on GitHub", Url = "https://github.com/MrYossu/Pixata.Utilities" }
  ];
}
