using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using PracticeLLD.OpenRouter;

namespace PracticeLLD.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OpenRouterTestController : ControllerBase
{
    private readonly IOpenRouterClient _openRouterClient;

    public OpenRouterTestController(IOpenRouterClient openRouterClient)
    {
        _openRouterClient = openRouterClient;
    }

    /// <summary>
    /// Send a simple prompt to OpenRouter and get a text response.
    /// </summary>
    [HttpPost("prompt")]
    public async Task<IActionResult> SendPrompt([FromBody] PromptRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserPrompt))
        {
            return BadRequest(new { error = "userPrompt is required." });
        }

        var result = await _openRouterClient.SendPromptAsync(
            userPrompt: request.UserPrompt,
            systemPrompt: request.SystemPrompt,
            assistantPrompt: request.AssistantPrompt,
            temperature: request.Temperature,
            reasoningEffort: request.ReasoningEffort,
            maxOutputTokens: request.MaxOutputTokens,
            cancellationToken: cancellationToken);

        if (!result.IsSuccess)
        {
            return StatusCode(502, new
            {
                error = result.ErrorMessage
            });
        }

        return Ok(new PromptResponse
        {
            TextResponse = result.TextResponse,
            ReasoningSummary = result.ReasoningSummary,
            Usage = result.Usage
        });
    }

    /// <summary>
    /// Send a multi-turn conversation to OpenRouter.
    /// </summary>
    [HttpPost("conversation")]
    public async Task<IActionResult> SendConversation([FromBody] ConversationRequest request, CancellationToken cancellationToken)
    {
        if (request.Messages == null || request.Messages.Count == 0)
        {
            return BadRequest(new { error = "At least one message is required." });
        }

        var messages = request.Messages.Select(m => new MessageInput
        {
            Role = m.Role,
            Content =
            [
                new MessageContent
                {
                    Type = m.Role == "assistant" ? "output_text" : "input_text",
                    Text = m.Content
                }
            ]
        }).ToList();

        var result = await _openRouterClient.SendMessagesAsync(
            messages: messages,
            temperature: request.Temperature,
            reasoningEffort: request.ReasoningEffort,
            maxOutputTokens: request.MaxOutputTokens,
            cancellationToken: cancellationToken);

        if (!result.IsSuccess)
        {
            return StatusCode(502, new
            {
                error = result.ErrorMessage
            });
        }

        return Ok(new PromptResponse
        {
            TextResponse = result.TextResponse,
            ReasoningSummary = result.ReasoningSummary,
            Usage = result.Usage
        });
    }
}

#region Request / Response DTOs

public class PromptRequest
{
    public string? UserPrompt { get; set; }
    public string? SystemPrompt { get; set; }
    public string? AssistantPrompt { get; set; }
    public double? Temperature { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ReasoningEffort ReasoningEffort { get; set; } = ReasoningEffort.None;

    public int? MaxOutputTokens { get; set; }
}

public class ConversationRequest
{
    public List<ConversationMessage>? Messages { get; set; }
    public double? Temperature { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ReasoningEffort ReasoningEffort { get; set; } = ReasoningEffort.None;

    public int? MaxOutputTokens { get; set; }
}

public class ConversationMessage
{
    public required string Role { get; set; }
    public required string Content { get; set; }
}

public class PromptResponse
{
    public string? TextResponse { get; set; }
    public List<string>? ReasoningSummary { get; set; }
    public UsageInfo? Usage { get; set; }
}

#endregion
