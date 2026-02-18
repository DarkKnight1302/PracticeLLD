using NewHorizonLib;
using PracticeLLD.Constants;
using PracticeLLD.OpenRouter;
using PracticeLLD.Services.LldQuestion;
using PracticeLLD.Services.ModelComparison;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Register OpenRouter client
// Note: This requires NewHorizonLib be initialized to provide ISecretService service
builder.Services.AddOpenRouterClient();

// Register OpenRouter Chat Completions API client
builder.Services.AddOpenRouterCompletionClient();

// Register LLD Question generation service
builder.Services.AddSingleton<ILldQuestionService, LldQuestionService>();

// Register Model Comparison service
builder.Services.AddSingleton<IModelComparisonService, ModelComparisonService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();

// Add explicit logging providers to ensure output is visible in IDE
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

Registration.InitializeServices(builder.Services, builder.Configuration, "PracticeLLD", 0, GlobalConstant.Issuer, "PracticeLLDClient");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
