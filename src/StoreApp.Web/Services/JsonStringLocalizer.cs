using System.Text.Json;

namespace StoreApp.Web.Services;

/// <summary>
/// Reusable module. The Russian text IS the key: T["Товары"].
/// Resources/tk.json and en.json map key → translation. A missing key falls back to the key itself (Russian).
/// Implements the factory too, so DataAnnotations messages are localized with the same files.
/// </summary>
public sealed class JsonStringLocalizer : IStringLocalizer, IStringLocalizerFactory
{
    public static readonly string[] Supported = ["ru", "tk", "en"];
    private readonly Dictionary<string, Dictionary<string, string>> _data = new();

    public JsonStringLocalizer(IWebHostEnvironment env)
    {
        foreach (var lang in Supported.Where(l => l != "ru"))
        {
            var file = Path.Combine(env.ContentRootPath, "Resources", $"{lang}.json");
            if (File.Exists(file))
                _data[lang] = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file)) ?? [];
        }
    }

    private static string Lang => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
    private string? Find(string key) => _data.TryGetValue(Lang, out var d) && d.TryGetValue(key, out var v) ? v : null;

    public LocalizedString this[string name]
    {
        get { var v = Find(name); return new LocalizedString(name, v ?? name, v is null); }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get { var v = Find(name); return new LocalizedString(name, string.Format(v ?? name, arguments), v is null); }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        => _data.TryGetValue(Lang, out var d) ? d.Select(p => new LocalizedString(p.Key, p.Value)) : [];

    public IStringLocalizer Create(Type resourceSource) => this;
    public IStringLocalizer Create(string baseName, string location) => this;
}

/// <summary>Identity errors (password too short, duplicate email) in the user's language.</summary>
public sealed class LocalizedIdentityErrorDescriber(IStringLocalizer t) : IdentityErrorDescriber
{
    public override IdentityError PasswordTooShort(int length)
        => new() { Code = nameof(PasswordTooShort), Description = t["Пароль слишком короткий (минимум {0} символов).", length] };
    public override IdentityError DuplicateEmail(string email)
        => new() { Code = nameof(DuplicateEmail), Description = t["Этот email уже зарегистрирован."] };
    public override IdentityError DuplicateUserName(string userName)
        => new() { Code = nameof(DuplicateUserName), Description = t["Этот email уже зарегистрирован."] };
    public override IdentityError InvalidEmail(string? email)
        => new() { Code = nameof(InvalidEmail), Description = t["Некорректный email"] };
}
