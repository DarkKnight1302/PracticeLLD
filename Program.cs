using NewHorizonLib;
using PracticeLLD.Constants;
using PracticeLLD.OpenRouter;
using PracticeLLD.Services.LldQuestion;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Register OpenRouter client
// Note: This requires NewHorizonLib be initialized to provide ISecretService service
builder.Services.AddOpenRouterClient();

// Register LLD Question generation service
builder.Services.AddSingleton<ILldQuestionService, LldQuestionService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();

Registration.InitializeServices(builder.Services, builder.Configuration, "PracticeLLD", 0, GlobalConstant.Issuer, "PracticeLLDClient");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
