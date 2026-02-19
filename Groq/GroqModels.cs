using System.Text.Json.Serialization;
using PracticeLLD.OpenRouter.Completion;

namespace PracticeLLD.Groq;

/// <summary>
/// Request payload for Groq Chat Completions API.
/// Groq uses top-level reasoning parameters instead of a nested reasoning object.
/// </summary>
public class GroqCompletionRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; set; }

    [JsonPropertyName("messages")]
    public required List<CompletionMessage> Messages { get; set; }

    [JsonPropertyName("max_completion_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxCompletionTokens { get; set; }

    [JsonPropertyName("temperature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Temperature { get; set; }

    [JsonPropertyName("top_p")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? TopP { get; set; }

    [JsonPropertyName("stream")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Stream { get; set; }

    [JsonPropertyName("response_format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonSchemaResponseFormat? ResponseFormat { get; set; }

    [JsonPropertyName("stop")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Stop { get; set; }

    [JsonPropertyName("seed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Seed { get; set; }

    /// <summary>
    /// Controls how model reasoning is presented in the response.
    /// Options: "parsed", "raw", "hidden".
    /// Must be "parsed" or "hidden" when using JSON mode or tool use.
    /// Mutually exclusive with <see cref="IncludeReasoning"/>.
    /// Supported by non-GPT-OSS reasoning models (e.g., qwen/qwen3-32b).
    /// </summary>
    [JsonPropertyName("reasoning_format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReasoningFormat { get; set; }

    /// <summary>
    /// Controls the level of effort the model puts into reasoning.
    /// For qwen/qwen3-32b: "none" or "default".
    /// For GPT-OSS models: "low", "medium", or "high".
    /// </summary>
    [JsonPropertyName("reasoning_effort")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReasoningEffort { get; set; }

    /// <summary>
    /// Whether to include reasoning in a dedicated message.reasoning field.
    /// Mutually exclusive with <see cref="ReasoningFormat"/>.
    /// Only for GPT-OSS models.
    /// </summary>
    [JsonPropertyName("include_reasoning")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IncludeReasoning { get; set; }
}
