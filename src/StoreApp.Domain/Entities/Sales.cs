using StoreApp.Domain.Common;
namespace StoreApp.Domain.Entities;

public class Customer : BaseEntity
{
    public string Name { get; set; } = "";
    public string? Company { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public decimal DiscountPercent { get; set; }
}

public enum SaleStatus { Draft, Completed, Refunded }
public enum PaymentMethod { Cash, Card, Transfer }

public class Sale : BaseEntity
{
    /// <summary>Assigned when the sale is completed; null for drafts.</summary>
    public string? Number { get; set; }
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public SaleStatus Status { get; set; } = SaleStatus.Draft;
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? UserId { get; set; }
    public List<SaleItem> Items { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
}

public class SaleItem
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public Sale? Sale { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    /// <summary>Cost at the time of sale, so profit stays correct when purchase prices change.</summary>
    public decimal UnitCost { get; set; }
}

public class Payment
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public Sale? Sale { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
}
