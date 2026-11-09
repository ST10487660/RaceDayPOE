using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC. Every POST is protected against cross-site request forgery.
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

// Session management: the logged-in user's id, role and the API session live in server-side session state.
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "RaceDay.Web.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(60);
});
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// The ONLY way this app gets data: HTTP calls to the Part 2 API.
var apiBase = (builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5080").TrimEnd('/') + "/";
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBase);
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddSingleton<IAssetUrlResolver, AssetUrlResolver>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); // friendly page, no stack traces for users
}
app.UseStatusCodePagesWithReExecute("/Home/HttpStatus", "?code={0}");

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
