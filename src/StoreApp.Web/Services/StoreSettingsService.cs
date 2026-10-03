namespace StoreApp.Web.Services;

/// <summary>Cached store settings + the store's clock. All timestamps in the DB are UTC; this converts them for people.</summary>
public sealed class StoreSettingsService(IServiceScopeFactory scopes)
{
    private volatile StoreSettings _current = new();
    private volatile TimeZoneInfo _zone = Fallback();

    public StoreSettings Current => _current;
    public TimeZoneInfo Zone => _zone;

    public async Task RefreshAsync()
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var s = await db.Settings.AsNoTracking().OrderBy(x => x.Id).FirstOrDefaultAsync() ?? new StoreSettings();
        _zone = Resolve(s.TimeZoneId);
        _current = s;
    }

    private static TimeZoneInfo Resolve(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch { return Fallback(); }
    }

    private static TimeZoneInfo Fallback()
        => TimeZoneInfo.CreateCustomTimeZone("TM", TimeSpan.FromHours(5), "Turkmenistan", "Turkmenistan");

    public DateTime ToLocal(DateTime utc)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), _zone);

    public string Local(DateTime utc, string format = "dd.MM.yyyy HH:mm")
        => ToLocal(utc).ToString(format, CultureInfo.InvariantCulture);

    /// <summary>[start, end) of a store-local day in UTC. Use for "sales today" and daily reports.</summary>
    public (DateTime StartUtc, DateTime EndUtc) DayUtc(DateTime localDate)
    {
        var d = DateTime.SpecifyKind(localDate.Date, DateTimeKind.Unspecified);
        return (TimeZoneInfo.ConvertTimeToUtc(d, _zone), TimeZoneInfo.ConvertTimeToUtc(d.AddDays(1), _zone));
    }

    public (DateTime StartUtc, DateTime EndUtc) TodayUtc() => DayUtc(ToLocal(DateTime.UtcNow));

    public string Money(decimal v) => $"{v.ToString("N2", CultureInfo.CurrentCulture)} {_current.Currency}";
}