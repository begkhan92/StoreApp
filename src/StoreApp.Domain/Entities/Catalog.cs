using StoreApp.Domain.Common;
namespace StoreApp.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = "";
}

public class Product : BaseEntity
{
    public string Sku { get; set; } = "";
    public string? Barcode { get; set; }
    public string Name { get; set; } = "";
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    public string Unit { get; set; } = "шт";
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public int StockQty { get; set; }
    public int MinStock { get; set; }
    public string? PhotoPath { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Lower-cased "name sku barcode", filled on save. SQLite LIKE is not case-insensitive for Cyrillic, so we search this column.</summary>
    public string SearchText { get; set; } = "";
}

public enum StockMovementType { Receipt, WriteOff, Adjustment, Sale, Return }

public class StockMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public StockMovementType Type { get; set; }
    /// <summary>Signed: positive increases stock, negative decreases.</summary>
    public int Quantity { get; set; }
    public string? Reason { get; set; }
    public string? Reference { get; set; }
    public int? UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
