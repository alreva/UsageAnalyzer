using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
// using Dto; // Removed because Dto is now loaded dynamically
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

    public IEnumerable<string> GetSourceFiles()
    {
        var sourceFiles = new List<string>();
        _logger.LogDebug("Opening solution file: {SolutionPath}", _solutionPath);
        
        // Parse the solution file to find project paths
        var solutionFile = File.ReadAllText(_solutionPath);
        var solutionDir = Path.GetDirectoryName(_solutionPath) ?? throw new InvalidOperationException("Solution directory is null");
        var projectMatches = Regex.Matches(solutionFile, @"Project\(""[^""]+""\) = ""[^""]+"", ""([^""]+)""");
        
        foreach (Match match in projectMatches)
        {
            // Normalize the path to use the correct directory separator
            var projectPath = Path.Combine(solutionDir, match.Groups[1].Value.Replace('\\', Path.DirectorySeparatorChar));
            _logger.LogDebug("Found project path: {ProjectPath}", projectPath);
            
            if (File.Exists(projectPath))
            {
                var projectName = Path.GetFileNameWithoutExtension(projectPath);
                _logger.LogInformation("Processing project: {ProjectName}", projectName);
                
                // Skip the Dto project
                if (projectName == "Dto")
                {
                    _logger.LogInformation("Skipping Dto project.");
                    continue;
                }
                
                // Find all .cs files in the project directory
                var projectDir = Path.GetDirectoryName(projectPath) ?? throw new InvalidOperationException("Project directory is null");
                _logger.LogDebug("Searching for .cs files in: {ProjectDir}", projectDir);
                
                var csFiles = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) && 
                               !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) &&
                               !projectName.Contains("Tests"));
                
                foreach (var file in csFiles)
                {
                    _logger.LogDebug("Found file: {File}", file);
                    sourceFiles.Add(file);
                }
            }
            else
            {
                _logger.LogWarning("Project file not found: {ProjectPath}", projectPath);
            }
        }
        
        _logger.LogDebug("Total files found: {SourceFilesCount}", sourceFiles.Count);
        return sourceFiles;
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
        _logger.LogDebug("Getting deep properties for type: {TypeFullName}, prefix: {Prefix}", type.FullName, prefix);
        var properties = new List<(PropertyInfo Property, string FullPath)>();
        
        foreach (var prop in type.GetProperties())
        {
            var fullPath = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
            properties.Add((prop, fullPath));
            _logger.LogDebug("Added property: {FullPath}", fullPath);
            
            // If the property type is not a primitive or array of primitives, continue recursion
            if (!IsPrimitiveOrArrayOfPrimitives(prop.PropertyType))
            {
                _logger.LogDebug("Property {FullPath} is complex type, recursing into {PropertyTypeFullName}", fullPath, prop.PropertyType.FullName);
                properties.AddRange(GetDeepProperties(prop.PropertyType, fullPath));
            }
            else
            {
                _logger.LogDebug("Property {FullPath} is primitive type: {PropertyTypeFullName}", fullPath, prop.PropertyType.FullName);
            }
        }
        
        return properties;
    }

    private HashSet<Type> GetNestedTypes(Type type)
    {
        _logger.LogDebug("Getting nested types for: {TypeFullName}", type.FullName);
        var types = new HashSet<Type>();
        
        foreach (var prop in type.GetProperties())
        {
            var propType = prop.PropertyType;
            var propTypeFullName = propType.FullName!;
            _logger.LogDebug("Checking property type: {PropTypeFullName}", propTypeFullName);
            
            // Handle arrays and collections
            if (propType.IsArray)
            {
                propType = propType.GetElementType();
                _logger.LogDebug("Array type, element type: {PropTypeFullName}", propType?.FullName);
            }
            else if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(List<>))
            {
                propType = propType.GetGenericArguments()[0];
                _logger.LogDebug("List type, element type: {PropTypeFullName}", propTypeFullName);
            }
            
            // skip if propType is null
            if (propType == null)
            {
                _logger.LogDebug("Property type is null, skipping.");
                continue;
            }

            // Only skip system types, primitives, enums, and already analyzed types
            if (propType.IsClass && 
                propType != typeof(string) && 
                !propType.IsPrimitive && 
                !propType.IsEnum &&
                (propType.Namespace == null || !propType.Namespace.StartsWith("System")) &&
                !_analyzedTypes.Contains(propType))
            {
                _logger.LogDebug("Adding nested type: {PropTypeFullName}", propTypeFullName);
                types.Add(propType);
            }
            else
            {
                _logger.LogDebug(
                    "Skipping type {PropTypeFullName}:" +
                    " IsClass={PropTypeIsClass}," +
                    " IsPrimitive={PropTypeIsPrimitive}," +
                    " IsEnum={PropTypeIsEnum}," +
                    " IsSystem={StartsWith}," +
                    " AlreadyAnalyzed={Contains}",
                    propTypeFullName,
                    propType.IsClass,
                    propType.IsPrimitive,
                    propType.IsEnum,
                    propType.Namespace?.StartsWith("System"),
                    _analyzedTypes.Contains(propType));
            }
        }
        
        return types;
    }

    public (Dictionary<string, int> classUsage, Dictionary<string, int> propertyUsage) AnalyzeUsage(
        Type selectedClass,
        IEnumerable<string> sourceFiles,
        Action<string> progressCallback)
    {
        _logger.LogDebug("Starting analysis for class: {SelectedClassFullName}", selectedClass.FullName);
        var classUsage = new Dictionary<string, int>();
        var propertyUsage = new Dictionary<string, int>();
        _analyzedTypes.Clear();

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
            _logger.LogDebug("Queue size after dequeue: {Count}", typesToAnalyze.Count);

            foreach (var file in sourceFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                _logger.LogDebug("Analyzing file: {FileName}", fileName);
                
                var content = File.ReadAllText(file);
                var projectName = Path.GetFileName(Path.GetDirectoryName(file));
                _logger.LogDebug("Project name: {ProjectName}", projectName);

                // Count class usage (including type references)
                var className = currentType.Name;
                var classMatches = Regex.Matches(content, $@"\b{className}\b");
                if (classMatches.Count > 0)
                {
                    var key = $"{projectName}/{fileName}";
                    if (!classUsage.ContainsKey(key))
                    {
                        classUsage[key] = 0;
                    }
                    classUsage[key] += classMatches.Count;
                    _logger.LogDebug("Found {ClassMatchesCount} usages of class {ClassName} in {Key}", classMatches.Count, className, key);
                }

                // Count property usage with full property paths
                foreach (var (prop, fullPath) in GetDeepProperties(currentType))
                {
                    _logger.LogDebug("Checking property: {FullPath}", fullPath);
                    var propMatches = Regex.Matches(content, $@"\b{prop.Name}\b");
                    if (propMatches.Count > 0)
                    {
                        var key = $"{projectName}/{fileName}|{fullPath}";
                        if (!propertyUsage.ContainsKey(key))
                        {
                            propertyUsage[key] = 0;
                        }
                        propertyUsage[key] += propMatches.Count;
                        _logger.LogDebug("Found {PropMatchesCount} usages of property {FullPath} in {Key}", propMatches.Count, fullPath, key);
                    }
                }
            }

            // Add nested types to the queue for analysis (only DTO classes)
            var nestedTypes = GetNestedTypes(currentType).ToList();
            _logger.LogDebug("Found {NestedTypesCount} nested types in {CurrentTypeFullName}", nestedTypes.Count, currentType.FullName);
            
            foreach (var nestedType in nestedTypes)
            {
                if (nestedType.Assembly == selectedClass.Assembly)
                {
                    _logger.LogDebug("Adding nested type to queue: {NestedTypeFullName}", nestedType.FullName);
                    typesToAnalyze.Enqueue(nestedType);
                }
                else
                {
                    _logger.LogDebug("Skipping nested type from different assembly: {NestedTypeFullName}", nestedType.FullName);
                }
            }
        }

        _logger.LogDebug("Analysis complete. Found {ClassUsageCount} class usages and {PropertyUsageCount} property usages", classUsage.Count, propertyUsage.Count);
        return (classUsage, propertyUsage);
    }
} 