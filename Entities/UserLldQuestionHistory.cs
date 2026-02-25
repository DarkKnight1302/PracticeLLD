namespace PracticeLLD.Entities;

/// <summary>
/// Tracks the LLD questions already asked to a specific user.
/// Stored one document per user in the "UserLldQuestionHistory" Cosmos DB collection.
/// </summary>
public class UserLldQuestionHistory
{
    /// <summary>
    /// The user ID (also used as partition key).
    /// </summary>
    public string id { get; set; } = string.Empty;

    /// <summary>
    /// Short titles of LLD questions already asked to this user.
    /// </summary>
    public List<string> AskedShortTitles { get; set; } = [];

    /// <summary>
    /// Last updated timestamp.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
