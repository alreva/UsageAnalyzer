namespace DtoUsageAnalyzer.Exceptions;

/// <summary>
/// Exception thrown when invalid input parameters are provided to analysis methods.
/// </summary>
public class InvalidAnalysisInputException : AnalysisException
{
  /// <summary>
  /// Gets the name of the parameter that was invalid.
  /// </summary>
  public string ParameterName { get; }

  /// <summary>
  /// Initializes a new instance of the <see cref="InvalidAnalysisInputException"/> class.
  /// </summary>
  /// <param name="parameterName">The name of the invalid parameter.</param>
  /// <param name="message">Description of why the parameter is invalid.</param>
  /// <param name="innerException">The underlying exception that caused the failure.</param>
  public InvalidAnalysisInputException(string parameterName, string message, Exception? innerException = null)
      : base($"Invalid parameter '{parameterName}': {message}", innerException)
  {
    this.ParameterName = parameterName;
  }

  /// <summary>
  /// Creates an InvalidAnalysisInputException for null or empty string parameters.
  /// </summary>
  /// <param name="parameterName">The name of the parameter.</param>
  /// <returns>A configured InvalidAnalysisInputException with appropriate message.</returns>
  public static InvalidAnalysisInputException NullOrEmpty(string parameterName)
  {
    return new InvalidAnalysisInputException(
      parameterName,
      "Parameter cannot be null or empty.");
  }

  /// <summary>
  /// Creates an InvalidAnalysisInputException for null parameters.
  /// </summary>
  /// <param name="parameterName">The name of the parameter.</param>
  /// <returns>A configured InvalidAnalysisInputException with appropriate message.</returns>
  public static InvalidAnalysisInputException Null(string parameterName)
  {
    return new InvalidAnalysisInputException(
      parameterName,
      "Parameter cannot be null.");
  }

  /// <summary>
  /// Creates an InvalidAnalysisInputException for invalid file paths.
  /// </summary>
  /// <param name="parameterName">The name of the parameter.</param>
  /// <param name="path">The invalid path value.</param>
  /// <returns>A configured InvalidAnalysisInputException with appropriate message.</returns>
  public static InvalidAnalysisInputException InvalidPath(string parameterName, string path)
  {
    return new InvalidAnalysisInputException(
      parameterName,
      $"Path '{path}' is not a valid absolute path.");
  }
}
