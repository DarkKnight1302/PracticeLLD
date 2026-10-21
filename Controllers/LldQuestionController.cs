using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewHorizonLib.Attributes;
using NewHorizonLib.Services.Interfaces;
using PracticeLLD.Constants;
using PracticeLLD.Filters;
using PracticeLLD.OpenRouter;
using PracticeLLD.Services.LldQuestion;
using PracticeLLD.Services.ModelComparison;

namespace PracticeLLD.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LldQuestionController : ControllerBase
{
    private readonly ILldQuestionService _lldQuestionService;
    private readonly ITokenService _tokenService;

    public LldQuestionController(ILldQuestionService lldQuestionService, ITokenService tokenService)
    {
        _lldQuestionService = lldQuestionService;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Generate a new LLD interview question.
    /// </summary>
    [Authorize]
    [HttpPost("generate")]
    [RateLimit(15, 1440)]
    [ServiceFilter(typeof(LldQuestionHistoryFilter))]
    public async Task<IActionResult> GenerateQuestion([FromBody] GenerateLldQuestionRequest request, CancellationToken cancellationToken)
    {
        string userId = HttpContext.Request.Headers["x-uid"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest("User ID header is required");
        }

        bool isValid = _tokenService.IsValidAuth(userId, HttpContext, GlobalConstant.Issuer);
        if (!isValid)
        {
            return Unauthorized("Invalid authentication");
        }

        bool hasProvider = request.Provider.HasValue;
        bool hasModelId = !string.IsNullOrWhiteSpace(request.ModelId);

        if (hasProvider != hasModelId)
        {
            return BadRequest("Both 'provider' and 'modelId' must be provided together.");
        }

        // No explicit provider/model: use the default chain (beam-free primary, Groq fallback).
        var result = hasProvider
            ? await _lldQuestionService.GenerateQuestionAsync(
                modelId: request.ModelId!,
                provider: request.Provider!.Value,
                difficulty: request.Difficulty,
                reasoningEffort: request.ReasoningEffort,
                alreadyAskedShortTitles: request.AlreadyAskedShortTitles,
                cancellationToken: cancellationToken)
            : await _lldQuestionService.GenerateQuestionAsync(
                difficulty: request.Difficulty,
                alreadyAskedShortTitles: request.AlreadyAskedShortTitles,
                cancellationToken: cancellationToken);

        if (!result.IsSuccess)
        {
            return StatusCode(502, new { error = result.ErrorMessage });
        }

        return Ok(result.Question);
    }
}

#region Request DTOs

public class GenerateLldQuestionRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Medium;

    public List<string>? AlreadyAskedShortTitles { get; set; }

    /// <summary>
    /// Optional model provider. Provide together with <see cref="ModelId"/> to use a specific model
    /// instead of the default Groq fallback chain.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ModelProvider? Provider { get; set; }

    /// <summary>
    /// Optional model ID belonging to <see cref="Provider"/> (e.g. "beam-free" for OpenCode).
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// Reasoning effort forwarded to the selected provider. Ignored by the default Groq chain.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ReasoningEffort ReasoningEffort { get; set; } = ReasoningEffort.Medium;
}

#endregion
