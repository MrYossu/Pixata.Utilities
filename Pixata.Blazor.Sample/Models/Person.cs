using System;

namespace Pixata.Blazor.Sample.Models {
  public class Person {
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string Surname { get; set; }
    public DateTime DateOfBirth { get; set; }
    public DateTime TeaTime { get; set; }
    public bool EmailConfirmed { get; set; }
    public int NumberOfDependents { get; set; }
    public string Notes { get; set; }
  }
}