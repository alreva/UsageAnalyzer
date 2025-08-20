namespace DtoUsageAnalyzer.Tests;

using System.Diagnostics;
using System.IO;
using System.Reflection;
using Analyze;
using Dto;
using DtoUsageAnalyzer;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

public class AnalysisServiceTests(ITestOutputHelper testOutputHelper)
{
  [Fact]
  public void GetDtoAssemblyTypes_ReturnsDtoTypes()
  {
    var service = CreateService();
    var solutionPath = GetSolutionPath();
    var solutionDir = Path.GetDirectoryName(solutionPath)!;
    var tf = ProjectHelper.GetTargetFramework(solutionDir);
    var dtoAssemblyPath = Path.Combine(solutionDir, "Dto", "bin", "Debug", tf, "Dto.dll");
    var types = service.GetDtoAssemblyTypes(dtoAssemblyPath);
    Assert.Contains(types, t => t.Name == nameof(UserEventDto));
  }

  [Fact]
  public void IsNullable_DetectsNullableTypes()
  {
    Assert.True(AnalysisService.IsNullable(typeof(int?)));
    Assert.False(AnalysisService.IsNullable(typeof(int)));
  }

  [Fact]
  public void IsPrimitiveOrArrayOfPrimitives_ReturnsExpected()
  {
    Assert.True(AnalysisService.IsPrimitiveOrArrayOfPrimitives(typeof(int)));
    Assert.True(AnalysisService.IsPrimitiveOrArrayOfPrimitives(typeof(string)));
    Assert.True(AnalysisService.IsPrimitiveOrArrayOfPrimitives(typeof(decimal)));
    Assert.True(AnalysisService.IsPrimitiveOrArrayOfPrimitives(typeof(DateTime)));
    Assert.True(AnalysisService.IsPrimitiveOrArrayOfPrimitives(typeof(string[])));
    Assert.True(AnalysisService.IsPrimitiveOrArrayOfPrimitives(typeof(List<int>)));
    Assert.True(AnalysisService.IsPrimitiveOrArrayOfPrimitives(typeof(DayOfWeek))); // enum
    Assert.True(AnalysisService.IsPrimitiveOrArrayOfPrimitives(typeof(DayOfWeek[]))); // enum array
    Assert.True(AnalysisService.IsPrimitiveOrArrayOfPrimitives(typeof(List<DayOfWeek>))); // enum list
    Assert.False(AnalysisService.IsPrimitiveOrArrayOfPrimitives(typeof(DeviceInfo)));
  }

  [Fact]
  public void GetDeepMembers_ReturnsPropertiesAndFields()
  {
    var service = CreateService();
    var result = service.GetDeepMembers(typeof(User));
    var membersByPath = result.ToDictionary(m => m.FullPath, m => m);

    // Properties should be included (Address.City, DeviceInfo.IpAddress)
    Assert.True(membersByPath.ContainsKey("Address.City"));
    Assert.True(membersByPath.ContainsKey("DeviceInfo.IpAddress"));
    Assert.True(membersByPath["Address.City"].Member is PropertyInfo);
    Assert.True(membersByPath["DeviceInfo.IpAddress"].Member is PropertyInfo);

    // Init-only properties should be included (treated as properties, not fields)
    Assert.True(membersByPath.ContainsKey("Address.Street"));
    Assert.True(membersByPath.ContainsKey("DeviceInfo.DeviceType"));
    Assert.True(membersByPath["Address.Street"].Member is PropertyInfo);
    Assert.True(membersByPath["DeviceInfo.DeviceType"].Member is PropertyInfo);

    // Fields should be included (ActivityLog has actual fields)
    if (membersByPath.TryGetValue("ActivityLog.ProductId", out var value))
    {
      Assert.True(value.Member is FieldInfo);
    }

    if (membersByPath.TryGetValue("ActivityLog.ViewCount", out var value1))
    {
      Assert.True(value1.Member is FieldInfo);
    }

    // Both should have correct names and types
    Assert.Equal("Street", membersByPath["Address.Street"].Name);
    Assert.Equal(typeof(string), membersByPath["Address.Street"].MemberType);
  }

  [Fact]
  public void GetDeepMembers_HandlesFieldsInRootType()
  {
    var service = CreateService();
    var result = service.GetDeepMembers(typeof(ActivityLog));
    var membersByName = result.ToDictionary(m => m.Name, m => m);

    // Fields
    Assert.True(membersByName.ContainsKey("ProductId"));
    Assert.True(membersByName.ContainsKey("ViewCount"));
    Assert.True(membersByName["ProductId"].Member is FieldInfo);
    Assert.True(membersByName["ViewCount"].Member is FieldInfo);

    // Properties
    Assert.True(membersByName.ContainsKey("Action"));
    Assert.True(membersByName.ContainsKey("Timestamp"));
    Assert.True(membersByName["Action"].Member is PropertyInfo);
    Assert.True(membersByName["Timestamp"].Member is PropertyInfo);
  }

  [Fact]
  public async Task AnalyzeUsageAsync_ReturnsUsageCounts()
  {
    // Use the same exclusion patterns as the original test to maintain expected behavior
    var options = new AnalysisOptions { ExcludePatterns = ["*Tests", "Analyze", "Dto"] };
    var service = CreateService(options);
    var solutionPath = GetSolutionPath();
    var result = await service.AnalyzeUsageAsync(
        solutionPath,
        typeof(UserEventDto));

    var aggregated = result
        .GroupBy(r => r.Property.Attribute)
        .ToDictionary(g => g.Key, g => g.Sum(x => x.UsageCount));

    Assert.Equal(2, aggregated[new ClassAndField("Address", "ZipCode")]);
    Assert.Equal(2, aggregated[new ClassAndField("User", "FavoriteCategories")]);
    Assert.Equal(2, aggregated[new ClassAndField("ActivityLog", "ProductId")]);
    Assert.Equal(0, aggregated[new ClassAndField("User", "CreatedAt")]);
    Assert.Equal(1, aggregated[new ClassAndField("UserEventDto", "EventId")]);

    // Verify init-only property usage is tracked - Street is an init-only property in Address
    Assert.True(aggregated.ContainsKey(new ClassAndField("Address", "Street")));
    Assert.Equal(1, aggregated[new ClassAndField("Address", "Street")]);

    // Verify field usage is tracked - ViewCount is a field in ActivityLog
    Assert.True(aggregated.ContainsKey(new ClassAndField("ActivityLog", "ViewCount")));
    Assert.Equal(1, aggregated[new ClassAndField("ActivityLog", "ViewCount")]);
  }

  [Fact]
  public void AnalysisService_WithDefaultOptions_HasEmptyExcludePatterns()
  {
    var service = CreateService();
    var defaultOptions = new AnalysisOptions();

    // Default options should have no exclude patterns (library makes no assumptions)
    Assert.NotNull(service);
    Assert.Empty(defaultOptions.ExcludePatterns);
  }

  [Fact]
  public void AnalysisService_WithCustomOptions_UsesCustomPatterns()
  {
    var options = new AnalysisOptions
    {
      ExcludePatterns = ["*Integration*", "Mock*"],
    };
    var service = CreateService(options);

    // Verify custom patterns are configured correctly
    Assert.NotNull(service);
    Assert.Equal(2, options.ExcludePatterns.Length);
    Assert.Contains("*Integration*", options.ExcludePatterns);
    Assert.Contains("Mock*", options.ExcludePatterns);
  }

  [Fact]
  public void AnalysisService_WithNullOptions_UsesEmptyDefaults()
  {
    var service = CreateService(null!);

    // Should work with null options (uses empty defaults)
    Assert.NotNull(service);
  }

  [Fact]
  public async Task AnalyzeUsageAsync_WithNoExcludePatterns_ProcessesAllProjects()
  {
    // Test with empty exclude patterns - should process all projects including tests
    var options = new AnalysisOptions { ExcludePatterns = [] };
    var service = CreateService(options);
    var solutionPath = GetSolutionPath();

    var result = await service.AnalyzeUsageAsync(solutionPath, typeof(UserEventDto));

    // Should get results since no projects are excluded
    Assert.NotEmpty(result);
  }

  [Fact]
  public async Task AnalyzeUsageAsync_IncludesFieldUsageInResults()
  {
    var options = new AnalysisOptions { ExcludePatterns = ["*Tests", "Analyze", "Dto"] };
    var service = CreateService(options);
    var solutionPath = GetSolutionPath();
    var result = await service.AnalyzeUsageAsync(solutionPath, typeof(UserEventDto));

    var allMembers = result.Select(r => r.Property.Attribute).ToHashSet();

    // Verify both fields and properties are included in results
    Assert.Contains(new ClassAndField("Address", "Street"), allMembers); // Init-only property
    Assert.Contains(new ClassAndField("Address", "City"), allMembers); // Property
    Assert.Contains(new ClassAndField("Address", "Country"), allMembers); // Init-only property
    Assert.Contains(new ClassAndField("DeviceInfo", "DeviceType"), allMembers); // Init-only property
    Assert.Contains(new ClassAndField("DeviceInfo", "Browser"), allMembers); // Init-only property
    Assert.Contains(new ClassAndField("DeviceInfo", "Os"), allMembers); // Property
    Assert.Contains(new ClassAndField("DeviceInfo", "IpAddress"), allMembers); // Property
    Assert.Contains(new ClassAndField("ActivityLog", "ProductId"), allMembers); // Field
    Assert.Contains(new ClassAndField("ActivityLog", "ViewCount"), allMembers); // Field

    // Verify mixed usage (some fields used, some not)
    var usedMembers = result.Where(r => r.UsageCount > 0).Select(r => r.Property.Attribute).ToHashSet();
    var unusedMembers = result.Where(r => r.UsageCount == 0).Select(r => r.Property.Attribute).ToHashSet();

    Assert.True(usedMembers.Count > 0, "Should have some used members");
    Assert.True(unusedMembers.Count > 0, "Should have some unused members");
  }

  [Fact]
  public void GetDeepMembers_HandlesArraysOfComplexObjects()
  {
    var service = CreateService();
    var result = service.GetDeepMembers(typeof(User));
    var membersByPath = result.ToDictionary(m => m.FullPath, m => m);

    // Verify array property navigation - SocialMediaAccounts is SocialMedia[]
    Assert.True(membersByPath.ContainsKey("SocialMediaAccounts.Item.Facebook"));
    Assert.True(membersByPath.ContainsKey("SocialMediaAccounts.Item.Twitter"));
    Assert.True(membersByPath.ContainsKey("SocialMediaAccounts.Item.Instagram"));

    // Verify the member types are correct
    Assert.Equal(typeof(string), membersByPath["SocialMediaAccounts.Item.Facebook"].MemberType);
    Assert.Equal(typeof(string), membersByPath["SocialMediaAccounts.Item.Twitter"].MemberType);
    Assert.Equal(typeof(string), membersByPath["SocialMediaAccounts.Item.Instagram"].MemberType);

    // Verify the names are correct
    Assert.Equal("Facebook", membersByPath["SocialMediaAccounts.Item.Facebook"].Name);
    Assert.Equal("Twitter", membersByPath["SocialMediaAccounts.Item.Twitter"].Name);
    Assert.Equal("Instagram", membersByPath["SocialMediaAccounts.Item.Instagram"].Name);
  }

  [Fact]
  public void GetDeepMembers_WithCircularReference_NoStackOverflow()
  {
    // This test runs GetDeepMembers with circular reference in a separate process
    // to verify it actually causes a StackOverflowException
    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    Directory.CreateDirectory(tempDir);

    try
    {
      CreateCircularReferenceTestProgram(tempDir);
      this.RunCircularReferenceTest(tempDir);
    }
    finally
    {
      // Clean up temp directory
      if (Directory.Exists(tempDir))
      {
        Directory.Delete(tempDir, true);
      }
    }
  }

  private static void CreateCircularReferenceTestProgram(string tempDir)
  {
    // Create a test program that calls GetDeepMembers with circular references
    var testProgram = @"
using System;
using System.Collections.Generic;
using System.Threading;
using DtoUsageAnalyzer;
using Microsoft.Extensions.Logging;

public class Department
{
  public required string DepartmentId { get; set; }
  public required string Name { get; set; }
  public Employee? Manager { get; set; }
  public required List<Employee> Employees { get; set; } = new();
}

public class Employee
{
  public required string EmployeeId { get; set; }
  public required string FirstName { get; set; }
  public required string LastName { get; set; }
  public required Department Department { get; set; }
  public Employee? Supervisor { get; set; }
  public required List<Employee> DirectReports { get; set; } = new();
}

class Program
{
  static int Main()
  {
    // Reduce stack size to force StackOverflowException more quickly
    var thread = new Thread(() =>
    {
      try
      {
        Console.WriteLine(""Starting GetDeepMembers test..."");
        var logger = LoggerFactory.Create(builder => { }).CreateLogger<AnalysisService>();
        var service = new AnalysisService(logger);
        var result = service.GetDeepMembers(typeof(Employee));
        Console.WriteLine(""Unexpected success - should have crashed"");
        Environment.Exit(0); // Should never reach here due to StackOverflow
      }
      catch (StackOverflowException)
      {
        Console.WriteLine(""StackOverflowException occurred as expected"");
        Environment.Exit(2); // Expected but may not be caught
      }
      catch (Exception ex)
      {
        Console.WriteLine($""Unexpected exception: {ex.GetType().Name}: {ex.Message}"");
        Environment.Exit(1);
      }
    }); // Use default stack size

    thread.Start();
    thread.Join(TimeSpan.FromSeconds(3)); // Wait max 3 seconds

    if (thread.IsAlive)
    {
      Console.WriteLine(""Thread stuck in infinite recursion"");
      Environment.Exit(3); // Infinite recursion detected
    }

    return 0;
  }
}";

    var testFile = Path.Combine(tempDir, "CircularTest.cs");
    File.WriteAllText(testFile, testProgram);

    var projectFile = Path.Combine(tempDir, "CircularTest.csproj");

    // Get the DtoUsageAnalyzer project path using assembly location
    var testAssemblyLocation = Assembly.GetExecutingAssembly().Location;
    var testDir = Path.GetDirectoryName(testAssemblyLocation)!;
    var solutionDir = Path.GetFullPath(Path.Combine(testDir, "../../../../"));
    var dtoUsageAnalyzerPath = Path.Combine(solutionDir, "DtoUsageAnalyzer", "DtoUsageAnalyzer.csproj");

    if (!File.Exists(dtoUsageAnalyzerPath))
    {
      Assert.Fail(
          $"Could not find DtoUsageAnalyzer project at: {dtoUsageAnalyzerPath}. TestDir: {testDir}, SolutionDir: {solutionDir}");
    }

    var projectContent = $@"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <UseAppHost>false</UseAppHost>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include=""{dtoUsageAnalyzerPath}"" />
  </ItemGroup>
</Project>";
    File.WriteAllText(projectFile, projectContent);

    // Build the test project
    var buildProcess = Process.Start(
        new ProcessStartInfo
        {
          FileName = "dotnet",
          Arguments = "build --configuration Release",
          WorkingDirectory = tempDir,
          UseShellExecute = false,
          RedirectStandardOutput = true,
          RedirectStandardError = true,
        });

    buildProcess?.WaitForExit(30000);

    if (buildProcess?.ExitCode != 0)
    {
      var output = buildProcess?.StandardOutput.ReadToEnd();
      var error = buildProcess?.StandardError.ReadToEnd();
      Assert.Fail($"Build failed with exit code {buildProcess?.ExitCode}. Output: {output}. Error: {error}");
    }
  }

  private static AnalysisService CreateService()
  {
    var logger = LoggerFactory.Create(_ => { }).CreateLogger<AnalysisService>();
    return new AnalysisService(logger);
  }

  private static AnalysisService CreateService(AnalysisOptions options)
  {
    var logger = LoggerFactory.Create(_ => { }).CreateLogger<AnalysisService>();
    return new AnalysisService(logger, options);
  }

  private static string GetSolutionPath()
  {
    var baseDir = AppContext.BaseDirectory;
    var path = Path.Combine(baseDir, "../../../../UsageAnalyzer.sln");
    return Path.GetFullPath(path);
  }

  private void RunCircularReferenceTest(string tempDir)
  {
    // Run the built executable directly
    var exePath = Path.Combine(tempDir, "bin", "Release", "net8.0", "CircularTest.dll");
    var runProcess = Process.Start(
        new ProcessStartInfo
        {
          FileName = "dotnet",
          Arguments = $"\"{exePath}\"",
          UseShellExecute = false,
          RedirectStandardOutput = true,
          RedirectStandardError = true,
        });

    var processCompleted = runProcess?.WaitForExit(2000); // 2-second timeout
    if (processCompleted == false)
    {
      // Process is likely stuck in infinite recursion - kill it
      runProcess?.Kill();
      runProcess?.WaitForExit(1000);
    }

    Assert.True(runProcess?.HasExited, "Process should have exited");

    var exitCode = runProcess?.ExitCode ?? 0;

    if (exitCode == 0)
    {
      return;
    }

    var runOutput = runProcess?.StandardOutput.ReadToEnd() ?? string.Empty;
    var runError = runProcess?.StandardError.ReadToEnd() ?? string.Empty;

    // Output process information for debugging
    testOutputHelper.WriteLine($"=== Separate Process Test Results ===");
    testOutputHelper.WriteLine($"Process Completed: {processCompleted}");
    testOutputHelper.WriteLine($"Process HasExited: {runProcess?.HasExited}");
    testOutputHelper.WriteLine($"Exit Code: {runProcess?.ExitCode}");
    testOutputHelper.WriteLine($"Standard Output: '{runOutput}'");
    testOutputHelper.WriteLine($"Standard Error: '{runError}'");
    testOutputHelper.WriteLine($"=====================================");
    Assert.Fail($"Process exited with exit code {exitCode}");
  }
}
