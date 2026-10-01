namespace StoreApp.Web.Pages.Products;

public class IndexModel(AppDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Q { get; set; }
    public List<Product> Items { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var query = db.Products.AsNoTracking().Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(Q))
        {
            var q = Q.Trim().ToLowerInvariant();
            query = query.Where(p => p.SearchText.Contains(q));
        }
        Items = await query.OrderBy(p => p.Name).Take(50).ToListAsync();

        Response.Headers.Vary = "HX-Request"; // browser back button must not show the partial
        return Request.Headers.ContainsKey("HX-Request") ? Partial("_ProductRows", this) : Page();
    }
}
