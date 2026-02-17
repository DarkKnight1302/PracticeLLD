namespace PracticeLLD.Services.LldQuestion;

/// <summary>
/// Result of an LLD question generation request.
/// </summary>
public class LldQuestionResult
{
    /// <summary>
    /// Whether the question was generated successfully.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// The generated question data.
    /// </summary>
    public LldQuestionResponse? Question { get; set; }

    /// <summary>
    /// Error message if the generation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
