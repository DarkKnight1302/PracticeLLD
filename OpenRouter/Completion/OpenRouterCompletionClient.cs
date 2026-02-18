using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NewHorizonLib.Services;

namespace PracticeLLD.OpenRouter.Completion;

/// <summary>
/// Client for interacting with the OpenRouter Chat Completions API (/api/v1/chat/completions).
/// This is a singleton service that uses ISecretService for API key retrieval.
/// </summary>
public class OpenRouterCompletionClient : IOpenRouterCompletionClient
{
    private const string BaseUrl = "https://openrouter.ai/api/v1/chat/completions";
    private const string ApiKeySecretName = "openRouterKey";
    private const string DefaultModel = "arcee-ai/trinity-large-preview:free";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly JsonSerializerOptions _requestOptions;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private readonly ILogger<OpenRouterCompletionClient> _logger;

    public OpenRouterCompletionClient(ISecretService secretService, ILogger<OpenRouterCompletionClient> logger)
    {
        ArgumentNullException.ThrowIfNull(secretService);

        _logger = logger;
        _apiKey = secretService.GetSecretValue(ApiKeySecretName);
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException($"API key '{ApiKeySecretName}' not found in secret service.");
        }

        _model = DefaultModel;
        _httpClient = new HttpClient();

        // No naming policy: [JsonPropertyName] attributes are the sole authority for outgoing request JSON.
        // This prevents CamelCase from corrupting anonymous schema objects (e.g. additionalProperties).
        _requestOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // Case-insensitive for API response deserialization and typed result payloads.
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <inheritdoc />
    public async Task<CompletionResult> SendPromptAsync(
        string userPrompt,
        string? systemPrompt = null,
        string? assistantPrompt = null,
        double? temperature = null,
        ReasoningEffort reasoningEffort = ReasoningEffort.None,
        int? maxTokens = null,
        CancellationToken cancellationToken = default)
    {
        var messages = BuildMessages(userPrompt, systemPrompt, assistantPrompt);
        return await SendMessagesAsync(messages, temperature, reasoningEffort, maxTokens, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CompletionResult<T>> SendPromptJsonAsync<T>(
        string userPrompt,
        object jsonSchema,
        string schemaName = "response",
        string? systemPrompt = null,
        string? assistantPrompt = null,
        double? temperature = null,
        ReasoningEffort reasoningEffort = ReasoningEffort.None,
        int? maxTokens = null,
        CancellationToken cancellationToken = default)
    {
        var messages = BuildMessages(userPrompt, systemPrompt, assistantPrompt);
        return await SendMessagesJsonAsync<T>(messages, jsonSchema, schemaName, temperature, reasoningEffort, maxTokens, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CompletionResult> SendMessagesAsync(
        List<CompletionMessage> messages,
        double? temperature = null,
        ReasoningEffort reasoningEffort = ReasoningEffort.None,
        int? maxTokens = null,
        CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(messages, temperature, reasoningEffort, maxTokens, responseFormat: null);
        return await ExecuteRequestAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CompletionResult<T>> SendMessagesJsonAsync<T>(
        List<CompletionMessage> messages,
        object jsonSchema,
        string schemaName = "response",
        double? temperature = null,
        ReasoningEffort reasoningEffort = ReasoningEffort.None,
        int? maxTokens = null,
        CancellationToken cancellationToken = default)
    {
        var responseFormat = BuildResponseFormat(schemaName, jsonSchema);
        var request = BuildRequest(messages, temperature, reasoningEffort, maxTokens, responseFormat);
        var result = await ExecuteRequestAsync(request, cancellationToken);
        return BuildTypedResult<T>(result);
    }

    /// <inheritdoc />
    public async Task<CompletionResult<T>> SendPromptJsonAsync<T>(
        string model,
        string userPrompt,
        object jsonSchema,
        string schemaName = "response",
        string? systemPrompt = null,
        string? assistantPrompt = null,
        double? temperature = null,
        ReasoningEffort reasoningEffort = ReasoningEffort.None,
        int? maxTokens = null,
        CancellationToken cancellationToken = default)
    {
        // Models like Gemma don't support the "system" role; merge into the user prompt.
        if (!string.IsNullOrEmpty(systemPrompt) && RequiresSystemPromptMerge(model))
        {
            userPrompt = $"{systemPrompt}\n\n{userPrompt}";
            systemPrompt = null;
        }

        var messages = BuildMessages(userPrompt, systemPrompt, assistantPrompt);
        var responseFormat = BuildResponseFormat(schemaName, jsonSchema);

        // First attempt: structured request with response_format.
        var request = BuildRequest(messages, temperature, reasoningEffort, maxTokens, responseFormat, model);
        var result = await ExecuteRequestAsync(request, cancellationToken);

        // If the model rejected response_format, retry without it and extract JSON from free-form text.
        if (!result.IsSuccess)
        {
            _logger.LogWarning(
                "OpenRouter Completions [{Model}]: response_format rejected ({Error}), retrying as plain text.",
                model, result.ErrorMessage);

            var plainRequest = BuildRequest(messages, temperature, reasoningEffort, maxTokens, responseFormat: null, model);
            result = await ExecuteRequestAsync(plainRequest, cancellationToken);
        }

        return BuildTypedResult<T>(result);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static bool RequiresSystemPromptMerge(string model) =>
        model.StartsWith("google/gemma-", StringComparison.OrdinalIgnoreCase);

    private static JsonSchemaResponseFormat BuildResponseFormat(string schemaName, object jsonSchema) =>
        new JsonSchemaResponseFormat
        {
            JsonSchema = new JsonSchemaDefinition
            {
                Name = schemaName,
                Schema = jsonSchema,
                Strict = true
            }
        };

    private List<CompletionMessage> BuildMessages(string userPrompt, string? systemPrompt, string? assistantPrompt)
    {
        var messages = new List<CompletionMessage>();

        if (!string.IsNullOrEmpty(systemPrompt))
            messages.Add(new CompletionMessage { Role = "system", Content = systemPrompt });

        messages.Add(new CompletionMessage { Role = "user", Content = userPrompt });

        if (!string.IsNullOrEmpty(assistantPrompt))
            messages.Add(new CompletionMessage { Role = "assistant", Content = assistantPrompt });

        return messages;
    }

    private CompletionRequest BuildRequest(
        List<CompletionMessage> messages,
        double? temperature,
        ReasoningEffort reasoningEffort,
        int? maxTokens,
        JsonSchemaResponseFormat? responseFormat,
        string? modelOverride = null)
    {
        var request = new CompletionRequest
        {
            Model = modelOverride ?? _model,
            Messages = messages,
            Temperature = temperature,
            MaxTokens = maxTokens,
            ResponseFormat = responseFormat,
            Reasoning = new CompletionReasoningConfig { Enabled = true }
        };

        if (reasoningEffort != ReasoningEffort.None)
            request.Reasoning.Effort = reasoningEffort.ToString().ToLowerInvariant();

        return request;
    }

    private async Task<CompletionResult> ExecuteRequestAsync(CompletionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);
            var json = JsonSerializer.Serialize(request, _requestOptions);

            _logger.LogInformation("OpenRouter Completions Request [{Model}]: {RequestBody}", request.Model, json);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation("OpenRouter Completions Response [{Model}] (HTTP {StatusCode}): {ResponseBody}",
                request.Model, (int)httpResponse.StatusCode, responseContent);

            var response = JsonSerializer.Deserialize<CompletionResponse>(responseContent, _jsonOptions);

            if (response?.Error != null)
            {
                return new CompletionResult
                {
                    IsSuccess = false,
                    ErrorMessage = response.Error.Message ?? "Unknown error",
                    RawResponse = response
                };
            }

            if (!httpResponse.IsSuccessStatusCode)
            {
                return new CompletionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"HTTP {(int)httpResponse.StatusCode}: {httpResponse.ReasonPhrase}",
                    RawResponse = response
                };
            }

            return new CompletionResult
            {
                IsSuccess = true,
                TextResponse = response?.Choices?[0].Message?.Content,
                RawResponse = response,
                Usage = response?.Usage,
                Reasoning = response?.Choices?[0].Message?.Reasoning
            };
        }
        catch (HttpRequestException ex)
        {
            return new CompletionResult { IsSuccess = false, ErrorMessage = $"Network error: {ex.Message}" };
        }
        catch (JsonException ex)
        {
            return new CompletionResult { IsSuccess = false, ErrorMessage = $"JSON parsing error: {ex.Message}" };
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            return new CompletionResult { IsSuccess = false, ErrorMessage = "Request was cancelled" };
        }
        finally
        {
            Task.Delay(2000);
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Builds a typed result from a base result.
    /// Tries direct JSON deserialization first; falls back to extracting a JSON block
    /// from free-form or markdown text for models that ignore response_format.
    /// </summary>
    private CompletionResult<T> BuildTypedResult<T>(CompletionResult result)
    {
        var typedResult = new CompletionResult<T>
        {
            IsSuccess = result.IsSuccess,
            TextResponse = result.TextResponse,
            RawResponse = result.RawResponse,
            ErrorMessage = result.ErrorMessage,
            Usage = result.Usage,
            Reasoning = result.Reasoning
        };

        if (!result.IsSuccess || string.IsNullOrEmpty(result.TextResponse))
            return typedResult;

        // 1. Try parsing the content directly as JSON.
        if (TryDeserialize<T>(result.TextResponse, out var direct))
        {
            typedResult.Data = direct;
            return typedResult;
        }

        // 2. Extract a JSON object block from markdown or prose and try again.
        var extracted = TryExtractJsonFromText(result.TextResponse);
        if (extracted != null && TryDeserialize<T>(extracted, out var fromExtracted))
        {
            typedResult.Data = fromExtracted;
            typedResult.TextResponse = extracted;
            return typedResult;
        }

        typedResult.IsSuccess = false;
        typedResult.ErrorMessage = "Response did not contain valid JSON matching the expected schema.";
        return typedResult;
    }

    private bool TryDeserialize<T>(string json, out T? result)
    {
        try
        {
            result = JsonSerializer.Deserialize<T>(json, _jsonOptions);
            return result != null;
        }
        catch (JsonException)
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Extracts the first complete JSON object from free-form or markdown-fenced text.
    /// </summary>
    private static string? TryExtractJsonFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        // Strip markdown code fences: ```json\n...\n``` or ```\n...\n```
        var fenceStart = text.IndexOf("```", StringComparison.Ordinal);
        if (fenceStart >= 0)
        {
            var contentStart = text.IndexOf('\n', fenceStart);
            if (contentStart >= 0)
            {
                var fenceEnd = text.IndexOf("```", contentStart, StringComparison.Ordinal);
                if (fenceEnd > contentStart)
                    text = text[contentStart..fenceEnd].Trim();
            }
        }

        // Walk the string to find the outermost { ... } block.
        var braceStart = text.IndexOf('{');
        if (braceStart < 0)
            return null;

        int depth = 0;
        bool inString = false;
        bool escape = false;

        for (int i = braceStart; i < text.Length; i++)
        {
            char c = text[i];

            if (escape) { escape = false; continue; }
            if (c == '\\' && inString) { escape = true; continue; }
            if (c == '"') { inString = !inString; continue; }
            if (inString) continue;

            if (c == '{') depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                    return text[braceStart..(i + 1)];
            }
        }

        return null;
    }
}
