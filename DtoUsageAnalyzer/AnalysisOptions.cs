namespace DtoUsageAnalyzer;

/// <summary>
/// Configuration options for controlling analysis behavior, particularly project filtering.
/// </summary>
public class AnalysisOptions
{
  /// <summary>
  /// Gets or sets patterns used to exclude projects from analysis.
  /// Supports wildcard patterns: "*Tests" (suffix), "Mock*" (prefix), "*Integration*" (contains), "Exact" (exact match).
  /// </summary>
  /// <value>
  /// An array of project name patterns to exclude. Empty array by default (no exclusions).
  /// </value>
  /// <example>
  /// <code>
  /// var options = new AnalysisOptions
  /// {
  ///     ExcludePatterns = ["*Tests", "*Integration*", "MockProject"]
  /// };
  /// </code>
  /// </example>
  public string[] ExcludePatterns { get; set; } = [];
}
