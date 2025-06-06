using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Analyze;

public class AnalysisService
{
    private readonly HashSet<Type> _analyzedTypes = new();
    private readonly ILogger<AnalysisService> _logger;
    private readonly string _solutionDir;
    private readonly string _solutionPath;

    public AnalysisService(ILogger<AnalysisService> logger)
    {
        _logger = logger;
        _solutionPath = FindSolutionFile();
        _solutionDir = Path.GetDirectoryName(_solutionPath) ??
                       throw new InvalidOperationException("Solution directory is null");
        _logger.LogInformation("Resolved solution directory: {SolutionDir}", _solutionDir);
    }

    private static string FindSolutionFile()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var solutionFiles = Directory.GetFiles(currentDir, "*.sln");

        if (solutionFiles.Length == 1)
        {
            return solutionFiles[0];
        }

        if (solutionFiles.Length > 1)
        {
            var selectedSolution = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Multiple solution files found. Please select one:")
                    .AddChoices(solutionFiles.Select(f =>
                        Path.GetFileName(f) ?? throw new InvalidOperationException("Solution file name is null"))));
            return solutionFiles.First(f => Path.GetFileName(f) == selectedSolution);
        }

        // If no solution file found in current directory, look in parent directory
        var parentDir = new DirectoryInfo(currentDir);
        for (var numTry = 0; numTry < 5; numTry++)
        {
            parentDir = parentDir.Parent;
            if (parentDir == null)
            {
                break;
            }
            
            var parentSolutionFiles = Directory.GetFiles(parentDir.FullName, "*.sln");
            switch (parentSolutionFiles.Length)
            {
                case 1:
                    return parentSolutionFiles[0];
                case > 1:
                {
                    var selectedSolution = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Multiple solution files found in parent directory. Please select one:")
                            .AddChoices(parentSolutionFiles.Select(f =>
                                Path.GetFileName(f) ??
                                throw new InvalidOperationException("Solution file name is null"))));
                    return parentSolutionFiles.First(f => Path.GetFileName(f) == selectedSolution);
                }
            }
        }

        throw new FileNotFoundException("No solution file found in current or parent directory.");
    }

    public IEnumerable<Type> GetDtoClasses()
    {
        // Path to the Dto.dll (adjust if needed)
        var dtoDllPath = Path.Combine(_solutionDir, "Dto", "bin", "Debug", "net10.0", "Dto.dll");
        if (!File.Exists(dtoDllPath))
        {
            throw new FileNotFoundException($"Dto.dll not found at {dtoDllPath}. Please build the Dto project first.");
        }

        var assembly = Assembly.LoadFrom(dtoDllPath);
        return assembly.GetTypes()
            .Where(t => t.IsClass && t.Namespace == "Dto")
            .ToList();
    }

    public async Task<(Dictionary<string, int>, Dictionary<UsageKey, int>)> AnalyzeUsageAsync(
        Type selectedClass,
        Action<string> progressCallback)
    {
        _logger.LogDebug("Starting analysis for class: {SelectedClassFullName}", selectedClass.FullName);
        var classUsage = new Dictionary<string, int>();
        var propertyUsage = new Dictionary<UsageKey, int>();
        _analyzedTypes.Clear();

        using var workspace = new AdhocWorkspace(MefHostServices.Create(MefHostServices.DefaultAssemblies));
        workspace.WorkspaceFailed += (sender, args) => { Console.WriteLine($"Warning: {args.Diagnostic}"); };
        var projectPaths = GetProjectPathsFromSolution(_solutionPath);
        foreach (var projectPath in projectPaths)
        {
            LoadProjectIntoWorkspace(workspace, projectPath.Replace("\\", "/"));
        }

        var solution = workspace.CurrentSolution;

        _logger.LogDebug("Loaded solution with {ProjectCount} projects.", solution.Projects.Count());

        // Queue of types to analyze
        var typesToAnalyze = new Queue<Type>();
        typesToAnalyze.Enqueue(selectedClass);
        _logger.LogDebug("Initial queue size: {Count}", typesToAnalyze.Count);

        while (typesToAnalyze.Count > 0)
        {
            var currentType = typesToAnalyze.Dequeue();

            // Skip if already analyzed
            if (_analyzedTypes.Contains(currentType))
            {
                _logger.LogDebug("Skipping already analyzed type: {CurrentTypeFullName}", currentType.FullName);
                continue;
            }

            _analyzedTypes.Add(currentType);
            _logger.LogInformation("Analyzing type: {CurrentTypeFullName}", currentType.FullName);

            foreach (var project in solution.Projects)
            {
                // Skip Analyze project
                if (project.Name == "Analyze" ||
                    project.Name == "Dto")
                {
                    _logger.LogInformation("Skipping {ProjectName} project.", project.Name);
                    continue;
                }

                var dtoAssemblyPath = Path.Combine(_solutionDir, "Dto", "bin", "Debug", "net10.0", "Dto.dll");

                var compilation = (await project
                        .GetCompilationAsync())?
                    .AddReferences(MetadataReference.CreateFromFile(dtoAssemblyPath));
                if (compilation == null)
                {
                    continue;
                }

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

                    _logger.LogDebug("Analyzing file: {FilePath}", filePath);

                    // Find property references
                    var deepProperties = GetDeepProperties(currentType);
                    _logger.LogDebug(
                        "Found {Count} deep properties in {CurrentTypeFullName}",
                        deepProperties.Count,
                        currentType.FullName);

                    var usages = root
                        .DescendantNodes()
                        .OfType<MemberAccessExpressionSyntax>()
                        .Where(m =>
                            deepProperties
                                .Select(p => p.Property)
                                .Select(p => p.Name)
                                .Contains(m.Name.Identifier.Text))
                        .ToArray();

                    _logger.LogDebug("Found {Count} usages of properties in {File}", usages.Length, filePath);

                    foreach (var usage in usages)
                    {
                        var attribute = GetClassAndFieldName(semanticModel, usage);
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
            }

            // Add nested types to the queue for analysis
            var nestedTypes = GetNestedTypes(currentType).ToList();
            foreach (var nestedType in nestedTypes)
            {
                if (nestedType.Assembly == selectedClass.Assembly)
                {
                    typesToAnalyze.Enqueue(nestedType);
                }
            }
        }

        return (classUsage, propertyUsage);
    }

    private static bool IsPrimitiveOrArrayOfPrimitives(Type type)
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

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            var elementType = type.GetGenericArguments()[0];
            return elementType.IsPrimitive || elementType == typeof(string);
        }

        return false;
    }

    private static List<(PropertyInfo Property, string FullPath)> GetDeepProperties(Type type, string prefix = "")
    {
        var properties = new List<(PropertyInfo Property, string FullPath)>();

        foreach (var prop in type.GetProperties())
        {
            var fullPath = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
            properties.Add((prop, fullPath));

            if (!IsPrimitiveOrArrayOfPrimitives(prop.PropertyType))
            {
                properties.AddRange(GetDeepProperties(prop.PropertyType, fullPath));
            }
        }

        return properties;
    }

    private HashSet<Type> GetNestedTypes(Type type)
    {
        var types = new HashSet<Type>();

        foreach (var prop in type.GetProperties())
        {
            var propType = prop.PropertyType;

            if (propType.IsArray)
            {
                propType = propType.GetElementType();
            }
            else if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(List<>))
            {
                propType = propType.GetGenericArguments()[0];
            }

            if (propType == null)
            {
                continue;
            }

            if (propType.IsClass &&
                propType != typeof(string) &&
                !propType.IsPrimitive &&
                !propType.IsEnum &&
                (propType.Namespace == null || !propType.Namespace.StartsWith("System")) &&
                !_analyzedTypes.Contains(propType))
            {
                types.Add(propType);
            }
        }

        return types;
    }

    private static void LoadProjectIntoWorkspace(AdhocWorkspace workspace, string projectPath)
    {
        if (!File.Exists(projectPath))
        {
            Console.WriteLine($"Warning: Project file not found: {projectPath}");
            return;
        }

        var projectName = Path.GetFileNameWithoutExtension(projectPath);
        Console.WriteLine($"Loading project: {projectName}");

        var projectInfo = ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Create(),
            projectName,
            projectName,
            LanguageNames.CSharp
        );

        var project = workspace.AddProject(projectInfo);

        var documents = Directory.GetFiles(Path.GetDirectoryName(projectPath)!, "*.cs", SearchOption.AllDirectories);
        foreach (var docPath in documents)
        {
            var sourceText = SourceText.From(File.ReadAllText(docPath));
            var documentInfo = DocumentInfo.Create(
                DocumentId.CreateNewId(project.Id),
                Path.GetFileName(docPath),
                loader: TextLoader.From(TextAndVersion.Create(sourceText, VersionStamp.Create())),
                filePath: docPath
            );

            workspace.AddDocument(documentInfo);
        }
    }

    private static List<string> GetProjectPathsFromSolution(string solutionPath)
    {
        var projectPaths = new List<string>();
        var solutionDir = Path.GetDirectoryName(solutionPath)!;

        foreach (var line in File.ReadAllLines(solutionPath))
        {
            if (line.Trim().StartsWith("Project(") && line.Contains(".csproj"))
            {
                var parts = line.Split(',');
                if (parts.Length > 1)
                {
                    var relativePath = parts[1].Trim().Trim('"');
                    var fullPath = Path.Combine(solutionDir, relativePath);
                    projectPaths.Add(fullPath);
                }
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
        return new ClassAndField(type is null ? "" : type.Name, fieldName);
    }
}