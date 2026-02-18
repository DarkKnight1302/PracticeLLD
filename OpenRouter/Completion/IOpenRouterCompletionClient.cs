namespace PracticeLLD.OpenRouter.Completion;

/// <summary>
/// Interface for OpenRouter Chat Completions API client.
/// </summary>
public interface IOpenRouterCompletionClient
{
    /// <summary>
    /// Sends a simple prompt to the OpenRouter Chat Completions API and returns a text response.
    /// </summary>
    Task<CompletionResult> SendPromptAsync(
        string userPrompt,
        string? systemPrompt = null,
        string? assistantPrompt = null,
        double? temperature = null,
        ReasoningEffort reasoningEffort = ReasoningEffort.None,
        int? maxTokens = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a prompt to the OpenRouter Chat Completions API and returns a JSON response deserialized to the specified type.
    /// </summary>
    Task<CompletionResult<T>> SendPromptJsonAsync<T>(
        string userPrompt,
        object jsonSchema,
        string schemaName = "response",
        string? systemPrompt = null,
        string? assistantPrompt = null,
        double? temperature = null,
        ReasoningEffort reasoningEffort = ReasoningEffort.None,
        int? maxTokens = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends structured messages to the OpenRouter Chat Completions API.
    /// </summary>
    Task<CompletionResult> SendMessagesAsync(
        List<CompletionMessage> messages,
        double? temperature = null,
        ReasoningEffort reasoningEffort = ReasoningEffort.None,
        int? maxTokens = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends structured messages to the OpenRouter Chat Completions API and returns a JSON response.
    /// </summary>
    Task<CompletionResult<T>> SendMessagesJsonAsync<T>(
        List<CompletionMessage> messages,
        object jsonSchema,
        string schemaName = "response",
        double? temperature = null,
        ReasoningEffort reasoningEffort = ReasoningEffort.None,
        int? maxTokens = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a prompt using a specific model to the OpenRouter Chat Completions API and returns a JSON response.
    /// </summary>
    Task<CompletionResult<T>> SendPromptJsonAsync<T>(
        string model,
        string userPrompt,
        object jsonSchema,
        string schemaName = "response",
        string? systemPrompt = null,
        string? assistantPrompt = null,
        double? temperature = null,
        ReasoningEffort reasoningEffort = ReasoningEffort.None,
        int? maxTokens = null,
        CancellationToken cancellationToken = default);
}
