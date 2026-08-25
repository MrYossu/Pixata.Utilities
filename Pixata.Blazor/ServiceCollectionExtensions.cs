using System;
using System.Collections.Generic;
using System.Linq;
using Blazored.LocalStorage;
using Microsoft.Extensions.DependencyInjection;
using Pixata.Blazor.Extensions;
using Pixata.Blazor.Notifications;

namespace Pixata.Blazor;

public static class ServiceCollectionExtensions {
  public static IServiceCollection AddPixataBlazor(this IServiceCollection services) {
    // ScrollStateService (and so the VirtualiseWithState component) needs Blazored LocalStorage. This package references it, but used to
    // leave it to the app to register, which meant the container failed validation at startup unless you knew to add it yourself
    if (services.Any(s => s.ServiceType == typeof(ILocalStorageService))) {
      Console.WriteLine($"A service of type {nameof(ILocalStorageService)} has already been registered");
    } else {
      services.AddBlazoredLocalStorage();
    }

    List<Type> types = [
      typeof(MessageBrokerInstance),
      typeof(NotificationHelper),
      typeof(PasswordOptionsHelper),
      typeof(PersistentStateHelper<>),
      typeof(ScrollStateService),
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