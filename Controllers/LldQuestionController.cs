using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using PracticeLLD.Services.LldQuestion;

namespace PracticeLLD.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LldQuestionController : ControllerBase
{
    private readonly ILldQuestionService _lldQuestionService;

    public LldQuestionController(ILldQuestionService lldQuestionService)
    {
        _lldQuestionService = lldQuestionService;
    }

    /// <summary>
    /// Generate a new LLD interview question.
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateQuestion([FromBody] GenerateLldQuestionRequest request, CancellationToken cancellationToken)
    {
        var result = await _lldQuestionService.GenerateQuestionAsync(
            difficulty: request.Difficulty,
            alreadyAskedShortCodes: request.AlreadyAskedShortCodes,
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

    public List<string>? AlreadyAskedShortCodes { get; set; }
}

#endregion
