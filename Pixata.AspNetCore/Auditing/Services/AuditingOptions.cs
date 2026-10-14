using System.Linq.Expressions;
using System.Reflection;

namespace Pixata.AspNetCore.Auditing.Services;

/// <summary>
/// Options for <c>AddAuditing&lt;TContext&gt;()</c>. Inherits the retention settings from <see cref="AuditRetentionOptions"/>
/// </summary>
public class AuditingOptions : AuditRetentionOptions {
  private readonly List<(Type EntityType, string PropertyName)> _excludedProperties = [];

  /// <summary>
  /// Whether added entities, and the audit rows that record their creation, are saved in one transaction. The audit rows for added
  /// entities can only be written after the database has generated their keys, which needs a second save. When this is true (the default)
  /// and there is no transaction already, the interceptor starts one before the first save and commits it after the second, so either
  /// both are saved or neither is. It does not do this when the provider isn't relational, or when the context uses a retrying execution
  /// strategy (eg <c>EnableRetryOnFailure</c>), as EF Core doesn't allow a transaction to be started inside one. In those cases, wrap the
  /// save in a transaction of your own if you need the guarantee
  /// </summary>
  public bool UseTransactionForAddedEntities { get; set; } = true;

  /// <summary>
  /// Leaves a property out of the audit trail, for types you can't decorate with <c>[NoAudit]</c> (ASP.NET Core Identity's user, for example).
  /// The property is written as <see cref="Pixata.Extensions.Auditing.Models.Audit.HiddenValue"/> in the snapshot, and in the changed
  /// properties when its value changes, so you can see that it changed, but not what to. Applies to <typeparamref name="T"/> and any type
  /// derived from it
  /// </summary>
  public AuditingOptions ExcludeProperty<T>(Expression<Func<T, object?>> property) {
    Expression body = property.Body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary ? unary.Operand : property.Body;
    if (body is not MemberExpression { Member: PropertyInfo or FieldInfo } member || member.Expression is not ParameterExpression) {
      throw new ArgumentException($"The expression must be a property of {typeof(T).Name}, eg u => u.PasswordHash", nameof(property));
    }
    return ExcludeProperty<T>(member.Member.Name);
  }

  /// <summary>
  /// Leaves a property out of the audit trail by name. Use this for shadow properties, which have no CLR property to point at
  /// </summary>
  public AuditingOptions ExcludeProperty<T>(string propertyName) {
    _excludedProperties.Add((typeof(T), propertyName));
    return this;
  }

  internal bool IsExcluded(Type entityType, string propertyName) =>
    _excludedProperties.Any(p => p.PropertyName == propertyName && p.EntityType.IsAssignableFrom(entityType));
}
