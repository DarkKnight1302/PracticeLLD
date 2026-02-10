namespace PracticeLLD.OpenRouter;

/// <summary>
/// Reasoning effort levels for OpenRouter API requests.
/// Controls how much computational effort the model puts into reasoning.
/// </summary>
public enum ReasoningEffort
{
    /// <summary>
    /// No reasoning - standard response without reasoning chain.
    /// </summary>
    None,

    /// <summary>
    /// Basic reasoning with minimal computational effort.
    /// </summary>
    Minimal,

    /// <summary>
    /// Light reasoning for simple problems.
    /// </summary>
    Low,

    /// <summary>
    /// Balanced reasoning for moderate complexity.
    /// </summary>
    Medium,

    /// <summary>
    /// Deep reasoning for complex problems.
    /// </summary>
    High
}
