using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PracticeLLD.Groq;

/// <summary>
/// Extension methods for registering Groq services.
/// </summary>
public static class GroqServiceExtensions
{
    /// <summary>
    /// Adds the Groq Chat Completions API client as a singleton to the service collection.
    /// </summary>
    public static IServiceCollection AddGroqCompletionClient(this IServiceCollection services)
    {
        services.AddSingleton<IGroqCompletionClient>(sp =>
        {
            var secretService = sp.GetRequiredService<NewHorizonLib.Services.ISecretService>();
            var logger = sp.GetRequiredService<ILogger<GroqCompletionClient>>();
            return new GroqCompletionClient(secretService, logger);
        });

        return services;
    }
}
