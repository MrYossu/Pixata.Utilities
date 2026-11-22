using System;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Validation;

namespace Pixata.Blazor.Tests;

[TestClass]
public class SatisfiesValidator_Tests {
  private class Booking {
    public int Places { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Code { get; set; } = "";
  }

  private class BookingValidator : AbstractValidator<Booking> {
    public BookingValidator() {
      RuleFor(b => b.Places).Satisfies(p => p > 0).WithMessage("At least one place");
      RuleFor(b => b.Start).Satisfies((b, start) => start < b.End).WithMessage("Start must be before end");
      RuleFor(b => b.Code).Satisfies(c => c.Length == 3).When(b => b.Places > 1);
    }
  }

  private static readonly DateTime Today = new(2026, 11, 22);

  [TestMethod]
  public void Passes_when_every_predicate_is_true() {
    ValidationResult result = new BookingValidator().Validate(new Booking { Places = 2, Start = Today, End = Today.AddDays(1), Code = "ABC" });
    Assert.IsTrue(result.IsValid);
  }

  [TestMethod]
  public void Fails_with_the_message_given_when_the_value_predicate_is_false() {
    ValidationResult result = new BookingValidator().Validate(new Booking { Places = 0, Start = Today, End = Today.AddDays(1) });
    Assert.AreEqual(1, result.Errors.Count);
    Assert.AreEqual(nameof(Booking.Places), result.Errors[0].PropertyName);
    Assert.AreEqual("At least one place", result.Errors[0].ErrorMessage);
  }

  [TestMethod]
  public void Gives_the_predicate_the_object_being_validated() {
    ValidationResult result = new BookingValidator().Validate(new Booking { Places = 1, Start = Today, End = Today });
    Assert.AreEqual(1, result.Errors.Count);
    Assert.AreEqual(nameof(Booking.Start), result.Errors[0].PropertyName);
    Assert.AreEqual("Start must be before end", result.Errors[0].ErrorMessage);
  }

  [TestMethod]
  public void Uses_the_same_default_message_as_Must() {
    ValidationResult result = new BookingValidator().Validate(new Booking { Places = 2, Start = Today, End = Today.AddDays(1), Code = "AB" });
    Assert.AreEqual(1, result.Errors.Count);
    Assert.AreEqual("The specified condition was not met for 'Code'.", result.Errors[0].ErrorMessage);
  }

  [TestMethod]
  public void Honours_When() {
    ValidationResult result = new BookingValidator().Validate(new Booking { Places = 1, Start = Today, End = Today.AddDays(1), Code = "" });
    Assert.IsTrue(result.IsValid);
  }
}
