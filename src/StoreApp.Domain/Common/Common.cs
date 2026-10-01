namespace StoreApp.Domain.Common;

/// <summary>Marker: changes to these entities are written to the audit log.</summary>
public interface IAuditable { int Id { get; } }

public abstract class BaseEntity : IAuditable
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public interface ICurrentUser
{
    int? UserId { get; }
    string? UserName { get; }
}

/// <summary>Scoped. Set Reason before SaveChanges (e.g. "Продажа №1842") to attach it to audit rows.</summary>
public class AuditContext
{
    public string? Reason { get; set; }
    public bool Suppress { get; set; }
}

public static class Roles
{
    public const string Owner = "Owner";
    public const string Manager = "Manager";
    public const string Employee = "Employee";
    public const string Demo = "Demo";
    public static readonly string[] All = [Owner, Manager, Employee, Demo];
}
