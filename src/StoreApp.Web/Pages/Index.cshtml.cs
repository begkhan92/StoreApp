namespace StoreApp.Web.Pages;

public class IndexModel(AppDbContext db) : PageModel
{
    public int ProductCount { get; private set; }
    public int LowStock { get; private set; }

    public async Task OnGetAsync()
    {
        ProductCount = await db.Products.CountAsync(p => p.IsActive);
        LowStock = await db.Products.CountAsync(p => p.IsActive && p.StockQty <= p.MinStock);
    }
}
