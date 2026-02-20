using System.Collections.Concurrent;
using PracticeLLD.OpenRouter;
using PracticeLLD.Services.LldQuestion;

namespace PracticeLLD.Services.ModelComparison;

/// <summary>
/// Identifies which API provider a model belongs to.
/// </summary>
public enum ModelProvider
{
    OpenRouter,
    Groq
}

/// <summary>
/// Represents a model available for comparison, along with its provider.
/// </summary>
public record ModelEntry(string ModelId, ModelProvider Provider)
{
    /// <summary>
    /// Display name including provider prefix for clarity.
    /// </summary>
    public string DisplayName => $"[{Provider}] {ModelId}";
}

/// <summary>
/// Service for comparative analysis of LLD question generation across different models.
/// Maintains in-memory short code history per model and scoring.
/// </summary>
public class ModelComparisonService : IModelComparisonService
{
    private readonly ILldQuestionService _lldQuestionService;
    private readonly Random _random = new();

    private static readonly List<ModelEntry> Models =
    [
        // Groq models
        //new("meta-llama/llama-4-scout-17b-16e-instruct", ModelProvider.Groq),
        //new("meta-llama/llama-4-maverick-17b-128e-instruct", ModelProvider.Groq),
        //new("openai/gpt-oss-20b", ModelProvider.Groq),
        new("openai/gpt-oss-120b", ModelProvider.Groq),
        new("moonshotai/kimi-k2-instruct-0905", ModelProvider.Groq),
        new("moonshotai/kimi-k2-instruct", ModelProvider.Groq),
    ];

    /// <summary>
    /// In-memory dictionary of short titles returned by each model.
    /// Key: model display name, Value: list of short titles.
    /// </summary>
    private readonly ConcurrentDictionary<string, List<string>> _modelShortTitles = new();

    /// <summary>
    /// Tracks how many times each model was shown and selected.
    /// Key: model display name, Value: (timesShown, timesSelected).
    /// </summary>
    private readonly ConcurrentDictionary<string, (int TimesShown, int TimesSelected)> _modelScores = new();

    private int _totalRounds;

    public ModelComparisonService(ILldQuestionService lldQuestionService)
    {
        _lldQuestionService = lldQuestionService;
    }

    public async Task<ComparisonRoundResult> GenerateComparisonRoundAsync(
        DifficultyLevel difficulty,
        ReasoningEffort reasoningEffort,
        CancellationToken cancellationToken = default)
    {
        var (entryA, entryB) = PickTwoRandomModels();

        var resultA = await GenerateFromModelAsync(entryA, difficulty, reasoningEffort, cancellationToken);
        var resultB = await GenerateFromModelAsync(entryB, difficulty, reasoningEffort, cancellationToken);

        // Track short titles for successful responses
        if (resultA.IsSuccess && resultA.Question != null)
        {
            AddShortTitle(entryA.DisplayName, resultA.Question.ShortTitle);
        }
        if (resultB.IsSuccess && resultB.Question != null)
        {
            AddShortTitle(entryB.DisplayName, resultB.Question.ShortTitle);
        }

        return new ComparisonRoundResult
        {
            IsSuccess = resultA.IsSuccess || resultB.IsSuccess,
            ErrorMessage = (!resultA.IsSuccess && !resultB.IsSuccess)
                ? $"Both models failed. Model A ({entryA.DisplayName}): {resultA.ErrorMessage}; Model B ({entryB.DisplayName}): {resultB.ErrorMessage}"
                : null,
            ModelA = resultA,
            ModelB = resultB
        };
    }

    public void RecordVote(string winningModel, string losingModel)
    {
        Interlocked.Increment(ref _totalRounds);

        _modelScores.AddOrUpdate(
            winningModel,
            (1, 1),
            (_, existing) => (existing.TimesShown + 1, existing.TimesSelected + 1));

        _modelScores.AddOrUpdate(
            losingModel,
            (1, 0),
            (_, existing) => (existing.TimesShown + 1, existing.TimesSelected));
    }

    public AnalysisResult GetAnalysisResults()
    {
        var scores = _modelScores
            .Select(kvp => new ModelScore
            {
                ModelName = kvp.Key,
                TimesShown = kvp.Value.TimesShown,
                TimesSelected = kvp.Value.TimesSelected
            })
            .OrderByDescending(s => s.SelectionPercentage)
            .ThenByDescending(s => s.TimesSelected)
            .ToList();

        return new AnalysisResult
        {
            Scores = scores,
            TotalRounds = _totalRounds
        };
    }

    public void Reset()
    {
        _modelShortTitles.Clear();
        _modelScores.Clear();
        Interlocked.Exchange(ref _totalRounds, 0);
    }

    public List<string> GetAvailableModels() => Models.Select(m => m.DisplayName).ToList();

    private (ModelEntry EntryA, ModelEntry EntryB) PickTwoRandomModels()
    {
        var shuffled = Models.OrderBy(_ => _random.Next()).ToList();
        return (shuffled[0], shuffled[1]);
    }

    private async Task<ModelQuestionResult> GenerateFromModelAsync(
        ModelEntry entry,
        DifficultyLevel difficulty,
        ReasoningEffort reasoningEffort,
        CancellationToken cancellationToken)
    {
        try
        {
            var shortTitles = GetShortTitles(entry.DisplayName);

            var lldResult = await _lldQuestionService.GenerateQuestionAsync(
                modelId: entry.ModelId,
                provider: entry.Provider,
                difficulty: difficulty,
                reasoningEffort: reasoningEffort,
                alreadyAskedShortTitles: shortTitles,
                cancellationToken: cancellationToken);

            if (!lldResult.IsSuccess)
            {
                return new ModelQuestionResult { ModelName = entry.DisplayName, IsSuccess = false, ErrorMessage = lldResult.ErrorMessage };
            }

            return new ModelQuestionResult { ModelName = entry.DisplayName, IsSuccess = true, Question = lldResult.Question };
        }
        catch (Exception ex)
        {
            return new ModelQuestionResult { ModelName = entry.DisplayName, IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    private List<string> GetShortTitles(string modelDisplayName)
    {
        return _modelShortTitles.TryGetValue(modelDisplayName, out var titles) ? [.. titles] : [];
    }

    private void AddShortTitle(string modelDisplayName, string shortTitle)
    {
        if (string.IsNullOrEmpty(shortTitle)) return;

        _modelShortTitles.AddOrUpdate(
            modelDisplayName,
            [shortTitle],
            (_, existing) =>
            {
                lock (existing)
                {
                    if (!existing.Contains(shortTitle))
                    {
                        existing.Add(shortTitle);
                    }
                }
                return existing;
            });
    }
}
