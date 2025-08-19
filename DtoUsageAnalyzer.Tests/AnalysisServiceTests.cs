namespace DtoUsageAnalyzer.Tests;

using System.IO;
using System.Reflection;
using Analyze;
using Dto;
using DtoUsageAnalyzer;
using Microsoft.Extensions.Logging;

public class AnalysisServiceTests
{
  private static AnalysisService CreateService()
  {
    ILogger<AnalysisService> logger = LoggerFactory.Create(builder => { }).CreateLogger<AnalysisService>();
    return new AnalysisService(logger);
  }

  private static AnalysisService CreateService(AnalysisOptions options)
  {
    ILogger<AnalysisService> logger = LoggerFactory.Create(builder => { }).CreateLogger<AnalysisService>();
    return new AnalysisService(logger, options);
  }

  private static string GetSolutionPath()
  {
    var baseDir = AppContext.BaseDirectory;
    var path = Path.Combine(baseDir, "../../../../UsageAnalyzer.sln");
    return Path.GetFullPath(path);
  }

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
    if (membersByPath.ContainsKey("ActivityLog.ProductId"))
    {
      Assert.True(membersByPath["ActivityLog.ProductId"].Member is FieldInfo);
    }

    if (membersByPath.ContainsKey("ActivityLog.ViewCount"))
    {
      Assert.True(membersByPath["ActivityLog.ViewCount"].Member is FieldInfo);
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
    // Use same exclusion patterns as the original test to maintain expected behavior
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
    Assert.Contains(new ClassAndField("Address", "Street"), allMembers);       // Init-only property
    Assert.Contains(new ClassAndField("Address", "City"), allMembers);         // Property
    Assert.Contains(new ClassAndField("Address", "Country"), allMembers);      // Init-only property
    Assert.Contains(new ClassAndField("DeviceInfo", "DeviceType"), allMembers); // Init-only property
    Assert.Contains(new ClassAndField("DeviceInfo", "Browser"), allMembers);   // Init-only property
    Assert.Contains(new ClassAndField("DeviceInfo", "Os"), allMembers);        // Property
    Assert.Contains(new ClassAndField("DeviceInfo", "IpAddress"), allMembers); // Property
    Assert.Contains(new ClassAndField("ActivityLog", "ProductId"), allMembers); // Field
    Assert.Contains(new ClassAndField("ActivityLog", "ViewCount"), allMembers); // Field

    // Verify mixed usage (some fields used, some not)
    var usedMembers = result.Where(r => r.UsageCount > 0).Select(r => r.Property.Attribute).ToHashSet();
    var unusedMembers = result.Where(r => r.UsageCount == 0).Select(r => r.Property.Attribute).ToHashSet();

    Assert.True(usedMembers.Count > 0, "Should have some used members");
    Assert.True(unusedMembers.Count > 0, "Should have some unused members");
  }
}
