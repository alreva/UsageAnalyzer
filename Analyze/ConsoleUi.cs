namespace Analyze;

using DtoUsageAnalyzer;
using Microsoft.Extensions.Logging;
using Spectre.Console;

public class ConsoleUi(ILogger logger)
{
  public enum PropertyUsageFormat
  {
    TotalUsages,
    UsagesPerFile,
    UnusedPropertiesOnly,
  }

  public void DisplayWelcome()
  {
    AnsiConsole.MarkupLine("[bold blue]Welcome to the DTO Usage Analyzer![/]");
    AnsiConsole.MarkupLine("This tool will help you analyze the usage of DTO classes in your solution.");
  }

  public Type? PromptForClassSelection(IEnumerable<Type> dtoClasses)
  {
    var classes = dtoClasses.ToList();
    if (!classes.Any())
    {
      AnsiConsole.MarkupLine("[red]No DTO classes found in the Dto project.[/]");
      return null;
    }

    var selectedClass = AnsiConsole.Prompt(
        new SelectionPrompt<Type>()
            .Title("Select a DTO class to analyze:")
            .AddChoices(classes)
            .UseConverter(t => t.Name));

    return selectedClass;
  }

  public void DisplayAnalysisStart(Type selectedClass)
  {
    AnsiConsole.MarkupLine($"[green]Analyzing usage of {selectedClass.Name}...[/]");
  }

  public PropertyUsageFormat PromptForPropertyUsageFormat()
  {
    var choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("How would you like to display property usage?")
            .AddChoices("Show usages per file", "Show only total usages", "Show unused properties only"));
    return choice switch
    {
      "Show usages per file" => PropertyUsageFormat.UsagesPerFile,
      "Show only total usages" => PropertyUsageFormat.TotalUsages,
      "Show unused properties only" => PropertyUsageFormat.UnusedPropertiesOnly,
      _ => throw new InvalidOperationException("Invalid property usage format selected"),
    };
  }

  public bool PromptToSkipTestProjects()
  {
    return AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("Skip test project analysis?")
            .AddChoices("Yes", "No")) == "Yes";
  }

  public string FindSolutionFile()
  {
    var currentDir = Directory.GetCurrentDirectory();
    var solutionFiles = Directory.GetFiles(currentDir, "*.sln");

    if (solutionFiles.Length == 1)
    {
      return solutionFiles[0];
    }

    if (solutionFiles.Length > 1)
    {
      var selectedSolution = AnsiConsole.Prompt(
          new SelectionPrompt<string>()
              .Title("Multiple solution files found. Please select one:")
              .AddChoices(solutionFiles.Select(f =>
                  Path.GetFileName(f) ?? throw new InvalidOperationException("Solution file name is null"))));
      return solutionFiles.First(f => Path.GetFileName(f) == selectedSolution);
    }

    // If no solution file found in current directory, look in parent directory
    var parentDir = new DirectoryInfo(currentDir);
    for (var numTry = 0; numTry < 5; numTry++)
    {
      parentDir = parentDir.Parent;
      if (parentDir == null)
      {
        break;
      }

      var parentSolutionFiles = Directory.GetFiles(parentDir.FullName, "*.sln");
      switch (parentSolutionFiles.Length)
      {
        case 1:
          return parentSolutionFiles[0];
        case > 1:
          {
            var selectedSolution = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Multiple solution files found in parent directory. Please select one:")
                    .AddChoices(parentSolutionFiles.Select(f =>
                        Path.GetFileName(f) ??
                        throw new InvalidOperationException("Solution file name is null"))));
            return parentSolutionFiles.First(f => Path.GetFileName(f) == selectedSolution);
          }
      }
    }

    throw new FileNotFoundException("No solution file found in current or parent directory.");
  }

  public void DisplayResults(IReadOnlyList<PropertyUsage> propertyUsage, PropertyUsageFormat propertyUsageFormat)
  {
    // Property Usage Table
    AnsiConsole.MarkupLine("\n[bold blue]Property Usage Analysis[/]");
    switch (propertyUsageFormat)
    {
      case PropertyUsageFormat.UnusedPropertiesOnly:
        {
          var propertyTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[bold]Property[/]").LeftAligned());

          var propertyUsageData = propertyUsage
            .Where(u => u.UsageCount == 0)
            .Select(u => u.Property)
            .Distinct()
            .OrderBy(x => x.Attribute.ClassName)
            .ThenBy(x => x.Attribute.FieldName)
            .Select(x => new
            {
              PropertyPath = x.Attribute,
            });

          foreach (var key in propertyUsageData)
          {
            var (className, fieldName) = key.PropertyPath;
            propertyTable.AddRow(
              FormatBasedOnUsageCount(0, $"{className}.{fieldName}"));
          }

          AnsiConsole.Write(propertyTable);

          break;
        }

      case PropertyUsageFormat.TotalUsages:
        {
          var propertyTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[bold]Property[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Total Usages[/]").RightAligned());

          var propertyUsageData = propertyUsage
            .Select(u => new { PropertyPath = u.Property.Attribute, Count = u.UsageCount })
            .GroupBy(x => x.PropertyPath)
            .OrderBy(x => x.Key.ClassName)
            .ThenBy(x => x.Key.FieldName)
            .Select(x => new
            {
              PropertyPath = x.Key,
              Count = x.Sum(y => y.Count),
            });

          foreach (var usage in propertyUsageData)
          {
            var (className, fieldName) = usage.PropertyPath;
            var totalUsages = usage.Count;
            propertyTable.AddRow(
              FormatBasedOnUsageCount(totalUsages, $"{className}.{fieldName}"),
              FormatBasedOnUsageCount(totalUsages, totalUsages));
          }

          AnsiConsole.Write(propertyTable);
          break;
        }

      case PropertyUsageFormat.UsagesPerFile:
        {
          var propertyTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[bold]Property[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]File[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Usages[/]").RightAligned());

          var propertyUsageData = propertyUsage
            .Select(u => new { File = u.Property.FilePath, PropertyPath = u.Property.Attribute, Count = u.UsageCount })
            .OrderBy(g => g.PropertyPath.ClassName)
            .ThenBy(g => g.PropertyPath.FieldName)
            .ThenBy(g => g.File);

          foreach (var usage in propertyUsageData)
          {
            var (className, fieldName) = usage.PropertyPath;
            propertyTable.AddRow(
              FormatBasedOnUsageCount(usage.Count, $"{className}.{fieldName}"),
              $"[blue]{usage.File}[/]",
              FormatBasedOnUsageCount(usage.Count, usage.Count));
          }

          AnsiConsole.Write(propertyTable);
          break;
        }
    }
  }

  public void DisplayError(Exception ex)
  {
    AnsiConsole.MarkupLine($"[red]An error occurred: {ex.Message}[/]");
    logger.LogError(ex, "An error occurred during analysis");
  }

  private static string FormatBasedOnUsageCount(int usageCount, object value)
  {
    return $"[{(usageCount == 0 ? "yellow" : "green")}]{value}[/]";
  }
}
