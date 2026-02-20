using PracticeLLD.Groq;
using PracticeLLD.OpenRouter;
using PracticeLLD.OpenRouter.Completion;
using PracticeLLD.Services.ModelComparison;

namespace PracticeLLD.Services.LldQuestion;

/// <summary>
/// Service for generating LLD interview questions using the OpenRouter Chat Completions API.
/// </summary>
public class LldQuestionService : ILldQuestionService
{
    private readonly IOpenRouterCompletionClient _completionClient;
    private readonly IGroqCompletionClient _groqClient;

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
            short_title = new { type = "string", description = "A unique short title which can be identifier for the question." }
        },
        required = new[] { "question", "constraints", "short_title" },
        additionalProperties = false
    };

    public LldQuestionService(IOpenRouterCompletionClient completionClient, IGroqCompletionClient groqClient)
    {
        _completionClient = completionClient;
        _groqClient = groqClient;
    }

    /// <inheritdoc />
    public async Task<LldQuestionResult> GenerateQuestionAsync(
        DifficultyLevel difficulty,
        List<string>? alreadyAskedShortTitles = null,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = BuildSystemPrompt(difficulty);
        var userPrompt = BuildUserPrompt(alreadyAskedShortTitles);

        var result = await _completionClient.SendPromptJsonAsync<LldQuestionResponse>(
            userPrompt: userPrompt,
            jsonSchema: JsonSchema,
            schemaName: "lld_question",
            systemPrompt: systemPrompt,
            temperature: 0.9,
            reasoningEffort: ReasoningEffort.Medium,
            cancellationToken: cancellationToken);

        return ToResult(result);
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

        CompletionResult<LldQuestionResponse> result;

        if (provider == ModelProvider.Groq)
        {
            result = await _groqClient.SendPromptJsonAsync<LldQuestionResponse>(
                model: modelId,
                userPrompt: userPrompt,
                jsonSchema: JsonSchema,
                schemaName: "lld_question",
                systemPrompt: systemPrompt,
                temperature: 0.9,
                reasoningEffort: reasoningEffort,
                cancellationToken: cancellationToken);
        }
        else
        {
            result = await _completionClient.SendPromptJsonAsync<LldQuestionResponse>(
                model: modelId,
                userPrompt: userPrompt,
                jsonSchema: JsonSchema,
                schemaName: "lld_question",
                systemPrompt: systemPrompt,
                temperature: 0.9,
                reasoningEffort: reasoningEffort,
                cancellationToken: cancellationToken);
        }

        return ToResult(result);
    }

    private static LldQuestionResult ToResult(CompletionResult<LldQuestionResponse> result)
    {
        if (!result.IsSuccess)
        {
            return new LldQuestionResult { IsSuccess = false, ErrorMessage = result.ErrorMessage };
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
            - Include clear constraints that define the scope of the problem.
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
