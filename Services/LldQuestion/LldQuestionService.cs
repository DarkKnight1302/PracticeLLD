using PracticeLLD.Groq;
using PracticeLLD.OpenCode;
using PracticeLLD.OpenRouter;
using PracticeLLD.OpenRouter.Completion;
using PracticeLLD.Services.ModelComparison;

namespace PracticeLLD.Services.LldQuestion;

/// <summary>
/// Service for generating LLD interview questions using a configurable model provider
/// (Groq, OpenRouter, or OpenCode).
/// </summary>
public class LldQuestionService : ILldQuestionService
{
    private readonly IGroqCompletionClient _groqClient;
    private readonly IOpenRouterCompletionClient _openRouterClient;
    private readonly IOpenCodeCompletionClient _openCodeClient;

    /// <summary>
    /// Model/provider chain to try in priority order for default question generation.
    /// beam-free (OpenCode Go) is the primary generator; Groq is the fallback.
    /// </summary>
    private static readonly (string Model, ModelProvider Provider)[] DefaultModelChain =
    [
        ("beam-free", ModelProvider.OpenCode),
        ("openai/gpt-oss-120b", ModelProvider.Groq)
    ];

    private static readonly object JsonSchema = new
    {
        type = "object",
        properties = new
        {
            question = new { type = "string", description = "The full LLD interview question text." },
            short_title = new { type = "string", description = "A unique short title which can be identifier for the question." },
            functional_requirements = new
            {
                type = "array",
                items = new { type = "string" },
                description = "List of functional requirements that the design should fulfill."
            },
            non_functional_requirements = new
            {
                type = "array",
                items = new { type = "string" },
                description = "List of non-functional requirements such as scalability, performance, thread-safety, and reliability considerations."
            }
        },
        required = new[] { "question", "short_title", "functional_requirements", "non_functional_requirements" },
        additionalProperties = false
    };

    public LldQuestionService(
        IGroqCompletionClient groqClient,
        IOpenRouterCompletionClient openRouterClient,
        IOpenCodeCompletionClient openCodeClient)
    {
        _groqClient = groqClient;
        _openRouterClient = openRouterClient;
        _openCodeClient = openCodeClient;
    }

    /// <inheritdoc />
    public async Task<LldQuestionResult> GenerateQuestionAsync(
        DifficultyLevel difficulty,
        List<string>? alreadyAskedShortTitles = null,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = BuildSystemPrompt(difficulty);
        var userPrompt = BuildUserPrompt(alreadyAskedShortTitles);

        LldQuestionResult? lastResult = null;

        foreach (var (model, provider) in DefaultModelChain)
        {
            var completion = await SendPromptJsonAsync(
                modelId: model,
                provider: provider,
                systemPrompt: systemPrompt,
                userPrompt: userPrompt,
                reasoningEffort: ReasoningEffort.Medium,
                cancellationToken: cancellationToken);

            lastResult = ToResult(completion);

            if (lastResult.IsSuccess)
            {
                return lastResult;
            }
        }

        return lastResult!;
    }

    /// <inheritdoc />
    public async Task<LldQuestionResult> GenerateQuestionAsync(
        string modelId,
        ModelProvider provider,
        DifficultyLevel difficulty,
        ReasoningEffort reasoningEffort,
        List<string>? alreadyAskedShortTitles = null,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = BuildSystemPrompt(difficulty);
        var userPrompt = BuildUserPrompt(alreadyAskedShortTitles);

        var result = await SendPromptJsonAsync(
            modelId: modelId,
            provider: provider,
            systemPrompt: systemPrompt,
            userPrompt: userPrompt,
            reasoningEffort: reasoningEffort,
            cancellationToken: cancellationToken);

        return ToResult(result);
    }

    private Task<CompletionResult<LldQuestionResponse>> SendPromptJsonAsync(
        string modelId,
        ModelProvider provider,
        string systemPrompt,
        string userPrompt,
        ReasoningEffort reasoningEffort,
        CancellationToken cancellationToken)
    {
        return provider switch
        {
            ModelProvider.OpenCode => _openCodeClient.SendPromptJsonAsync<LldQuestionResponse>(
                model: modelId,
                userPrompt: userPrompt,
                jsonSchema: JsonSchema,
                schemaName: "lld_question",
                systemPrompt: systemPrompt,
                temperature: 0.9,
                reasoningEffort: reasoningEffort,
                cancellationToken: cancellationToken),

            ModelProvider.OpenRouter => _openRouterClient.SendPromptJsonAsync<LldQuestionResponse>(
                model: modelId,
                userPrompt: userPrompt,
                jsonSchema: JsonSchema,
                schemaName: "lld_question",
                systemPrompt: systemPrompt,
                assistantPrompt: null,
                temperature: 0.9,
                reasoningEffort: reasoningEffort,
                cancellationToken: cancellationToken),

            _ => _groqClient.SendPromptJsonAsync<LldQuestionResponse>(
                model: modelId,
                userPrompt: userPrompt,
                jsonSchema: JsonSchema,
                schemaName: "lld_question",
                systemPrompt: systemPrompt,
                temperature: 0.9,
                reasoningEffort: reasoningEffort,
                cancellationToken: cancellationToken)
        };
    }

    private static LldQuestionResult ToResult(CompletionResult<LldQuestionResponse> result)
    {
        if (!result.IsSuccess)
        {
            return new LldQuestionResult { IsSuccess = false, ErrorMessage = result.ErrorMessage };
        }

        // Some models ignore the JSON schema and return a shape that deserializes to empty fields.
        if (result.Data is null
            || string.IsNullOrWhiteSpace(result.Data.Question)
            || string.IsNullOrWhiteSpace(result.Data.ShortTitle))
        {
            return new LldQuestionResult
            {
                IsSuccess = false,
                ErrorMessage = "Model response did not match the expected LLD question schema."
            };
        }

        return new LldQuestionResult { IsSuccess = true, Question = result.Data };
    }

    private static string BuildSystemPrompt(DifficultyLevel difficulty)
    {
        return $"""
            You are an experienced software engineering interviewer specializing in Low Level Design (LLD) questions.
            Your task is to ask a Low Level Design question for a software engineering interview of {difficulty.ToString().ToLowerInvariant()} difficulty.
            
            Guidelines:
            - The question should require the candidate to design classes, interfaces, and their relationships.
            - The "question" field must contain ONLY the scenario and the task for the candidate. Do NOT list the requirements inside it.
            - Put functional requirements ONLY in "functional_requirements" and non-functional requirements ONLY in "non_functional_requirements".
            - Never repeat or restate the requirements inside the "question" field, and never duplicate them across fields.
            - Functional requirements describe the core behaviors and features the design must support.
            - Non-functional requirements cover scalability, performance, thread-safety, extensibility, and reliability considerations relevant to the design.
            - Match the scope to the requested difficulty: an easy question targets a single focused component, while a hard question may combine several components and constraints.
            - The short_title should be a concise uppercase identifier that reflects the core concept of the question.
            - You should NOT ask any question whose short title matches one from the already asked list provided by the user.
            - Make sure the question is practical and commonly asked in real interviews.
            """;
    }

    private static string BuildUserPrompt(List<string>? alreadyAskedShortTitles)
    {
        if (alreadyAskedShortTitles == null || alreadyAskedShortTitles.Count == 0)
        {
            return "No questions have been asked yet. Generate a new LLD interview question.";
        }

        var titles = string.Join(", ", alreadyAskedShortTitles);
        return $"Already asked questions (short titles): {titles}. Generate a new LLD interview question that is different from the ones listed.";
    }
}
