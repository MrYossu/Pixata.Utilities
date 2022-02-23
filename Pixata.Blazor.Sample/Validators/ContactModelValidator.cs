using FluentValidation;
using Pixata.Blazor.Sample.Pages;

namespace Pixata.Blazor.Sample.Validators {
  public class ContactModelValidator : AbstractValidator<SendEmail.ContactModel> {
    public ContactModelValidator() {
      RuleFor(p => p.Name).NotEmpty().WithMessage("who are you?");
      RuleFor(p => p.Subject).NotEmpty().WithMessage("what's it about?");
      RuleFor(p => p.Body).NotEmpty().WithMessage("say something!");
      RuleFor(p => p.Email).Cascade(CascadeMode.Stop)
        .NotEmpty().WithMessage("required")
        .EmailAddress().WithMessage("invalid");
    }
  }
}