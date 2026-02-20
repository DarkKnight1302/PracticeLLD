using PracticeLLD.OpenRouter;
using PracticeLLD.Services.ModelComparison;

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
    /// <param name="alreadyAskedShortTitles">Short titles of questions that have already been asked.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A generated LLD question with constraints and short title.</returns>
    Task<LldQuestionResult> GenerateQuestionAsync(
        DifficultyLevel difficulty,
        List<string>? alreadyAskedShortTitles = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a new LLD interview question using a specific model and provider.
    /// </summary>
    /// <param name="modelId">The ID of the model to use for generating the question.</param>
    /// <param name="provider">The provider of the model (e.g., Groq, OpenRouter).</param>
    /// <param name="difficulty">The difficulty level of the question.</param>
    /// <param name="reasoningEffort">The level of reasoning effort required for the question.</param>
    /// <param name="alreadyAskedShortTitles">Short titles of questions that have already been asked.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A generated LLD question with constraints and short title.</returns>
    Task<LldQuestionResult> GenerateQuestionAsync(
        string modelId,
        ModelProvider provider,
        DifficultyLevel difficulty,
        ReasoningEffort reasoningEffort,
        List<string>? alreadyAskedShortTitles = null,
        CancellationToken cancellationToken = default);
}
