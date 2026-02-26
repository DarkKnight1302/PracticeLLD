using System.Text.Json.Serialization;

namespace PracticeLLD.Services.LldQuestion;

/// <summary>
/// Represents a generated LLD interview question.
/// </summary>
public class LldQuestionResponse
{
    /// <summary>
    /// The full question text.
    /// </summary>
    [JsonPropertyName("question")]
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// The constraints for the question.
    /// </summary>
    [JsonPropertyName("constraints")]
    public List<string> Constraints { get; set; } = [];

    /// <summary>
    /// A short identifier for the question (used to track already asked questions).
    /// </summary>
    [JsonPropertyName("short_title")]
    public string ShortTitle { get; set; } = string.Empty;

    /// <summary>
    /// Functional requirements that the design should fulfill.
    /// </summary>
    [JsonPropertyName("functional_requirements")]
    public List<string> FunctionalRequirements { get; set; } = [];

    /// <summary>
    /// Non-functional requirements such as scalability, performance, and reliability considerations.
    /// </summary>
    [JsonPropertyName("non_functional_requirements")]
    public List<string> NonFunctionalRequirements { get; set; } = [];
}
