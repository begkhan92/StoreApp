using Microsoft.AspNetCore.RateLimiting;
namespace StoreApp.Web.Pages.Account;

[AllowAnonymous]
[EnableRateLimiting("register")]
public class RegisterModel(UserManager<AppUser> users, SignInManager<AppUser> signIn) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Введите имя")] public string FullName { get; set; } = "";
        [Required(ErrorMessage = "Введите email"), EmailAddress(ErrorMessage = "Некорректный email")]
        public string Email { get; set; } = "";
        [Required(ErrorMessage = "Введите пароль")] public string Password { get; set; } = "";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var user = new AppUser { UserName = Input.Email, Email = Input.Email, FullName = Input.FullName };
        var res = await users.CreateAsync(user, Input.Password);
        if (!res.Succeeded)
        {
            foreach (var e in res.Errors) ModelState.AddModelError("", e.Description);
            return Page();
        }
        await users.AddToRoleAsync(user, Roles.Demo); // open registration = Demo role only
        await signIn.SignInAsync(user, isPersistent: true);
        return LocalRedirect("/");
    }
}
