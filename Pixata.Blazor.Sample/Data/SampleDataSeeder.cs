using System;
using System.Linq;
using System.Text.Json;
using Pixata.AspNetCore.Auditing.Models;
using Pixata.Blazor.Sample.Models;

namespace Pixata.Blazor.Sample.Data;

public static class SampleDataSeeder {
  public static void Seed(SampleDbContext db) {
    if (db.Audits.Any()) {
      return;
    }

    string personType = typeof(Person).FullName!;
    string customerType = typeof(Customer).FullName!;

    // Person 1 - created then updated twice
    db.Audits.AddRange(
      new Audit {
        EntityType = personType,
        EntityId = "1",
        Operation = AuditOperation.Created,
        ChangedBy = "admin",
        ChangedAt = new DateTime(2026, 1, 10, 9, 0, 0),
        FullSnapshot = JsonSerializer.Serialize(new { Id = 1, Title = "Mr", FirstName = "Jim", Surname = "Smith", Email = "jim@example.com", DateOfBirth = "1985-03-15", EmailConfirmed = false, NumberOfDependents = 0 }),
        ChangedProperties = null
      },
      new Audit {
        EntityType = personType,
        EntityId = "1",
        Operation = AuditOperation.Updated,
        ChangedBy = "admin",
        ChangedAt = new DateTime(2026, 2, 5, 14, 30, 0),
        FullSnapshot = JsonSerializer.Serialize(new { Id = 1, Title = "Mr", FirstName = "Jim", Surname = "Smith", Email = "jim.smith@example.com", DateOfBirth = "1985-03-15", EmailConfirmed = true, NumberOfDependents = 0 }),
        ChangedProperties = JsonSerializer.Serialize(new { Email = new[] { "jim@example.com", "jim.smith@example.com" }, EmailConfirmed = new[] { "False", "True" } })
      },
      new Audit {
        EntityType = personType,
        EntityId = "1",
        Operation = AuditOperation.Updated,
        ChangedBy = "editor",
        ChangedAt = new DateTime(2026, 3, 12, 10, 15, 0),
        FullSnapshot = JsonSerializer.Serialize(new { Id = 1, Title = "Mr", FirstName = "Jim", Surname = "Smith", Email = "jim.smith@example.com", DateOfBirth = "1985-03-15", EmailConfirmed = true, NumberOfDependents = 2 }),
        ChangedProperties = JsonSerializer.Serialize(new { NumberOfDependents = new[] { "0", "2" } })
      }
    );

    // Person 2 - created then deleted
    db.Audits.AddRange(
      new Audit {
        EntityType = personType,
        EntityId = "2",
        Operation = AuditOperation.Created,
        ChangedBy = "admin",
        ChangedAt = new DateTime(2026, 1, 15, 11, 0, 0),
        FullSnapshot = JsonSerializer.Serialize(new { Id = 2, Title = "Mrs", FirstName = "Jane", Surname = "Doe", Email = "jane@example.com", DateOfBirth = "1990-07-22", EmailConfirmed = true, NumberOfDependents = 1 }),
        ChangedProperties = null
      },
      new Audit {
        EntityType = personType,
        EntityId = "2",
        Operation = AuditOperation.Deleted,
        ChangedBy = "admin",
        ChangedAt = new DateTime(2026, 4, 1, 16, 0, 0),
        FullSnapshot = JsonSerializer.Serialize(new { Id = 2, Title = "Mrs", FirstName = "Jane", Surname = "Doe", Email = "jane@example.com", DateOfBirth = "1990-07-22", EmailConfirmed = true, NumberOfDependents = 1 }),
        ChangedProperties = null
      }
    );

    // Customer 1 - created then updated
    db.Audits.AddRange(
      new Audit {
        EntityType = customerType,
        EntityId = "1",
        Operation = AuditOperation.Created,
        ChangedBy = "system",
        ChangedAt = new DateTime(2026, 1, 20, 8, 0, 0),
        FullSnapshot = JsonSerializer.Serialize(new { Id = 1, Name = "Acme Corp", Age = 5 }),
        ChangedProperties = null
      },
      new Audit {
        EntityType = customerType,
        EntityId = "1",
        Operation = AuditOperation.Updated,
        ChangedBy = "admin",
        ChangedAt = new DateTime(2026, 3, 10, 13, 45, 0),
        FullSnapshot = JsonSerializer.Serialize(new { Id = 1, Name = "Acme Corporation", Age = 5 }),
        ChangedProperties = JsonSerializer.Serialize(new { Name = new[] { "Acme Corp", "Acme Corporation" } })
      }
    );

    // Customer 2 - created only
    db.Audits.Add(
      new Audit {
        EntityType = customerType,
        EntityId = "2",
        Operation = AuditOperation.Created,
        ChangedBy = "system",
        ChangedAt = new DateTime(2026, 2, 1, 10, 0, 0),
        FullSnapshot = JsonSerializer.Serialize(new { Id = 2, Name = "Globex Inc", Age = 12 }),
        ChangedProperties = null
      }
    );

    db.SaveChanges();
  }
}
