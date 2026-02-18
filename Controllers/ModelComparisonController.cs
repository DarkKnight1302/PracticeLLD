using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using PracticeLLD.OpenRouter;
using PracticeLLD.Services.LldQuestion;
using PracticeLLD.Services.ModelComparison;

namespace PracticeLLD.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModelComparisonController : ControllerBase
{
    private readonly IModelComparisonService _comparisonService;

    public ModelComparisonController(IModelComparisonService comparisonService)
    {
        _comparisonService = comparisonService;
    }

    /// <summary>
    /// Generate questions from two random models for comparison.
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateRound([FromBody] ComparisonGenerateRequest request, CancellationToken cancellationToken)
    {
        var result = await _comparisonService.GenerateComparisonRoundAsync(
            request.Difficulty,
            request.ReasoningEffort,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return StatusCode(502, new { error = result.ErrorMessage });
        }

        return Ok(result);
    }

    /// <summary>
    /// Record the user's vote for the winning model.
    /// </summary>
    [HttpPost("vote")]
    public IActionResult RecordVote([FromBody] VoteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.WinningModel) || string.IsNullOrWhiteSpace(request.LosingModel))
        {
            return BadRequest(new { error = "Both winningModel and losingModel are required." });
        }

        _comparisonService.RecordVote(request.WinningModel, request.LosingModel);
        return Ok(new { message = "Vote recorded." });
    }

    /// <summary>
    /// Get the final analysis results.
    /// </summary>
    [HttpGet("results")]
    public IActionResult GetResults()
    {
        var results = _comparisonService.GetAnalysisResults();
        return Ok(results);
    }

    /// <summary>
    /// Reset all scores and short code history.
    /// </summary>
    [HttpPost("reset")]
    public IActionResult Reset()
    {
        _comparisonService.Reset();
        return Ok(new { message = "Analysis reset." });
    }

    /// <summary>
    /// Get available models.
    /// </summary>
    [HttpGet("models")]
    public IActionResult GetModels()
    {
        return Ok(_comparisonService.GetAvailableModels());
    }
}

#region Request DTOs

public class ComparisonGenerateRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Medium;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ReasoningEffort ReasoningEffort { get; set; } = ReasoningEffort.None;
}

public class VoteRequest
{
    public string WinningModel { get; set; } = string.Empty;
    public string LosingModel { get; set; } = string.Empty;
}

#endregion
