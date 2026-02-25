using PracticeLLD.Entities;

namespace PracticeLLD.Repositories;

/// <summary>
/// Repository for managing per-user LLD question history in Cosmos DB.
/// </summary>
public interface IUserLldQuestionHistoryRepository
{
    /// <summary>
    /// Gets the list of already-asked short titles for a user.
    /// </summary>
    Task<List<string>> GetAskedShortTitlesAsync(string userId);

    /// <summary>
    /// Adds a short title to the user's question history.
    /// </summary>
    Task AddShortTitleAsync(string userId, string shortTitle);
}
