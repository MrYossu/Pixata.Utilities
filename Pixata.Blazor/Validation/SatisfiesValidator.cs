using System;
using FluentValidation;
using FluentValidation.Validators;

namespace Pixata.Blazor.Validation;

/// <summary>
/// A predicate rule for FluentValidation, to use in place of its <c>Must</c> and <c>Custom</c> in any validator a
/// Blazor WebAssembly app uses. In a published (Release) WASM app, merely constructing a validator that uses
/// <c>Must</c> or <c>Custom</c> stops the .NET runtime with a Mono assertion (<c>loader.c:1826</c>), whereas this
/// works. See Readme.Validation.md
/// </summary>
public class SatisfiesValidator<T, TProperty>(Func<T, TProperty, bool> predicate) : PropertyValidator<T, TProperty> {
  public override string Name => "PredicateValidator";

  public override bool IsValid(ValidationContext<T> context, TProperty value) =>
    predicate(context.InstanceToValidate, value);

  // The same default message as Must's, "The specified condition was not met for '{PropertyName}'"
  protected override string GetDefaultMessageTemplate(string errorCode) =>
    Localized(errorCode, Name);
}

public static class SatisfiesExtensions {
  /// <summary>
  /// Fails when the predicate, given the property's value, returns false. Use this in place of FluentValidation's
  /// <c>Must</c>, which crashes a published Blazor WebAssembly app
  /// </summary>
  public static IRuleBuilderOptions<T, TProperty> Satisfies<T, TProperty>(this IRuleBuilder<T, TProperty> ruleBuilder, Func<TProperty, bool> predicate) {
    ArgumentNullException.ThrowIfNull(predicate);
    return ruleBuilder.SetValidator(new SatisfiesValidator<T, TProperty>((_, value) => predicate(value)));
  }

  /// <summary>
  /// Fails when the predicate, given the object being validated and the property's value, returns false. Use this in
  /// place of FluentValidation's <c>Must</c> and <c>Custom</c>, which crash a published Blazor WebAssembly app
  /// </summary>
  public static IRuleBuilderOptions<T, TProperty> Satisfies<T, TProperty>(this IRuleBuilder<T, TProperty> ruleBuilder, Func<T, TProperty, bool> predicate) {
    ArgumentNullException.ThrowIfNull(predicate);
    return ruleBuilder.SetValidator(new SatisfiesValidator<T, TProperty>(predicate));
  }
}
