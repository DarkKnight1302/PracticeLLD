using PracticeLLD.OpenRouter;
using PracticeLLD.OpenRouter.Completion;

namespace PracticeLLD.OpenCode;

/// <summary>
/// Interface for the OpenCode Go inference API client (OpenAI-compatible Chat Completions).
/// </summary>
public interface IOpenCodeCompletionClient
{
    /// <summary>
    /// Sends a prompt using a specific OpenCode model and returns a JSON response.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the JSON response to.</typeparam>
    /// <param name="model">The OpenCode Go model ID (e.g. "beam-free").</param>
    /// <param name="userPrompt">The user's prompt.</param>
    /// <param name="jsonSchema">The JSON schema for the expected response format.</param>
    /// <param name="schemaName">The name of the schema. Default is "response".</param>
    /// <param name="systemPrompt">Optional system prompt to set context.</param>
    /// <param name="temperature">Sampling temperature (0-2). Default is null (uses API default).</param>
    /// <param name="reasoningEffort">The reasoning effort level. Accepted for symmetry with other providers; OpenCode chat models vary in support, so it is not forwarded.</param>
    /// <param name="maxTokens">Maximum tokens to generate. Default is null.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result containing the deserialized response.</returns>
    Task<CompletionResult<T>> SendPromptJsonAsync<T>(
        string model,
        string userPrompt,
        object jsonSchema,
        string schemaName = "response",
        string? systemPrompt = null,
        double? temperature = null,
        ReasoningEffort reasoningEffort = ReasoningEffort.None,
        int? maxTokens = null,
        CancellationToken cancellationToken = default);
}
