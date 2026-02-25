using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewHorizonLib.Attributes;
using NewHorizonLib.Services.Interfaces;
using PracticeLLD.Constants;
using PracticeLLD.Filters;
using PracticeLLD.Services.LldQuestion;

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

        var result = await _lldQuestionService.GenerateQuestionAsync(
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
}

#endregion
