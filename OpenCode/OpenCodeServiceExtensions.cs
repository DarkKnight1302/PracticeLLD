using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PracticeLLD.OpenCode;

/// <summary>
/// Extension methods for registering OpenCode services.
/// </summary>
public static class OpenCodeServiceExtensions
{
    /// <summary>
    /// Adds the OpenCode Go inference API client as a singleton to the service collection.
    /// </summary>
    public static IServiceCollection AddOpenCodeCompletionClient(this IServiceCollection services)
    {
        services.AddSingleton<IOpenCodeCompletionClient>(sp =>
        {
            var secretService = sp.GetRequiredService<NewHorizonLib.Services.ISecretService>();
            var logger = sp.GetRequiredService<ILogger<OpenCodeCompletionClient>>();
            return new OpenCodeCompletionClient(secretService, logger);
        });

        return services;
    }
}
