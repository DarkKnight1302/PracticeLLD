using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NewHorizonLib.Services;

namespace PracticeLLD.OpenRouter;

/// <summary>
/// Client for interacting with the OpenRouter Responses API (Beta).
/// This is a singleton service that uses ISecretService for API key retrieval.
/// </summary>
public class OpenRouterClient : IOpenRouterClient
{
    private const string BaseUrl = "https://openrouter.ai/api/v1/responses";
    private const string ApiKeySecretName = "openRouterKey";
    private const string DefaultModel = "openai/o4-mini";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the OpenRouterClient.
    /// </summary>
    /// <param name="secretService">The secret service to retrieve the API key.</param>
    /// <param name="model">The model to use (e.g., "openai/o4-mini"). Defaults to "openai/o4-mini".</param>
    public OpenRouterClient(ISecretService secretService, string model = DefaultModel)
    {
        ArgumentNullException.ThrowIfNull(secretService);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        _apiKey = secretService.GetSecretValue(ApiKeySecretName);
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException($"API key '{ApiKeySecretName}' not found in secret service.");
        }

        _model = model;
        _httpClient = new HttpClient();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <inheritdoc />
    public async Task<OpenRouterResult> SendPromptAsync(
        string userPrompt,
        string? systemPrompt = null,
        string? assistantPrompt = null,
        double? temperature = null,
        ReasoningEffort reasoningEffort = ReasoningEffort.None,
        int? maxOutputTokens = null,
        CancellationToken cancellationToken = default)
    {
        var messages = BuildMessages(userPrompt, systemPrompt, assistantPrompt);
        return await SendMessagesAsync(messages, temperature, reasoningEffort, maxOutputTokens, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<OpenRouterResult<T>> SendPromptJsonAsync<T>(
        string userPrompt,
        object jsonSchema,
        string schemaName = "response",
        string? systemPrompt = null,
        string? assistantPrompt = null,
        double? temperature = null,
        ReasoningEffort reasoningEffort = ReasoningEffort.None,
        int? maxOutputTokens = null,
        CancellationToken cancellationToken = default)
    {
        var messages = BuildMessages(userPrompt, systemPrompt, assistantPrompt);
        return await SendMessagesJsonAsync<T>(messages, jsonSchema, schemaName, temperature, reasoningEffort, maxOutputTokens, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<OpenRouterResult> SendMessagesAsync(
        List<MessageInput> messages,
        double? temperature = null,
        ReasoningEffort reasoningEffort = ReasoningEffort.None,
        int? maxOutputTokens = null,
        CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(messages, temperature, reasoningEffort, maxOutputTokens, jsonFormat: null);
        return await ExecuteRequestAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<OpenRouterResult<T>> SendMessagesJsonAsync<T>(
        List<MessageInput> messages,
        object jsonSchema,
        string schemaName = "response",
        double? temperature = null,
        ReasoningEffort reasoningEffort = ReasoningEffort.None,
        int? maxOutputTokens = null,
        CancellationToken cancellationToken = default)
    {
        var jsonFormat = new TextResponseFormat
        {
            Format = new ResponseFormat
            {
                Type = "json_schema",
                Name = schemaName,
                Schema = jsonSchema,
                Strict = true
            }
        };

        var request = BuildRequest(messages, temperature, reasoningEffort, maxOutputTokens, jsonFormat);
        var result = await ExecuteRequestAsync(request, cancellationToken);

        var typedResult = new OpenRouterResult<T>
        {
            IsSuccess = result.IsSuccess,
            TextResponse = result.TextResponse,
            RawResponse = result.RawResponse,
            ErrorMessage = result.ErrorMessage,
            Usage = result.Usage,
            ReasoningSummary = result.ReasoningSummary
        };

        if (result.IsSuccess && !string.IsNullOrEmpty(result.TextResponse))
        {
            try
            {
                typedResult.Data = JsonSerializer.Deserialize<T>(result.TextResponse, _jsonOptions);
            }
            catch (JsonException ex)
            {
                typedResult.IsSuccess = false;
                typedResult.ErrorMessage = $"Failed to deserialize JSON response: {ex.Message}";
            }
        }

        return typedResult;
    }

    private List<MessageInput> BuildMessages(string userPrompt, string? systemPrompt, string? assistantPrompt)
    {
        var messages = new List<MessageInput>();

        if (!string.IsNullOrEmpty(systemPrompt))
        {
            messages.Add(new MessageInput
            {
                Role = "system",
                Content =
                [
                    new MessageContent
                    {
                        Type = "input_text",
                        Text = systemPrompt
                    }
                ]
            });
        }

        if (!string.IsNullOrEmpty(assistantPrompt))
        {
            messages.Add(new MessageInput
            {
                Role = "assistant",
                Id = $"msg_{Guid.NewGuid():N}",
                Status = "completed",
                Content =
                [
                    new MessageContent
                    {
                        Type = "output_text",
                        Text = assistantPrompt,
                        Annotations = []
                    }
                ]
            });
        }

        messages.Add(new MessageInput
        {
            Role = "user",
            Content =
            [
                new MessageContent
                {
                    Type = "input_text",
                    Text = userPrompt
                }
            ]
        });

        return messages;
    }

    private OpenRouterRequest BuildRequest(
        List<MessageInput> messages,
        double? temperature,
        ReasoningEffort reasoningEffort,
        int? maxOutputTokens,
        TextResponseFormat? jsonFormat)
    {
        var request = new OpenRouterRequest
        {
            Model = _model,
            Input = messages,
            Temperature = temperature,
            MaxOutputTokens = maxOutputTokens,
            Text = jsonFormat
        };

        if (reasoningEffort != ReasoningEffort.None)
        {
            request.Reasoning = new ReasoningConfig
            {
                Effort = reasoningEffort.ToString().ToLowerInvariant()
            };
        }

        return request;
    }

    private async Task<OpenRouterResult> ExecuteRequestAsync(OpenRouterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

            var response = JsonSerializer.Deserialize<OpenRouterResponse>(responseContent, _jsonOptions);

            if (response?.Error != null)
            {
                return new OpenRouterResult
                {
                    IsSuccess = false,
                    ErrorMessage = response.Error.Message ?? "Unknown error",
                    RawResponse = response
                };
            }

            if (!httpResponse.IsSuccessStatusCode)
            {
                return new OpenRouterResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"HTTP {(int)httpResponse.StatusCode}: {httpResponse.ReasonPhrase}",
                    RawResponse = response
                };
            }

            var textResponse = ExtractTextResponse(response);
            var reasoningSummary = ExtractReasoningSummary(response);

            return new OpenRouterResult
            {
                IsSuccess = true,
                TextResponse = textResponse,
                RawResponse = response,
                Usage = response?.Usage,
                ReasoningSummary = reasoningSummary
            };
        }
        catch (HttpRequestException ex)
        {
            return new OpenRouterResult
            {
                IsSuccess = false,
                ErrorMessage = $"Network error: {ex.Message}"
            };
        }
        catch (JsonException ex)
        {
            return new OpenRouterResult
            {
                IsSuccess = false,
                ErrorMessage = $"JSON parsing error: {ex.Message}"
            };
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            return new OpenRouterResult
            {
                IsSuccess = false,
                ErrorMessage = "Request was cancelled"
            };
        }
    }

    private static string? ExtractTextResponse(OpenRouterResponse? response)
    {
        if (response?.Output == null)
            return null;

        foreach (var output in response.Output)
        {
            if (output.Type == "message" && output.Content != null)
            {
                foreach (var content in output.Content)
                {
                    if (content.Type == "output_text" && !string.IsNullOrEmpty(content.Text))
                    {
                        return content.Text;
                    }
                }
            }
        }

        return null;
    }

    private static List<string>? ExtractReasoningSummary(OpenRouterResponse? response)
    {
        if (response?.Output == null)
            return null;

        foreach (var output in response.Output)
        {
            if (output.Type == "reasoning" && output.Summary != null && output.Summary.Count > 0)
            {
                return output.Summary
                    .Where(s => !string.IsNullOrEmpty(s.Text))
                    .Select(s => s.Text!)
                    .ToList();
            }
        }

        return null;
    }
}
