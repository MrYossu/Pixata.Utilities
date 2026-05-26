using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pixata.AspNetCore.Auditing.Interceptors;

namespace Pixata.AspNetCore.Auditing.Extensions;

public static class DbContextOptionsBuilderExtensions {
  public static DbContextOptionsBuilder AddAuditingInterceptor(this DbContextOptionsBuilder optionsBuilder, IServiceProvider serviceProvider) {
    AuditingInterceptor interceptor = serviceProvider.GetRequiredService<AuditingInterceptor>();
    optionsBuilder.AddInterceptors(interceptor);
    return optionsBuilder;
  }
}
