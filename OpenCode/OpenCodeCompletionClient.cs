using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NewHorizonLib.Services;
using PracticeLLD.OpenRouter;
using PracticeLLD.OpenRouter.Completion;

namespace PracticeLLD.OpenCode;

/// <summary>
/// Client for the OpenCode Go inference API (https://opencode.ai/inference/go/openai/v1/chat/completions).
/// The endpoint is OpenAI-compatible and exposes the OpenCode Go models (GLM, Kimi, DeepSeek, and the
/// free "beam-free" / "minimax-m3.1-flash-free" models). OpenCode Go asks clients to identify
/// themselves with a user agent and send a stable session id per conversation.
/// </summary>
public class OpenCodeCompletionClient : IOpenCodeCompletionClient
{
    private const string BaseUrl = "https://opencode.ai/inference/go/openai/v1/chat/completions";
    private const string ApiKeySecretName = "openCodeApiKey";
    private const string UserAgent = "PracticeLLD/1.0";

    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly JsonSerializerOptions _requestOptions;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ILogger<OpenCodeCompletionClient> _logger;

    public OpenCodeCompletionClient(ISecretService secretService, ILogger<OpenCodeCompletionClient> logger)
    {
        ArgumentNullException.ThrowIfNull(secretService);

        _logger = logger;

        // A missing key is not fatal at construction time (so the app still starts), but OpenCode
        // will reject paid models without it.
        try
        {
            _apiKey = secretService.GetSecretValue(ApiKeySecretName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read API key '{ApiKeySecretName}' from secret service.", ApiKeySecretName);
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning(
                "API key '{ApiKeySecretName}' not found in secret service. OpenCode requests will likely be rejected.",
                ApiKeySecretName);
        }

        _httpClient = new HttpClient();

        // No naming policy: [JsonPropertyName] attributes are the sole authority for outgoing request JSON.
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
    public async Task<CompletionResult<T>> SendPromptJsonAsync<T>(
        string model,
        string userPrompt,
        object jsonSchema,
        string schemaName = "response",
        string? systemPrompt = null,
        double? temperature = null,
        ReasoningEffort reasoningEffort = ReasoningEffort.None,
        int? maxTokens = null,
        CancellationToken cancellationToken = default)
    {
        var messages = BuildMessages(userPrompt, systemPrompt);
        var responseFormat = BuildResponseFormat(schemaName, jsonSchema);

        // Reasoning effort is intentionally not forwarded: OpenCode chat models differ in how (and whether)
        // they accept reasoning parameters, and an unsupported field would fail the request.

        var request = BuildRequest(model, messages, temperature, maxTokens, responseFormat);
        var result = await ExecuteRequestAsync(request, cancellationToken).ConfigureAwait(false);

        // If the model rejected response_format, retry without it and extract JSON from free-form text.
        if (!result.IsSuccess)
        {
            _logger.LogWarning(
                "OpenCode Completions [{Model}]: response_format rejected ({Error}), retrying as plain text.",
                model, result.ErrorMessage);

            var plainRequest = BuildRequest(model, messages, temperature, maxTokens, responseFormat: null);
            result = await ExecuteRequestAsync(plainRequest, cancellationToken).ConfigureAwait(false);
        }

        return BuildTypedResult<T>(result);
    }

    private static List<CompletionMessage> BuildMessages(string userPrompt, string? systemPrompt)
    {
        var messages = new List<CompletionMessage>();

        if (!string.IsNullOrEmpty(systemPrompt))
            messages.Add(new CompletionMessage { Role = "system", Content = systemPrompt });

        messages.Add(new CompletionMessage { Role = "user", Content = userPrompt });

        return messages;
    }

    private static JsonSchemaResponseFormat BuildResponseFormat(string schemaName, object jsonSchema) =>
        new()
        {
            JsonSchema = new JsonSchemaDefinition
            {
                Name = schemaName,
                Schema = jsonSchema,
                Strict = true
            }
        };

    private static CompletionRequest BuildRequest(
        string model,
        List<CompletionMessage> messages,
        double? temperature,
        int? maxTokens,
        JsonSchemaResponseFormat? responseFormat) =>
        new()
        {
            Model = model,
            Messages = messages,
            Temperature = temperature,
            MaxTokens = maxTokens,
            ResponseFormat = responseFormat
        };

    private async Task<CompletionResult> ExecuteRequestAsync(CompletionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);
            var json = JsonSerializer.Serialize(request, _requestOptions);

            _logger.LogInformation("OpenCode Completions Request [{Model}]: {RequestBody}", request.Model, json);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            }

            // OpenCode Go requires clients to identify themselves and send a session id per conversation.
            httpRequest.Headers.UserAgent.ParseAdd(UserAgent);
            httpRequest.Headers.Add("x-opencode-session", $"ses_practicelld_{Guid.NewGuid():N}");

            using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation("OpenCode Completions Response [{Model}] (HTTP {StatusCode}): {ResponseBody}",
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
            _semaphore.Release();
        }
    }

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

        if (TryDeserialize<T>(result.TextResponse, out var direct))
        {
            typedResult.Data = direct;
            return typedResult;
        }

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
