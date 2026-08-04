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
using NimPulse.Api.BackgroundServices;
using NimPulse.Api.Components;
using NimPulse.Core.Ai;
using NimPulse.Core.Auth;
using NimPulse.Core.Health;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Enum values as lowerCamelCase strings ("quantity"/"category") — readable on the
        // wire and matches the Swift-side string literals, instead of raw ordinals (0/1).
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    });

// Web-Dashboard (Login/Register/Reports/Admin/KI-Gateway im Browser) — static server rendering für
// fast alle Seiten, kein SignalR-Circuit nötig, klassische Formular-Posts/Redirects reichen für
// dieses Admin-/Family-Scale-UI völlig. Einzige Ausnahme: /coach (KI-Chat) braucht laufende
// Interaktion ohne Full-Page-Reload — deshalb AddInteractiveServerComponents() zusätzlich zu Static
// SSR, aber NUR Coach.razor deklariert @rendermode InteractiveServer; alle anderen Seiten bleiben
// unverändert statisch.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
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
builder.Services.AddScoped<DailyScoreService>();
builder.Services.AddScoped<ChatCoachService>();
builder.Services.AddScoped<WeeklyInsightService>();
builder.Services.AddHostedService<WeeklyInsightBackgroundService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<AiModelListingService>();

builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.AddScoped<JwtTokenService>();

var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();

// Security guard: never run a real deployment on the well-known dev signing key (or a
// missing/too-short one). Anyone who knows the key can forge JWTs for any user/role — a full
// auth bypass — so fail fast instead of booting silently insecure. Development keeps the
// convenience fallback so local runs don't need any setup.
if (!builder.Environment.IsDevelopment())
{
    if (string.IsNullOrWhiteSpace(authOptions.JwtSigningKey)
        || authOptions.JwtSigningKey == AuthOptions.InsecureDevSigningKey
        || Encoding.UTF8.GetByteCount(authOptions.JwtSigningKey) < 32)
    {
        throw new InvalidOperationException(
            "Auth:JwtSigningKey is missing, too short, or the insecure development default. " +
            "Set a strong (>= 32 byte) Auth__JwtSigningKey before starting outside Development.");
    }
}

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

// Real EF Core migrations from here on (replacing EnsureCreated() + ad-hoc ALTER TABLE, which
// caused a real production incident in v0.3.2 — EnsureCreated() never alters an existing table
// when the model gains a column). Every deployment before this point was created via
// EnsureCreated(), so its tables already exist but there's no __EFMigrationsHistory row for
// "InitialBaseline" (which defines those same tables) — running it normally would fail with
// "table already exists". BootstrapDatabase detects that case and marks InitialBaseline as
// already applied without re-running it, before handing off to Database.Migrate() for anything
// newer. A genuinely fresh database has no tables at all, so it just runs InitialBaseline like
// any other migration. Every schema change from now on is `dotnet ef migrations add ...`, not a
// new manual ALTER TABLE block.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NimPulseDbContext>();
    BootstrapDatabase(db);
}

static void BootstrapDatabase(NimPulseDbContext db)
{
    const string baselineMigrationId = "20260804104900_InitialBaseline";
    const string productVersion = "8.0.29";

    var connection = db.Database.GetDbConnection();
    connection.Open();
    try
    {
        bool usersTableExists;
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Users'";
            usersTableExists = Convert.ToInt64(cmd.ExecuteScalar()) > 0;
        }

        bool historyTableExists;
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory'";
            historyTableExists = Convert.ToInt64(cmd.ExecuteScalar()) > 0;
        }

        if (usersTableExists && !historyTableExists)
        {
            using (var createHistory = connection.CreateCommand())
            {
                createHistory.CommandText = """
                    CREATE TABLE "__EFMigrationsHistory" (
                        "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                        "ProductVersion" TEXT NOT NULL
                    );
                    """;
                createHistory.ExecuteNonQuery();
            }

            using (var markApplied = connection.CreateCommand())
            {
                markApplied.CommandText = "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ($id, $version)";
                var idParam = markApplied.CreateParameter();
                idParam.ParameterName = "$id";
                idParam.Value = baselineMigrationId;
                markApplied.Parameters.Add(idParam);
                var versionParam = markApplied.CreateParameter();
                versionParam.ParameterName = "$version";
                versionParam.Value = productVersion;
                markApplied.Parameters.Add(versionParam);
                markApplied.ExecuteNonQuery();
            }
        }
    }
    finally
    {
        connection.Close();
    }

    db.Database.Migrate();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

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
