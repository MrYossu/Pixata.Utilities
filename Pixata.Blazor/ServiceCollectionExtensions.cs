using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Pixata.Blazor.Extensions;
using Pixata.Blazor.Notifications;

namespace Pixata.Blazor;

public static class ServiceCollectionExtensions {
  public static IServiceCollection AddPixataBlazor(this IServiceCollection services) {
    List<Type> types = [
      typeof(MessageBrokerInstance),
      typeof(NotificationHelper),
      typeof(PasswordOptionsHelper),
      typeof(PersistentStateHelper<>),
      typeof(TemplateHelper),
    ];

    types.ForEach(t => {
      if (services.Any(s => s.ServiceType == t)) {
        Console.WriteLine($"A service of type {t.Name} has already been registered");
      } else {
        services.AddScoped(t);
      }
    });

    return services;
  }
}