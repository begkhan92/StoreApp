using Microsoft.AspNetCore.Mvc.Rendering;
using StoreApp.Web.Security;
namespace StoreApp.Web.Pages.Admin;

[Authorize(Policy = Policies.ManageSettings)]
public class SettingsModel(AppDbContext db, StoreSettingsService store) : PageModel
{
    private static readonly string[] CurrencyList = ["TMT", "USD", "EUR", "RUB"];
    private static readonly string[] ZoneList =
        ["Asia/Ashgabat", "Asia/Tashkent", "Asia/Almaty", "Europe/Moscow", "Europe/Istanbul", "UTC"];

    public IEnumerable<SelectListItem> Currencies => CurrencyList.Select(c => new SelectListItem(c, c));
    public IEnumerable<SelectListItem> Zones => ZoneList.Select(z => new SelectListItem(z, z));
    public IEnumerable<SelectListItem> Languages =>
        [new("Русский", "ru"), new("Türkmençe", "tk"), new("English", "en")];

    [BindProperty] public InputModel Input { get; set; } = new();
    public bool Saved { get; private set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Заполните поле"), StringLength(100)] public string StoreName { get; set; } = "";
        [Required(ErrorMessage = "Заполните поле")] public string Currency { get; set; } = "";
        [Required(ErrorMessage = "Заполните поле")] public string TimeZoneId { get; set; } = "";
        [Required(ErrorMessage = "Заполните поле")] public string DefaultLanguage { get; set; } = "ru";
        [StringLength(500)] public string? ReceiptHeader { get; set; }
        [StringLength(500)] public string? ReceiptFooter { get; set; }
    }

    public async Task OnGetAsync()
    {
        var s = await db.Settings.AsNoTracking().OrderBy(x => x.Id).FirstAsync();
        Input = new InputModel
        {
            StoreName = s.StoreName,
            Currency = s.Currency,
            TimeZoneId = s.TimeZoneId,
            DefaultLanguage = s.DefaultLanguage,
            ReceiptHeader = s.ReceiptHeader,
            ReceiptFooter = s.ReceiptFooter,
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!CurrencyList.Contains(Input.Currency)) ModelState.AddModelError("Input.Currency", "Заполните поле");
        if (!ZoneList.Contains(Input.TimeZoneId)) ModelState.AddModelError("Input.TimeZoneId", "Заполните поле");
        if (!JsonStringLocalizer.Supported.Contains(Input.DefaultLanguage))
            ModelState.AddModelError("Input.DefaultLanguage", "Заполните поле");
        if (!ModelState.IsValid) return Page();

        var s = await db.Settings.OrderBy(x => x.Id).FirstAsync();
        s.StoreName = Input.StoreName.Trim();
        s.Currency = Input.Currency;
        s.TimeZoneId = Input.TimeZoneId;
        s.DefaultLanguage = Input.DefaultLanguage;
        s.ReceiptHeader = Input.ReceiptHeader?.Trim() ?? "";
        s.ReceiptFooter = Input.ReceiptFooter?.Trim() ?? "";
        await db.SaveChangesAsync(); // the audit log records who changed what
        await store.RefreshAsync();

        Saved = true;
        return Page();
    }
}