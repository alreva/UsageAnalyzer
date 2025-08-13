namespace DtoUsageAnalyzer.Exceptions;

/// <summary>
/// Exception thrown when a DTO assembly cannot be loaded or analyzed.
/// </summary>
public class AssemblyLoadException : AnalysisException
{
  /// <summary>
  /// Initializes a new instance of the <see cref="AssemblyLoadException"/> class.
  /// </summary>
  /// <param name="assemblyPath">The path to the assembly file that failed to load.</param>
  /// <param name="message">Additional error details.</param>
  /// <param name="innerException">The underlying exception that caused the failure.</param>
  public AssemblyLoadException(string assemblyPath, string message, Exception? innerException = null)
      : base(
        $"Failed to load assembly '{assemblyPath}': {message}. Ensure the assembly file exists and was built successfully.",
        innerException)
  {
    this.AssemblyPath = assemblyPath;
  }

  /// <summary>
  /// Gets the path to the assembly file that failed to load.
  /// </summary>
  public string AssemblyPath { get; }

  /// <summary>
  /// Creates an AssemblyLoadException for when an assembly file doesn't exist.
  /// </summary>
  /// <param name="assemblyPath">The path to the missing assembly file.</param>
  /// <returns>A configured AssemblyLoadException with appropriate message.</returns>
  public static AssemblyLoadException FileNotFound(string assemblyPath)
  {
    return new AssemblyLoadException(
      assemblyPath,
      "Assembly file not found. Build the project first using 'dotnet build'.");
  }

  /// <summary>
  /// Creates an AssemblyLoadException for when no DTO types are found in the assembly.
  /// </summary>
  /// <param name="assemblyPath">The path to the assembly file.</param>
  /// <param name="namespaceName">The namespace that was searched for DTO types.</param>
  /// <returns>A configured AssemblyLoadException with appropriate message.</returns>
  public static AssemblyLoadException NoTypesFound(string assemblyPath, string namespaceName)
  {
    return new AssemblyLoadException(
      assemblyPath,
      $"No DTO classes found in namespace '{namespaceName}'. Verify the namespace contains public classes.");
  }
}
