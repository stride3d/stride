// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Packages;
using Xunit;

namespace Stride.Launcher.Tests;

public sealed class DevRedirectStubTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "stride-dev-redirect-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(root))
            Directory.Delete(root, true);
    }

    // The props of a stub made by build/redirect-packages.cs, reduced to what the store reads
    private string WriteStub(string? configuration)
    {
        var package = Path.Combine(root, "packages", "stride.assetcompiler", "4.5.0-dev");
        Directory.CreateDirectory(Path.Combine(package, "build"));
        var configurationProperty = configuration is null ? "" : $"<StrideDevConfiguration Condition=\"'$(StrideDevConfiguration)' == ''\">{configuration}</StrideDevConfiguration>";
        File.WriteAllText(Path.Combine(package, "build", "Stride.AssetCompiler.props"), $"""
            <Project>
              <PropertyGroup>
                <StrideDevRoot Condition="'$(StrideDevRoot)' == ''">{Path.Combine(root, "checkout")}</StrideDevRoot>
                {configurationProperty}
              </PropertyGroup>
              <ItemGroup Condition="false">
                <Reference Include="Stride.AssetCompiler">
                  <StrideDevProjectDirectory>$(StrideDevRoot)/sources/assets/Stride.AssetCompiler</StrideDevProjectDirectory>
                </Reference>
              </ItemGroup>
            </Project>
            """);
        return package;
    }

    private string ProjectOutput(string configuration)
        => Path.Combine(root, "checkout", "sources", "assets", "Stride.AssetCompiler", "bin", configuration);

    [Theory]
    [InlineData("Release", "Release")]
    // Without the property, the stubs default to Debug
    [InlineData(null, "Debug")]
    public void TryGetDevRedirectOutputDirectory_IsTheCheckoutBuildOutput(string? configuration, string expectedConfiguration)
    {
        var package = WriteStub(configuration);
        Directory.CreateDirectory(ProjectOutput(expectedConfiguration));

        Assert.Equal(ProjectOutput(expectedConfiguration), NugetStore.TryGetDevRedirectOutputDirectory(package, "Stride.AssetCompiler"));
    }

    [Fact]
    public void TryGetDevRedirectOutputDirectory_NullWhenTheProjectIsNotBuilt()
    {
        var package = WriteStub("Debug");
        Directory.CreateDirectory(Path.Combine(root, "checkout", "sources", "assets", "Stride.AssetCompiler"));

        Assert.Null(NugetStore.TryGetDevRedirectOutputDirectory(package, "Stride.AssetCompiler"));
    }

    [Fact]
    public void GetRealPath_OfADevStubIsItsCheckoutProject()
    {
        var package = WriteStub("Debug");
        var project = Path.Combine(root, "checkout", "sources", "assets", "Stride.AssetCompiler");
        Directory.CreateDirectory(project);

        Assert.Equal(project, NugetStore.GetRealPath(package, "Stride.AssetCompiler", PackageVersion.Parse("4.5.0-dev")));
    }

    [Fact]
    public void GetRealPath_OfARealPackageIsItsFolder()
    {
        var package = Path.Combine(root, "packages", "stride.assetcompiler", "4.4.1");
        Directory.CreateDirectory(package);

        Assert.Equal(package, NugetStore.GetRealPath(package, "Stride.AssetCompiler", PackageVersion.Parse("4.4.1")));
    }

    [Fact]
    public void TryGetDevRedirectOutputDirectory_SkipsCommentedOutElements()
    {
        var package = WriteStub("Release");
        var props = Path.Combine(package, "build", "Stride.AssetCompiler.props");
        File.WriteAllText(props, File.ReadAllText(props).Replace("<PropertyGroup>",
            "<!-- <StrideDevConfiguration>Debug</StrideDevConfiguration> --><PropertyGroup>", StringComparison.Ordinal));
        Directory.CreateDirectory(ProjectOutput("Debug"));
        Directory.CreateDirectory(ProjectOutput("Release"));

        Assert.Equal(ProjectOutput("Release"), NugetStore.TryGetDevRedirectOutputDirectory(package, "Stride.AssetCompiler"));
    }

    [Fact]
    public void TryGetDevRedirectOutputDirectory_NullForARealPackage()
    {
        var package = Path.Combine(root, "packages", "stride.assetcompiler", "4.4.1");
        Directory.CreateDirectory(Path.Combine(package, "tools", "net10.0"));

        Assert.Null(NugetStore.TryGetDevRedirectOutputDirectory(package, "Stride.AssetCompiler"));
    }

    [Fact]
    public void GetDevRedirectAssemblies_ReadsTheReferencesOfTheStubAsXml()
    {
        var package = Path.Combine(root, "packages", "stride.native", "4.5.0-dev");
        Directory.CreateDirectory(Path.Combine(package, "build"));
        var assembly = Path.Combine(root, "checkout", "sources", "Stride.Native", "bin", "Release", "net10.0", "Stride.Native.dll");
        var commentedOut = Path.Combine(root, "checkout", "Old.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(assembly)!);
        File.WriteAllText(assembly, "");
        File.WriteAllText(commentedOut, "");
        // A comment, an attribute and a value on several lines, as an edited props file may have
        File.WriteAllText(Path.Combine(package, "build", "Stride.Native.props"), $"""
            <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
              <PropertyGroup>
                <StrideDevRedirect>true</StrideDevRedirect>
                <StrideDevRoot Condition="'$(StrideDevRoot)' == ''">{Path.Combine(root, "checkout")}</StrideDevRoot>
                <StrideDevConfiguration Condition="'$(StrideDevConfiguration)' == ''">
                  Release
                </StrideDevConfiguration>
              </PropertyGroup>
              <!-- <Reference Include="Old"><HintPath>$(StrideDevRoot)/Old.dll</HintPath></Reference> -->
              <ItemGroup Condition="false">
                <Reference Include="Stride.Native">
                  <HintPath Condition="true">$(StrideDevRoot)/sources/Stride.Native/bin/$(StrideDevConfiguration)/net10.0/Stride.Native.dll</HintPath>
                </Reference>
              </ItemGroup>
            </Project>
            """);

        Assert.Equal([assembly], NugetStore.GetDevRedirectAssemblies(package, "Stride.Native"));
    }
}
