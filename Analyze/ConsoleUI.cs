using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Spectre.Console;
using Microsoft.Extensions.Logging;
using Dto;

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
        AnsiConsole.MarkupLine("[bold blue]Welcome to the Usage Analyzer![/]");
        AnsiConsole.MarkupLine("[yellow]Please select a class to analyze:[/]");
    }

    public Type PromptForClassSelection(IEnumerable<Type> dtoClasses)
    {
        if (!dtoClasses.Any())
        {
            AnsiConsole.MarkupLine("[red]No classes found in the Dto namespace.[/]");
            return null;
        }

        return AnsiConsole.Prompt(
            new SelectionPrompt<Type>()
                .Title("Select a class to analyze:")
                .PageSize(10)
                .AddChoices(dtoClasses)
                .UseConverter(type => type.Name));
    }

    public void DisplayAnalysisStart(Type selectedClass)
    {
        AnsiConsole.MarkupLine($"\n[green]Analyzing usage of: {selectedClass.Name}[/]");
    }

    public void DisplayAnalysisProgress(Action<StatusContext> analysisAction)
    {
        AnsiConsole.Status()
            .Start("Analyzing files...", analysisAction);
    }

    public void DisplayResults(Dictionary<string, int> classUsage, Dictionary<string, int> propertyUsage, Type selectedClass)
    {
        DisplayClassUsage(classUsage);
        DisplayPropertyUsage(propertyUsage, selectedClass);
    }

    private void DisplayClassUsage(Dictionary<string, int> classUsage)
    {
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
    }

    private void DisplayPropertyUsage(Dictionary<string, int> propertyUsage, Type selectedClass)
    {
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

    public void DisplayError(Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]An error occurred: {ex.Message}[/]");
        AnsiConsole.MarkupLine($"[red]Stack trace: {ex.StackTrace}[/]");
        _logger.LogError(ex, "An error occurred during analysis");
    }
} 