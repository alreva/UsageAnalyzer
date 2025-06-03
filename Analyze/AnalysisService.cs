using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Dto;
using Spectre.Console;

namespace Analyze;

public class AnalysisService
{
    private readonly ILogger _logger;
    private readonly string _solutionDir;

    public AnalysisService(ILogger logger)
    {
        _logger = logger;
        _solutionDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".."));
        _logger.LogInformation($"Resolved solution directory: {_solutionDir}");
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
        var projectDirs = new[] { "Processors", "Dto", "Dto.Tests", "Processors.Tests" };

        foreach (var dir in projectDirs)
        {
            var dirPath = Path.Combine(_solutionDir, dir);
            _logger.LogInformation($"Checking project directory: {dirPath}");
            
            if (Directory.Exists(dirPath))
            {
                var files = Directory.GetFiles(dirPath, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) && 
                               !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
                    .ToList();
                
                _logger.LogInformation($"Found {files.Count} .cs files in {dirPath}");
                foreach (var file in files)
                {
                    _logger.LogDebug($"Found file: {file}");
                }
                sourceFiles.AddRange(files);
            }
            else
            {
                _logger.LogWarning($"Directory does not exist: {dirPath}");
            }
        }

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