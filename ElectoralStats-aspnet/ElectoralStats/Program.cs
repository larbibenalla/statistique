using System.Globalization;
using ElectoralStats.Data;
using ElectoralStats.Hubs;
using ElectoralStats.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

builder.Services.AddLocalization(o => o.ResourcesPath = "Resources");
builder.Services.AddSignalR();
builder.Services.AddScoped<ExcelImportService>();
builder.Services.AddScoped<StatsService>();

var supportedCultures = new[]
{
    new CultureInfo("ar-MA"),
    new CultureInfo("fr-FR"),
    new CultureInfo("en-US")
};

builder.Services.Configure<RequestLocalizationOptions>(o =>
{
    o.DefaultRequestCulture = new RequestCulture("ar-MA");
    o.SupportedCultures = supportedCultures;
    o.SupportedUICultures = supportedCultures;
    o.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseRequestLocalization();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.MapHub<StatsHub>("/hubs/stats");

app.Run();
