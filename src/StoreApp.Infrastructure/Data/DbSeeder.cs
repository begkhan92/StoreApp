using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StoreApp.Domain.Common;
using StoreApp.Domain.Entities;

namespace StoreApp.Infrastructure.Data;

public static class DbSeeder
{
    private static readonly (string Cat, string Name, decimal Cost, decimal Price)[] Items =
    [
        ("Смартфоны", "Смартфон Galaxy A15 128GB", 150, 189), ("Смартфоны", "Смартфон Redmi Note 13 128GB", 170, 215),
        ("Смартфоны", "Смартфон iPhone 13 128GB", 480, 559), ("Смартфоны", "Смартфон Poco X6 256GB", 230, 279),
        ("Смартфоны", "Смартфон Honor X8b 256GB", 190, 239), ("Смартфоны", "Смартфон Realme C67 128GB", 140, 175),
        ("Ноутбуки", "Ноутбук Lenovo IdeaPad 3 15\"", 410, 489), ("Ноутбуки", "Ноутбук ASUS Vivobook 15", 450, 529),
        ("Ноутбуки", "Ноутбук HP 250 G9", 380, 455), ("Ноутбуки", "Ноутбук Acer Aspire 5", 430, 509),
        ("Ноутбуки", "Ноутбук MacBook Air M1", 760, 869), ("Ноутбуки", "Ноутбук MSI Modern 14", 400, 479),
        ("Телевизоры", "Телевизор LG 43\" 4K", 280, 335), ("Телевизоры", "Телевизор Samsung 50\" 4K", 340, 405),
        ("Телевизоры", "Телевизор Xiaomi 55\" 4K", 330, 389), ("Телевизоры", "Телевизор Hisense 32\"", 140, 175),
        ("Аксессуары", "Кабель USB-C 1м", 1.5m, 4.9m), ("Аксессуары", "Кабель Lightning 1м", 2.5m, 7.9m),
        ("Аксессуары", "Зарядка 20W USB-C", 4, 9.9m), ("Аксессуары", "Наушники TWS Basic", 9, 19.9m),
        ("Аксессуары", "Чехол для смартфона универсальный", 1, 4.5m), ("Аксессуары", "Защитное стекло 6.5\"", 0.8m, 3.9m),
        ("Аксессуары", "Powerbank 10000 mAh", 8, 17.9m), ("Аксессуары", "Мышь беспроводная", 4, 9.9m),
        ("Аксессуары", "Клавиатура USB", 5, 12.9m), ("Аксессуары", "Флешка 64GB", 3.5m, 8.5m),
        ("Бытовая техника", "Чайник электрический 1.8л", 11, 19.9m), ("Бытовая техника", "Пылесос 2000W", 45, 69),
        ("Бытовая техника", "Микроволновая печь 20л", 55, 79), ("Бытовая техника", "Утюг паровой", 14, 24.9m),
        ("Бытовая техника", "Блендер погружной", 12, 22.9m), ("Бытовая техника", "Фен 2200W", 9, 17.9m),
    ];

    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        sp.GetRequiredService<AuditContext>().Suppress = true; // seed data is not user activity
        var db = sp.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();
        await EnableWalAsync(db);
        await SeedIdentityAsync(sp, config);
        if (!await db.Products.AnyAsync()) await SeedCatalogAsync(db);
    }

    private static async Task EnableWalAsync(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL;"; // persistent in the db file
        await cmd.ExecuteScalarAsync();
    }

    private static async Task SeedIdentityAsync(IServiceProvider sp, IConfiguration config)
    {
        var roles = sp.GetRequiredService<RoleManager<IdentityRole<int>>>();
        foreach (var r in Roles.All)
            if (!await roles.RoleExistsAsync(r)) await roles.CreateAsync(new IdentityRole<int>(r));

        var email = config["Seed:AdminEmail"];
        var password = config["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;

        var users = sp.GetRequiredService<UserManager<AppUser>>();
        if (await users.FindByEmailAsync(email) is not null) return;
        var admin = new AppUser { UserName = email, Email = email, FullName = "Владелец", EmailConfirmed = true };
        var res = await users.CreateAsync(admin, password);
        if (res.Succeeded) await users.AddToRoleAsync(admin, Roles.Owner);
    }

    private static async Task SeedCatalogAsync(AppDbContext db)
    {
        var cats = Items.Select(i => i.Cat).Distinct().Select(n => new Category { Name = n }).ToList();
        db.Categories.AddRange(cats);
        await db.SaveChangesAsync();

        var rnd = new Random(42);
        var products = Items.Select((it, i) => new Product
        {
            Sku = $"SKU-{i + 1:D4}",
            Barcode = $"46010{i + 1:D8}",
            Name = it.Name,
            CategoryId = cats.First(c => c.Name == it.Cat).Id,
            PurchasePrice = it.Cost,
            SalePrice = it.Price,
            StockQty = rnd.Next(0, 31),
            MinStock = rnd.Next(2, 8),
        }).ToList();
        db.Products.AddRange(products);
        await db.SaveChangesAsync();

        db.StockMovements.AddRange(products.Where(p => p.StockQty > 0).Select(p => new StockMovement
        {
            ProductId = p.Id, Type = StockMovementType.Receipt, Quantity = p.StockQty, Reason = "Начальный остаток"
        }));
        await db.SaveChangesAsync();

        db.Customers.AddRange(
            new Customer { Name = "Иван Петров", Phone = "+99365000001" },
            new Customer { Name = "ООО «Ремонт+»", Company = "Ремонт+", Phone = "+99365000002", DiscountPercent = 5 },
            new Customer { Name = "Мария Ахмедова", Phone = "+99365000003" });
        await db.SaveChangesAsync();
    }
}
