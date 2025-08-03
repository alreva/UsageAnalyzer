namespace DtoUsageAnalyzer;

using System.Reflection;
using System.Runtime.Loader;
using System.Xml;
using System.Xml.Linq;
using DtoUsageAnalyzer.Exceptions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;

/// <summary>
/// Provides Roslyn-based static code analysis for DTO property usage across .NET solutions.
/// </summary>
/// <param name="logger">Logger instance for diagnostic information during analysis.</param>
public class AnalysisService(ILogger<AnalysisService> logger)
{
  /// <summary>
  /// Determines the target framework for a solution by reading Directory.Build.props.
  /// </summary>
  /// <param name="solutionDir">The directory containing the solution file.</param>
  /// <returns>The target framework moniker (e.g., "net8.0"). Defaults to "net8.0" if not found.</returns>
  /// <exception cref="InvalidAnalysisInputException">Thrown when solutionDir is null or empty.</exception>
  public string GetTargetFramework(string solutionDir)
  {
    try
    {
      ValidateStringParameter(solutionDir, nameof(solutionDir));
    }
    catch (InvalidAnalysisInputException ex)
    {
      logger.LogError(
        "Parameter validation failed in {MethodName}. Parameter {ParameterName} {ValidationFailure}",
        nameof(this.GetTargetFramework), nameof(solutionDir), "was null or empty");
      throw;
    }

    var propsPath = Path.Combine(solutionDir, "Directory.Build.props");
    var fileExists = File.Exists(propsPath);

    if (!fileExists)
    {
      logger.LogDebug(
        "Directory.Build.props not found at {PropsPath}, using default framework {DefaultFramework}",
        propsPath, "net8.0");
      return "net8.0";
    }

    try
    {
      var doc = XDocument.Load(propsPath);
      var tfElement = doc.Descendants("TargetFramework").FirstOrDefault();
      var framework = tfElement?.Value ?? "net8.0";

      logger.LogDebug(
        "Read target framework {TargetFramework} from {PropsPath}",
        framework, propsPath);

      return framework;
    }
    catch (Exception ex) when (ex is XmlException || ex is IOException)
    {
      logger.LogError(ex, "Failed to read Directory.Build.props. PropsPath: {PropsPath}, FileExists: {FileExists}, ErrorType: {ErrorType}",
        propsPath, fileExists, ex.GetType().Name);

      throw new AnalysisException(
        $"Failed to read Directory.Build.props at '{propsPath}'. " +
        "Ensure the file is valid XML and accessible.", ex);
    }
  }

  /// <summary>
  /// Recursively discovers all properties in a type hierarchy, including nested objects and collections.
  /// </summary>
  /// <param name="type">The root type to analyze for properties.</param>
  /// <param name="prefix">Optional prefix for nested property paths (used internally for recursion).</param>
  /// <returns>
  /// A list of tuples containing:
  /// - Property: The PropertyInfo of the discovered property
  /// - Type: The declaring type of the property
  /// - FullPath: The full dotted path to the property (e.g., "Address.City", "SocialMedia.Twitter").
  /// </returns>
  /// <exception cref="InvalidAnalysisInputException">Thrown when type is null.</exception>
  /// <example>
  /// For a User type with nested Address, this returns paths like:
  /// - "Name" (primitive property)
  /// - "Address.City" (nested object property)
  /// - "SocialMedia.Twitter" (nested object property).
  /// </example>
  public List<(PropertyInfo Property, Type Type, string FullPath)> GetDeepProperties(Type type, string prefix = "")
  {
    try
    {
      ValidateObjectParameter(type, nameof(type));
    }
    catch (InvalidAnalysisInputException ex)
    {
      logger.LogError(
        "Parameter validation failed in {MethodName}. Parameter {ParameterName} {ValidationFailure}",
        nameof(this.GetDeepProperties), nameof(type), "was null");
      throw;
    }

    var properties = new List<(PropertyInfo Property, Type Type, string FullPath)>();
    logger.LogDebug(
      "Starting deep property discovery for type {TypeName} with prefix '{Prefix}'",
      type.Name, prefix ?? string.Empty);

    foreach (var prop in type.GetProperties())
    {
      var propType = prop.PropertyType;
      var fullPath = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";

      if (IsPrimitiveOrArrayOfPrimitives(propType))
      {
        properties.Add((prop, type, fullPath));
        continue;
      }

      if (IsNullable(propType))
      {
        var itemProp = propType.GetProperty("Value")!;
        if (IsPrimitiveOrArrayOfPrimitives(itemProp.PropertyType))
        {
          properties.Add((prop, type, fullPath));
        }
        else
        {
          properties.AddRange(this.GetDeepProperties(itemProp.PropertyType, fullPath + ".Value"));
        }

        continue;
      }

      if (IsGenericList(propType))
      {
        var itemProp = propType.GetProperty("Item")!;
        properties.AddRange(this.GetDeepProperties(itemProp.PropertyType, fullPath + ".Item"));
        continue;
      }

      properties.AddRange(this.GetDeepProperties(propType, fullPath));
    }

    logger.LogDebug(
      "Completed deep property discovery for type {TypeName}. Found {PropertyCount} properties with prefix '{Prefix}'",
      type.Name, properties.Count, prefix ?? string.Empty);

    return properties;
  }

  /// <summary>
  /// Determines if a type is a primitive type, string, or an array/collection of primitives.
  /// </summary>
  /// <param name="type">The type to check.</param>
  /// <returns>
  /// True if the type is a primitive (int, bool, etc.), string, decimal, DateTime,
  /// or an array/IEnumerable of primitives; otherwise false.
  /// </returns>
  /// <example>
  /// Returns true for: int, string, DateTime, int[], List&lt;string&gt;
  /// Returns false for: custom classes, complex objects.
  /// </example>
  public static bool IsPrimitiveOrArrayOfPrimitives(Type type)
  {
    if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime))
    {
      return true;
    }

    if (type.IsArray)
    {
      var elementType = type.GetElementType();
      return elementType != null && (elementType.IsPrimitive || elementType == typeof(string));
    }

    if (IsGenericEnumerable(type))
    {
      var elementType = type.GetGenericArguments()[0];
      return elementType.IsPrimitive || elementType == typeof(string);
    }

    return false;
  }

  /// <summary>
  /// Checks if a type is a nullable value type (e.g., int?, DateTime?).
  /// </summary>
  /// <param name="type">The type to check.</param>
  /// <returns>True if the type is Nullable&lt;T&gt;; otherwise false.</returns>
  /// <example>
  /// Returns true for: int?, DateTime?, bool?
  /// Returns false for: int, string, object (reference types).
  /// </example>
  public static bool IsNullable(Type type)
  {
    return Nullable.GetUnderlyingType(type) != null;
  }

  /// <summary>
  /// Loads types from a DTO assembly file and filters for classes in the "Dto" namespace.
  /// </summary>
  /// <param name="dtoAssemblyPath">Absolute path to the compiled DTO assembly (.dll file).</param>
  /// <returns>Array of Type objects representing DTO classes found in the assembly.</returns>
  /// <exception cref="InvalidAnalysisInputException">Thrown when dtoAssemblyPath is null or empty.</exception>
  /// <exception cref="AssemblyLoadException">Thrown when the assembly file doesn't exist or contains no DTO types.</exception>
  /// <example>
  /// <code>
  /// var types = GetDtoAssemblyTypes("/path/to/Dto.dll");
  /// // Returns types like UserEventDto, AddressDto, etc.
  /// </code>
  /// </example>
  /// <remarks>
  /// This method currently filters for types in the "Dto" namespace only.
  /// Ensure the DTO project is built before calling this method.
  /// </remarks>
  public Type[] GetDtoAssemblyTypes(string dtoAssemblyPath)
  {
    try
    {
      ValidateStringParameter(dtoAssemblyPath, nameof(dtoAssemblyPath));
    }
    catch (InvalidAnalysisInputException ex)
    {
      logger.LogError(
        "Parameter validation failed in {MethodName}. Parameter {ParameterName} {ValidationFailure}",
        nameof(this.GetDtoAssemblyTypes), nameof(dtoAssemblyPath), "was null or empty");
      throw;
    }

    var fileExists = File.Exists(dtoAssemblyPath);
    if (!fileExists)
    {
      logger.LogError(
        "Assembly file not found. AssemblyPath: {AssemblyPath}, ErrorType: {ErrorType}",
        dtoAssemblyPath, "FileNotFound");
      throw AssemblyLoadException.FileNotFound(dtoAssemblyPath);
    }

    try
    {
      logger.LogDebug("Loading DTO assembly from {AssemblyPath}", dtoAssemblyPath);

      var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(dtoAssemblyPath);
      var types = assembly.GetTypes()
          .Where(t => t is { IsClass: true, Namespace: "Dto" })
          .ToArray();

      if (types.Length == 0)
      {
        logger.LogError(
          "No DTO types found in assembly. AssemblyPath: {AssemblyPath}, Namespace: {Namespace}, TotalTypes: {TotalTypes}",
          dtoAssemblyPath, "Dto", assembly.GetTypes().Length);
        throw AssemblyLoadException.NoTypesFound(dtoAssemblyPath, "Dto");
      }

      logger.LogDebug(
        "Successfully loaded {TypeCount} DTO types from {AssemblyPath}: {TypeNames}",
        types.Length, dtoAssemblyPath, string.Join(", ", types.Select(t => t.Name)));

      return types;
    }
    catch (Exception ex) when (ex is BadImageFormatException || ex is FileLoadException)
    {
      logger.LogError(ex, "Failed to load assembly. AssemblyPath: {AssemblyPath}, ErrorType: {ErrorType}, FileExists: {FileExists}",
        dtoAssemblyPath, ex.GetType().Name, fileExists);

      throw new AssemblyLoadException(
        dtoAssemblyPath,
        "Assembly file is corrupted or not a valid .NET assembly.", ex);
    }
  }

  /// <summary>
  /// Analyzes property usage for a specific DTO class across all projects in a solution.
  /// </summary>
  /// <param name="solutionPath">Absolute path to the .sln file to analyze.</param>
  /// <param name="selectedClass">The DTO class type to analyze for property usage patterns.</param>
  /// <param name="shouldSkipTestProjects">
  /// If true, excludes projects ending with "Tests" from analysis.
  /// Also excludes "Analyze" and "Dto" projects by default.
  /// </param>
  /// <returns>
  /// A collection of property usage data where each item contains:
  /// - Property: UsageKey with file path and property information
  /// - UsageCount: Number of times the property is accessed in that location
  /// Properties with 0 usage are included to identify unused properties.
  /// </returns>
  /// <exception cref="InvalidAnalysisInputException">Thrown when input parameters are invalid.</exception>
  /// <exception cref="SolutionLoadException">Thrown when the solution file cannot be loaded or analyzed.</exception>
  /// <example>
  /// <code>
  /// var service = new AnalysisService(logger);
  /// var results = await service.AnalyzeUsageAsync(
  ///     "/path/to/solution.sln",
  ///     typeof(UserEventDto),
  ///     skipTests: true);
  ///
  /// foreach (var usage in results)
  /// {
  ///     Console.WriteLine($"{usage.Property.Attribute.ClassName}.{usage.Property.Attribute.FieldName}: {usage.UsageCount} usages");
  /// }
  /// </code>
  /// </example>
  public async Task<IReadOnlyList<PropertyUsage>> AnalyzeUsageAsync(
      string solutionPath,
      Type selectedClass,
      bool shouldSkipTestProjects)
  {
    try
    {
      ValidateStringParameter(solutionPath, nameof(solutionPath));
      ValidateObjectParameter(selectedClass, nameof(selectedClass));
    }
    catch (InvalidAnalysisInputException ex)
    {
      logger.LogError(
        "Parameter validation failed in {MethodName}. SolutionPath: {SolutionPath}, ClassName: {ClassName}",
        nameof(this.AnalyzeUsageAsync), solutionPath ?? "null", selectedClass?.Name ?? "null");
      throw;
    }

    var fileExists = File.Exists(solutionPath);
    if (!fileExists)
    {
      logger.LogError(
        "Solution file not found. SolutionPath: {SolutionPath}, ErrorType: {ErrorType}",
        solutionPath, "FileNotFound");
      throw SolutionLoadException.FileNotFound(solutionPath);
    }

    logger.LogDebug("Starting analysis for class: {SelectedClassFullName}", selectedClass.FullName);
    var propertyUsage = new Dictionary<UsageKey, int>();

    // Find property references
    var deepProperties = this.GetDeepProperties(selectedClass);
    logger.LogDebug(
        "Found {Count} deep properties in {CurrentTypeFullName}",
        deepProperties.Count,
        selectedClass.FullName);

    var solution = this.LoadSolutionWorkspace(solutionPath);

    foreach (var project in solution.Projects)
    {
      // Skip Test project
      if (shouldSkipTestProjects && project.Name.EndsWith("Tests"))
      {
        logger.LogInformation("Skipping test project {ProjectName}.", project.Name);
        continue;
      }

      // Skip Analyze project
      if (project.Name is "Analyze" or "Dto")
      {
        logger.LogInformation("Skipping {ProjectName} project.", project.Name);
        continue;
      }

      var compilation = await SetupProjectCompilation(project, selectedClass.Assembly.Location);
      if (compilation == null)
      {
        continue;
      }

      await this.AnalyzeProjectDocuments(project, compilation, deepProperties, propertyUsage, selectedClass);
    }

    // add unused properties from deepProperties:
    foreach (var deepProperty in deepProperties)
    {
      var attribute = new ClassAndField(deepProperty.Type.Name, deepProperty.Property.Name);

      if (propertyUsage.Any(k => k.Key.Attribute == attribute))
      {
        continue; // already exists
      }

      UsageKey key = new("N/A", attribute);
      propertyUsage.TryAdd(key, 0);
    }

    var results = propertyUsage.Select(kvp => new PropertyUsage(kvp.Key, kvp.Value)).ToList();
    var totalUsages = results.Sum(r => r.UsageCount);
    var unusedProperties = results.Count(r => r.UsageCount == 0);

    logger.LogInformation(
      "Analysis completed for class {ClassName} in solution {SolutionPath}. " +
      "PropertiesAnalyzed: {PropertyCount}, TotalUsages: {TotalUsages}, UnusedProperties: {UnusedProperties}, SkipTests: {SkipTests}",
      selectedClass.Name, solutionPath, results.Count, totalUsages, unusedProperties, shouldSkipTestProjects);

    return results;
  }

  private static async Task<Compilation?> SetupProjectCompilation(Project project, string assemblyPath)
  {
    var coreAssemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
    return (await project.GetCompilationAsync())?
        .AddReferences(
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(coreAssemblyPath, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(coreAssemblyPath, "System.Collections.dll")),
            MetadataReference.CreateFromFile(Path.Combine(coreAssemblyPath, "System.Console.dll")),
            MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(assemblyPath));
  }

  private Solution LoadSolutionWorkspace(string solutionPath)
  {
    logger.LogDebug("Loading solution workspace from {SolutionPath}", solutionPath);

    try
    {
      using var workspace = new AdhocWorkspace(MefHostServices.Create(MefHostServices.DefaultAssemblies));
      workspace.WorkspaceFailed += (_, args) =>
      {
        logger.LogWarning("Workspace diagnostic: {Diagnostic} for solution {SolutionPath}", args.Diagnostic, solutionPath);
      };

      var projectPaths = GetProjectPathsFromSolution(solutionPath);
      logger.LogDebug("Found {ProjectPathCount} projects in solution {SolutionPath}", projectPaths.Count, solutionPath);

      if (projectPaths.Count == 0)
      {
        logger.LogError("No projects found in solution {SolutionPath}", solutionPath);
        throw SolutionLoadException.NoProjects(solutionPath);
      }

      foreach (var projectPath in projectPaths)
      {
        logger.LogDebug("Loading project {ProjectPath} into workspace", projectPath);
        this.LoadProjectIntoWorkspace(workspace, projectPath.Replace("\\", "/"));
      }

      var solution = workspace.CurrentSolution;
      var loadedProjectCount = solution.Projects.Count();
      logger.LogInformation("Successfully loaded solution {SolutionPath} with {ProjectCount} projects", solutionPath, loadedProjectCount);

      if (!solution.Projects.Any())
      {
        logger.LogError("No projects loaded into workspace for solution {SolutionPath}", solutionPath);
        throw SolutionLoadException.NoProjects(solutionPath);
      }

      return solution;
    }
    catch (Exception ex) when (!(ex is SolutionLoadException))
    {
      logger.LogError(ex, "Unexpected error loading solution workspace. SolutionPath: {SolutionPath}, ErrorType: {ErrorType}",
        solutionPath, "UnexpectedError");
      throw new SolutionLoadException(
        solutionPath,
        "Unexpected error during solution loading. Check that all projects build successfully.", ex);
    }
  }

  private async Task AnalyzeProjectDocuments(
      Project project,
      Compilation compilation,
      List<(PropertyInfo Property, Type Type, string FullPath)> deepProperties,
      Dictionary<UsageKey, int> propertyUsage,
      Type selectedClass)
  {
    foreach (var document in project.Documents)
    {
      var filePath = document.FilePath!;
      if (filePath.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
          || filePath.Contains("/bin/"))
      {
        continue;
      }

      var syntaxTree = await document.GetSyntaxTreeAsync();
      if (syntaxTree == null)
      {
        continue;
      }

      var semanticModel = compilation.GetSemanticModel(syntaxTree);
      var root = await syntaxTree.GetRootAsync();

      logger.LogDebug("Analyzing file: {FilePath}", filePath);

      var memberAccessExpressionSyntaxes = root
          .DescendantNodes()
          .OfType<MemberAccessExpressionSyntax>();

      var deepPropertyNames = deepProperties
          .Select(p => p.Property.Name)
          .ToHashSet(StringComparer.OrdinalIgnoreCase);

      this.AnalyzePropertyUsage(
          memberAccessExpressionSyntaxes,
          deepPropertyNames,
          deepProperties,
          semanticModel,
          filePath,
          selectedClass,
          propertyUsage);
    }
  }

  private void AnalyzePropertyUsage(
      IEnumerable<MemberAccessExpressionSyntax> memberAccessExpressions,
      HashSet<string> deepPropertyNames,
      List<(PropertyInfo Property, Type Type, string FullPath)> deepProperties,
      SemanticModel semanticModel,
      string filePath,
      Type selectedClass,
      Dictionary<UsageKey, int> propertyUsage)
  {
    foreach (var usageCandidate in memberAccessExpressions)
    {
      if (!deepPropertyNames.Contains(usageCandidate.Name.Identifier.Text))
      {
        continue;
      }

      var attribute = GetClassAndFieldName(semanticModel, usageCandidate);
      var deepProperty = deepProperties
          .SingleOrDefault(p =>
              attribute.ClassName == p.Type.Name
              && attribute.FieldName == p.Property.Name);

      if (deepProperty == default)
      {
        logger.LogDebug(
            "Property usage for {PropertyName}" +
            " in file {FilePath} is of type {ActualClassName}" +
            " and does not match expected class {ExpectedClassName}.",
            attribute.FieldName,
            filePath,
            attribute.ClassName,
            selectedClass.Name);
        continue;
      }

      if (string.IsNullOrWhiteSpace(attribute.ClassName))
      {
        attribute = attribute with { ClassName = selectedClass.Name };
      }

      UsageKey key = new(filePath, attribute);
      if (!propertyUsage.TryAdd(key, 1))
      {
        propertyUsage[key]++;
      }
    }
  }

  private static bool IsGenericList(Type type)
  {
    return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));
  }

  private static bool IsGenericEnumerable(Type type)
  {
    return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
  }

  private void LoadProjectIntoWorkspace(AdhocWorkspace workspace, string projectPath)
  {
    if (!File.Exists(projectPath))
    {
      logger.LogWarning(
        "Project file not found during workspace loading. ProjectPath: {ProjectPath}, FileExists: {FileExists}",
        projectPath, false);
      return;
    }

    var projectName = Path.GetFileNameWithoutExtension(projectPath);
    logger.LogDebug("Loading project into workspace. ProjectName: {ProjectName}, ProjectPath: {ProjectPath}", projectName, projectPath);

    try
    {
      var projectInfo = ProjectInfo.Create(
          ProjectId.CreateNewId(),
          VersionStamp.Create(),
          projectName,
          projectName,
          LanguageNames.CSharp);

      var project = workspace.AddProject(projectInfo);

      var projectDirectory = Path.GetDirectoryName(projectPath)!;
      var documents = Directory.GetFiles(projectDirectory, "*.cs", SearchOption.AllDirectories);
      logger.LogDebug("Found {DocumentCount} C# files in project {ProjectName}", documents.Length, projectName);

      foreach (var docPath in documents)
      {
        var sourceText = SourceText.From(File.ReadAllText(docPath));
        var documentInfo = DocumentInfo.Create(
            DocumentId.CreateNewId(project.Id),
            Path.GetFileName(docPath),
            loader: TextLoader.From(TextAndVersion.Create(sourceText, VersionStamp.Create())),
            filePath: docPath);

        workspace.AddDocument(documentInfo);
      }

      logger.LogDebug("Successfully loaded project {ProjectName} with {DocumentCount} documents", projectName, documents.Length);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to load project into workspace. ProjectName: {ProjectName}, ProjectPath: {ProjectPath}, ErrorType: {ErrorType}",
        projectName, projectPath, "ProjectLoadError");
      throw;
    }
  }

  private static List<string> GetProjectPathsFromSolution(string solutionPath)
  {
    var projectPaths = new List<string>();
    var solutionDir = Path.GetDirectoryName(solutionPath)!;

    var projectLines = File.ReadAllLines(solutionPath)
      .Where(line => line.Trim().StartsWith("Project(") && line.Contains(".csproj"));
    foreach (var line in projectLines)
    {
      var parts = line.Split(',');
      if (parts.Length > 1)
      {
        var relativePath = parts[1].Trim().Trim('"');
        var fullPath = Path.Combine(solutionDir, relativePath);
        projectPaths.Add(fullPath);
      }
    }

    return projectPaths;
  }

  private static ClassAndField GetClassAndFieldName(SemanticModel model, MemberAccessExpressionSyntax usage)
  {
    var fieldName = usage.Name.ToString();
    var expression = usage.Expression; // This is 'address' in 'address.ZipCode'
    var typeInfo = model.GetTypeInfo(expression);
    var type = typeInfo.Type;
    return new ClassAndField(type is null ? string.Empty : type.Name, fieldName);
  }

  /// <summary>
  /// Validates that a string parameter is not null or empty.
  /// </summary>
  /// <param name="value">The parameter value to validate.</param>
  /// <param name="parameterName">The name of the parameter being validated.</param>
  /// <exception cref="InvalidAnalysisInputException">Thrown when the parameter is null or empty.</exception>
  private static void ValidateStringParameter(string? value, string parameterName)
  {
    if (string.IsNullOrEmpty(value))
    {
      throw InvalidAnalysisInputException.NullOrEmpty(parameterName);
    }
  }

  /// <summary>
  /// Validates that an object parameter is not null.
  /// </summary>
  /// <param name="value">The parameter value to validate.</param>
  /// <param name="parameterName">The name of the parameter being validated.</param>
  /// <exception cref="InvalidAnalysisInputException">Thrown when the parameter is null.</exception>
  private static void ValidateObjectParameter(object? value, string parameterName)
  {
    if (value is null)
    {
      throw InvalidAnalysisInputException.Null(parameterName);
    }
  }
}
