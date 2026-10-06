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

  private readonly List<PendingAddedAudit> _pendingAddedAudits = [];

  // The transaction this interceptor started (if any) so that added entities and their audit rows are saved together
  private IDbContextTransaction? _ownedTransaction;

  // True while the interceptor is saving the audit rows for added entities, so that it doesn't try to audit its own save
  private bool _savingAudits;

  #region Synchronous

  public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) {
    if (eventData.Context is not null && !_savingAudits) {
      AddAudits(eventData.Context, GetChangedBy(GetAuthenticationStateUserName()));
      if (ShouldStartTransaction(eventData.Context)) {
        _ownedTransaction = eventData.Context.Database.BeginTransaction();
      }
    }
    return base.SavingChanges(eventData, result);
  }

  public override int SavedChanges(SaveChangesCompletedEventData eventData, int result) {
    if (_pendingAddedAudits.Count > 0 && eventData.Context is not null && !_savingAudits) {
      DbContext context = eventData.Context;
      try {
        AddPendingAddedAudits(context);
        _savingAudits = true;
        context.SaveChanges();
        _savingAudits = false;
        _ownedTransaction?.Commit();
        DisposeOwnedTransaction();
      }
      catch {
        _savingAudits = false;
        _ownedTransaction?.Rollback();
        DisposeOwnedTransaction();
        throw;
      }
    }
    return base.SavedChanges(eventData, result);
  }

  public override void SaveChangesFailed(DbContextErrorEventData eventData) {
    if (!_savingAudits) {
      _pendingAddedAudits.Clear();
      _ownedTransaction?.Rollback();
      DisposeOwnedTransaction();
    }
    base.SaveChangesFailed(eventData);
  }

  #endregion

  #region Asynchronous

  public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) {
    if (eventData.Context is not null && !_savingAudits) {
      string? authenticationStateUserName = await GetAuthenticationStateUserNameAsync();
      AddAudits(eventData.Context, GetChangedBy(authenticationStateUserName));
      if (ShouldStartTransaction(eventData.Context)) {
        _ownedTransaction = await eventData.Context.Database.BeginTransactionAsync(cancellationToken);
      }
    }
    return await base.SavingChangesAsync(eventData, result, cancellationToken);
  }

  public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default) {
    if (_pendingAddedAudits.Count > 0 && eventData.Context is not null && !_savingAudits) {
      DbContext context = eventData.Context;
      try {
        AddPendingAddedAudits(context);
        _savingAudits = true;
        await context.SaveChangesAsync(cancellationToken);
        _savingAudits = false;
        if (_ownedTransaction is not null) {
          await _ownedTransaction.CommitAsync(cancellationToken);
        }
        await DisposeOwnedTransactionAsync();
      }
      catch {
        _savingAudits = false;
        if (_ownedTransaction is not null) {
          await _ownedTransaction.RollbackAsync(CancellationToken.None);
        }
        await DisposeOwnedTransactionAsync();
        throw;
      }
    }
    return await base.SavedChangesAsync(eventData, result, cancellationToken);
  }

  public override async Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default) {
    if (!_savingAudits) {
      _pendingAddedAudits.Clear();
      if (_ownedTransaction is not null) {
        await _ownedTransaction.RollbackAsync(CancellationToken.None);
      }
      await DisposeOwnedTransactionAsync();
    }
    await base.SaveChangesFailedAsync(eventData, cancellationToken);
  }

  #endregion

  private void AddAudits(DbContext context, string changedBy) {
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
        _pendingAddedAudits.Add(new PendingAddedAudit(entry, entityTypeName, changedBy, changedAt));
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

  private void AddPendingAddedAudits(DbContext context) {
    foreach (PendingAddedAudit pending in _pendingAddedAudits) {
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
    _pendingAddedAudits.Clear();
  }

  // Only start a transaction when there will be a second save, and only when we can. EF Core won't start one inside a retrying execution
  // strategy, and the in-memory provider doesn't support them at all
  private bool ShouldStartTransaction(DbContext context) =>
    options.UseTransactionForAddedEntities
    && _pendingAddedAudits.Count > 0
    && _ownedTransaction is null
    && context.Database.IsRelational()
    && context.Database.CurrentTransaction is null
    && System.Transactions.Transaction.Current is null
    && !context.Database.CreateExecutionStrategy().RetriesOnFailure;

  private void DisposeOwnedTransaction() {
    _ownedTransaction?.Dispose();
    _ownedTransaction = null;
  }

  private async Task DisposeOwnedTransactionAsync() {
    if (_ownedTransaction is not null) {
      await _ownedTransaction.DisposeAsync();
    }
    _ownedTransaction = null;
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
