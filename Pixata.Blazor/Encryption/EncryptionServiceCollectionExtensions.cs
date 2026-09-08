using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Pixata.Extensions.Encryption;

namespace Pixata.Blazor.Encryption;

public static class EncryptionServiceCollectionExtensions {
  public static IServiceCollection AddEncryptedHttpClient<TInterface, TImplementation>(
    this IServiceCollection services,
    Action<EncryptionOptions> configureOptions,
    Action<HttpClient> configureClient)
    where TInterface : class
    where TImplementation : class, TInterface {
    services.Configure(configureOptions);
    services.AddSingleton<PayloadEncryptorInterface, SubtleCryptoEncryptor>();
    services.AddTransient<EncryptingHandler>();

    services.AddHttpClient("EncryptionHandshake", configureClient);

    services.AddHttpClient<TInterface, TImplementation>(configureClient)
      .AddHttpMessageHandler<EncryptingHandler>();

    return services;
  }

  public static IServiceCollection AddEncryptedHttpClient<TInterface, TImplementation>(
    this IServiceCollection services,
    string siteId,
    Action<HttpClient> configureClient)
    where TInterface : class
    where TImplementation : class, TInterface =>
    services.AddEncryptedHttpClient<TInterface, TImplementation>(
      options => options.SiteId = siteId,
      configureClient);
}
