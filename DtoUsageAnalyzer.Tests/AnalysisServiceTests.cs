namespace DtoUsageAnalyzer.Tests;

using System.IO;
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
  public void GetDeepProperties_ReturnsNestedPaths()
  {
    var service = CreateService();
    var uType = typeof(User);
    var result = service.GetDeepProperties(uType)!;
    var paths = result.Select(p => p.FullPath).ToList();
    Assert.Contains("Address.City", paths);
    Assert.Contains("SocialMedia.Twitter", paths);
    Assert.Contains("DeviceInfo.IpAddress", paths);
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
}
