using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Analyze;

public class Program
{
    private static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false)
            .Build();

        var services = new ServiceCollection();
        ConfigureServices(services, configuration);
        var serviceProvider = services.BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var analysisService = serviceProvider.GetRequiredService<AnalysisService>();
        var consoleUi = new ConsoleUi(logger);

        try
        {
            consoleUi.DisplayWelcome();

            // Get all DTO classes
            var dtoClasses = analysisService.GetDtoClasses().ToList();
            if (!dtoClasses.Any())
            {
                AnsiConsole.MarkupLine("[red]No DTO classes found in the Dto project.[/]");
                return;
            }

            // Let user select a class
            var selectedClass = consoleUi.PromptForClassSelection(dtoClasses);
            if (selectedClass == null)
            {
                return;
            }

            // Ask user for property usage output format
            var propertyUsageFormat = consoleUi.PromptForPropertyUsageFormat();

            consoleUi.DisplayAnalysisStart(selectedClass);

            // Analyze usage
            var (classUsage, propertyUsage) = await analysisService.AnalyzeUsageAsync(
                selectedClass,
                progress => { });

            // Display results
            consoleUi.DisplayResults(classUsage, propertyUsage, selectedClass, propertyUsageFormat);
        }
        catch (Exception ex)
        {
            consoleUi.DisplayError(ex);
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