namespace StoreApp.Web.Pages.Products;

public class IndexModel(AppDbContext db) : TablePageModel
{
    [BindProperty(SupportsGet = true)] public bool Low { get; set; }
    public PagedResult<Product> Result { get; private set; } = null!;

    protected override void AddFilters(RouteValueDictionary v) => v["low"] = Low ? "true" : null;

    public async Task<IActionResult> OnGetAsync()
    {
        var query = db.Products.AsNoTracking().Include(p => p.Category).Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(Q))
        {
            var s = Q.Trim().ToLowerInvariant();
            query = query.Where(p => p.SearchText.Contains(s));
        }
        if (Low) query = query.Where(p => p.StockQty <= p.MinStock);

        var ordered = (Sort, Desc) switch
        {
            ("sku", false) => query.OrderBy(p => p.Sku),
            ("sku", true) => query.OrderByDescending(p => p.Sku),
            ("price", false) => query.OrderBy(p => p.SalePrice),
            ("price", true) => query.OrderByDescending(p => p.SalePrice),
            ("stock", false) => query.OrderBy(p => p.StockQty),
            ("stock", true) => query.OrderByDescending(p => p.StockQty),
            ("name", true) => query.OrderByDescending(p => p.Name),
            _ => query.OrderBy(p => p.Name),
        };
        // The tie-breaker keeps pages stable when many rows share the same value.
        Result = await ordered.ThenBy(p => p.Id).ToPagedAsync(PageNo, PageSize);

        PageNo = Result.Page;
        PageSize = Result.PageSize;
        Pager = PagerFor(Result);
        return PageOrPartial("_Table");
    }
}