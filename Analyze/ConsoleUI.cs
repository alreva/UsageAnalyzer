using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

    public void DisplayResults(
        Dictionary<string, int> classUsage,
        Dictionary<string, int> propertyUsage,
        Type selectedClass)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[bold]File[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Usages[/]").RightAligned());

        AnsiConsole.MarkupLine("\n[bold blue]Class Usage Analysis[/]");
        if (classUsage.Any())
        {
            foreach (var usage in classUsage.OrderByDescending(u => u.Value))
            {
                table.AddRow(
                    $"[green]{usage.Key}[/]",
                    $"[yellow]{usage.Value}[/]"
                );
            }
        }
        else
        {
            table.AddRow("[red]No direct class usage found[/]", "0");
        }
        AnsiConsole.Write(table);

        table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[bold]Property[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]File[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Usages[/]").RightAligned());

        AnsiConsole.MarkupLine("\n[bold blue]Property Usage Analysis[/]");
        foreach (var prop in selectedClass.GetProperties())
        {
            var usages = propertyUsage.Where(u => u.Key.EndsWith($".{prop.Name}")).ToList();
            if (usages.Any())
            {
                foreach (var usage in usages.OrderByDescending(u => u.Value))
                {
                    table.AddRow(
                        $"[green]{prop.Name}[/]",
                        $"[blue]{usage.Key}[/]",
                        $"[yellow]{usage.Value}[/]"
                    );
                }
            }
            else
            {
                table.AddRow(
                    $"[green]{prop.Name}[/]",
                    "[red]No usage found[/]",
                    "0"
                );
            }
        }
        AnsiConsole.Write(table);
    }

    public void DisplayError(Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]An error occurred: {ex.Message}[/]");
        AnsiConsole.MarkupLine($"[red]Stack trace: {ex.StackTrace}[/]");
        _logger.LogError(ex, "An error occurred during analysis");
    }
} 