using System.Collections.Concurrent;
using PracticeLLD.OpenRouter;
using PracticeLLD.OpenRouter.Completion;
using PracticeLLD.Services.LldQuestion;

namespace PracticeLLD.Services.ModelComparison;

/// <summary>
/// Service for comparative analysis of LLD question generation across different models.
/// Maintains in-memory short code history per model and scoring.
/// </summary>
public class ModelComparisonService : IModelComparisonService
{
    private readonly IOpenRouterCompletionClient _completionClient;
    private readonly IOpenRouterClient _responsesClient;
    private readonly Random _random = new();

    // Models that must use the Responses API (/api/v1/responses) instead of Chat Completions.
    private static readonly HashSet<string> ResponsesApiModels = new(StringComparer.OrdinalIgnoreCase)
    {
        "openai/gpt-oss-120b:free"
    };

    private static readonly List<string> Models =
    [
        "openrouter/aurora-alpha",
        //"sourceful/riverflow-v2-pro",
        //"sourceful/riverflow-v2-fast",
        //"stepfun/step-3.5-flash:free",
        "arcee-ai/trinity-large-preview:free",
        //"liquid/lfm-2.5-1.2b-thinking:free",
        //"liquid/lfm-2.5-1.2b-instruct:free",
        "nvidia/nemotron-3-nano-30b-a3b:free",
        //"sourceful/riverflow-v2-max-preview",
        //"sourceful/riverflow-v2-standard-preview",        
        //"sourceful/riverflow-v2-fast-preview", Need credits
        "arcee-ai/trinity-mini:free",
        "nvidia/nemotron-nano-12b-v2-vl:free",
        //"qwen/qwen3-next-80b-a3b-instruct:free",`
        "nvidia/nemotron-nano-9b-v2:free",
        "openai/gpt-oss-120b:free",
        //"openai/gpt-oss-20b:free",
        //"z-ai/glm-4.5-air:free",
       //"qwen/qwen3-235b-a22b-thinking-2507",
        //"qwen/qwen3-coder:free",
        //"cognitivecomputations/dolphin-mistral-24b-venice-edition:free",
        //"google/gemma-3n-e2b-it:free",
        "deepseek/deepseek-r1-0528:free",
        //"google/gemma-3n-e4b-it:free",
        //"qwen/qwen3-4b:free",
        //"mistralai/mistral-small-3.1-24b-instruct:free",
        //"google/gemma-3-4b-it:free",
        //"google/gemma-3-12b-it:free",
        //"google/gemma-3-27b-it:free",
        //"meta-llama/llama-3.3-70b-instruct:free",
        //"meta-llama/llama-3.2-3b-instruct:free",
        //"nousresearch/hermes-3-llama-3.1-405b:free"
    ];

    /// <summary>
    /// In-memory dictionary of short codes returned by each model.
    /// Key: model name, Value: list of short codes.
    /// </summary>
    private readonly ConcurrentDictionary<string, List<string>> _modelShortCodes = new();

    /// <summary>
    /// Tracks how many times each model was shown and selected.
    /// Key: model name, Value: (timesShown, timesSelected).
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

    public ModelComparisonService(IOpenRouterCompletionClient completionClient, IOpenRouterClient responsesClient)
    {
        _completionClient = completionClient;
        _responsesClient = responsesClient;
    }

    public async Task<ComparisonRoundResult> GenerateComparisonRoundAsync(
        DifficultyLevel difficulty,
        ReasoningEffort reasoningEffort,
        CancellationToken cancellationToken = default)
    {
        var (modelA, modelB) = PickTwoRandomModels();

        var resultA = await GenerateFromModelAsync(modelA, difficulty, reasoningEffort, cancellationToken);
        var resultB = await GenerateFromModelAsync(modelB, difficulty, reasoningEffort, cancellationToken);


        // Track short codes for successful responses
        if (resultA.IsSuccess && resultA.Question != null)
        {
            AddShortCode(modelA, resultA.Question.ShortCode);
        }
        if (resultB.IsSuccess && resultB.Question != null)
        {
            AddShortCode(modelB, resultB.Question.ShortCode);
        }

        return new ComparisonRoundResult
        {
            IsSuccess = resultA.IsSuccess || resultB.IsSuccess,
            ErrorMessage = (!resultA.IsSuccess && !resultB.IsSuccess)
                ? $"Both models failed. Model A ({modelA}): {resultA.ErrorMessage}; Model B ({modelB}): {resultB.ErrorMessage}"
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

    public List<string> GetAvailableModels() => [.. Models];

    private (string ModelA, string ModelB) PickTwoRandomModels()
    {
        var shuffled = Models.OrderBy(_ => _random.Next()).ToList();
        return (shuffled[0], shuffled[1]);
    }

    private async Task<ModelQuestionResult> GenerateFromModelAsync(
        string model,
        DifficultyLevel difficulty,
        ReasoningEffort reasoningEffort,
        CancellationToken cancellationToken)
    {
        try
        {
            var shortCodes = GetShortCodes(model);
            var systemPrompt = BuildSystemPrompt(difficulty);
            var userPrompt = BuildUserPrompt(shortCodes);

            if (ResponsesApiModels.Contains(model))
            {
                var result = await _responsesClient.SendPromptJsonAsync<LldQuestionResponse>(
                    model: model,
                    userPrompt: userPrompt,
                    jsonSchema: JsonSchema,
                    schemaName: "lld_question",
                    systemPrompt: systemPrompt,
                    temperature: 0.9,
                    reasoningEffort: reasoningEffort,
                    cancellationToken: cancellationToken);

                if (!result.IsSuccess)
                {
                    return new ModelQuestionResult { ModelName = model, IsSuccess = false, ErrorMessage = result.ErrorMessage };
                }

                return new ModelQuestionResult { ModelName = model, IsSuccess = true, Question = result.Data };
            }
            else
            {
                var result = await _completionClient.SendPromptJsonAsync<LldQuestionResponse>(
                    model: model,
                    userPrompt: userPrompt,
                    jsonSchema: JsonSchema,
                    schemaName: "lld_question",
                    systemPrompt: systemPrompt,
                    temperature: 0.9,
                    reasoningEffort: reasoningEffort,
                    cancellationToken: cancellationToken);

                if (!result.IsSuccess)
                {
                    return new ModelQuestionResult { ModelName = model, IsSuccess = false, ErrorMessage = result.ErrorMessage };
                }

                return new ModelQuestionResult { ModelName = model, IsSuccess = true, Question = result.Data };
            }
        }
        catch (Exception ex)
        {
            return new ModelQuestionResult { ModelName = model, IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    private List<string> GetShortCodes(string model)
    {
        return _modelShortCodes.TryGetValue(model, out var codes) ? [.. codes] : [];
    }

    private void AddShortCode(string model, string shortCode)
    {
        if (string.IsNullOrEmpty(shortCode)) return;

        _modelShortCodes.AddOrUpdate(
            model,
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
