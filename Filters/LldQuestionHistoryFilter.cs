using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PracticeLLD.Controllers;
using PracticeLLD.Repositories;
using PracticeLLD.Services.LldQuestion;

namespace PracticeLLD.Filters;

/// <summary>
/// Action filter that enriches LLD question generation requests with already-asked
/// question history from Cosmos DB, and saves the newly generated question's short title
/// back to the user's history after a successful response.
/// </summary>
public class LldQuestionHistoryFilter : IAsyncActionFilter
{
    private readonly IUserLldQuestionHistoryRepository _historyRepository;
    private readonly ILogger<LldQuestionHistoryFilter> _logger;

    public LldQuestionHistoryFilter(IUserLldQuestionHistoryRepository historyRepository, ILogger<LldQuestionHistoryFilter> logger)
    {
        _historyRepository = historyRepository;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        string? userId = context.HttpContext.Request.Headers["x-uid"].FirstOrDefault();

        // Before: Fetch already-asked short titles and inject into the request
        if (!string.IsNullOrWhiteSpace(userId)
            && context.ActionArguments.TryGetValue("request", out var requestObj)
            && requestObj is GenerateLldQuestionRequest request)
        {
            var askedTitles = await _historyRepository.GetAskedShortTitlesAsync(userId);
            if (askedTitles.Count > 0)
            {
                // Merge with any titles the client may have sent
                request.AlreadyAskedShortTitles = request.AlreadyAskedShortTitles != null
                    ? [.. request.AlreadyAskedShortTitles, .. askedTitles]
                    : askedTitles;
            }
        }

        // Execute the action
        var executedContext = await next();

        // After: Save the generated question's short title to the user's history
        if (!string.IsNullOrWhiteSpace(userId)
            && executedContext.Exception == null
            && executedContext.Result is ObjectResult { StatusCode: 200 or null } objectResult
            && objectResult.Value is LldQuestionResponse questionResponse
            && !string.IsNullOrWhiteSpace(questionResponse.ShortTitle))
        {
            try
            {
                await _historyRepository.AddShortTitleAsync(userId, questionResponse.ShortTitle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save question history for user {UserId}", userId);
            }
        }
    }
}
