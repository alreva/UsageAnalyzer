using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;

namespace Analyze;

public class AnalysisService(ILogger<AnalysisService> logger)
{
    public Type[] GetDtoAssemblyTypes(string solutionPath)
    {
        // Path to the Dto.dll (adjust if needed)
        var dtoAssemblyPath = GetDtoAssemblyPath(solutionPath);
        if (!File.Exists(dtoAssemblyPath))
        {
            throw new FileNotFoundException($"Dto.dll not found at {dtoAssemblyPath}. Please build the Dto project first.");
        }

        var assembly = Assembly.LoadFrom(dtoAssemblyPath);
        return assembly.GetTypes()
            .Where(t => t is { IsClass: true, Namespace: "Dto" })
            .ToArray();
    }

    private static string GetDtoAssemblyPath(string solutionPath)
    {
        var solutionDir = Path.GetDirectoryName(solutionPath)!;
        var targetFramework = GetTargetFramework(solutionDir);
        var dtoAssemblyPath = Path.Combine(solutionDir, "Dto", "bin", "Debug", targetFramework, "Dto.dll");
        return dtoAssemblyPath;
    }

    private static string GetTargetFramework(string solutionDir)
    {
        var propsPath = Path.Combine(solutionDir, "Directory.Build.props");
        if (!File.Exists(propsPath))
        {
            return "net8.0";
        }

        var doc = XDocument.Load(propsPath);
        var tfElement = doc.Descendants("TargetFramework").FirstOrDefault();
        return tfElement?.Value ?? "net8.0";
    }

    public async Task<Dictionary<UsageKey, int>> AnalyzeUsageAsync(string solutionPath, Type selectedClass, bool shouldSkipTestProjects)
    {
        logger.LogDebug("Starting analysis for class: {SelectedClassFullName}", selectedClass.FullName);
        var propertyUsage = new Dictionary<UsageKey, int>();

        // Find property references
        var deepProperties = GetDeepProperties(selectedClass);
        logger.LogDebug(
            "Found {Count} deep properties in {CurrentTypeFullName}",
            deepProperties.Count,
            selectedClass.FullName);

        var solution = LoadSolutionWorkspace(solutionPath);
        
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

            var compilation = await SetupProjectCompilation(project, solutionPath);
            if (compilation == null)
            {
                continue;
            }

            await AnalyzeProjectDocuments(project, compilation, deepProperties, propertyUsage, selectedClass);
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
        
        return propertyUsage;
    }

    private async Task<Compilation?> SetupProjectCompilation(Project project, string solutionPath)
    {
        var dtoAssemblyPath = GetDtoAssemblyPath(solutionPath);
        var coreAssemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        return (await project.GetCompilationAsync())?
            .AddReferences([
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location), // System.Private.CoreLib
                MetadataReference.CreateFromFile(Path.Combine(coreAssemblyPath, "System.Runtime.dll")),
                MetadataReference.CreateFromFile(Path.Combine(coreAssemblyPath, "System.Collections.dll")),
                MetadataReference.CreateFromFile(Path.Combine(coreAssemblyPath, "System.Console.dll")),
                MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location), // System.Collections.Generic
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location), // System.Linq
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location), // System.Console
                MetadataReference.CreateFromFile(dtoAssemblyPath)
            ]);
    }

    private Solution LoadSolutionWorkspace(string solutionPath)
    {
        using var workspace = new AdhocWorkspace(MefHostServices.Create(MefHostServices.DefaultAssemblies));
        workspace.WorkspaceFailed += (_, args) =>
        {
            logger.LogWarning("Workspace failed: {Diagnostic}", args.Diagnostic);
        };
        var projectPaths = GetProjectPathsFromSolution(solutionPath);
        foreach (var projectPath in projectPaths)
        {
            LoadProjectIntoWorkspace(workspace, projectPath.Replace("\\", "/"));
        }

        var solution = workspace.CurrentSolution;
        logger.LogDebug("Loaded solution with {ProjectCount} projects.", solution.Projects.Count());
        return solution;
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

            AnalyzePropertyUsage(
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
                    &&  attribute.FieldName == p.Property.Name);

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

    private static List<(PropertyInfo Property, Type Type, string FullPath)> GetDeepProperties(Type type, string prefix = "")
    {
        var properties = new List<(PropertyInfo Property, Type Type, string FullPath)>();

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
                    properties.AddRange(GetDeepProperties(itemProp.PropertyType, fullPath + ".Value"));
                }
                continue;
            }

            if (IsGenericList(propType))
            {
                var itemProp = propType.GetProperty("Item")!;
                properties.AddRange(GetDeepProperties(itemProp.PropertyType, fullPath + ".Item"));
                continue;
            }

            properties.AddRange(GetDeepProperties(propType, fullPath));
        }

        return properties;
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

        if (IsGenericEnumerable(type))
        {
            var elementType = type.GetGenericArguments()[0];
            return elementType.IsPrimitive || elementType == typeof(string);
        }

        return false;
    }

    private static bool IsGenericList(Type type)
    {
        return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));
    }

    private static bool IsGenericEnumerable(Type type)
    {
        return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
    }
    
    private static bool IsNullable(Type type)
    {
        return Nullable.GetUnderlyingType(type) != null;
    }


    private void LoadProjectIntoWorkspace(AdhocWorkspace workspace, string projectPath)
    {
        if (!File.Exists(projectPath))
        {
            logger.LogWarning("Project file not found: {ProjectPath}", projectPath);
            return;
        }

        var projectName = Path.GetFileNameWithoutExtension(projectPath);
        logger.LogInformation("Loading project: {ProjectName} from {ProjectPath}", projectName, projectPath);

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