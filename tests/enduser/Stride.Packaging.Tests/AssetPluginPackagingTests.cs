// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Stride.Packaging.Tests;

/// <summary>
/// End-to-end of the "ship assets + a dll, consume them from another project" workflow:
/// <c>dotnet pack</c> a user-authored asset plugin into a local feed, then <c>dotnet build</c>
/// a consumer that PackageReferences it and attaches the plugin's script type in a scene.
///
/// Positive: the packed plugin declares its asset assembly (stride/&lt;Id&gt;.sdpkg), so the
/// consumer's asset compiler loads it and resolves the scene's <c>!StrideAssetPlugin.SpinScript</c>
/// tag. Negative: packing with <c>StrideContainsAssetTypes=false</c> ships no sdpkg, so the assembly
/// never registers as an asset assembly and the consumer build fails to resolve the type — proving
/// the declaration is load-bearing, not incidental.
/// </summary>
[Collection("Packaging")]
public class AssetPluginPackagingTests
{
    private readonly ITestOutputHelper output;
    public AssetPluginPackagingTests(ITestOutputHelper output) => this.output = output;

    // The asset compiler emits this (as a warning, then substitutes IUnloadable and continues) when
    // a scene references a type whose assembly was not loaded for asset compilation.
    private const string UnresolvedSpinScript = "Unable to resolve tag [!StrideAssetPlugin.SpinScript,StrideAssetPlugin]";

    [Fact]
    public void ConsumerResolvesPluginTypeWhenAssetAssemblyDeclared()
    {
        var result = RunCase(declareAssetAssembly: true);
        Assert.True(result.ExitCode == 0, $"Consumer build should succeed (exit {result.ExitCode}).");
        Assert.DoesNotContain(UnresolvedSpinScript, result.Output);
    }

    [Fact]
    public void ConsumerFailsWhenAssetAssemblyNotDeclared()
    {
        var result = RunCase(declareAssetAssembly: false);
        // No declared asset assembly → the plugin dll never registers as an asset assembly, so the
        // scene's script type cannot resolve. (The compiler downgrades this to a warning + IUnloadable,
        // so assert on the diagnostic rather than the exit code.)
        Assert.Contains(UnresolvedSpinScript, result.Output);
    }

    /// <summary>
    /// Pack the plugin into an isolated feed, then build the consumer against it. Each case runs in
    /// its own temp tree with its own NuGet cache so the fixed 1.0.0 plugin package never collides.
    /// </summary>
    private ExecResult RunCase(bool declareAssetAssembly)
    {
        var version = TestEnvironment.ResolveStrideVersion();
        var fixtures = TestEnvironment.FixturesDir();

        var caseDir = Path.Combine(Path.GetTempPath(), "stride-packaging-tests",
            $"plugin-{(declareAssetAssembly ? "pos" : "neg")}-{Guid.NewGuid():N}");
        var pluginDir = Path.Combine(caseDir, "plugin");
        var consumerDir = Path.Combine(caseDir, "consumer");
        var feedDir = Path.Combine(caseDir, "feed");
        var nugetCache = Path.Combine(caseDir, "nuget");
        TestEnvironment.CopyDirectory(Path.Combine(fixtures, "Plugin"), pluginDir);
        TestEnvironment.CopyDirectory(Path.Combine(fixtures, "Consumer"), consumerDir);
        Directory.CreateDirectory(feedDir);

        // Plugin restores Stride.* from bin/packages.
        NuGetConsumerFeed.WriteStrictNuGetConfig(pluginDir);
        var packArgs = new List<string>
        {
            "pack", Path.Combine(pluginDir, "StrideAssetPlugin.csproj"),
            "-c", "Debug", "-v:m",
            $"-p:StrideEngineVersion={version}",
            $"-p:RestorePackagesPath={nugetCache}",
            "-o", feedDir,
        };
        if (!declareAssetAssembly)
            packArgs.Add("-p:StrideContainsAssetTypes=false");
        var pack = Dotnet.Exec(packArgs, pluginDir, output, timeoutMin: 10);
        Assert.True(pack.ExitCode == 0, $"Plugin pack failed with exit {pack.ExitCode}");

        // Consumer restores Stride.* from bin/packages and StrideAssetPlugin from the fresh feed.
        NuGetConsumerFeed.WriteStrictNuGetConfig(consumerDir,
            [new ExtraFeed("plugin-feed", feedDir, "StrideAssetPlugin")]);
        return ConsumerBuild.Run(Path.Combine(consumerDir, "Consumer.csproj"), consumerDir, output, timeoutMin: 10,
            $"-p:StrideEngineVersion={version}", $"-p:RestorePackagesPath={nugetCache}");
    }
}
