using StoreApp.Web.Security;
namespace StoreApp.Web.Pages.Admin;

[Authorize(Policy = Policies.ViewAudit)]
public class AuditModel(AppDbContext db) : TablePageModel
{
    public PagedResult<AuditLog> Result { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync()
    {
        Result = await db.AuditLogs.AsNoTracking().OrderByDescending(a => a.Id).ToPagedAsync(PageNo, PageSize);
        PageNo = Result.Page;
        PageSize = Result.PageSize;
        Pager = PagerFor(Result);
        return PageOrPartial("_AuditTable");
    }
}