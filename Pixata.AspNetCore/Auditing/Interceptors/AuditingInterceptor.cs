using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Pixata.AspNetCore.Auditing.Services;
using Pixata.Extensions.Auditing.Attributes;
using Pixata.Extensions.Auditing.Models;

namespace Pixata.AspNetCore.Auditing.Interceptors;

public class AuditingInterceptor(IHttpContextAccessor httpContextAccessor, AuditUserContextInterface auditUserContext, AuditingOptions options, IServiceProvider serviceProvider) : SaveChangesInterceptor {
  private static readonly JsonSerializerOptions JsonOptions = new() {
    WriteIndented = false,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  private record PendingAddedAudit(EntityEntry Entry, string EntityTypeName, string ChangedBy, DateTime ChangedAt);

  // What the interceptor needs to remember between the start and end of one context's save
  private class SaveState {
    public List<PendingAddedAudit> PendingAddedAudits { get; } = [];

    // The transaction this interceptor started (if any) so that added entities and their audit rows are saved together
    public IDbContextTransaction? OwnedTransaction { get; set; }

    // True while the interceptor is saving the audit rows for added entities, so that it doesn't try to audit its own save
    public bool SavingAudits { get; set; }
  }

  // One interceptor can serve several contexts at once, eg every context an IDbContextFactory creates in a Blazor Server circuit, and two of
  // those can be saving at the same time. So the state is kept per context, and goes when the context does
  private readonly ConditionalWeakTable<DbContext, SaveState> _states = new();

  private SaveState GetState(DbContext context) =>
    _states.GetValue(context, _ => new SaveState());

  #region Synchronous

  public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) {
    if (eventData.Context is { } context && GetState(context) is { SavingAudits: false } state) {
      AddAudits(context, state, GetChangedBy(GetAuthenticationStateUserName()));
      if (ShouldStartTransaction(context, state)) {
        state.OwnedTransaction = context.Database.BeginTransaction();
      }
    }
    return base.SavingChanges(eventData, result);
  }

  public override int SavedChanges(SaveChangesCompletedEventData eventData, int result) {
    if (eventData.Context is { } context && GetState(context) is { SavingAudits: false, PendingAddedAudits.Count: > 0 } state) {
      try {
        AddPendingAddedAudits(context, state);
        state.SavingAudits = true;
        context.SaveChanges();
        state.SavingAudits = false;
        state.OwnedTransaction?.Commit();
        DisposeOwnedTransaction(state);
      }
      catch {
        state.SavingAudits = false;
        state.OwnedTransaction?.Rollback();
        DisposeOwnedTransaction(state);
        throw;
      }
    }
    return base.SavedChanges(eventData, result);
  }

  public override void SaveChangesFailed(DbContextErrorEventData eventData) {
    AbandonSave(eventData.Context);
    base.SaveChangesFailed(eventData);
  }

  public override void SaveChangesCanceled(DbContextEventData eventData) {
    AbandonSave(eventData.Context);
    base.SaveChangesCanceled(eventData);
  }

  #endregion

  #region Asynchronous

  public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) {
    if (eventData.Context is { } context && GetState(context) is { SavingAudits: false } state) {
      string? authenticationStateUserName = await GetAuthenticationStateUserNameAsync();
      AddAudits(context, state, GetChangedBy(authenticationStateUserName));
      if (ShouldStartTransaction(context, state)) {
        state.OwnedTransaction = await context.Database.BeginTransactionAsync(cancellationToken);
      }
    }
    return await base.SavingChangesAsync(eventData, result, cancellationToken);
  }

  public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default) {
    if (eventData.Context is { } context && GetState(context) is { SavingAudits: false, PendingAddedAudits.Count: > 0 } state) {
      try {
        AddPendingAddedAudits(context, state);
        state.SavingAudits = true;
        await context.SaveChangesAsync(cancellationToken);
        state.SavingAudits = false;
        if (state.OwnedTransaction is not null) {
          await state.OwnedTransaction.CommitAsync(cancellationToken);
        }
        await DisposeOwnedTransactionAsync(state);
      }
      catch {
        state.SavingAudits = false;
        if (state.OwnedTransaction is not null) {
          await state.OwnedTransaction.RollbackAsync(CancellationToken.None);
        }
        await DisposeOwnedTransactionAsync(state);
        throw;
      }
    }
    return await base.SavedChangesAsync(eventData, result, cancellationToken);
  }

  public override async Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default) {
    await AbandonSaveAsync(eventData.Context);
    await base.SaveChangesFailedAsync(eventData, cancellationToken);
  }

  public override async Task SaveChangesCanceledAsync(DbContextEventData eventData, CancellationToken cancellationToken = default) {
    await AbandonSaveAsync(eventData.Context);
    await base.SaveChangesCanceledAsync(eventData, cancellationToken);
  }

  #endregion

  // The save failed or was cancelled, so throw away the pending audit rows and roll back our transaction. A failure of our own save of
  // the audit rows is dealt with where it's made
  private void AbandonSave(DbContext? context) {
    if (context is not null && GetState(context) is { SavingAudits: false } state) {
      state.PendingAddedAudits.Clear();
      state.OwnedTransaction?.Rollback();
      DisposeOwnedTransaction(state);
    }
  }

  private async Task AbandonSaveAsync(DbContext? context) {
    if (context is not null && GetState(context) is { SavingAudits: false } state) {
      state.PendingAddedAudits.Clear();
      if (state.OwnedTransaction is not null) {
        await state.OwnedTransaction.RollbackAsync(CancellationToken.None);
      }
      await DisposeOwnedTransactionAsync(state);
    }
  }

  private void AddAudits(DbContext context, SaveState state, string changedBy) {
    DateTime changedAt = DateTime.UtcNow;

    List<EntityEntry> entries = context.ChangeTracker.Entries()
      .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
      .Where(e => e.Entity.GetType() != typeof(Audit))
      .Where(e => !e.Entity.GetType().IsDefined(typeof(NoAuditAttribute), inherit: true))
      .ToList();

    foreach (EntityEntry entry in entries) {
      Type entityType = entry.Entity.GetType();
      string entityTypeName = entityType.FullName ?? entityType.Name;

      if (entry.State == EntityState.Added) {
        // Defer audit creation for Added entities: the database-generated ID is not yet
        // available at this point. The real ID will be resolved after SaveChanges completes.
        state.PendingAddedAudits.Add(new PendingAddedAudit(entry, entityTypeName, changedBy, changedAt));
        continue;
      }

      AuditOperation operation = entry.State switch {
        EntityState.Modified => AuditOperation.Updated,
        EntityState.Deleted => AuditOperation.Deleted,
        _ => throw new ArgumentOutOfRangeException()
      };

      Audit audit = new() {
        EntityType = entityTypeName,
        EntityId = GetPrimaryKeyValue(entry),
        Operation = operation,
        ChangedBy = changedBy,
        ChangedAt = changedAt,
        FullSnapshot = SerialiseEntity(entry),
        ChangedProperties = operation == AuditOperation.Updated ? SerialiseChangedProperties(entry) : null
      };

      context.Set<Audit>().Add(audit);
    }
  }

  private void AddPendingAddedAudits(DbContext context, SaveState state) {
    foreach (PendingAddedAudit pending in state.PendingAddedAudits) {
      // The database has now assigned the real ID, so we can read it from the entry.
      Audit audit = new() {
        EntityType = pending.EntityTypeName,
        EntityId = GetPrimaryKeyValue(pending.Entry),
        Operation = AuditOperation.Created,
        ChangedBy = pending.ChangedBy,
        ChangedAt = pending.ChangedAt,
        FullSnapshot = SerialiseEntity(pending.Entry),
        ChangedProperties = null
      };

      context.Set<Audit>().Add(audit);
    }
    state.PendingAddedAudits.Clear();
  }

  // Only start a transaction when there will be a second save, and only when we can. EF Core won't start one inside a retrying execution
  // strategy, and the in-memory provider doesn't support them at all
  private bool ShouldStartTransaction(DbContext context, SaveState state) =>
    options.UseTransactionForAddedEntities
    && state.PendingAddedAudits.Count > 0
    && state.OwnedTransaction is null
    && context.Database.IsRelational()
    && context.Database.CurrentTransaction is null
    && System.Transactions.Transaction.Current is null
    && !context.Database.CreateExecutionStrategy().RetriesOnFailure;

  private static void DisposeOwnedTransaction(SaveState state) {
    state.OwnedTransaction?.Dispose();
    state.OwnedTransaction = null;
  }

  private static async Task DisposeOwnedTransactionAsync(SaveState state) {
    if (state.OwnedTransaction is not null) {
      await state.OwnedTransaction.DisposeAsync();
    }
    state.OwnedTransaction = null;
  }

  // Order of precedence: an identifier set explicitly on AuditUserContextInterface, the user on the current HttpContext, the user from
  // Blazor's AuthenticationStateProvider (as HttpContext is null inside an interactive Blazor Server circuit), and finally "System"
  private string GetChangedBy(string? authenticationStateUserName) {
    if (!string.IsNullOrWhiteSpace(auditUserContext.UserIdentifier)) {
      return auditUserContext.UserIdentifier;
    }

    string? identityName = httpContextAccessor.HttpContext?.User?.Identity?.Name;
    if (!string.IsNullOrWhiteSpace(identityName)) {
      return identityName;
    }

    if (!string.IsNullOrWhiteSpace(authenticationStateUserName)) {
      return authenticationStateUserName;
    }

    return "System";
  }

  private bool NeedAuthenticationState() =>
    string.IsNullOrWhiteSpace(auditUserContext.UserIdentifier) && string.IsNullOrWhiteSpace(httpContextAccessor.HttpContext?.User?.Identity?.Name);

  private async Task<string?> GetAuthenticationStateUserNameAsync() {
    if (!NeedAuthenticationState() || serviceProvider.GetService<AuthenticationStateProvider>() is not { } provider) {
      return null;
    }
    try {
      AuthenticationState state = await provider.GetAuthenticationStateAsync();
      return state.User.Identity?.Name;
    }
    catch (InvalidOperationException) {
      // Thrown by the server-side provider when it's resolved outside a Blazor circuit (eg in an API request), before the state was set
      return null;
    }
  }

  // The synchronous version only uses the state if it's already available, as blocking on it inside a Blazor circuit could deadlock. In a
  // Blazor Server app, the state is normally available by the time anything is saved
  private string? GetAuthenticationStateUserName() {
    if (!NeedAuthenticationState() || serviceProvider.GetService<AuthenticationStateProvider>() is not { } provider) {
      return null;
    }
    try {
      Task<AuthenticationState> stateTask = provider.GetAuthenticationStateAsync();
      return stateTask.IsCompletedSuccessfully ? stateTask.Result.User.Identity?.Name : null;
    }
    catch (InvalidOperationException) {
      return null;
    }
  }

  private static string GetPrimaryKeyValue(EntityEntry entry) {
    Microsoft.EntityFrameworkCore.Metadata.IKey? primaryKey = entry.Metadata.FindPrimaryKey();
    if (primaryKey is null) {
      return "";
    }

    List<object?> keyValues = primaryKey.Properties
      .Select(p => entry.Property(p.Name).CurrentValue)
      .ToList();

    if (keyValues.Count == 1) {
      return keyValues[0]?.ToString() ?? "";
    }

    return JsonSerializer.Serialize(keyValues, JsonOptions);
  }

  private bool IsHidden(EntityEntry entry, PropertyEntry property) =>
    property.Metadata.PropertyInfo?.IsDefined(typeof(NoAuditAttribute), inherit: true) == true
    || options.IsExcluded(entry.Entity.GetType(), property.Metadata.Name);

  private string SerialiseEntity(EntityEntry entry) {
    Dictionary<string, object?> properties = new();
    foreach (PropertyEntry property in entry.Properties) {
      properties[property.Metadata.Name] = IsHidden(entry, property)
        ? Audit.HiddenValue
        : entry.State == EntityState.Deleted
          ? property.OriginalValue
          : property.CurrentValue;
    }

    return JsonSerializer.Serialize(properties, JsonOptions);
  }

  private string? SerialiseChangedProperties(EntityEntry entry) {
    Dictionary<string, object?[]> changed = new();
    foreach (PropertyEntry property in entry.Properties) {
      if (property.IsModified && !Equals(property.OriginalValue, property.CurrentValue)) {
        changed[property.Metadata.Name] = IsHidden(entry, property)
          ? [Audit.HiddenValue, Audit.HiddenValue]
          : [property.OriginalValue, property.CurrentValue];
      }
    }

    return changed.Count > 0 ? JsonSerializer.Serialize(changed, JsonOptions) : null;
  }
}
