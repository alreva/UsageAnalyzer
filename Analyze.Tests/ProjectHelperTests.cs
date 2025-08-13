namespace Analyze.Tests;

using System.IO;

public class ProjectHelperTests
{
  [Fact]
  public void GetTargetFramework_ReturnsNet80WhenFileMissing()
  {
    var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    Directory.CreateDirectory(temp);
    try
    {
      var framework = ProjectHelper.GetTargetFramework(temp);
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
      var framework = ProjectHelper.GetTargetFramework(temp);
      Assert.Equal("net7.0", framework);
    }
    finally
    {
      Directory.Delete(temp, true);
    }
  }
}
