using Microsoft.Azure.Cosmos;
using NewHorizonLib.Services;
using PracticeLLD.Entities;

namespace PracticeLLD.Repositories;

/// <summary>
/// Cosmos DB repository for per-user LLD question history.
/// Uses the "UserLldQuestionHistory" container with userId as partition key.
/// </summary>
public class UserLldQuestionHistoryRepository : IUserLldQuestionHistoryRepository
{
    private readonly ICosmosDbService _cosmosDbService;
    private readonly ILogger<UserLldQuestionHistoryRepository> _logger;

    public UserLldQuestionHistoryRepository(ICosmosDbService cosmosDbService, ILogger<UserLldQuestionHistoryRepository> logger)
    {
        _cosmosDbService = cosmosDbService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<string>> GetAskedShortTitlesAsync(string userId)
    {
        var container = GetContainer();
        try
        {
            var response = await container.ReadItemAsync<UserLldQuestionHistory>(userId, new PartitionKey(userId));
            return response.Resource?.AskedShortTitles ?? [];
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get LLD question history for user {UserId}", userId);
            return [];
        }
    }

    /// <inheritdoc />
    public async Task AddShortTitleAsync(string userId, string shortTitle)
    {
        var container = GetContainer();
        try
        {
            UserLldQuestionHistory history;
            try
            {
                var response = await container.ReadItemAsync<UserLldQuestionHistory>(userId, new PartitionKey(userId));
                history = response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                history = new UserLldQuestionHistory
                {
                    id = userId,
                    AskedShortTitles = []
                };
            }

            history.AskedShortTitles.Add(shortTitle);
            history.UpdatedAt = DateTimeOffset.UtcNow;

            await container.UpsertItemAsync(history, new PartitionKey(userId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save LLD question short title for user {UserId}", userId);
        }
    }

    private Container GetContainer()
    {
        return _cosmosDbService.GetContainer("UserLldQuestionHistory");
    }
}
