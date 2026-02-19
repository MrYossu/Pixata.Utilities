using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Pixata.Extensions;

namespace Pixata.AspNetCore.Extensions;

public class ValidationEndpointFilter(Func<ValidationFailure, string>? format = null, string nameSpace = "") : IEndpointFilter {
  public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) {
    IServiceProvider services = context.HttpContext.RequestServices;
    foreach (object? arg in context.Arguments) {
      if (arg is null) {
        continue;
      }
      Type t = arg.GetType();
      if (t.Namespace is null) {
        continue;
      }
      if (!string.IsNullOrWhiteSpace(nameSpace) && !t.Namespace.StartsWith(nameSpace)) {
        continue;
      }
      Type validatorInterface = typeof(FluentValidation.IValidator<>).MakeGenericType(t);
      object? validator = services.GetService(validatorInterface);
      if (validator is null) {
        continue;
      }
      try {
        Type validationContextType = typeof(FluentValidation.ValidationContext<>).MakeGenericType(t);
        object validationContext = Activator.CreateInstance(validationContextType, arg)!;
        object? dynRes = await ((dynamic)validator).ValidateAsync((dynamic)validationContext).ConfigureAwait(false);
        if (dynRes is ValidationResult { IsValid: false } res) {
          return new ApiResponse<object>(ApiResponseStates.Failure, Message: $"Validation error{res.Errors.Count.S()} - " + (format is null ? res.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}").JoinStr() : res.Errors.Select(format).JoinStr()));
        }
      }
      catch (Exception ex) {
        // TODO AYS - Get a logger from the service provider (if one has been registered) and log this
        Console.WriteLine($"Validator invocation failed: {ex.Message}");
      }
    }
    return await next(context).ConfigureAwait(false);
  }
}