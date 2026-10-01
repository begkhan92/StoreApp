using StoreApp.Web.Security;
namespace StoreApp.Web.Pages.Admin;

[Authorize(Policy = Policies.ViewAudit)]
public class AuditModel(AppDbContext db) : PageModel
{
    public List<AuditLog> Items { get; private set; } = [];
    public async Task OnGetAsync()
        => Items = await db.AuditLogs.AsNoTracking().OrderByDescending(a => a.Id).Take(100).ToListAsync();
}
