using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Analyze;

public class ConsoleUi
{
    public enum PropertyUsageFormat
    {
        TotalUsages,
        UsagesPerFile
    }

    private readonly ILogger _logger;

    public ConsoleUi(ILogger logger)
    {
        _logger = logger;
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

    public void DisplayAnalysisProgress(Action<StatusContext> updateStatus)
    {
        AnsiConsole.Status()
            .Start("Analyzing...", ctx => updateStatus(ctx));
    }

    public PropertyUsageFormat PromptForPropertyUsageFormat()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("How would you like to display property usage?")
                .AddChoices("Show usages per file", "Show only total usages"));
        return choice == "Show usages per file" ? PropertyUsageFormat.UsagesPerFile : PropertyUsageFormat.TotalUsages;
    }

    public void DisplayResults(
        Dictionary<UsageKey, int> propertyUsage,
        Type selectedClass,
        PropertyUsageFormat propertyUsageFormat)
    {
        // Property Usage Table
        AnsiConsole.MarkupLine("\n[bold blue]Property Usage Analysis[/]");
        if (propertyUsageFormat == PropertyUsageFormat.TotalUsages)
        {
            var propertyTable = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn(new TableColumn("[bold]Property[/]").LeftAligned())
                .AddColumn(new TableColumn("[bold]Total Usages[/]").RightAligned());

            var propertyUsageData = propertyUsage
                .Select(u => new { PropertyPath = u.Key.Attribute, Count = u.Value })
                .GroupBy(x => x.PropertyPath)
                .OrderBy(x => x.Key.ClassName)
                .ThenBy(x => x.Key.FieldName)
                .Select(x => new
                {
                    PropertyPath = x.Key,
                    Count = x.Sum(y => y.Count)
                });

            foreach (var usage in propertyUsageData)
            {
                var (className, fieldName) = usage.PropertyPath;
                var totalUsages = usage.Count;
                propertyTable.AddRow(
                    $"[green]{className}.{fieldName}[/]",
                    FormatUsageCount(totalUsages)
                );
            }

            AnsiConsole.Write(propertyTable);
        }
        else // UsagesPerFile
        {
            var propertyTable = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn(new TableColumn("[bold]Property[/]").LeftAligned())
                .AddColumn(new TableColumn("[bold]File[/]").LeftAligned())
                .AddColumn(new TableColumn("[bold]Usages[/]").RightAligned());

            var propertyUsageData = propertyUsage
                .Select(u => new { File = u.Key.FilePath, PropertyPath = u.Key.Attribute, Count = u.Value })
                .OrderBy(g => g.PropertyPath.ClassName)
                .ThenBy(g => g.PropertyPath.FieldName)
                .ThenBy(g => g.File);

            foreach (var usage in propertyUsageData)
            {
                var (className, fieldName) = usage.PropertyPath;
                propertyTable.AddRow(
                    $"[green]{className}.{fieldName}[/]",
                    $"[blue]{usage.File}[/]",
                    FormatUsageCount(usage.Count)
                );
            }
            

            AnsiConsole.Write(propertyTable);
        }
    }

    private static string FormatUsageCount(int usageCount)
    {
        return $"[{(usageCount == 0 ? "yellow" : "green")}]{usageCount}[/]";
    }

    public void DisplayError(Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]An error occurred: {ex.Message}[/]");
        // AnsiConsole.MarkupLine($"[red]Stack trace: {ex.StackTrace}[/]");
        _logger.LogError(ex, "An error occurred during analysis");
    }
}