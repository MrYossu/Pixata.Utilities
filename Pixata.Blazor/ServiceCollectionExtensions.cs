using Microsoft.Extensions.DependencyInjection;
using Pixata.Blazor.Extensions;
using Pixata.Blazor.Notifications;

namespace Pixata.Blazor;

public static class ServiceCollectionExtensions {
  public static IServiceCollection AddPixataBlazor(this IServiceCollection services) {
    services.AddScoped<MessageBrokerInstance>();
    services.AddScoped<NotificationHelper>();
    services.AddScoped<PasswordOptionsHelper>();
    services.AddScoped<TemplateHelper>();
    return services;
  }
}