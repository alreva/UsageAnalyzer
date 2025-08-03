namespace Analyze.Tests;

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
    var tf = service.GetTargetFramework(solutionDir);
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
  public void GetTargetFramework_ReturnsNet80WhenFileMissing()
  {
    var service = CreateService();
    var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    Directory.CreateDirectory(temp);
    try
    {
      var framework = service.GetTargetFramework(temp);
      Assert.Equal("net8.0", framework);
    }
    finally
    {
      Directory.Delete(temp, true);
    }
  }

  [Fact]
  public void GetTargetFramework_ReadsValueFromProps()
  {
    var service = CreateService();
    var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    Directory.CreateDirectory(temp);
    const string buildProps = """
                              <Project>
                                <PropertyGroup>
                                  <TargetFramework>net7.0</TargetFramework>
                                </PropertyGroup>
                              </Project>
                              """;
    File.WriteAllText(Path.Combine(temp, "Directory.Build.props"), buildProps);
    try
    {
      var framework = service.GetTargetFramework(temp);
      Assert.Equal("net7.0", framework);
    }
    finally
    {
      Directory.Delete(temp, true);
    }
  }

  [Fact]
  public async Task AnalyzeUsageAsync_ReturnsUsageCounts()
  {
    var service = CreateService();
    var solutionPath = GetSolutionPath();
    var result = await service.AnalyzeUsageAsync(
        solutionPath,
        typeof(UserEventDto),
        true);

    var aggregated = result
        .GroupBy(r => r.Property.Attribute)
        .ToDictionary(g => g.Key, g => g.Sum(x => x.UsageCount));

    Assert.Equal(2, aggregated[new ClassAndField("Address", "ZipCode")]);
    Assert.Equal(2, aggregated[new ClassAndField("User", "FavoriteCategories")]);
    Assert.Equal(2, aggregated[new ClassAndField("ActivityLog", "ProductId")]);
    Assert.Equal(0, aggregated[new ClassAndField("User", "CreatedAt")]);
    Assert.Equal(1, aggregated[new ClassAndField("UserEventDto", "EventId")]);
  }
}
