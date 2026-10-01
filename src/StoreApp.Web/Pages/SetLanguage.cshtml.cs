namespace StoreApp.Web.Pages;

[AllowAnonymous]
public class SetLanguageModel : PageModel
{
    public IActionResult OnGet() => LocalRedirect("/");

    public IActionResult OnPost(string culture, string? returnUrl)
    {
        if (JsonStringLocalizer.Supported.Contains(culture))
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax });
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }
}
