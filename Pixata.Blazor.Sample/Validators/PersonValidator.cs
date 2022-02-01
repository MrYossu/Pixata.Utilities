using System;
using FluentValidation;
using Pixata.Blazor.Sample.Models;

namespace Pixata.Blazor.Sample.Validators {
  public class PersonValidator : AbstractValidator<Person> {
    public PersonValidator() {
      RuleFor(p => p.Title).NotEmpty().WithMessage("required");
      RuleFor(p => p.FirstName).NotEmpty().WithMessage("required");
      RuleFor(p => p.Surname).NotEmpty().WithMessage("required");
      RuleFor(p => p.Email).Cascade(CascadeMode.Stop)
        .NotEmpty().WithMessage("required")
        .EmailAddress().WithMessage("invalid");
      RuleFor(p => p.DateOfBirth).GreaterThan(new DateTime(2000, 1, 1)).WithMessage("after 1st Jan 2000");
      RuleFor(p => p.TeaTime).GreaterThan(new DateTime(1, 1, 1, 12, 0, 0)).WithMessage("must be after noon");
      RuleFor(p => p.Notes).Custom((notes, ctx) => {
        if (notes.Contains("weird")) {
          ctx.AddFailure("not nice to describe him as weird");
        }
      });
    }
  }
}