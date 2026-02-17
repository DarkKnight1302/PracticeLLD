using PracticeLLD.OpenRouter;

namespace PracticeLLD.Services.LldQuestion;

/// <summary>
/// Service for generating LLD interview questions using the OpenRouter API.
/// </summary>
public class LldQuestionService : ILldQuestionService
{
    private readonly IOpenRouterClient _openRouterClient;

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
            short_code = new { type = "string", description = "A unique short code identifier for the question (e.g., PARKING_LOT, ELEVATOR_SYSTEM)." }
        },
        required = new[] { "question", "constraints", "short_code" },
        additionalProperties = false
    };

    public LldQuestionService(IOpenRouterClient openRouterClient)
    {
        _openRouterClient = openRouterClient;
    }

    /// <inheritdoc />
    public async Task<LldQuestionResult> GenerateQuestionAsync(
        DifficultyLevel difficulty,
        List<string>? alreadyAskedShortCodes = null,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = BuildSystemPrompt(difficulty);
        var userPrompt = BuildUserPrompt(alreadyAskedShortCodes);

        var result = await _openRouterClient.SendPromptJsonAsync<LldQuestionResponse>(
            userPrompt: userPrompt,
            jsonSchema: JsonSchema,
            schemaName: "lld_question",
            systemPrompt: systemPrompt,
            temperature: 0.9,
            reasoningEffort: ReasoningEffort.Medium,
            cancellationToken: cancellationToken);

        if (!result.IsSuccess)
        {
            return new LldQuestionResult
            {
                IsSuccess = false,
                ErrorMessage = result.ErrorMessage
            };
        }

        return new LldQuestionResult
        {
            IsSuccess = true,
            Question = result.Data
        };
    }

    private static string BuildSystemPrompt(DifficultyLevel difficulty)
    {
        return $"""
            You are an experienced software engineering interviewer specializing in Low Level Design (LLD) questions.
            Your task is to ask a Low Level Design question for a software engineering interview of {difficulty.ToString().ToLowerInvariant()} difficulty.
            
            Guidelines:
            - The question should require the candidate to design classes, interfaces, and their relationships.
            - Include clear constraints that define the scope of the problem.
            - The short_code should be a concise uppercase identifier using underscores (e.g., PARKING_LOT, ELEVATOR_SYSTEM, CHESS_GAME).
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
