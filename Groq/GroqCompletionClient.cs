using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NewHorizonLib.Services;
using PracticeLLD.OpenRouter;
using PracticeLLD.OpenRouter.Completion;

namespace PracticeLLD.Groq;

/// <summary>
/// Client for interacting with the Groq Chat Completions API (https://api.groq.com/openai/v1/chat/completions).
/// Uses Groq-specific request models with top-level reasoning parameters.
/// </summary>
public class GroqCompletionClient : IGroqCompletionClient
{
    private const string BaseUrl = "https://api.groq.com/openai/v1/chat/completions";
    private const string ApiKeySecretName = "groqApiKey";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly JsonSerializerOptions _requestOptions;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ILogger<GroqCompletionClient> _logger;

    /// <summary>
    /// Groq reasoning models that use reasoning_format (non-GPT-OSS).
    /// </summary>
    private static readonly string[] ReasoningFormatModels = ["qwen/qwen3-32b"];

    /// <summary>
    /// Groq reasoning models that use include_reasoning and reasoning_effort (GPT-OSS).
    /// </summary>
    private static readonly string[] GptOssModels = ["openai/gpt-oss-20b", "openai/gpt-oss-120b", "openai/gpt-oss-safeguard-20b"];

    public GroqCompletionClient(ISecretService secretService, ILogger<GroqCompletionClient> logger)
    {
        ArgumentNullException.ThrowIfNull(secretService);

        _logger = logger;
        _apiKey = secretService.GetSecretValue(ApiKeySecretName);
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException($"API key '{ApiKeySecretName}' not found in secret service.");
        }

        _httpClient = new HttpClient();

        _requestOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

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
        var request = BuildRequest(model, messages, temperature, reasoningEffort, maxTokens, responseFormat);

        var result = await ExecuteRequestAsync(request, cancellationToken);

        // If the model rejected response_format, retry without it and extract JSON from free-form text.
        if (!result.IsSuccess)
        {
            _logger.LogWarning(
                "Groq Completions [{Model}]: response_format rejected ({Error}), retrying as plain text.",
                model, result.ErrorMessage);

            var plainRequest = BuildRequest(model, messages, temperature, reasoningEffort, maxTokens, responseFormat: null);
            result = await ExecuteRequestAsync(plainRequest, cancellationToken);
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

    private static bool IsGptOssModel(string model) =>
        GptOssModels.Any(m => model.Equals(m, StringComparison.OrdinalIgnoreCase));

    private static bool IsReasoningFormatModel(string model) =>
        ReasoningFormatModels.Any(m => model.Equals(m, StringComparison.OrdinalIgnoreCase));

    private static GroqCompletionRequest BuildRequest(
        string model,
        List<CompletionMessage> messages,
        double? temperature,
        ReasoningEffort reasoningEffort,
        int? maxTokens,
        JsonSchemaResponseFormat? responseFormat)
    {
        bool isUsingJsonMode = responseFormat != null;

        var request = new GroqCompletionRequest
        {
            Model = model,
            Messages = messages,
            Temperature = temperature,
            MaxCompletionTokens = maxTokens,
            ResponseFormat = responseFormat
        };

        if (IsGptOssModel(model))
        {
            // GPT-OSS models use include_reasoning and reasoning_effort (low/medium/high).
            // reasoning_format is NOT supported for these models.
            request.IncludeReasoning = true;

            if (reasoningEffort is ReasoningEffort.Low or ReasoningEffort.Medium or ReasoningEffort.High)
            {
                request.ReasoningEffort = reasoningEffort.ToString().ToLowerInvariant();
            }
        }
        else if (IsReasoningFormatModel(model))
        {
            // Non-GPT-OSS reasoning models use reasoning_format.
            // When using JSON mode, reasoning_format must be "parsed" or "hidden" (not "raw").
            request.ReasoningFormat = isUsingJsonMode ? "parsed" : "parsed";

            // qwen3-32b supports reasoning_effort: "none" or "default".
            if (reasoningEffort == ReasoningEffort.None)
            {
                request.ReasoningEffort = "none";
            }
            else
            {
                request.ReasoningEffort = "default";
            }
        }
        // For non-reasoning models (llama, kimi, etc.), no reasoning parameters are set.

        return request;
    }

    private async Task<CompletionResult> ExecuteRequestAsync(GroqCompletionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);
            var json = JsonSerializer.Serialize(request, _requestOptions);

            _logger.LogInformation("Groq Completions Request [{Model}]: {RequestBody}", request.Model, json);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation("Groq Completions Response [{Model}] (HTTP {StatusCode}): {ResponseBody}",
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
