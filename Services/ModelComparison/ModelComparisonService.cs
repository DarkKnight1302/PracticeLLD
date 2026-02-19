using System.Collections.Concurrent;
using PracticeLLD.Groq;
using PracticeLLD.OpenRouter;
using PracticeLLD.OpenRouter.Completion;
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
    private readonly IOpenRouterCompletionClient _completionClient;
    private readonly IOpenRouterClient _responsesClient;
    private readonly IGroqCompletionClient _groqClient;
    private readonly Random _random = new();

    private static readonly List<ModelEntry> Models =
    [
        // OpenRouter models
        //new("openrouter/aurora-alpha", ModelProvider.OpenRouter),
        //new("arcee-ai/trinity-large-preview:free", ModelProvider.OpenRouter),
        new("nvidia/nemotron-3-nano-30b-a3b:free", ModelProvider.OpenRouter),
        new("arcee-ai/trinity-mini:free", ModelProvider.OpenRouter),
        //new("nvidia/nemotron-nano-12b-v2-vl:free", ModelProvider.OpenRouter),
        new("nvidia/nemotron-nano-9b-v2:free", ModelProvider.OpenRouter),

        // Groq models
        //new("meta-llama/llama-4-scout-17b-16e-instruct", ModelProvider.Groq),
        //new("meta-llama/llama-4-maverick-17b-128e-instruct", ModelProvider.Groq),
        //new("openai/gpt-oss-20b", ModelProvider.Groq),
        new("openai/gpt-oss-120b", ModelProvider.Groq),
        new("moonshotai/kimi-k2-instruct-0905", ModelProvider.Groq),
        new("moonshotai/kimi-k2-instruct", ModelProvider.Groq),
    ];

    /// <summary>
    /// In-memory dictionary of short codes returned by each model.
    /// Key: model display name, Value: list of short codes.
    /// </summary>
    private readonly ConcurrentDictionary<string, List<string>> _modelShortCodes = new();

    /// <summary>
    /// Tracks how many times each model was shown and selected.
    /// Key: model display name, Value: (timesShown, timesSelected).
    /// </summary>
    private readonly ConcurrentDictionary<string, (int TimesShown, int TimesSelected)> _modelScores = new();

    private int _totalRounds;

    private static readonly object JsonSchema = new
    {
        type = "object",
        properties = new
        {
            question = new { type = "string", description = "The full LLD interview question text." },
            constraints = new
            {
                type = "array",
                items = new { type = "string" },
                description = "List of constraints or requirements for the question."
            },
            short_code = new { type = "string", description = "A unique short code identifier for the question." }
        },
        required = new[] { "question", "constraints", "short_code" },
        additionalProperties = false
    };

    public ModelComparisonService(
        IOpenRouterCompletionClient completionClient,
        IOpenRouterClient responsesClient,
        IGroqCompletionClient groqClient)
    {
        _completionClient = completionClient;
        _responsesClient = responsesClient;
        _groqClient = groqClient;
    }

    public async Task<ComparisonRoundResult> GenerateComparisonRoundAsync(
        DifficultyLevel difficulty,
        ReasoningEffort reasoningEffort,
        CancellationToken cancellationToken = default)
    {
        var (entryA, entryB) = PickTwoRandomModels();

        var resultA = await GenerateFromModelAsync(entryA, difficulty, reasoningEffort, cancellationToken);
        var resultB = await GenerateFromModelAsync(entryB, difficulty, reasoningEffort, cancellationToken);

        // Track short codes for successful responses
        if (resultA.IsSuccess && resultA.Question != null)
        {
            AddShortCode(entryA.DisplayName, resultA.Question.ShortCode);
        }
        if (resultB.IsSuccess && resultB.Question != null)
        {
            AddShortCode(entryB.DisplayName, resultB.Question.ShortCode);
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
        _modelShortCodes.Clear();
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
            var shortCodes = GetShortCodes(entry.DisplayName);
            var systemPrompt = BuildSystemPrompt(difficulty);
            var userPrompt = BuildUserPrompt(shortCodes);

            CompletionResult<LldQuestionResponse> result;

            switch (entry.Provider)
            {
                case ModelProvider.Groq:
                    result = await _groqClient.SendPromptJsonAsync<LldQuestionResponse>(
                        model: entry.ModelId,
                        userPrompt: userPrompt,
                        jsonSchema: JsonSchema,
                        schemaName: "lld_question",
                        systemPrompt: systemPrompt,
                        temperature: 0.9,
                        reasoningEffort: reasoningEffort,
                        cancellationToken: cancellationToken);
                    break;

                case ModelProvider.OpenRouter:
                default:
                    result = await _completionClient.SendPromptJsonAsync<LldQuestionResponse>(
                        model: entry.ModelId,
                        userPrompt: userPrompt,
                        jsonSchema: JsonSchema,
                        schemaName: "lld_question",
                        systemPrompt: systemPrompt,
                        temperature: 0.9,
                        reasoningEffort: reasoningEffort,
                        cancellationToken: cancellationToken);
                    break;
            }

            if (!result.IsSuccess)
            {
                return new ModelQuestionResult { ModelName = entry.DisplayName, IsSuccess = false, ErrorMessage = result.ErrorMessage };
            }

            return new ModelQuestionResult { ModelName = entry.DisplayName, IsSuccess = true, Question = result.Data };
        }
        catch (Exception ex)
        {
            return new ModelQuestionResult { ModelName = entry.DisplayName, IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    private List<string> GetShortCodes(string modelDisplayName)
    {
        return _modelShortCodes.TryGetValue(modelDisplayName, out var codes) ? [.. codes] : [];
    }

    private void AddShortCode(string modelDisplayName, string shortCode)
    {
        if (string.IsNullOrEmpty(shortCode)) return;

        _modelShortCodes.AddOrUpdate(
            modelDisplayName,
            [shortCode],
            (_, existing) =>
            {
                lock (existing)
                {
                    if (!existing.Contains(shortCode))
                    {
                        existing.Add(shortCode);
                    }
                }
                return existing;
            });
    }

    private static string BuildSystemPrompt(DifficultyLevel difficulty)
    {
        return $"""
            You are an experienced software engineering interviewer specializing in Low Level Design (LLD) questions.
            Your task is to ask a Low Level Design question for a software engineering interview of {difficulty.ToString().ToLowerInvariant()} difficulty.
            
            Guidelines:
            - The question should require the candidate to design classes, interfaces, and their relationships.
            - Include clear constraints that define the scope of the problem.
            - The short_code should be a concise uppercase identifier that reflects the core concept of the question.
            - You should NOT ask any question whose short code matches one from the already asked list provided by the user.
            - Make sure the question is practical and commonly asked in real interviews.
            """;
    }

    private static string BuildUserPrompt(List<string>? alreadyAskedShortCodes)
    {
        if (alreadyAskedShortCodes == null || alreadyAskedShortCodes.Count == 0)
        {
            return "No questions have been asked yet. Generate a new LLD interview question.";
        }

        var codes = string.Join(", ", alreadyAskedShortCodes);
        return $"Already asked questions (short codes): {codes}. Generate a new LLD interview question that is different from the ones listed.";
    }
}
