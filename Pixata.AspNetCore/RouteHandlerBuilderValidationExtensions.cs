using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Pixata.AspNetCore.Extensions;

namespace Pixata.AspNetCore;

public static class RouteHandlerBuilderValidationExtensions {
  public static RouteHandlerBuilder AddValidationEndpointFilter(this RouteHandlerBuilder builder) =>
    builder.AddEndpointFilter(async (context, next) =>
      await context.HttpContext.RequestServices.GetRequiredService<ValidationEndpointFilter>().InvokeAsync(context, next).ConfigureAwait(false));
}