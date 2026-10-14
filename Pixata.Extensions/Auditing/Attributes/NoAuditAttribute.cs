using System;

namespace Pixata.Extensions.Auditing.Attributes;

/// <summary>
/// On a class, stops the entity from being audited at all. On a property, the entity is audited, but the property's value is written as
/// <see cref="Models.Audit.HiddenValue"/>, so that sensitive data (password hashes, API tokens, bank details) isn't copied into the audit table
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class NoAuditAttribute : Attribute;
