using PracticeLLD.OpenRouter;
using PracticeLLD.Services.LldQuestion;

namespace PracticeLLD.Services.ModelComparison;

/// <summary>
/// Service for comparative analysis of LLD question generation across different models.
/// </summary>
public interface IModelComparisonService
{
    /// <summary>
    /// Generates questions from two random models simultaneously.
    /// </summary>
    Task<ComparisonRoundResult> GenerateComparisonRoundAsync(
        DifficultyLevel difficulty,
        ReasoningEffort reasoningEffort,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records the user's vote for the winning model.
    /// </summary>
    void RecordVote(string winningModel, string losingModel);

    /// <summary>
    /// Gets the final analysis results for all models.
    /// </summary>
    AnalysisResult GetAnalysisResults();

    /// <summary>
    /// Resets all scores and short code history.
    /// </summary>
    void Reset();

    /// <summary>
    /// Gets the list of available models.
    /// </summary>
    List<string> GetAvailableModels();
}
