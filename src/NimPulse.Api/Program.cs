using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NimPulse.Api.Components;
using NimPulse.Core.Ai;
using NimPulse.Core.Auth;
using NimPulse.Core.Health;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Enum values as lowerCamelCase strings ("quantity"/"category") — readable on the
        // wire and matches the Swift-side string literals, instead of raw ordinals (0/1).
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    });

// Web-Dashboard (Login/Register/Reports/Admin/KI-Gateway im Browser) — static server rendering,
// bewusst ohne Interactive-Server-Rendermode: kein SignalR-Circuit nötig, klassische Formular-
// Posts/Redirects reichen für dieses Admin-/Family-Scale-UI völlig.
builder.Services.AddRazorComponents();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, HttpContextAuthenticationStateProvider>();
builder.Services.AddAntiforgery();

builder.Services.Configure<AiOptions>(builder.Configuration.GetSection(AiOptions.SectionName));
// Scoped, not singleton — both providers read the (scoped) DbContext for the admin-configured
// KI-Gateway settings (including the API keys — see AiGatewaySettings), not just appsettings/env.
builder.Services.AddScoped<IAiProvider, ClaudeAiProvider>();
builder.Services.AddScoped<IAiProvider, AzureOpenAiProvider>();
builder.Services.AddScoped<IAiProvider, OpenAiProvider>();
builder.Services.AddScoped<AiProviderResolver>();
builder.Services.AddScoped<ReportService>();

builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.AddScoped<JwtTokenService>();

var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();

// Zwei Auth-Schemes: Bearer/JWT für die iOS-App und andere API-Clients, Cookie fürs Browser-
// Web-UI. "Smart" wählt anhand des Authorization-Headers, welches greift — beide teilen sich
// dieselben Claims (siehe JwtTokenService/ClaimsPrincipalExtensions) und [Authorize]-Attribute,
// ohne dass Controller oder Razor-Components ein Schema explizit angeben müssen.
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = "Smart";
        options.DefaultChallengeScheme = "Smart";
    })
    .AddPolicyScheme("Smart", "JWT or Cookie", options =>
    {
        options.ForwardDefaultSelector = context =>
            context.Request.Headers.ContainsKey("Authorization")
                ? JwtBearerDefaults.AuthenticationScheme
                : CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = authOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.JwtSigningKey)),
            ValidateLifetime = true,
        };
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(authOptions.TokenLifetimeHours);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=nimpulse.db";
builder.Services.AddDbContext<NimPulseDbContext>(options => options.UseSqlite(connectionString));

var app = builder.Build();

// No formal EF migrations yet — the schema is still moving too fast at this stage.
// Switch to Database.Migrate() once real user data exists that must survive schema changes.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NimPulseDbContext>();
    db.Database.EnsureCreated();
    EnsureAiGatewaySettingsColumns(db);
}

// EnsureCreated() only creates tables that don't exist yet — it never alters an existing table
// when the model gains a column (that's what migrations are for). The three AiGatewaySettings
// key/endpoint columns were added after the first deploy already created this table on a running
// server, so a live database can be stuck without them ("SQLite Error 1: no such column:
// AzureOpenAiApiKey" — reproduced against a copy of the pre-refactor schema). Additive, idempotent,
// no data loss — safe to run on every startup regardless of which schema version is on disk.
static void EnsureAiGatewaySettingsColumns(NimPulseDbContext db)
{
    var connection = db.Database.GetDbConnection();
    connection.Open();
    try
    {
        var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA table_info('AiGatewaySettings')";
            using var reader = pragma.ExecuteReader();
            var nameOrdinal = reader.GetOrdinal("name");
            while (reader.Read())
            {
                existingColumns.Add(reader.GetString(nameOrdinal));
            }
        }

        string[] requiredColumns =
        [
            "ClaudeApiKey", "AzureOpenAiEndpoint", "AzureOpenAiApiKey",
            "OpenAiModel", "OpenAiApiKey",
        ];
        foreach (var column in requiredColumns)
        {
            if (existingColumns.Contains(column))
            {
                continue;
            }

            using var alter = connection.CreateCommand();
            alter.CommandText = $"ALTER TABLE \"AiGatewaySettings\" ADD COLUMN \"{column}\" TEXT NULL";
            alter.ExecuteNonQuery();
        }
    }
    finally
    {
        connection.Close();
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers();
app.MapRazorComponents<App>();

app.MapPost("/logout", async (HttpContext context, IAntiforgery antiforgery) =>
{
    try
    {
        await antiforgery.ValidateRequestAsync(context);
    }
    catch (AntiforgeryValidationException)
    {
        return Results.BadRequest();
    }

    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.MapPost("/admin/users/{id:guid}/delete", async (Guid id, HttpContext context, NimPulseDbContext db, IAntiforgery antiforgery) =>
{
    if (!context.User.IsInRole("Admin"))
    {
        return Results.Forbid();
    }

    try
    {
        await antiforgery.ValidateRequestAsync(context);
    }
    catch (AntiforgeryValidationException)
    {
        return Results.BadRequest();
    }

    if (id == context.User.RequireUserId())
    {
        return Results.Redirect("/admin");
    }

    var user = await db.Users.FindAsync(id);
    if (user is not null)
    {
        db.Users.Remove(user);
        await db.SaveChangesAsync();
    }

    return Results.Redirect("/admin");
}).RequireAuthorization();

app.Run();
