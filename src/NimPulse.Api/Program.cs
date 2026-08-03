using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using NimPulse.Core.Ai;
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
builder.Services.AddSingleton<IAiProvider, ClaudeAiProvider>();
builder.Services.AddSingleton<IAiProvider, AzureOpenAiProvider>();
builder.Services.AddSingleton<AiProviderResolver>();

var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=nimpulse.db";
builder.Services.AddDbContext<NimPulseDbContext>(options => options.UseSqlite(connectionString));

var app = builder.Build();

// No formal EF migrations yet — the schema is still moving too fast at this stage.
// Switch to Database.Migrate() once multi-user (Phase 2) needs real migrations.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<NimPulseDbContext>().Database.EnsureCreated();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
