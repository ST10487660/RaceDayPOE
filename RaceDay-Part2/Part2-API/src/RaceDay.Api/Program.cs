using System.Reflection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Infrastructure;
using RaceDay.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// ---- Database (SQL Server via EF Core, code-first) ----
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RaceDayDb")));

// ---- Password hashing (PBKDF2 with per-user salt; plain passwords are never stored) ----
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

// ---- Create/migrate the database and seed demo data at application start ----
builder.Services.AddHostedService<DatabaseInitializer>();

// ---- Session authentication: encrypted HttpOnly cookie holding user id + role ----
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "RaceDay.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;

        // An API must answer with status codes, not redirect to a login page.
        options.Events.OnRedirectToLogin = ctx =>
            ErrorWriter.WriteAsync(ctx.HttpContext, StatusCodes.Status401Unauthorized, "Authentication is required. Please log in.");
        options.Events.OnRedirectToAccessDenied = ctx =>
            ErrorWriter.WriteAsync(ctx.HttpContext, StatusCodes.Status403Forbidden, "You do not have permission to perform this action.");
    });
builder.Services.AddAuthorization();

// ---- Controllers + consistent 400 body ----
builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(kv => kv.Value is { Errors.Count: > 0 })
                .ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value!.Errors
                        .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage)
                        .ToArray());
            return new BadRequestObjectResult(new ErrorResponse(StatusCodes.Status400BadRequest, "Invalid request data.", errors));
        };
    });

// ---- Swagger ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description =
            "REST API for RaceDay running, walking and cycling events.\n\n" +
            "**How to try protected endpoints:** call POST /api/v1/auth/login first. The API sets a session cookie " +
            "and Swagger UI sends it automatically on every later request. Demo accounts (password Password123!): " +
            "naledi.organiser@raceday.co.za (Organiser) and lerato.participant@raceday.co.za (Participant)."
    });

    var xmlFile = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xmlFile))
        options.IncludeXmlComments(xmlFile);
});

var app = builder.Build();

// ---- Pipeline ----
app.UseExceptionHandler(errorApp => errorApp.Run(context =>
    ErrorWriter.WriteAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.")));

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "RaceDay API";
});

if (!app.Environment.IsEnvironment("Testing"))
    app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapControllers();

app.Run();

/// <summary>Exposed so the test project can use WebApplicationFactory&lt;Program&gt;.</summary>
public partial class Program { }
