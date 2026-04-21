using FluentValidation;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Pixata.AspNetCore.Extensions;
using Pixata.AspNetCore.Helpers;

namespace Pixata.AspNetCore;

public static class ServiceCollectionExtensions {
  public static IServiceCollection AddPixataAspNetCore<T>(this IServiceCollection services) {
    services.AddScoped<HtmlRenderer>();
    services.AddScoped<DocumentTemplateHelper>();
    services.AddValidatorsFromAssemblyContaining<T>();
    services.AddTransient<ValidationEndpointFilter>();
    return services;
  }
}