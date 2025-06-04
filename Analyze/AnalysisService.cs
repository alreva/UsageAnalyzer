using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Spectre.Console;

namespace Analyze;

public class AnalysisService
{
    private readonly ILogger<AnalysisService> _logger;
    private readonly string _solutionDir;
    private readonly string _solutionPath;
    private HashSet<Type> _analyzedTypes = new();

    public AnalysisService(ILogger<AnalysisService> logger)
    {
        _logger = logger;
        _solutionPath = FindSolutionFile();
        _solutionDir = Path.GetDirectoryName(_solutionPath) ?? throw new InvalidOperationException("Solution directory is null");
        _logger.LogInformation("Resolved solution directory: {SolutionDir}", _solutionDir);
    }

    private string FindSolutionFile()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var solutionFiles = Directory.GetFiles(currentDir, "*.sln");

        if (solutionFiles.Length == 1)
        {
            return solutionFiles[0];
        }
        else if (solutionFiles.Length > 1)
        {
            var selectedSolution = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Multiple solution files found. Please select one:")
                    .AddChoices(solutionFiles.Select(f => Path.GetFileName(f) ?? throw new InvalidOperationException("Solution file name is null"))));
            return solutionFiles.First(f => Path.GetFileName(f) == selectedSolution);
        }

        // If no solution file found in current directory, look in parent directory
        var parentDir = Directory.GetParent(currentDir);
        if (parentDir != null)
        {
            var parentSolutionFiles = Directory.GetFiles(parentDir.FullName, "*.sln");
            if (parentSolutionFiles.Length == 1)
            {
                return parentSolutionFiles[0];
            }
            else if (parentSolutionFiles.Length > 1)
            {
                var selectedSolution = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Multiple solution files found in parent directory. Please select one:")
                        .AddChoices(parentSolutionFiles.Select(f => Path.GetFileName(f) ?? throw new InvalidOperationException("Solution file name is null"))));
                return parentSolutionFiles.First(f => Path.GetFileName(f) == selectedSolution);
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

    public async Task<(Dictionary<string, int> classUsage, Dictionary<string, int> propertyUsage)> AnalyzeUsageAsync(
        Type selectedClass,
        Action<string> progressCallback)
    {
        _logger.LogDebug("Starting analysis for class: {SelectedClassFullName}", selectedClass.FullName);
        var classUsage = new Dictionary<string, int>();
        var propertyUsage = new Dictionary<string, int>();
        _analyzedTypes.Clear();

        using var workspace = MSBuildWorkspace.Create();
        var solution = await workspace.OpenSolutionAsync(_solutionPath);
        
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
                // Skip Dto and Analyze projects
                if (project.Name == "Dto" || project.Name == "Analyze")
                {
                    _logger.LogInformation("Skipping {ProjectName} project.", project.Name);
                    continue;
                }

                var compilation = await project.GetCompilationAsync();
                if (compilation == null) continue;

                foreach (var document in project.Documents)
                {
                    var syntaxTree = await document.GetSyntaxTreeAsync();
                    if (syntaxTree == null) continue;

                    var semanticModel = compilation.GetSemanticModel(syntaxTree);
                    var root = await syntaxTree.GetRootAsync();

                    // Find class references
                    var classReferences = root.DescendantNodes()
                        .OfType<IdentifierNameSyntax>()
                        .Where(id => id.Identifier.Text == currentType.Name)
                        .Select(id => semanticModel.GetSymbolInfo(id).Symbol)
                        .Where(symbol => symbol != null && symbol.ContainingType?.ToDisplayString() == currentType.FullName);

                    if (classReferences.Any())
                    {
                        var key = $"{project.Name}/{Path.GetFileNameWithoutExtension(document.FilePath)}";
                        if (!classUsage.ContainsKey(key))
                        {
                            classUsage[key] = 0;
                        }
                        classUsage[key] += classReferences.Count();
                    }

                    // Find property references
                    foreach (var (prop, fullPath) in GetDeepProperties(currentType))
                    {
                        var propertyReferences = root.DescendantNodes()
                            .OfType<IdentifierNameSyntax>()
                            .Where(id => id.Identifier.Text == prop.Name)
                            .Select(id => semanticModel.GetSymbolInfo(id).Symbol)
                            .Where(symbol => symbol != null && symbol.Name == prop.Name);

                        if (propertyReferences.Any())
                        {
                            var key = $"{project.Name}/{Path.GetFileNameWithoutExtension(document.FilePath)}|{fullPath}";
                            if (!propertyUsage.ContainsKey(key))
                            {
                                propertyUsage[key] = 0;
                            }
                            propertyUsage[key] += propertyReferences.Count();
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

    private bool IsPrimitiveOrArrayOfPrimitives(Type type)
    {
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime))
            return true;

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

    private IEnumerable<(PropertyInfo Property, string FullPath)> GetDeepProperties(Type type, string prefix = "")
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
            
            if (propType == null) continue;

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
} 