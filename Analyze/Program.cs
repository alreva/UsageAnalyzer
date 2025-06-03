using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.IO;
using Dto;
using Spectre.Console;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

// Load configuration from appsettings.json
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// Set up logger using configuration
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddConfiguration(configuration.GetSection("Logging"))
        .AddConsole();
});
ILogger logger = loggerFactory.CreateLogger("Analyzer");

try
{
    AnsiConsole.MarkupLine("[bold blue]Welcome to the Usage Analyzer![/]");
    AnsiConsole.MarkupLine("[yellow]Please select a class to analyze:[/]");

    // Get all classes from the Dto namespace
    var dtoClasses = Assembly.GetAssembly(typeof(UserEventDto))
        ?.GetTypes()
        .Where(t => t.Namespace == "Dto" && t.IsClass)
        .ToList();

    if (dtoClasses == null || !dtoClasses.Any())
    {
        AnsiConsole.MarkupLine("[red]No classes found in the Dto namespace.[/]");
        return;
    }

    // Create a selection prompt
    var selectedClass = AnsiConsole.Prompt(
        new SelectionPrompt<Type>()
            .Title("Select a class to analyze:")
            .PageSize(10)
            .AddChoices(dtoClasses)
            .UseConverter(type => type.Name));

    AnsiConsole.MarkupLine($"\n[green]Analyzing usage of: {selectedClass.Name}[/]");
    
    // Get all source files in the solution
    var solutionDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".."));
    logger.LogInformation($"Resolved solution directory: {solutionDir}");
    var sourceFiles = new List<string>();
    
    // Add files from specific project directories
    var projectDirs = new[] { "Processors", "Dto", "Dto.Tests", "Processors.Tests" };
    foreach (var dir in projectDirs)
    {
        var dirPath = Path.Combine(solutionDir, dir);
        logger.LogInformation($"Checking project directory: {dirPath}");
        if (Directory.Exists(dirPath))
        {
            var files = Directory.GetFiles(dirPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) && 
                           !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
                .ToList();
            logger.LogInformation($"Found {files.Count} .cs files in {dirPath}");
            foreach (var file in files)
            {
                logger.LogDebug($"Found file: {file}");
            }
            sourceFiles.AddRange(files);
        }
        else
        {
            logger.LogWarning($"Directory does not exist: {dirPath}");
        }
    }

    // Analyze class and property usage
    var classUsage = new Dictionary<string, int>();
    var propertyUsage = new Dictionary<string, int>();

    // Show progress
    AnsiConsole.Status()
        .Start("Analyzing files...", ctx => 
        {
            foreach (var file in sourceFiles)
            {
                ctx.Status($"Analyzing {Path.GetFileName(file)}");
                logger.LogDebug($"Analyzing file: {file}");
                var content = File.ReadAllText(file);
                var fileName = Path.GetFileName(file);
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
        });

    // Display results
    var classTable = new Table()
        .Border(TableBorder.Rounded)
        .Title("[bold blue]Class Usage Statistics[/]")
        .AddColumn("File")
        .AddColumn("References");

    if (classUsage.Any())
    {
        foreach (var usage in classUsage.OrderByDescending(u => u.Value))
        {
            classTable.AddRow(usage.Key, usage.Value.ToString());
        }
    }
    else
    {
        classTable.AddRow("[red]No direct class usage found[/]", "");
    }

    AnsiConsole.Write(classTable);

    // Property usage table
    var propertyTable = new Table()
        .Border(TableBorder.Rounded)
        .Title("[bold blue]Property Usage Statistics[/]")
        .AddColumn("Property")
        .AddColumn("Type")
        .AddColumn("File")
        .AddColumn("References");

    var properties = selectedClass.GetProperties();
    foreach (var prop in properties)
    {
        var propUsages = propertyUsage
            .Where(u => u.Key.EndsWith($".{prop.Name}"))
            .OrderByDescending(u => u.Value);

        if (propUsages.Any())
        {
            foreach (var usage in propUsages)
            {
                var fileInfo = usage.Key.Split('.')[0];
                propertyTable.AddRow(
                    prop.Name,
                    prop.PropertyType.Name,
                    fileInfo,
                    usage.Value.ToString()
                );
            }
        }
        else
        {
            propertyTable.AddRow(
                prop.Name,
                prop.PropertyType.Name,
                "[red]No usage found[/]",
                ""
            );
        }
    }

    AnsiConsole.Write(propertyTable);
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red]An error occurred: {ex.Message}[/]");
    AnsiConsole.MarkupLine($"[red]Stack trace: {ex.StackTrace}[/]");
    logger.LogError(ex, "An error occurred during analysis");
}
