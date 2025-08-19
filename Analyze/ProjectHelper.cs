namespace Analyze;

using System.Xml;
using System.Xml.Linq;

/// <summary>
/// Helper class for project-specific operations in the Analyze console application.
/// </summary>
public static class ProjectHelper
{
  private const string DefaultTargetFramework = "net8.0";
  private const string DirectoryBuildPropsFile = "Directory.Build.props";

  /// <summary>
  /// Determines the target framework for a solution by reading Directory.Build.props.
  /// </summary>
  /// <param name="solutionDir">The directory containing the solution file.</param>
  /// <returns>The target framework moniker (e.g., "net8.0"). Defaults to "net8.0" if not found.</returns>
  /// <exception cref="ArgumentException">Thrown when solutionDir is null or empty.</exception>
  /// <exception cref="InvalidOperationException">Thrown when Directory.Build.props cannot be read.</exception>
  public static string GetTargetFramework(string solutionDir)
  {
    if (string.IsNullOrEmpty(solutionDir))
    {
      throw new ArgumentException("Solution directory cannot be null or empty.", nameof(solutionDir));
    }

    var propsPath = Path.Combine(solutionDir, DirectoryBuildPropsFile);

    if (!File.Exists(propsPath))
    {
      return DefaultTargetFramework;
    }

    try
    {
      var doc = XDocument.Load(propsPath);
      var tfElement = doc.Descendants("TargetFramework").FirstOrDefault();
      return tfElement?.Value ?? DefaultTargetFramework;
    }
    catch (Exception ex) when (ex is XmlException || ex is IOException)
    {
      throw new InvalidOperationException(
          $"Failed to read Directory.Build.props at '{propsPath}'. Ensure the file is valid XML and accessible.",
          ex);
    }
  }
}
