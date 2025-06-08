using System.Diagnostics;
using Analyze;

namespace Analyze.Tests;

public class AnalysisServiceTests
{
    private static string GetTargetFramework()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory!, "../../../../"));
        var propsPath = Path.Combine(repoRoot, "Directory.Build.props");
        if (!File.Exists(propsPath))
        {
            return "net8.0";
        }

        var doc = System.Xml.Linq.XDocument.Load(propsPath);
        var tf = doc.Descendants("TargetFramework").FirstOrDefault();
        return tf?.Value ?? "net8.0";
    }

    private static void BuildDto()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory!, "../../../../"));
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "build Dto/Dto.csproj -c Debug",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });
        process!.WaitForExit();
    }

    [Fact]
    public void GetDtoAssemblyTypes_ReturnsUserEventDtoType()
    {
        BuildDto();
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<AnalysisService>.Instance;
        var service = new AnalysisService(logger);
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory!, "../../../../"));
        var solutionPath = Path.Combine(repoRoot, "UsageAnalyzer.sln");
        var types = service.GetDtoAssemblyTypes(solutionPath);
        Assert.Contains(types, t => t.FullName == "Dto.UserEventDto");
    }

    [Fact]
    public async Task AnalyzeUsageAsync_FindsPropertyUsage()
    {
        BuildDto();
        using var temp = new TempSolution();
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<AnalysisService>.Instance;
        var service = new AnalysisService(logger);
        var results = await service.AnalyzeUsageAsync(temp.SolutionPath, typeof(Dto.UserEventDto), false);
        var expectedKey = new UsageKey(temp.FilePath, new ClassAndField("User", "UserId"));
        Assert.True(results.TryGetValue(expectedKey, out var count) && count == 1);
        Assert.Contains(results, kvp => kvp.Key.Attribute.FieldName == "Email" && kvp.Value == 0);
    }

    private sealed class TempSolution : IDisposable
    {
        public string SolutionDir { get; }
        public string SolutionPath { get; }
        public string FilePath { get; }

        public TempSolution()
        {
            SolutionDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(SolutionDir);
            var projectPath = Path.Combine(SolutionDir, "TestProj.csproj");
            var tf = GetTargetFramework();
            var dtoDll = Path.Combine(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory!, "../../../../")), "Dto", "bin", "Debug", tf, "Dto.dll");
            File.WriteAllText(projectPath, @$"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>{tf}</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""Dto"">
      <HintPath>{dtoDll}</HintPath>
    </Reference>
  </ItemGroup>
</Project>");
            var targetDllPath = Path.Combine(SolutionDir, "Dto", "bin", "Debug", tf, "Dto.dll");
            Directory.CreateDirectory(Path.GetDirectoryName(targetDllPath)!);
            File.Copy(dtoDll, targetDllPath, true);
            FilePath = Path.Combine(SolutionDir, "Program.cs");
            File.WriteAllText(FilePath, "using Dto; public class P { void M(UserEventDto dto) { var id = dto.User.UserId; } }");
            SolutionPath = Path.Combine(SolutionDir, "Test.sln");
            File.WriteAllText(SolutionPath, $"Microsoft Visual Studio Solution File, Format Version 12.00\n# Visual Studio Version 17\nProject(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"TestProj\", \"TestProj.csproj\", \"{{{Guid.NewGuid()}}}\"\nEndProject\nGlobal\nEndGlobal");
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(SolutionDir, true);
            }
            catch { }
        }
    }
}
