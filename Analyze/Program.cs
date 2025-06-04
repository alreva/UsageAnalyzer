using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Spectre.Console;

namespace Analyze;

class Program
{
    static void Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var services = new ServiceCollection();
        ConfigureServices(services, configuration);
        var serviceProvider = services.BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var analysisService = serviceProvider.GetRequiredService<AnalysisService>();
        var consoleUI = new ConsoleUI(logger);

        try
        {
            consoleUI.DisplayWelcome();

            // Get all DTO classes
            var dtoClasses = analysisService.GetDtoClasses().ToList();
            if (!dtoClasses.Any())
            {
                AnsiConsole.MarkupLine("[red]No DTO classes found in the Dto project.[/]");
                return;
            }

            // Let user select a class
            var selectedClass = consoleUI.PromptForClassSelection(dtoClasses);
            if (selectedClass == null)
            {
                return;
            }

            // Ask user for property usage output format
            var propertyUsageFormat = consoleUI.PromptForPropertyUsageFormat();

            consoleUI.DisplayAnalysisStart(selectedClass);

            // Get source files
            var sourceFiles = analysisService.GetSourceFiles().ToList();
            if (!sourceFiles.Any())
            {
                AnsiConsole.MarkupLine("[red]No source files found to analyze.[/]");
                return;
            }

            // Analyze usage
            var (classUsage, propertyUsage) = analysisService.AnalyzeUsage(
                selectedClass,
                sourceFiles,
                progress => {});

            // Display results
            consoleUI.DisplayResults(classUsage, propertyUsage, selectedClass, propertyUsageFormat);
        }
        catch (Exception ex)
        {
            consoleUI.DisplayError(ex);
        }
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddConfiguration(configuration.GetSection("Logging"));
        });

        services.AddSingleton<AnalysisService>();
    }
}
