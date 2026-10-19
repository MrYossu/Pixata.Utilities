using Microsoft.EntityFrameworkCore;
using Pixata.Extensions.Auditing.Models;

namespace Pixata.AspNetCore.Auditing.Extensions;

public static class ModelBuilderExtensions {
  /// <summary>
  /// Configures the <see cref="Audit"/> entity. Call it from your context's <c>OnModelCreating</c>. <see cref="Audit.FullSnapshot"/> and
  /// <see cref="Audit.ChangedProperties"/> hold JSON of any length, so this makes them unlimited, even if your context gives strings a
  /// maximum length in <c>ConfigureConventions</c> (eg with <c>Properties&lt;string&gt;().HaveMaxLength(200)</c>). Without it, they pick
  /// up that length, and on SQL Server, saving an entity whose JSON is longer throws "String or binary data would be truncated"
  /// </summary>
  public static ModelBuilder ConfigureAuditing(this ModelBuilder modelBuilder) {
    modelBuilder.Entity<Audit>(audit => {
      audit.Property(a => a.FullSnapshot).Metadata.SetMaxLength(null);
      audit.Property(a => a.ChangedProperties).Metadata.SetMaxLength(null);
    });
    return modelBuilder;
  }
}
