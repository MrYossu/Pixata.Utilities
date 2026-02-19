using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Pixata.AspNetCore.Extensions;

namespace Pixata.AspNetCore;

public static class ServiceCollectionExtensions {
  public static IServiceCollection AddPixataAspNetCore<T>(this IServiceCollection services) {
    services.AddValidatorsFromAssemblyContaining<T>();
    services.AddTransient<ValidationEndpointFilter>();
    return services;
  }
}