using System.Text.Json;
using System.Text.Json.Serialization;

namespace PracticeLLD.OpenRouter.Completion;

#region Request Models

/// <summary>
/// Request payload for OpenRouter Chat Completions API (/api/v1/chat/completions).
/// </summary>
public class CompletionRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; set; }

    [JsonPropertyName("messages")]
    public required List<CompletionMessage> Messages { get; set; }

    [JsonPropertyName("max_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("temperature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Temperature { get; set; }

    [JsonPropertyName("top_p")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? TopP { get; set; }

    [JsonPropertyName("stream")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Stream { get; set; }

    [JsonPropertyName("reasoning")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CompletionReasoningConfig? Reasoning { get; set; }

    [JsonPropertyName("response_format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonSchemaResponseFormat? ResponseFormat { get; set; }

    [JsonPropertyName("stop")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Stop { get; set; }

    [JsonPropertyName("seed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Seed { get; set; }

    [JsonPropertyName("frequency_penalty")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? FrequencyPenalty { get; set; }

    [JsonPropertyName("presence_penalty")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? PresencePenalty { get; set; }
}

/// <summary>
/// A message in the chat completions conversation.
/// </summary>
public class CompletionMessage
{
    [JsonPropertyName("role")]
    public required string Role { get; set; }

    [JsonPropertyName("content")]
    public required string Content { get; set; }

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }
}

/// <summary>
/// Configuration for reasoning behavior in chat completions.
/// </summary>
public class CompletionReasoningConfig
{
    [JsonPropertyName("effort")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Effort { get; set; }

    [JsonPropertyName("max_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("exclude")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Exclude { get; set; }

    [JsonPropertyName("enabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Enabled { get; set; }
}

/// <summary>
/// Strict JSON schema response format for structured outputs.
/// </summary>
public class JsonSchemaResponseFormat
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "json_schema";

    [JsonPropertyName("json_schema")]
    public required JsonSchemaDefinition JsonSchema { get; set; }
}

/// <summary>
/// JSON schema definition for structured outputs.
/// </summary>
public class JsonSchemaDefinition
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("strict")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Strict { get; set; }

    [JsonPropertyName("schema")]
    public required object Schema { get; set; }
}

#endregion

#region Response Models

/// <summary>
/// Response from OpenRouter Chat Completions API.
/// </summary>
public class CompletionResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("created")]
    public long? Created { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("choices")]
    public List<CompletionChoice>? Choices { get; set; }

    [JsonPropertyName("usage")]
    public CompletionUsageInfo? Usage { get; set; }

    [JsonPropertyName("system_fingerprint")]
    public string? SystemFingerprint { get; set; }

    [JsonPropertyName("error")]
    public CompletionErrorInfo? Error { get; set; }
}

/// <summary>
/// A choice in the completions response (non-streaming).
/// </summary>
public class CompletionChoice
{
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }

    [JsonPropertyName("native_finish_reason")]
    public string? NativeFinishReason { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public CompletionResponseMessage? Message { get; set; }

    [JsonPropertyName("error")]
    public CompletionErrorInfo? Error { get; set; }
}

/// <summary>
/// Message in a completions response.
/// </summary>
public class CompletionResponseMessage
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("reasoning")]
    public string? Reasoning { get; set; }

    [JsonPropertyName("reasoning_details")]
    public List<JsonElement>? ReasoningDetails { get; set; }
}

/// <summary>
/// Token usage information for chat completions.
/// </summary>
public class CompletionUsageInfo
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }

    [JsonPropertyName("prompt_tokens_details")]
    public PromptTokenDetails? PromptTokensDetails { get; set; }

    [JsonPropertyName("completion_tokens_details")]
    public CompletionTokenDetails? CompletionTokensDetails { get; set; }

    [JsonPropertyName("cost")]
    public double? Cost { get; set; }
}

/// <summary>
/// Breakdown of prompt tokens.
/// </summary>
public class PromptTokenDetails
{
    [JsonPropertyName("cached_tokens")]
    public int CachedTokens { get; set; }

    [JsonPropertyName("audio_tokens")]
    public int? AudioTokens { get; set; }
}

/// <summary>
/// Breakdown of completion tokens.
/// </summary>
public class CompletionTokenDetails
{
    [JsonPropertyName("reasoning_tokens")]
    public int? ReasoningTokens { get; set; }
}

/// <summary>
/// Error information from the completions API.
/// </summary>
public class CompletionErrorInfo
{
    [JsonPropertyName("code")]
    public JsonElement? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("metadata")]
    public JsonElement? Metadata { get; set; }
}

#endregion

#region Result Types

/// <summary>
/// Represents the result of an OpenRouter Chat Completions API call.
/// </summary>
public class CompletionResult
{
    public bool IsSuccess { get; set; }
    public string? TextResponse { get; set; }
    public CompletionResponse? RawResponse { get; set; }
    public string? ErrorMessage { get; set; }
    public CompletionUsageInfo? Usage { get; set; }
    public string? Reasoning { get; set; }
}

/// <summary>
/// Represents the result of an OpenRouter Chat Completions API call with typed JSON response.
/// </summary>
/// <typeparam name="T">The type to deserialize the JSON response to.</typeparam>
public class CompletionResult<T> : CompletionResult
{
    public T? Data { get; set; }
}

#endregion
