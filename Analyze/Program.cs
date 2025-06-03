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
using Analyze;

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
    var consoleUI = new ConsoleUI(logger);
    var analysisService = new AnalysisService(logger);

    consoleUI.DisplayWelcome();

    // Get all classes from the Dto namespace
    var dtoClasses = analysisService.GetDtoClasses();
    var selectedClass = consoleUI.PromptForClassSelection(dtoClasses);

    if (selectedClass == null)
    {
        return;
    }

    consoleUI.DisplayAnalysisStart(selectedClass);
    
    // Get all source files in the solution
    var sourceFiles = analysisService.GetSourceFiles();

    // Analyze class and property usage
    var result = analysisService.AnalyzeUsage(
        selectedClass,
        sourceFiles,
        status => consoleUI.DisplayAnalysisProgress(ctx => ctx.Status(status))
    );

    // Display results
    consoleUI.DisplayResults(result.classUsage, result.propertyUsage, selectedClass);
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red]An error occurred: {ex.Message}[/]");
    AnsiConsole.MarkupLine($"[red]Stack trace: {ex.StackTrace}[/]");
    logger.LogError(ex, "An error occurred during analysis");
}
