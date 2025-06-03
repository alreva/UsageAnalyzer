using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.IO;
using Dto;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            Console.WriteLine("Welcome to the Usage Analyzer!");
            Console.WriteLine("Please select a class to analyze:");

            // Get all classes from the Dto namespace
            var dtoClasses = Assembly.GetAssembly(typeof(UserEventDto))
                ?.GetTypes()
                .Where(t => t.Namespace == "Dto" && t.IsClass)
                .ToList();

            if (dtoClasses == null || !dtoClasses.Any())
            {
                Console.WriteLine("No classes found in the Dto namespace.");
                return;
            }

            // Display available classes
            for (int i = 0; i < dtoClasses.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {dtoClasses[i].Name}");
            }

            // Get user input
            Console.Write("\nEnter the number of the class you want to analyze: ");
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input) || 
                !int.TryParse(input.Trim(), out int selection) || 
                selection < 1 || 
                selection > dtoClasses.Count)
            {
                Console.WriteLine("Invalid selection. Please run the program again.");
                return;
            }

            // Get the selected class
            var selectedClass = dtoClasses[selection - 1];
            Console.WriteLine($"\nAnalyzing usage of: {selectedClass.Name}");
            
            // Get all source files in the solution
            var solutionDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".."));
            var sourceFiles = new List<string>();
            
            // Add files from specific project directories
            var projectDirs = new[] { "Processors", "Dto", "Dto.Tests", "Processors.Tests" };
            foreach (var dir in projectDirs)
            {
                var dirPath = Path.Combine(solutionDir, dir);
                if (Directory.Exists(dirPath))
                {
                    sourceFiles.AddRange(Directory.GetFiles(dirPath, "*.cs", SearchOption.AllDirectories)
                        .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) && 
                                   !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)));
                }
            }

            // Analyze class and property usage
            var classUsage = new Dictionary<string, int>();
            var propertyUsage = new Dictionary<string, int>();

            foreach (var file in sourceFiles)
            {
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

            // Display results
            Console.WriteLine("\nClass Usage Statistics:");
            Console.WriteLine("======================");
            if (classUsage.Any())
            {
                foreach (var usage in classUsage.OrderByDescending(u => u.Value))
                {
                    Console.WriteLine($"{usage.Key}: {usage.Value} references");
                }
            }
            else
            {
                Console.WriteLine("No direct class usage found.");
            }

            Console.WriteLine("\nProperty Usage Statistics:");
            Console.WriteLine("=========================");
            var properties = selectedClass.GetProperties();
            foreach (var prop in properties)
            {
                Console.WriteLine($"\n{prop.Name} ({prop.PropertyType.Name}):");
                var propUsages = propertyUsage
                    .Where(u => u.Key.EndsWith($".{prop.Name}"))
                    .OrderByDescending(u => u.Value);

                if (propUsages.Any())
                {
                    foreach (var usage in propUsages)
                    {
                        var fileInfo = usage.Key.Split('.')[0];
                        Console.WriteLine($"  - {fileInfo}: {usage.Value} references");
                    }
                }
                else
                {
                    Console.WriteLine("  No usage found");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nAn error occurred: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
