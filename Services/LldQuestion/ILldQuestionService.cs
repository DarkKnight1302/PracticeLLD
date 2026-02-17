namespace PracticeLLD.Services.LldQuestion;

/// <summary>
/// Service for generating LLD interview questions.
/// </summary>
public interface ILldQuestionService
{
    /// <summary>
    /// Generates a new LLD interview question of the specified difficulty,
    /// avoiding questions that have already been asked.
    /// </summary>
    /// <param name="difficulty">The difficulty level of the question.</param>
    /// <param name="alreadyAskedShortCodes">Short codes of questions that have already been asked.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A generated LLD question with constraints and short code.</returns>
    Task<LldQuestionResult> GenerateQuestionAsync(
        DifficultyLevel difficulty,
        List<string>? alreadyAskedShortCodes = null,
        CancellationToken cancellationToken = default);
}
