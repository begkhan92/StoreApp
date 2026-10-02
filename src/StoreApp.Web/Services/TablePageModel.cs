namespace StoreApp.Web.Services;

public sealed record PagerModel(
    int Page, int PageSize, int Total, int TotalPages, int From, int To,
    Func<int, string> PageUrl, Func<int, string> SizeUrl);

/// <summary>Base for list pages: search, paging and sorting live in the query string; htmx gets only the table.</summary>
public abstract class TablePageModel : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Q { get; set; }
    [BindProperty(SupportsGet = true, Name = "p")] public int PageNo { get; set; } = 1;
    [BindProperty(SupportsGet = true, Name = "size")] public int PageSize { get; set; } = 25;
    [BindProperty(SupportsGet = true)] public string? Sort { get; set; }
    [BindProperty(SupportsGet = true)] public bool Desc { get; set; }

    public PagerModel Pager { get; protected set; } = null!;

    /// <summary>True for htmx partial requests. Back/forward restore needs the full page.</summary>
    public bool IsHtmx =>
        Request.Headers.ContainsKey("HX-Request") && !Request.Headers.ContainsKey("HX-History-Restore-Request");

    /// <summary>Pages with extra filters add them here, so pager and sort links keep them.</summary>
    protected virtual void AddFilters(RouteValueDictionary values) { }

    public string UrlFor(int? p = null, int? size = null, string? sort = null, bool? desc = null)
    {
        var v = new RouteValueDictionary
        {
            ["q"] = string.IsNullOrWhiteSpace(Q) ? null : Q,
            ["p"] = p ?? PageNo,
            ["size"] = size ?? PageSize,
            ["sort"] = sort ?? Sort,
            ["desc"] = (desc ?? Desc) ? "true" : null,
        };
        AddFilters(v);
        return Url.Page(null, v) ?? "";
    }

    public string SortUrl(string column) => UrlFor(p: 1, sort: column, desc: Sort == column && !Desc);
    public string SortMark(string column) => Sort == column ? (Desc ? " ▼" : " ▲") : "";

    protected PagerModel PagerFor<T>(PagedResult<T> r) =>
        new(r.Page, r.PageSize, r.Total, r.TotalPages, r.From, r.To, n => UrlFor(p: n), s => UrlFor(p: 1, size: s));

    protected IActionResult PageOrPartial(string partialName)
    {
        Response.Headers.Vary = "HX-Request"; // the browser must not cache a partial as the page
        return IsHtmx ? Partial(partialName, this) : Page();
    }
}