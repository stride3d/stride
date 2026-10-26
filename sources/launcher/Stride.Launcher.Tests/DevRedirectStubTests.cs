// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

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

    // The props of a stub made by build/GenerateDevPackages.cs, reduced to what the store reads
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
    public void TryGetDevRedirectOutputDirectory_NullForARealPackage()
    {
        var package = Path.Combine(root, "packages", "stride.assetcompiler", "4.4.1");
        Directory.CreateDirectory(Path.Combine(package, "tools", "net10.0"));

        Assert.Null(NugetStore.TryGetDevRedirectOutputDirectory(package, "Stride.AssetCompiler"));
    }
}
