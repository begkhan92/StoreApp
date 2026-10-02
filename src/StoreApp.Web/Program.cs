using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using StoreApp.Web.Security;
using StoreApp.Web.Services;

var builder = WebApplication.CreateBuilder(args);
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "App_Data"));

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<JsonStringLocalizer>();
builder.Services.AddSingleton<IStringLocalizer>(sp => sp.GetRequiredService<JsonStringLocalizer>());
builder.Services.AddSingleton<IStringLocalizerFactory>(sp => sp.GetRequiredService<JsonStringLocalizer>());
builder.Services.Configure<RequestLocalizationOptions>(o =>
{
    var cultures = JsonStringLocalizer.Supported.Select(c => new CultureInfo(c)).ToList();
    o.DefaultRequestCulture = new RequestCulture("ru");
    o.SupportedCultures = cultures;
    o.SupportedUICultures = cultures;
    o.RequestCultureProviders = [new CookieRequestCultureProvider()]; // language = choice, not browser guess
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
}).AddEntityFrameworkStores<AppDbContext>().AddErrorDescriber<LocalizedIdentityErrorDescriber>().AddDefaultTokenProviders();

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
});

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("register", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromHours(1) }));
});

builder.Services.Configure<ForwardedHeadersOptions>(o =>
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
builder.Services.AddResponseCompression();
builder.Services.AddRazorPages().AddDataAnnotationsLocalization();
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");

var app = builder.Build();

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(a => a.Run(c => c.Response.WriteAsync("Ошибка сервера. Попробуйте ещё раз.")));
    app.UseHsts();
}
app.UseResponseCompression();
app.UseStaticFiles();
app.UseRequestLocalization();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

await DbSeeder.SeedAsync(app.Services, app.Configuration);
app.Run();
