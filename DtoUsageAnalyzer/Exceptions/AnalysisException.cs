namespace DtoUsageAnalyzer.Exceptions;

/// <summary>
/// Base exception for analysis-related errors in the DtoUsageAnalyzer library.
/// </summary>
public class AnalysisException : Exception
{
  /// <summary>
  /// Initializes a new instance of the <see cref="AnalysisException"/> class.
  /// </summary>
  /// <param name="message">The error message that explains the reason for the exception.</param>
  public AnalysisException(string message)
    : base(message)
  {
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="AnalysisException"/> class.
  /// </summary>
  /// <param name="message">The error message that explains the reason for the exception.</param>
  /// <param name="innerException">The exception that is the cause of the current exception.</param>
  public AnalysisException(string message, Exception innerException)
    : base(message, innerException)
  {
  }
}
