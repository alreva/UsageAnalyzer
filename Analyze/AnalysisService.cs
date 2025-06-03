using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Dto;
using Spectre.Console;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Host;

namespace Analyze;

public class AnalysisService
{
    private readonly ILogger<AnalysisService> _logger;
    private readonly string _solutionDir;
    private readonly string _solutionPath;

    public AnalysisService(ILogger<AnalysisService> logger)
    {
        _logger = logger;
        _solutionPath = FindSolutionFile();
        _solutionDir = Path.GetDirectoryName(_solutionPath) ?? throw new InvalidOperationException("Solution directory is null");
        _logger.LogInformation($"Resolved solution directory: {_solutionDir}");
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
        return Assembly.GetAssembly(typeof(UserEventDto))
            ?.GetTypes()
            .Where(t => t.Namespace == "Dto" && t.IsClass)
            .ToList() ?? Enumerable.Empty<Type>();
    }

    public IEnumerable<string> GetSourceFiles()
    {
        var sourceFiles = new List<string>();
        
        _logger.LogDebug($"Opening solution file: {_solutionPath}");
        
        // Create workspace with C# support
        var hostServices = MefHostServices.Create(MefHostServices.DefaultAssemblies);
        using var workspace = new AdhocWorkspace(hostServices);
        
        // Manually load the solution file
        var solutionFile = File.ReadAllText(_solutionPath);
        var solutionDir = Path.GetDirectoryName(_solutionPath) ?? throw new InvalidOperationException("Solution directory is null");
        
        // Parse the solution file to find project paths
        var projectMatches = System.Text.RegularExpressions.Regex.Matches(solutionFile, @"Project\(""[^""]+""\) = ""[^""]+"", ""([^""]+)""");
        
        foreach (Match match in projectMatches)
        {
            // Normalize the path to use the correct directory separator
            var projectPath = Path.Combine(solutionDir, match.Groups[1].Value.Replace('\\', Path.DirectorySeparatorChar));
            _logger.LogDebug($"Found project path: {projectPath}");
            
            if (File.Exists(projectPath))
            {
                var projectFile = File.ReadAllText(projectPath);
                var projectName = Path.GetFileNameWithoutExtension(projectPath);
                
                // Add the project to the workspace with C# language
                var projectInfo = ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    projectName,
                    projectName,
                    LanguageNames.CSharp,
                    filePath: projectPath);
                
                workspace.AddProject(projectInfo);
                _logger.LogInformation($"Processing project: {projectName}");
                
                // Find all .cs files in the project directory
                var projectDir = Path.GetDirectoryName(projectPath) ?? throw new InvalidOperationException("Project directory is null");
                _logger.LogDebug($"Searching for .cs files in: {projectDir}");
                
                var csFiles = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) && 
                               !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar));
                
                foreach (var file in csFiles)
                {
                    _logger.LogDebug($"Found file: {file}");
                    sourceFiles.Add(file);
                }
            }
            else
            {
                _logger.LogWarning($"Project file not found: {projectPath}");
            }
        }
        
        _logger.LogDebug($"Total files found: {sourceFiles.Count}");
        return sourceFiles;
    }

    public (Dictionary<string, int> classUsage, Dictionary<string, int> propertyUsage) AnalyzeUsage(
        Type selectedClass,
        IEnumerable<string> sourceFiles,
        Action<string> progressCallback)
    {
        var classUsage = new Dictionary<string, int>();
        var propertyUsage = new Dictionary<string, int>();

        foreach (var file in sourceFiles)
        {
            var fileName = Path.GetFileName(file);
            progressCallback($"Analyzing {fileName}");
            _logger.LogDebug($"Analyzing file: {file}");
            
            var content = File.ReadAllText(file);
            var projectName = Path.GetFileName(Path.GetDirectoryName(file));

            // Count class usage (including type references)
            var className = selectedClass.Name;
            var classMatches = Regex.Matches(content, $@"\b{className}\b");
            if (classMatches.Count > 0)
            {
                var key = $"{projectName}/{fileName}";
                classUsage[key] = classMatches.Count;
            }

            // Count property usage (including nested property access)
            foreach (var prop in selectedClass.GetProperties())
            {
                var propMatches = Regex.Matches(content, $@"\b{prop.Name}\b");
                if (propMatches.Count > 0)
                {
                    var key = $"{projectName}/{fileName}.{prop.Name}";
                    propertyUsage[key] = propMatches.Count;
                }
            }
        }

        return (classUsage, propertyUsage);
    }
} 