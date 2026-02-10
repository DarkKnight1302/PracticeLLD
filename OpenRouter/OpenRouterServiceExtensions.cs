using Microsoft.Extensions.DependencyInjection;

namespace PracticeLLD.OpenRouter;

/// <summary>
/// Extension methods for registering OpenRouter services.
/// </summary>
public static class OpenRouterServiceExtensions
{
    /// <summary>
    /// Adds the OpenRouter client as a singleton to the service collection.
    /// The API key is retrieved from ISecretService using the key "OpenRouterApiKey".
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="model">The model to use (e.g., "openai/o4-mini"). Defaults to "openai/o4-mini".</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOpenRouterClient(
        this IServiceCollection services,
        string model = "openai/o4-mini")
    {
        services.AddSingleton<IOpenRouterClient>(sp =>
        {
            var secretService = sp.GetRequiredService<NewHorizonLib.Services.ISecretService>();
            return new OpenRouterClient(secretService, model);
        });

        return services;
    }

    /// <summary>
    /// Adds the OpenRouter client as a singleton to the service collection using a configuration action.
    /// The API key is retrieved from ISecretService using the key "OpenRouterApiKey".
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure the OpenRouter options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOpenRouterClient(
        this IServiceCollection services,
        Action<OpenRouterOptions> configure)
    {
        var options = new OpenRouterOptions();
        configure(options);

        return services.AddOpenRouterClient(options.Model);
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
