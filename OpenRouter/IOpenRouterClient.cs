namespace PracticeLLD.OpenRouter;

/// <summary>
/// Interface for OpenRouter API client.
/// </summary>
public interface IOpenRouterClient
{
    /// <summary>
    /// Sends a simple prompt to the OpenRouter API and returns a text response.
    /// </summary>
    /// <param name="userPrompt">The user's prompt.</param>
    /// <param name="systemPrompt">Optional system prompt to set context.</param>
    /// <param name="assistantPrompt">Optional assistant prompt for conversation context.</param>
    /// <param name="temperature">Sampling temperature (0-2). Default is null (uses API default).</param>
    /// <param name="reasoningEffort">The reasoning effort level. Default is None.</param>
    /// <param name="maxOutputTokens">Maximum tokens to generate. Default is null.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result containing the text response.</returns>
    Task<OpenRouterResult> SendPromptAsync(
        string userPrompt,
        string? systemPrompt = null,
        string? assistantPrompt = null,
        double? temperature = null,
        ReasoningEffort reasoningEffort = ReasoningEffort.None,
        int? maxOutputTokens = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a prompt to the OpenRouter API and returns a JSON response deserialized to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the JSON response to.</typeparam>
    /// <param name="userPrompt">The user's prompt.</param>
    /// <param name="jsonSchema">The JSON schema for the expected response format.</param>
    /// <param name="schemaName">The name of the schema. Default is "response".</param>
    /// <param name="systemPrompt">Optional system prompt to set context.</param>
    /// <param name="assistantPrompt">Optional assistant prompt for conversation context.</param>
    /// <param name="temperature">Sampling temperature (0-2). Default is null (uses API default).</param>
    /// <param name="reasoningEffort">The reasoning effort level. Default is None.</param>
    /// <param name="maxOutputTokens">Maximum tokens to generate. Default is null.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result containing the deserialized response.</returns>
    Task<OpenRouterResult<T>> SendPromptJsonAsync<T>(
        string userPrompt,
        object jsonSchema,
        string schemaName = "response",
        string? systemPrompt = null,
        string? assistantPrompt = null,
        double? temperature = null,
        ReasoningEffort reasoningEffort = ReasoningEffort.None,
        int? maxOutputTokens = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a prompt with structured message history to the OpenRouter API.
    /// </summary>
    /// <param name="messages">The list of messages representing the conversation.</param>
    /// <param name="temperature">Sampling temperature (0-2). Default is null (uses API default).</param>
    /// <param name="reasoningEffort">The reasoning effort level. Default is None.</param>
    /// <param name="maxOutputTokens">Maximum tokens to generate. Default is null.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result containing the text response.</returns>
    Task<OpenRouterResult> SendMessagesAsync(
        List<MessageInput> messages,
        double? temperature = null,
        ReasoningEffort reasoningEffort = ReasoningEffort.None,
        int? maxOutputTokens = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a prompt with structured message history to the OpenRouter API and returns a JSON response.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the JSON response to.</typeparam>
    /// <param name="messages">The list of messages representing the conversation.</param>
    /// <param name="jsonSchema">The JSON schema for the expected response format.</param>
    /// <param name="schemaName">The name of the schema. Default is "response".</param>
    /// <param name="temperature">Sampling temperature (0-2). Default is null (uses API default).</param>
    /// <param name="reasoningEffort">The reasoning effort level. Default is None.</param>
    /// <param name="maxOutputTokens">Maximum tokens to generate. Default is null.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result containing the deserialized response.</returns>
    Task<OpenRouterResult<T>> SendMessagesJsonAsync<T>(
        List<MessageInput> messages,
        object jsonSchema,
        string schemaName = "response",
        double? temperature = null,
        ReasoningEffort reasoningEffort = ReasoningEffort.None,
        int? maxOutputTokens = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a prompt to the OpenRouter API using a specific model and returns a JSON response deserialized to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the JSON response to.</typeparam>
    /// <param name="model">The model to use for the request.</param>
    /// <param name="userPrompt">The user's prompt.</param>
    /// <param name="jsonSchema">The JSON schema for the expected response format.</param>
    /// <param name="schemaName">The name of the schema. Default is "response".</param>
    /// <param name="systemPrompt">Optional system prompt to set context.</param>
    /// <param name="assistantPrompt">Optional assistant prompt for conversation context.</param>
    /// <param name="temperature">Sampling temperature (0-2). Default is null (uses API default).</param>
    /// <param name="reasoningEffort">The reasoning effort level. Default is None.</param>
    /// <param name="maxOutputTokens">Maximum tokens to generate. Default is null.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result containing the deserialized response.</returns>
    Task<OpenRouterResult<T>> SendPromptJsonAsync<T>(
        string model,
        string userPrompt,
        object jsonSchema,
        string schemaName = "response",
        string? systemPrompt = null,
        string? assistantPrompt = null,
        double? temperature = null,
        ReasoningEffort reasoningEffort = ReasoningEffort.None,
        int? maxOutputTokens = null,
        CancellationToken cancellationToken = default);
}
