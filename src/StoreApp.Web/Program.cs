using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using Serilog.Events;
using StoreApp.Web.Security;

var builder = WebApplication.CreateBuilder(args);
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "App_Data"));

// Logs: console + daily files in App_Data/logs, 14 days kept.
builder.Host.UseSerilog((ctx, lc) => lc
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File(Path.Combine(ctx.HostingEnvironment.ContentRootPath, "App_Data", "logs", "log-.txt"),
        rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14));

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<StoreSettingsService>();
builder.Services.AddSingleton<JsonStringLocalizer>();
builder.Services.AddSingleton<IStringLocalizer>(sp => sp.GetRequiredService<JsonStringLocalizer>());
builder.Services.AddSingleton<IStringLocalizerFactory>(sp => sp.GetRequiredService<JsonStringLocalizer>());
builder.Services.Configure<RequestLocalizationOptions>(o =>
{
    var cultures = JsonStringLocalizer.Supported.Select(c => new CultureInfo(c)).ToList();
    o.DefaultRequestCulture = new RequestCulture("ru");
    o.SupportedCultures = cultures;
    o.SupportedUICultures = cultures;
    o.RequestCultureProviders =
    [
        new CookieRequestCultureProvider(), // the user's own choice
        new CustomRequestCultureProvider(ctx => // otherwise the store's default language
            Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(
                ctx.RequestServices.GetRequiredService<StoreSettingsService>().Current.DefaultLanguage))),
    ];
});

builder.Services.AddScoped<ICurrentUser, CurrentUserService>();
builder.Services.AddScoped<AuditContext>();
builder.Services.AddScoped<AuditSaveChangesInterceptor>();
builder.Services.AddDbContext<AppDbContext>((sp, o) => o
    .UseSqlite(builder.Configuration.GetConnectionString("Default"))
    .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

builder.Services.AddIdentity<AppUser, IdentityRole<int>>(o =>
{
    o.Password.RequiredLength = 6;
    o.Password.RequireNonAlphanumeric = false;
    o.Password.RequireUppercase = false;
    o.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<AppDbContext>()
  .AddErrorDescriber<LocalizedIdentityErrorDescriber>()
  .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/Denied";
    o.ExpireTimeSpan = TimeSpan.FromDays(7);
    o.SlidingExpiration = true;
});

builder.Services.AddAuthorization(o =>
{
    o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    o.AddPolicy(Policies.ManageProducts, p => p.RequireRole(Roles.Owner, Roles.Manager));
    o.AddPolicy(Policies.ViewReports, p => p.RequireRole(Roles.Owner, Roles.Manager));
    o.AddPolicy(Policies.ViewAudit, p => p.RequireRole(Roles.Owner, Roles.Manager));
    o.AddPolicy(Policies.ManageUsers, p => p.RequireRole(Roles.Owner));
    o.AddPolicy(Policies.ManageSettings, p => p.RequireRole(Roles.Owner));
});

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("register", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromHours(1) }));
});

builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");
builder.Services.Configure<ForwardedHeadersOptions>(o =>
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
builder.Services.AddResponseCompression();
builder.Services.AddHealthChecks().AddCheck<DbHealthCheck>("db");

builder.Services.AddRazorPages()
    .AddMvcOptions(o => o.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider()))
    .AddDataAnnotationsLocalization();

var app = builder.Build();

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/Error", "?code={0}");
app.UseResponseCompression();
app.UseStaticFiles();
app.UseSerilogRequestLogging();
app.UseRequestLocalization();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.MapHealthChecks("/health").AllowAnonymous();

await DbSeeder.SeedAsync(app.Services, app.Configuration);
await app.Services.GetRequiredService<StoreSettingsService>().RefreshAsync();
app.Run();