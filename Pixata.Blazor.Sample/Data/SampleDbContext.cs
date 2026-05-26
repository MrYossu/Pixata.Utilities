using Microsoft.EntityFrameworkCore;
using Pixata.AspNetCore.Auditing.Models;
using Pixata.Blazor.Sample.Models;

namespace Pixata.Blazor.Sample.Data;

public class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options) {
  public DbSet<Person> People { get; set; }
  public DbSet<Customer> Customers { get; set; }
  public DbSet<Audit> Audits { get; set; }
}
