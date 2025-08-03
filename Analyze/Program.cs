namespace Analyze;

using System.IO;
using DtoUsageAnalyzer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

public class Program
{
  public static async Task Main(string[] args)
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
      var solutionPath = consoleUi.FindSolutionFile();

      // Get all DTO classes
      var solutionDir = Path.GetDirectoryName(solutionPath)!;
      var tf = analysisService.GetTargetFramework(solutionDir);
      var dtoAssemblyPath = Path.Combine(solutionDir, "Dto", "bin", "Debug", tf, "Dto.dll");
      var dtoClasses = analysisService.GetDtoAssemblyTypes(dtoAssemblyPath).ToList();
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

      var skipTestProjects = consoleUi.PromptToSkipTestProjects();

      consoleUi.DisplayAnalysisStart(selectedClass);

      // Analyze usage
      var propertyUsage = await analysisService
          .AnalyzeUsageAsync(solutionPath, selectedClass, skipTestProjects);

      // Display results
      consoleUi.DisplayResults(propertyUsage, propertyUsageFormat);
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
