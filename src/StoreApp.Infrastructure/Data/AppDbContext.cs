using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using StoreApp.Domain.Entities;

namespace StoreApp.Infrastructure.Data;

/// <summary>SQLite has no real decimal type. Money is stored as integer hundredths: exact and fast.</summary>
public sealed class MoneyConverter() : ValueConverter<decimal, long>(
    v => (long)Math.Round(v * 100m, MidpointRounding.AwayFromZero),
    v => v / 100m);

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<int>, int>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<StoreSettings> Settings => Set<StoreSettings>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
        => builder.Properties<decimal>().HaveConversion<MoneyConverter>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        base.OnModelCreating(m);

        // No accidental cascade deletes: history must survive.
        foreach (var fk in m.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        m.Entity<Sale>().HasMany(s => s.Items).WithOne(i => i.Sale).HasForeignKey(i => i.SaleId).OnDelete(DeleteBehavior.Cascade);
        m.Entity<Sale>().HasMany(s => s.Payments).WithOne(p => p.Sale).HasForeignKey(p => p.SaleId).OnDelete(DeleteBehavior.Cascade);

        m.Entity<Product>(e =>
        {
            e.Property(p => p.Sku).HasMaxLength(64);
            e.Property(p => p.Name).HasMaxLength(200);
            e.Property(p => p.Barcode).HasMaxLength(64);
            e.HasIndex(p => p.Sku).IsUnique();
            e.HasIndex(p => p.Barcode).IsUnique().HasFilter("\"Barcode\" IS NOT NULL");
            e.HasIndex(p => p.Name);
        });
        m.Entity<Sale>().HasIndex(s => s.Number).IsUnique();
        m.Entity<Sale>().HasIndex(s => s.CompletedAt);
        m.Entity<StockMovement>().HasIndex(x => new { x.ProductId, x.CreatedAt });
        m.Entity<AuditLog>().HasIndex(a => a.Timestamp);
        m.Entity<AuditLog>().HasIndex(a => new { a.EntityName, a.EntityId });
    }
}
