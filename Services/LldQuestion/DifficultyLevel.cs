using System.Text.Json.Serialization;

namespace PracticeLLD.Services.LldQuestion;

/// <summary>
/// Difficulty levels for LLD interview questions.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DifficultyLevel
{
    Easy,
    Medium,
    Hard
}
