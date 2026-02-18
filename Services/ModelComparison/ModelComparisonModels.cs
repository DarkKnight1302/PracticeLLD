using PracticeLLD.Services.LldQuestion;

namespace PracticeLLD.Services.ModelComparison;

/// <summary>
/// Result of a single comparison round between two models.
/// </summary>
public class ComparisonRoundResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public ModelQuestionResult? ModelA { get; set; }
    public ModelQuestionResult? ModelB { get; set; }
}

/// <summary>
/// Question result from a single model.
/// </summary>
public class ModelQuestionResult
{
    public required string ModelName { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public LldQuestionResponse? Question { get; set; }
}

/// <summary>
/// Final analysis results across all models.
/// </summary>
public class AnalysisResult
{
    public List<ModelScore> Scores { get; set; } = [];
    public int TotalRounds { get; set; }
}

/// <summary>
/// Score for a single model.
/// </summary>
public class ModelScore
{
    public required string ModelName { get; set; }
    public int TimesSelected { get; set; }
    public int TimesShown { get; set; }
    public double SelectionPercentage => TimesShown > 0 ? Math.Round((double)TimesSelected / TimesShown * 100, 1) : 0;
}
