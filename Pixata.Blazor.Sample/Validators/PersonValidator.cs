using System;
using FluentValidation;
using Pixata.Blazor.Sample.Models;

namespace Pixata.Blazor.Sample.Validators {
  public class PersonValidator : AbstractValidator<Person> {
    public PersonValidator() {
      RuleFor(p => p.FirstName).NotEmpty().WithMessage("Required");
      RuleFor(p => p.Surname).NotEmpty().WithMessage("Required");
      RuleFor(p => p.DateOfBirth).GreaterThan(new DateTime(2000, 1, 1)).WithMessage("After 1st Jan 2000");
    }
  }
}