using Microsoft.AspNetCore.Identity;
using StoreApp.Domain.Common;
namespace StoreApp.Domain.Entities;

public class AppUser : IdentityUser<int>
{
    public string FullName { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AuditLog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string EntityName { get; set; } = "";
    public string? EntityId { get; set; }
    public string Action { get; set; } = "";
    /// <summary>JSON: Added/Deleted = {field: value}; Modified = {field: [old, new]}.</summary>
    public string Changes { get; set; } = "{}";
    public string? Reason { get; set; }
}

public class StoreSettings : BaseEntity
{
    public string StoreName { get; set; } = "Мой магазин";
    public string Currency { get; set; } = "TMT";
    public string TimeZoneId { get; set; } = "Asia/Ashgabat";
    public string DefaultLanguage { get; set; } = "ru";
    public string ReceiptHeader { get; set; } = "";
    public string ReceiptFooter { get; set; } = "";
}
