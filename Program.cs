using RegulatoryReportingPortal.Models;
using RegulatoryReportingPortal.Services;
using RegulatoryReportingPortal.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
var databaseProvider = builder.Configuration["DatabaseProvider"] ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString(databaseProvider)
    ?? throw new InvalidOperationException($"The {databaseProvider} connection string is not configured.");
builder.Services.AddDbContextFactory<ReportingDbContext>(options =>
{
    if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        options.UseSqlServer(connectionString);
    else
        options.UseSqlite(connectionString);
});
builder.Services.AddSingleton<ComplianceStore>();
builder.Services.AddSingleton<ReportXmlService>();
builder.Services.AddSingleton<LocalUserService>();
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".keys")))
    .SetApplicationName("RegulatoryReportingPortal");
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "regulatory_reporting_session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Analyst", policy => policy.RequireRole("Analyst", "Admin"));

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

var api = app.MapGroup("/api");

api.MapPost("/session/login", async (LoginRequest request, LocalUserService users, HttpContext context) =>
{
    var user = users.Validate(request.Username, request.Password);
    if (user is null) return Results.Unauthorized();

    var claims = new[]
    {
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, user.Role)
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    return Results.Ok(new { user.Username, user.Role });
});

api.MapPost("/session/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.NoContent();
});

api.MapGet("/session/me", (ClaimsPrincipal user) => Results.Ok(new
{
    authenticated = user.Identity?.IsAuthenticated ?? false,
    username = user.Identity?.Name,
    role = user.FindFirstValue(ClaimTypes.Role)
}));

api.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "Regulatory Reporting Portal",
    timestamp = DateTimeOffset.UtcNow
}));

api.MapGet("/clients", (ComplianceStore store) => Results.Ok(store.GetClients()));

api.MapGet("/clients/{id:guid}", (Guid id, ComplianceStore store) =>
    store.GetClient(id) is { } client ? Results.Ok(client) : Results.NotFound());

api.MapPost("/clients", (CreateClientRequest request, ComplianceStore store) =>
{
    var errors = ClientValidator.Validate(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var client = store.AddClient(request);
    return Results.Created($"/api/clients/{client.Id}", client);
}).RequireAuthorization("Analyst");

api.MapGet("/reports", (ComplianceStore store) => Results.Ok(store.GetReports()));

api.MapPost("/reports", (CreateReportRequest request, ComplianceStore store) =>
{
    if (!Enum.TryParse<ReportStandard>(request.Standard, true, out var standard))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["standard"] = ["The reporting standard must be FATCA or CRS."]
        });
    }

    var report = store.CreateReport(standard);
    return Results.Created($"/api/reports/{report.Id}", report);
}).RequireAuthorization("Analyst");

api.MapGet("/reports/{id:guid}/xml", (Guid id, ComplianceStore store, ReportXmlService xmlService) =>
{
    var report = store.GetReport(id);
    if (report is null)
    {
        return Results.NotFound();
    }

    var clients = store.GetClientsByIds(report.ClientIds);
    var xml = xmlService.Generate(report, clients);
    store.MarkReportGenerated(id);
    return Results.Text(xml, "application/xml");
}).RequireAuthorization("Analyst");

api.MapGet("/audit", (ComplianceStore store) => Results.Ok(store.GetAuditEvents()));

app.MapFallbackToFile("index.html");
app.Run();

public partial class Program;
