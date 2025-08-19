namespace DtoUsageAnalyzer;

using System.Reflection;
using System.Runtime.Loader;
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
public class AnalysisService
{
  private const string DtoNamespace = "Dto";
  private const string UnusedPropertyFilePath = "N/A";
  private const string ObjDirectoryPath = "/obj/";
  private const string BinDirectoryPath = "/bin/";
  private readonly ILogger<AnalysisService> logger;
  private readonly AnalysisOptions options;

  /// <summary>
  /// Initializes a new instance of the <see cref="AnalysisService"/> class.
  /// </summary>
  /// <param name="logger">Logger instance for diagnostic information during analysis.</param>
  /// <param name="options">Optional configuration for analysis behavior. If null, default options with no exclusions will be used.</param>
  public AnalysisService(ILogger<AnalysisService> logger, AnalysisOptions? options = null)
  {
    this.logger = logger;
    this.options = options ?? new();
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
  /// <returns>True if the type is Nullable&lt;T&gt; otherwise false.</returns>
  /// <example>
  /// Returns true for: int?, DateTime?, bool?
  /// Returns false for: int, string, object (reference types).
  /// </example>
  public static bool IsNullable(Type type)
  {
    return Nullable.GetUnderlyingType(type) != null;
  }

  /// <summary>
  /// Recursively discovers all members (properties and fields) in a type hierarchy, including nested objects and collections.
  /// </summary>
  /// <param name="type">The root type to analyze for members.</param>
  /// <returns>
  /// A list of AnalyzedMember objects containing member information and metadata.
  /// </returns>
  /// <exception cref="InvalidAnalysisInputException">Thrown when the type is null.</exception>
  /// <example>
  /// For a User type with nested Address, this returns paths like:
  /// - "Name" (primitive property)
  /// - "Address.City" (nested object property)
  /// - "deviceId" (field in a nested object)
  /// - "SocialMedia.Twitter" (nested object property).
  /// </example>
  public List<AnalyzedMember> GetDeepMembers(Type type)
  {
    return this.GetDeepMembers(type, string.Empty, new HashSet<Type>());
  }

  private List<AnalyzedMember> GetDeepMembers(Type type, string prefix, HashSet<Type> visitedTypes)
  {
    ValidateObjectParameter(type, nameof(type));

    // Check for circular reference
    if (visitedTypes.Contains(type))
    {
      this.logger.LogDebug(
        "Circular reference detected for type {TypeName} with prefix '{Prefix}' - skipping to prevent infinite recursion",
        type.Name,
        prefix);
      return new List<AnalyzedMember>();
    }

    // Add current type to visited set
    visitedTypes.Add(type);

    try
    {
      var members = new List<AnalyzedMember>();
      this.logger.LogDebug(
        "Starting deep member discovery for type {TypeName} with prefix '{Prefix}'",
        type.Name,
        prefix);

      // Get all properties
      foreach (var prop in type.GetProperties())
      {
        this.ProcessMember(prop, prop.PropertyType, prop.Name, type, prefix, members, visitedTypes);
      }

      // Get all public fields
      foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
      {
        this.ProcessMember(field, field.FieldType, field.Name, type, prefix, members, visitedTypes);
      }

      this.logger.LogDebug(
        "Completed deep member discovery for type {TypeName}. Found {MemberCount} members with prefix '{Prefix}'",
        type.Name,
        members.Count,
        prefix);

      return members;
    }
    finally
    {
      // Remove type from visited set when done processing this branch
      visitedTypes.Remove(type);
    }
  }

  /// <summary>
  /// Loads types from an assembly file and optionally filters for classes in a specific namespace.
  /// </summary>
  /// <param name="dtoAssemblyPath">Absolute path to the compiled assembly (.dll file).</param>
  /// <param name="namespaceFilter">Optional namespace to filter types. If null or empty, returns all class types.</param>
  /// <returns>Array of Type objects representing classes found in the assembly.</returns>
  /// <exception cref="InvalidAnalysisInputException">Thrown when dtoAssemblyPath is null or empty.</exception>
  /// <exception cref="AssemblyLoadException">Thrown when the assembly file doesn't exist or contains no matching types.</exception>
  /// <example>
  /// <code>
  /// var types = GetDtoAssemblyTypes("/path/to/Dto.dll", "Dto");
  /// // Returns types from the "Dto" namespace
  /// var allTypes = GetDtoAssemblyTypes("/path/to/Dto.dll");
  /// // Returns all class types from any namespace
  /// </code>
  /// </example>
  /// <remarks>
  /// Ensure the assembly project is built before calling this method.
  /// </remarks>
  public Type[] GetDtoAssemblyTypes(string dtoAssemblyPath, string? namespaceFilter = DtoNamespace)
  {
    ValidateStringParameter(dtoAssemblyPath, nameof(dtoAssemblyPath));

    var fileExists = File.Exists(dtoAssemblyPath);
    if (!fileExists)
    {
      this.logger.LogError(
        "Assembly file not found. AssemblyPath: {AssemblyPath}, ErrorType: {ErrorType}",
        dtoAssemblyPath,
        "FileNotFound");
      throw AssemblyLoadException.FileNotFound(dtoAssemblyPath);
    }

    try
    {
      this.logger.LogDebug("Loading DTO assembly from {AssemblyPath}", dtoAssemblyPath);

      var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(dtoAssemblyPath);
      var types = assembly.GetTypes()
          .Where(t => t.IsClass && (string.IsNullOrEmpty(namespaceFilter) || t.Namespace == namespaceFilter))
          .ToArray();

      if (types.Length == 0)
      {
        var filterDescription = string.IsNullOrEmpty(namespaceFilter) ? "any namespace" : $"namespace '{namespaceFilter}'";
        this.logger.LogError(
          "No class types found in assembly. AssemblyPath: {AssemblyPath}, NamespaceFilter: {NamespaceFilter}, TotalTypes: {TotalTypes}",
          dtoAssemblyPath,
          filterDescription,
          assembly.GetTypes().Length);
        throw AssemblyLoadException.NoTypesFound(dtoAssemblyPath, filterDescription);
      }

      this.logger.LogDebug(
        "Successfully loaded {TypeCount} DTO types from {AssemblyPath}: {TypeNames}",
        types.Length,
        dtoAssemblyPath,
        string.Join(", ", types.Select(t => t.Name)));

      return types;
    }
    catch (Exception ex) when (ex is BadImageFormatException || ex is FileLoadException)
    {
      this.logger.LogError(
        ex,
        "Failed to load assembly. AssemblyPath: {AssemblyPath}, ErrorType: {ErrorType}, FileExists: {FileExists}",
        dtoAssemblyPath,
        ex.GetType().Name,
        fileExists);

      throw new AssemblyLoadException(
        dtoAssemblyPath,
        "Assembly file is corrupted or not a valid .NET assembly.",
        ex);
    }
  }

  /// <summary>
  /// Analyzes property usage for a specific DTO class across all projects in a solution.
  /// Projects are filtered based on the ExcludePatterns configured in AnalysisOptions.
  /// </summary>
  /// <param name="solutionPath">Absolute path to the .sln file to analyze.</param>
  /// <param name="selectedClass">The DTO class type to analyze for property usage patterns.</param>
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
  /// var options = new AnalysisOptions { ExcludePatterns = ["*Tests", "MockProjects"] };
  /// var service = new AnalysisService(logger, options);
  /// var results = await service.AnalyzeUsageAsync("/path/to/solution.sln", typeof(UserEventDto));
  ///
  /// foreach (var usage in results)
  /// {
  ///     Console.WriteLine($"{usage.Property.Attribute.ClassName}.{usage.Property.Attribute.FieldName}: {usage.UsageCount} usages");
  /// }
  /// </code>
  /// </example>
  public async Task<IReadOnlyList<PropertyUsage>> AnalyzeUsageAsync(
      string solutionPath,
      Type selectedClass)
  {
    ValidateStringParameter(solutionPath, nameof(solutionPath));
    ValidateObjectParameter(selectedClass, nameof(selectedClass));

    var fileExists = File.Exists(solutionPath);
    if (!fileExists)
    {
      this.logger.LogError(
        "Solution file not found. SolutionPath: {SolutionPath}, ErrorType: {ErrorType}",
        solutionPath,
        "FileNotFound");
      throw SolutionLoadException.FileNotFound(solutionPath);
    }

    this.logger.LogDebug("Starting analysis for class: {SelectedClassFullName}", selectedClass.FullName);
    var propertyUsage = new Dictionary<UsageKey, int>();

    // Find member references (properties and fields)
    var deepMembers = this.GetDeepMembers(selectedClass);
    this.logger.LogDebug(
        "Found {Count} deep members in {CurrentTypeFullName}",
        deepMembers.Count,
        selectedClass.FullName);

    var solution = this.LoadSolutionWorkspace(solutionPath);

    foreach (var project in solution.Projects)
    {
      // Skip projects matching exclude patterns
      if (ShouldSkipProject(project.Name, this.options.ExcludePatterns))
      {
        this.logger.LogInformation("Skipping project {ProjectName} (matches exclude pattern).", project.Name);
        continue;
      }

      var compilation = await SetupProjectCompilation(project, selectedClass.Assembly.Location);
      if (compilation == null)
      {
        continue;
      }

      await this.AnalyzeProjectDocuments(project, compilation, deepMembers, propertyUsage, selectedClass);
    }

    // add unused members from deepMembers:
    foreach (var deepMember in deepMembers)
    {
      var attribute = new ClassAndField(deepMember.DeclaringType.Name, deepMember.Name);

      if (propertyUsage.Any(k => k.Key.Attribute == attribute))
      {
        continue; // already exists
      }

      UsageKey key = new(UnusedPropertyFilePath, attribute);
      propertyUsage.TryAdd(key, 0);
    }

    var results = propertyUsage.Select(kvp => new PropertyUsage(kvp.Key, kvp.Value)).ToList();
    var totalUsages = results.Sum(r => r.UsageCount);
    var unusedProperties = results.Count(r => r.UsageCount == 0);

    this.logger.LogInformation(
      "Analysis completed for class {ClassName} in solution {SolutionPath}. " +
      "MembersAnalyzed: {MemberCount}, TotalUsages: {TotalUsages}, UnusedMembers: {UnusedMembers}, ExcludePatterns: {ExcludePatterns}",
      selectedClass.Name,
      solutionPath,
      results.Count,
      totalUsages,
      unusedProperties,
      string.Join(", ", this.options.ExcludePatterns));

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

  private static bool ShouldSkipProject(string projectName, string[] excludePatterns)
  {
    return excludePatterns.Any(pattern => ProjectMatchesPattern(projectName, pattern));
  }

  private static bool ProjectMatchesPattern(string projectName, string pattern)
  {
    return pattern switch
    {
      _ when pattern.StartsWith('*') && pattern.EndsWith('*') => MatchesContainsPattern(projectName, pattern),
      _ when pattern.StartsWith('*') => MatchesSuffixPattern(projectName, pattern),
      _ when pattern.EndsWith('*') => MatchesPrefixPattern(projectName, pattern),
      _ => MatchesExactPattern(projectName, pattern),
    };
  }

  private static bool MatchesContainsPattern(string projectName, string pattern)
  {
    var substring = pattern[1..^1];
    return projectName.Contains(substring, StringComparison.OrdinalIgnoreCase);
  }

  private static bool MatchesSuffixPattern(string projectName, string pattern)
  {
    var suffix = pattern[1..];
    return projectName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
  }

  private static bool MatchesPrefixPattern(string projectName, string pattern)
  {
    var prefix = pattern[..^1];
    return projectName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
  }

  private static bool MatchesExactPattern(string projectName, string pattern)
  {
    return projectName.Equals(pattern, StringComparison.OrdinalIgnoreCase);
  }

  private static bool IsGenericList(Type type)
  {
    return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));
  }

  private static bool IsGenericEnumerable(Type type)
  {
    return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
  }

  private static List<string> GetProjectPathsFromSolution(string solutionPath)
  {
    var projectPaths = new List<string>();
    var solutionDir = Path.GetDirectoryName(solutionPath)!;

    var projectLines = File.ReadAllLines(solutionPath)
      .Where(line => line.Trim().StartsWith("Project(", StringComparison.OrdinalIgnoreCase) && line.Contains(".csproj", StringComparison.OrdinalIgnoreCase));
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

  private static void ValidateStringParameter(string? value, string parameterName)
  {
    if (string.IsNullOrEmpty(value))
    {
      throw InvalidAnalysisInputException.NullOrEmpty(parameterName);
    }
  }

  private static void ValidateObjectParameter(object? value, string parameterName)
  {
    if (value is null)
    {
      throw InvalidAnalysisInputException.Null(parameterName);
    }
  }

  private Solution LoadSolutionWorkspace(string solutionPath)
  {
    this.logger.LogDebug("Loading solution workspace from {SolutionPath}", solutionPath);

    try
    {
      using var workspace = new AdhocWorkspace(MefHostServices.Create(MefHostServices.DefaultAssemblies));
      workspace.WorkspaceFailed += (_, args) =>
      {
        this.logger.LogWarning("Workspace diagnostic: {Diagnostic} for solution {SolutionPath}", args.Diagnostic, solutionPath);
      };

      var projectPaths = GetProjectPathsFromSolution(solutionPath);
      this.logger.LogDebug("Found {ProjectPathCount} projects in solution {SolutionPath}", projectPaths.Count, solutionPath);

      if (projectPaths.Count == 0)
      {
        this.logger.LogError("No projects found in solution {SolutionPath}", solutionPath);
        throw SolutionLoadException.NoProjects(solutionPath);
      }

      foreach (var projectPath in projectPaths)
      {
        this.logger.LogDebug("Loading project {ProjectPath} into workspace", projectPath);
        this.LoadProjectIntoWorkspace(workspace, projectPath.Replace("\\", "/"));
      }

      var solution = workspace.CurrentSolution;
      var loadedProjectCount = solution.Projects.Count();
      this.logger.LogInformation("Successfully loaded solution {SolutionPath} with {ProjectCount} projects", solutionPath, loadedProjectCount);

      if (!solution.Projects.Any())
      {
        this.logger.LogError("No projects loaded into workspace for solution {SolutionPath}", solutionPath);
        throw SolutionLoadException.NoProjects(solutionPath);
      }

      return solution;
    }
    catch (Exception ex) when (!(ex is SolutionLoadException))
    {
      this.logger.LogError(
        ex,
        "Unexpected error loading solution workspace. SolutionPath: {SolutionPath}, ErrorType: {ErrorType}",
        solutionPath,
        "UnexpectedError");
      throw new SolutionLoadException(
        solutionPath,
        "Unexpected error during solution loading. Check that all projects build successfully.",
        ex);
    }
  }

  private async Task AnalyzeProjectDocuments(
      Project project,
      Compilation compilation,
      List<AnalyzedMember> deepMembers,
      Dictionary<UsageKey, int> propertyUsage,
      Type selectedClass)
  {
    foreach (var document in project.Documents)
    {
      var filePath = document.FilePath!;
      if (filePath.Contains(ObjDirectoryPath, StringComparison.OrdinalIgnoreCase)
          || filePath.Contains(BinDirectoryPath, StringComparison.OrdinalIgnoreCase))
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

      this.logger.LogDebug("Analyzing file: {FilePath}", filePath);

      var memberAccessExpressionSyntaxes = root
          .DescendantNodes()
          .OfType<MemberAccessExpressionSyntax>();

      var deepMemberNames = deepMembers
          .Select(m => m.Name)
          .ToHashSet(StringComparer.OrdinalIgnoreCase);

      this.AnalyzeMemberUsage(
          memberAccessExpressionSyntaxes,
          deepMemberNames,
          deepMembers,
          semanticModel,
          filePath,
          selectedClass,
          propertyUsage);
    }
  }

  private void AnalyzeMemberUsage(
      IEnumerable<MemberAccessExpressionSyntax> memberAccessExpressions,
      HashSet<string> deepMemberNames,
      List<AnalyzedMember> deepMembers,
      SemanticModel semanticModel,
      string filePath,
      Type selectedClass,
      Dictionary<UsageKey, int> propertyUsage)
  {
    foreach (var usageCandidate in memberAccessExpressions)
    {
      if (!deepMemberNames.Contains(usageCandidate.Name.Identifier.Text))
      {
        continue;
      }

      var attribute = GetClassAndFieldName(semanticModel, usageCandidate);
      var deepMember = deepMembers
          .SingleOrDefault(m =>
              attribute.ClassName == m.DeclaringType.Name
              && attribute.FieldName == m.Name);

      if (deepMember is null)
      {
        this.logger.LogDebug(
            "Member usage for {MemberName}" +
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

  private void LoadProjectIntoWorkspace(AdhocWorkspace workspace, string projectPath)
  {
    if (!File.Exists(projectPath))
    {
      this.logger.LogWarning(
        "Project file not found during workspace loading. ProjectPath: {ProjectPath}, FileExists: {FileExists}",
        projectPath,
        false);
      return;
    }

    var projectName = Path.GetFileNameWithoutExtension(projectPath);
    this.logger.LogDebug("Loading project into workspace. ProjectName: {ProjectName}, ProjectPath: {ProjectPath}", projectName, projectPath);

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
      this.logger.LogDebug("Found {DocumentCount} C# files in project {ProjectName}", documents.Length, projectName);

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

      this.logger.LogDebug("Successfully loaded project {ProjectName} with {DocumentCount} documents", projectName, documents.Length);
    }
    catch (Exception ex)
    {
      this.logger.LogError(
        ex,
        "Failed to load project into workspace. ProjectName: {ProjectName}, ProjectPath: {ProjectPath}, ErrorType: {ErrorType}",
        projectName,
        projectPath,
        "ProjectLoadError");

      // Wrap with contextual information for better error handling upstream
      throw new InvalidOperationException($"Failed to load project '{projectName}' from path '{projectPath}' into workspace. See inner exception for details.", ex);
    }
  }

  private void ProcessMember(
      MemberInfo member,
      Type memberType,
      string memberName,
      Type declaringType,
      string prefix,
      List<AnalyzedMember> members,
      HashSet<Type> visitedTypes)
  {
    var fullPath = string.IsNullOrEmpty(prefix) ? memberName : $"{prefix}.{memberName}";

    if (IsPrimitiveOrArrayOfPrimitives(memberType))
    {
      members.Add(new AnalyzedMember(member, declaringType, fullPath, memberType, memberName));
      return;
    }

    if (IsNullable(memberType))
    {
      var underlyingType = Nullable.GetUnderlyingType(memberType)!;
      if (IsPrimitiveOrArrayOfPrimitives(underlyingType))
      {
        members.Add(new AnalyzedMember(member, declaringType, fullPath, memberType, memberName));
      }
      else
      {
        members.AddRange(this.GetDeepMembers(underlyingType, fullPath + ".Value", visitedTypes));
      }

      return;
    }

    if (IsGenericList(memberType))
    {
      var itemType = memberType.GetGenericArguments()[0];
      members.AddRange(this.GetDeepMembers(itemType, fullPath + ".Item", visitedTypes));
      return;
    }

    members.AddRange(this.GetDeepMembers(memberType, fullPath, visitedTypes));
  }
}
