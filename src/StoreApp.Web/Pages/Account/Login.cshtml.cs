namespace StoreApp.Web.Pages.Account;

[AllowAnonymous]
public class LoginModel(SignInManager<AppUser> signIn, IStringLocalizer T) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public string? Error { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Введите email"), EmailAddress(ErrorMessage = "Некорректный email")]
        public string Email { get; set; } = "";
        [Required(ErrorMessage = "Введите пароль")]
        public string Password { get; set; } = "";
        public bool Remember { get; set; }
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid) return Page();
        var r = await signIn.PasswordSignInAsync(Input.Email, Input.Password, Input.Remember, lockoutOnFailure: true);
        if (r.Succeeded) return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
        Error = T["Неверный email или пароль"];
        return Page();
    }
}
