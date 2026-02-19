using PracticeLLD.OpenRouter;
using PracticeLLD.OpenRouter.Completion;

namespace PracticeLLD.Groq;

/// <summary>
/// Interface for Groq Chat Completions API client.
/// </summary>
public interface IGroqCompletionClient
{
    /// <summary>
    /// Sends a prompt using a specific model to the Groq Chat Completions API and returns a JSON response.
    /// </summary>
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
