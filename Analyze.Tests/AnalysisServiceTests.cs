using Analyze;
using Dto;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Analyze.Tests;

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
        var types = service.GetDtoAssemblyTypes(GetSolutionPath());
        Assert.Contains(types, t => t.Name == nameof(UserEventDto));
    }

    [Fact]
    public void IsNullable_DetectsNullableTypes()
    {
        var method = typeof(AnalysisService).GetMethod("IsNullable", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.True((bool)method.Invoke(null, new object[] { typeof(int?) })!);
        Assert.False((bool)method.Invoke(null, new object[] { typeof(int) })!);
    }

    [Fact]
    public void IsPrimitiveOrArrayOfPrimitives_ReturnsExpected()
    {
        var method = typeof(AnalysisService).GetMethod("IsPrimitiveOrArrayOfPrimitives", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.True((bool)method.Invoke(null, new object[] { typeof(int) })!);
        Assert.True((bool)method.Invoke(null, new object[] { typeof(string) })!);
        Assert.True((bool)method.Invoke(null, new object[] { typeof(decimal) })!);
        Assert.True((bool)method.Invoke(null, new object[] { typeof(DateTime) })!);
        Assert.True((bool)method.Invoke(null, new object[] { typeof(string[]) })!);
        Assert.True((bool)method.Invoke(null, new object[] { typeof(List<int>) })!);
        Assert.False((bool)method.Invoke(null, new object[] { typeof(DeviceInfo) })!);
    }

    [Fact]
    public void GetDeepProperties_ReturnsNestedPaths()
    {
        var uType = typeof(User);
        var result = AnalysisService.GetDeepProperties(uType)!;
        var paths = result.Select(p => p.FullPath).ToList();
        Assert.Contains("Address.City", paths);
        Assert.Contains("SocialMedia.Twitter", paths);
        Assert.Contains("DeviceInfo.IpAddress", paths);
    }

    [Fact]
    public void GetTargetFramework_ReturnsNet80WhenFileMissing()
    {
        var method = typeof(AnalysisService).GetMethod("GetTargetFramework", BindingFlags.NonPublic | BindingFlags.Static)!;
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(temp);
        try
        {
            var framework = (string)method.Invoke(null, new object[] { temp })!;
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
        var method = typeof(AnalysisService).GetMethod("GetTargetFramework", BindingFlags.NonPublic | BindingFlags.Static)!;
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(temp);
        File.WriteAllText(Path.Combine(temp, "Directory.Build.props"), """
<Project>
  <PropertyGroup>
    <TargetFramework>net7.0</TargetFramework>
  </PropertyGroup>
</Project>
""");
        try
        {
            var framework = (string)method.Invoke(null, new object[] { temp })!;
            Assert.Equal("net7.0", framework);
        }
        finally
        {
            Directory.Delete(temp, true);
        }
    }
}
