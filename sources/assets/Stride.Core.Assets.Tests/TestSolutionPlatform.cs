// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Tests;

public class TestSolutionPlatform
{
    [Fact]
    public void TestConstructor()
    {
        var platform = new SolutionPlatform();

        Assert.NotNull(platform.PlatformsPart);
        Assert.NotNull(platform.DefineConstants);
        Assert.NotNull(platform.Templates);
    }

    [Fact]
    public void TestTypeProperty()
    {
        var platform = new SolutionPlatform { Type = PlatformType.Windows };

        Assert.Equal(PlatformType.Windows, platform.Type);
    }

    [Fact]
    public void TestIsAvailableProperty()
    {
        var platform = new SolutionPlatform { IsAvailable = true };

        Assert.True(platform.IsAvailable);
    }

    [Fact]
    public void TestDefineConstants()
    {
        var platform = new SolutionPlatform();
        platform.DefineConstants.Add("DEBUG");
        platform.DefineConstants.Add("TRACE");

        Assert.Equal(2, platform.DefineConstants.Count);
        Assert.Contains("DEBUG", platform.DefineConstants);
        Assert.Contains("TRACE", platform.DefineConstants);
    }

    [Fact]
    public void TestTemplates()
    {
        var platform = new SolutionPlatform();
        var template = new SolutionPlatformTemplate("Windows", "Windows Platform");

        platform.Templates.Add(template);

        Assert.Single(platform.Templates);
        Assert.Contains(template, platform.Templates);
    }

    [Fact]
    public void TestGetParts()
    {
        var platform = new SolutionPlatform { Name = "Windows" };
        var part = new SolutionPlatformPart("x64");
        platform.PlatformsPart.Add(part);

        var parts = platform.GetParts().ToList();

        Assert.NotEmpty(parts);
        Assert.Contains(platform, parts);
        Assert.Contains(part, parts);
    }

    [Fact]
    public void TestToString()
    {
        var platform = new SolutionPlatform { Type = PlatformType.Linux };

        var result = platform.ToString();

        Assert.Contains("SolutionPlatform", result);
        Assert.Contains("Linux", result);
    }
}

public class TestSolutionPlatformPart
{
    [Fact]
    public void TestDefaultConstructor()
    {
        var part = new SolutionPlatformPart();

        Assert.True(part.UseWithExecutables);
        Assert.True(part.UseWithLibraries);
        Assert.True(part.IncludeInSolution);
        Assert.NotNull(part.Configurations);
        Assert.Equal(2, part.Configurations.Count); // Debug and Release
    }

    [Fact]
    public void TestConstructorWithName()
    {
        var part = new SolutionPlatformPart("x64");

        Assert.Equal("x64", part.Name);
        Assert.True(part.UseWithExecutables);
        Assert.True(part.UseWithLibraries);
    }

    [Fact]
    public void TestNameProperty()
    {
        var part = new SolutionPlatformPart { Name = "ARM64" };

        Assert.Equal("ARM64", part.Name);
    }

    [Fact]
    public void TestSolutionNameProperty()
    {
        var part = new SolutionPlatformPart { Name = "Windows", SolutionName = "Win32" };

        Assert.Equal("Win32", part.SolutionName);
    }

    [Fact]
    public void TestSafeSolutionName()
    {
        var part1 = new SolutionPlatformPart { Name = "Windows", SolutionName = "Win32" };
        Assert.Equal("Win32", part1.SafeSolutionName);

        var part2 = new SolutionPlatformPart { Name = "Linux" };
        Assert.Equal("Linux", part2.SafeSolutionName);
    }

    [Fact]
    public void TestDisplayNameProperty()
    {
        var part = new SolutionPlatformPart { DisplayName = "Windows x64" };

        Assert.Equal("Windows x64", part.DisplayName);
    }

    [Fact]
    public void TestCpuProperty()
    {
        var part = new SolutionPlatformPart { Cpu = "x64" };

        Assert.Equal("x64", part.Cpu);
    }

    [Fact]
    public void TestAliasProperty()
    {
        var part = new SolutionPlatformPart { Alias = "AnyCPU" };

        Assert.Equal("AnyCPU", part.Alias);
    }

    [Fact]
    public void TestInheritConfigurationsProperty()
    {
        var part = new SolutionPlatformPart { InheritConfigurations = true };

        Assert.True(part.InheritConfigurations);
    }

    [Fact]
    public void TestUseWithExecutablesProperty()
    {
        var part = new SolutionPlatformPart { UseWithExecutables = false };

        Assert.False(part.UseWithExecutables);
    }

    [Fact]
    public void TestUseWithLibrariesProperty()
    {
        var part = new SolutionPlatformPart { UseWithLibraries = false };

        Assert.False(part.UseWithLibraries);
    }

    [Fact]
    public void TestIncludeInSolutionProperty()
    {
        var part = new SolutionPlatformPart { IncludeInSolution = false };

        Assert.False(part.IncludeInSolution);
    }

    [Fact]
    public void TestIsProjectHandled()
    {
        var part = new SolutionPlatformPart();

        Assert.True(part.IsProjectHandled(ProjectType.Executable));
        Assert.True(part.IsProjectHandled(ProjectType.Library));

        part.UseWithExecutables = false;
        Assert.False(part.IsProjectHandled(ProjectType.Executable));
        Assert.True(part.IsProjectHandled(ProjectType.Library));
    }

    [Fact]
    public void TestGetProjectNameForExecutable()
    {
        var part = new SolutionPlatformPart("Windows") { ExecutableProjectName = "WindowsExe" };

        var name = part.GetProjectName(ProjectType.Executable);

        Assert.Equal("WindowsExe", name);
    }

    [Fact]
    public void TestGetProjectNameForLibrary()
    {
        var part = new SolutionPlatformPart("Linux") { LibraryProjectName = "LinuxLib" };

        var name = part.GetProjectName(ProjectType.Library);

        Assert.Equal("LinuxLib", name);
    }

    [Fact]
    public void TestGetProjectNameWithAlias()
    {
        var part = new SolutionPlatformPart("Windows") { Alias = "AnyCPU" };

        var name = part.GetProjectName(ProjectType.Executable);

        Assert.Equal("AnyCPU", name);
    }

    [Fact]
    public void TestToString()
    {
        var part = new SolutionPlatformPart("x64");

        var result = part.ToString();

        Assert.Contains("SolutionPlatformPart", result);
        Assert.Contains("x64", result);
    }
}

public class TestSolutionConfiguration
{
    [Fact]
    public void TestConstructor()
    {
        var config = new SolutionConfiguration("Debug");

        Assert.Equal("Debug", config.Name);
        Assert.NotNull(config.Properties);
        Assert.Empty(config.Properties);
    }

    [Fact]
    public void TestIsDebugProperty()
    {
        var config = new SolutionConfiguration("Debug") { IsDebug = true };

        Assert.True(config.IsDebug);
    }

    [Fact]
    public void TestProperties()
    {
        var config = new SolutionConfiguration("Release");
        config.Properties.Add("Optimize=true");
        config.Properties.Add("DebugSymbols=false");

        Assert.Equal(2, config.Properties.Count);
        Assert.Contains("Optimize=true", config.Properties);
    }
}
