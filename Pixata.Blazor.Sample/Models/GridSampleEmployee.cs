using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Pixata.Extensions.Auditing.Attributes;

namespace Pixata.Blazor.Sample.Models;

// Sample data for the PixataGrid page. It has a mix of types, a nested object and some attributes, to show what the grid
// does with each
[NoAudit]
public class GridSampleEmployee {
  public int Id { get; set; }
  public string FirstName { get; set; } = "";
  public string Surname { get; set; } = "";

  [Display(Name = "Email address")]
  public string Email { get; set; } = "";

  [DisplayFormat(DataFormatString = "{0:dd MMM yyyy}")]
  public DateTime DateOfBirth { get; set; }

  public decimal Salary { get; set; }
  public bool IsManager { get; set; }
  public Pets Pet { get; set; }

  [Display(AutoGenerateField = false)]
  public string Phone { get; set; } = "";

  public GridSampleAddress Address { get; set; } = new();

  public static List<GridSampleEmployee> All() {
    string[] cities = ["London", "Manchester", "Leeds", "Glasgow", "Cardiff", "Belfast", "Bristol"];
    return [.. PersonDto.People.Select(p => new GridSampleEmployee {
      Id = p.Id,
      FirstName = p.FirstName,
      Surname = p.Surname,
      Email = p.Email,
      DateOfBirth = p.DateOfBirth,
      Salary = 21000 + p.Id * 1375.5m,
      IsManager = p.Id % 4 == 0,
      Pet = (Pets)(p.Id % 4),
      Phone = p.Phone,
      Address = new() { City = cities[p.Id % cities.Length] }
    })];
  }
}

[NoAudit]
public class GridSampleAddress {
  public string City { get; set; } = "";
}
