using Spectre.Console;
using Microsoft.Extensions.Logging;

namespace Analyze;

public class ConsoleUI
{
    private readonly ILogger _logger;

    public ConsoleUI(ILogger logger)
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

    public enum PropertyUsageFormat
    {
        TotalUsages,
        UsagesPerFile
    }

    public PropertyUsageFormat PromptForPropertyUsageFormat()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("How would you like to display property usage?")
                .AddChoices("Show only total usages", "Show usages per file"));
        return choice == "Show usages per file" ? PropertyUsageFormat.UsagesPerFile : PropertyUsageFormat.TotalUsages;
    }

    public void DisplayResults(
        Dictionary<string, int> classUsage,
        Dictionary<string, int> propertyUsage,
        Type selectedClass,
        PropertyUsageFormat propertyUsageFormat)
    {
        // Class Usage Table
        var classTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[bold]File[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Usages[/]").RightAligned());

        AnsiConsole.MarkupLine("\n[bold blue]Class Usage Analysis[/]");
        if (classUsage.Any())
        {
            foreach (var usage in classUsage.OrderByDescending(u => u.Value))
            {
                classTable.AddRow(
                    $"[green]{usage.Key}[/]",
                    $"[yellow]{usage.Value}[/]"
                );
            }
        }
        else
        {
            classTable.AddRow("[red]No direct class usage found[/]", "0");
        }
        AnsiConsole.Write(classTable);

        // Property Usage Table
        AnsiConsole.MarkupLine("\n[bold blue]Property Usage Analysis[/]");
        if (propertyUsageFormat == PropertyUsageFormat.TotalUsages)
        {
            var propertyTable = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn(new TableColumn("[bold]Property[/]").LeftAligned())
                .AddColumn(new TableColumn("[bold]Total Usages[/]").RightAligned());

            var propertyGroups = propertyUsage
                .Select(u => {
                    var parts = u.Key.Split('|');
                    return new { PropertyPath = parts[1], Count = u.Value };
                })
                .GroupBy(x => x.PropertyPath)
                .OrderBy(g => g.Key);

            foreach (var group in propertyGroups)
            {
                var propertyPath = group.Key;
                var totalUsages = group.Sum(x => x.Count);
                propertyTable.AddRow(
                    $"[green]{propertyPath}[/]",
                    $"[yellow]{totalUsages}[/]"
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

            var propertyGroups = propertyUsage
                .Select(u => {
                    var parts = u.Key.Split('|');
                    return new { File = parts[0], PropertyPath = parts[1], Count = u.Value };
                })
                .GroupBy(x => x.PropertyPath)
                .OrderBy(g => g.Key);

            foreach (var group in propertyGroups)
            {
                var propertyPath = group.Key;
                var usages = group.ToList();
                foreach (var usage in usages.OrderByDescending(u => u.Count))
                {
                    propertyTable.AddRow(
                        $"[green]{propertyPath}[/]",
                        $"[blue]{usage.File}[/]",
                        $"[yellow]{usage.Count}[/]"
                    );
                }
            }
            AnsiConsole.Write(propertyTable);
        }
    }

    public void DisplayError(Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]An error occurred: {ex.Message}[/]");
        AnsiConsole.MarkupLine($"[red]Stack trace: {ex.StackTrace}[/]");
        _logger.LogError(ex, "An error occurred during analysis");
    }
} 