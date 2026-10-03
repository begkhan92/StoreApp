namespace StoreApp.Web.Pages;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ErrorModel : PageModel
{
    public int Code { get; private set; } = 500;
    public string RequestId => HttpContext.TraceIdentifier;

    public void OnGet(int? code) => Code = code ?? 500;
    public void OnPost(int? code) => Code = code ?? 500; // the exception handler re-runs the original verb
}