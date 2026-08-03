using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

builder.Services.Configure<AiOptions>(builder.Configuration.GetSection(AiOptions.SectionName));
// Scoped, not singleton — both providers read the (scoped) DbContext for the admin-configured
// KI-Gateway settings (including the API keys — see AiGatewaySettings), not just appsettings/env.
builder.Services.AddScoped<IAiProvider, ClaudeAiProvider>();
builder.Services.AddScoped<IAiProvider, AzureOpenAiProvider>();
builder.Services.AddScoped<AiProviderResolver>();

builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.AddScoped<JwtTokenService>();

var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
    });
builder.Services.AddAuthorization();

var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=nimpulse.db";
builder.Services.AddDbContext<NimPulseDbContext>(options => options.UseSqlite(connectionString));

var app = builder.Build();

// No formal EF migrations yet — the schema is still moving too fast at this stage.
// Switch to Database.Migrate() once real user data exists that must survive schema changes.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<NimPulseDbContext>().Database.EnsureCreated();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
