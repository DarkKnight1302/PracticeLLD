using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PracticeLLD.OpenRouter.Completion;

namespace PracticeLLD.OpenRouter;

/// <summary>
/// Extension methods for registering OpenRouter services.
/// </summary>
public static class OpenRouterServiceExtensions
{
    /// <summary>
    /// Adds the OpenRouter Responses API client as a singleton to the service collection.
    /// </summary>
    public static IServiceCollection AddOpenRouterClient(
        this IServiceCollection services)
    {
        services.AddSingleton<IOpenRouterClient>(sp =>
        {
            var secretService = sp.GetRequiredService<NewHorizonLib.Services.ISecretService>();
            var logger = sp.GetRequiredService<ILogger<OpenRouterClient>>();
            return new OpenRouterClient(secretService, logger);
        });

        return services;
    }

    /// <summary>
    /// Adds the OpenRouter Chat Completions API client as a singleton to the service collection.
    /// </summary>
    public static IServiceCollection AddOpenRouterCompletionClient(
        this IServiceCollection services)
    {
        services.AddSingleton<IOpenRouterCompletionClient>(sp =>
        {
            var secretService = sp.GetRequiredService<NewHorizonLib.Services.ISecretService>();
            var logger = sp.GetRequiredService<ILogger<OpenRouterCompletionClient>>();
            return new OpenRouterCompletionClient(secretService, logger);
        });

        return services;
    }

    /// <summary>
    /// Adds the OpenRouter client as a singleton to the service collection using a configuration action.
    /// </summary>
    public static IServiceCollection AddOpenRouterClient(
        this IServiceCollection services,
        Action<OpenRouterOptions> configure)
    {
        var options = new OpenRouterOptions();
        configure(options);

        return services.AddOpenRouterClient();
    }
}

/// <summary>
/// Configuration options for the OpenRouter client.
/// </summary>
public class OpenRouterOptions
{
    /// <summary>
    /// The model to use. Default is "openai/o4-mini".
    /// </summary>
    public string Model { get; set; } = "openai/o4-mini";
}
