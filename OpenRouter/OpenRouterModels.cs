using System.Text.Json.Serialization;

namespace PracticeLLD.OpenRouter;

#region Request Models

/// <summary>
/// Request payload for OpenRouter Responses API.
/// </summary>
public class OpenRouterRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; set; }

    [JsonPropertyName("input")]
    public required object Input { get; set; }

    [JsonPropertyName("max_output_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxOutputTokens { get; set; }

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
    public ReasoningConfig? Reasoning { get; set; }

    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TextResponseFormat? Text { get; set; }
}

/// <summary>
/// Configuration for reasoning behavior.
/// </summary>
public class ReasoningConfig
{
    [JsonPropertyName("effort")]
    public required string Effort { get; set; }
}

/// <summary>
/// Text response format configuration for JSON schema responses.
/// </summary>
public class TextResponseFormat
{
    [JsonPropertyName("format")]
    public required ResponseFormat Format { get; set; }
}

/// <summary>
/// Response format configuration.
/// </summary>
public class ResponseFormat
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    [JsonPropertyName("schema")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Schema { get; set; }

    [JsonPropertyName("strict")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Strict { get; set; }
}

/// <summary>
/// Message input for structured conversations.
/// </summary>
public class MessageInput
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "message";

    [JsonPropertyName("role")]
    public required string Role { get; set; }

    [JsonPropertyName("content")]
    public required List<MessageContent> Content { get; set; }

    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }

    [JsonPropertyName("status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Status { get; set; }
}

/// <summary>
/// Content of a message.
/// </summary>
public class MessageContent
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("text")]
    public required string Text { get; set; }

    [JsonPropertyName("annotations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<object>? Annotations { get; set; }
}

#endregion

#region Response Models

/// <summary>
/// Response from OpenRouter Responses API.
/// </summary>
public class OpenRouterResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("created_at")]
    public long? CreatedAt { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("output")]
    public List<OutputItem>? Output { get; set; }

    [JsonPropertyName("usage")]
    public UsageInfo? Usage { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("error")]
    public ErrorInfo? Error { get; set; }
}

/// <summary>
/// Output item in the response.
/// </summary>
public class OutputItem
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public List<OutputContent>? Content { get; set; }

    [JsonPropertyName("encrypted_content")]
    public string? EncryptedContent { get; set; }

    [JsonPropertyName("summary")]
    public List<string>? Summary { get; set; }
}

/// <summary>
/// Content of an output item.
/// </summary>
public class OutputContent
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("annotations")]
    public List<object>? Annotations { get; set; }
}

/// <summary>
/// Token usage information.
/// </summary>
public class UsageInfo
{
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }

    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }

    [JsonPropertyName("output_tokens_details")]
    public OutputTokenDetails? OutputTokensDetails { get; set; }
}

/// <summary>
/// Detailed breakdown of output tokens.
/// </summary>
public class OutputTokenDetails
{
    [JsonPropertyName("reasoning_tokens")]
    public int ReasoningTokens { get; set; }
}

/// <summary>
/// Error information from the API.
/// </summary>
public class ErrorInfo
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

#endregion

#region Result Types

/// <summary>
/// Represents the result of an OpenRouter API call.
/// </summary>
public class OpenRouterResult
{
    public bool IsSuccess { get; set; }
    public string? TextResponse { get; set; }
    public OpenRouterResponse? RawResponse { get; set; }
    public string? ErrorMessage { get; set; }
    public UsageInfo? Usage { get; set; }
    public List<string>? ReasoningSummary { get; set; }
}

/// <summary>
/// Represents the result of an OpenRouter API call with typed JSON response.
/// </summary>
/// <typeparam name="T">The type to deserialize the JSON response to.</typeparam>
public class OpenRouterResult<T> : OpenRouterResult
{
    public T? Data { get; set; }
}

#endregion
