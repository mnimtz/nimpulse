using NimPulse.Core.Ai;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.Configure<AiOptions>(builder.Configuration.GetSection(AiOptions.SectionName));
builder.Services.AddSingleton<IAiProvider, ClaudeAiProvider>();
builder.Services.AddSingleton<IAiProvider, AzureOpenAiProvider>();
builder.Services.AddSingleton<AiProviderResolver>();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
