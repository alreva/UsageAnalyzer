namespace DtoUsageAnalyzer.Exceptions;

/// <summary>
/// Exception thrown when a solution file cannot be loaded or analyzed.
/// </summary>
public class SolutionLoadException : AnalysisException
{
  /// <summary>
  /// Initializes a new instance of the <see cref="SolutionLoadException"/> class.
  /// </summary>
  /// <param name="solutionPath">The path to the solution file that failed to load.</param>
  /// <param name="message">Additional error details.</param>
  /// <param name="innerException">The underlying exception that caused the failure.</param>
  public SolutionLoadException(string solutionPath, string message, Exception? innerException = null)
      : base(
        $"Failed to load solution '{solutionPath}': {message}. " +
             "Ensure the solution file exists, is valid, and all projects build successfully.", innerException)
  {
    this.SolutionPath = solutionPath;
  }

  /// <summary>
  /// Gets the path to the solution file that failed to load.
  /// </summary>
  public string SolutionPath { get; }

  /// <summary>
  /// Creates a SolutionLoadException for when a solution file doesn't exist.
  /// </summary>
  /// <param name="solutionPath">The path to the missing solution file.</param>
  /// <returns>A configured SolutionLoadException with appropriate message.</returns>
  public static SolutionLoadException FileNotFound(string solutionPath)
  {
    return new SolutionLoadException(
      solutionPath,
      "Solution file not found. Verify the path is correct and the file exists.");
  }

  /// <summary>
  /// Creates a SolutionLoadException for when a solution has no analyzable projects.
  /// </summary>
  /// <param name="solutionPath">The path to the solution file.</param>
  /// <returns>A configured SolutionLoadException with appropriate message.</returns>
  public static SolutionLoadException NoProjects(string solutionPath)
  {
    return new SolutionLoadException(
      solutionPath,
      "No projects found to analyze. The solution may be empty or all projects are being skipped.");
  }
}
