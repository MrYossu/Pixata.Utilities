using System;
using System.ComponentModel.DataAnnotations;

namespace Pixata.Blazor.Sample.Models {
  public class Person {
    public int Id { get; set; }

    [Required(ErrorMessage = "required")]
    public string Title { get; set; }

    [Required(ErrorMessage = "required")]
    public string FirstName { get; set; }

    [Required(ErrorMessage = "required")]
    public string Surname { get; set; }

    [Required(ErrorMessage = "required")]
    [EmailAddress(ErrorMessage = "invalid")]
    public string Email { get; set; }

    public DateTime DateOfBirth { get; set; }
    public DateTime TeaTime { get; set; }
    public bool EmailConfirmed { get; set; }
    public int NumberOfDependents { get; set; }
    public string Notes { get; set; }
    public int FavouriteFoodId { get; set; }
    public Pets Pet { get; set; }
  }
}