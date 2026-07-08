// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Stride.Packaging.Tests;

/// <summary>
/// <c>dotnet pack</c> a user-authored asset plugin into a local feed, then <c>dotnet build</c> a
/// consumer game (real shape: executable + .Game library carrying the sdpkg) that PackageReferences
/// it and attaches the plugin's script type in a scene. With the plugin's asset-assembly declaration
/// (stride/&lt;Id&gt;.sdpkg) the type resolves; without it the scene tag fails to resolve.
///
/// The consumer's root assets include a UI page whose default button references engine assets —
/// regression guard for #3258 (bundle packing must resolve assets of a referenced project's package
/// from lock-file-loaded dependencies; a single-project consumer does not reproduce it).
///
/// The plugin is namespaced (StrideAssetNamespace=true) and ships a root asset of its own, so the
/// consumer build must compile it under its rooted /StrideAssetPlugin/ URL and resolve its
/// engine-asset references through the plugin package's own dependency closure.
/// </summary>
[Collection("Packaging")]
public class AssetPluginPackagingTests
{
    private readonly ITestOutputHelper output;
    public AssetPluginPackagingTests(ITestOutputHelper output) => this.output = output;

    // Warning emitted when a scene references a type whose assembly was not loaded for asset compilation.
    private const string UnresolvedSpinScript = "Unable to resolve tag [!StrideAssetPlugin.SpinScript,StrideAssetPlugin]";

    [Fact]
    public void ConsumerResolvesPluginTypeWhenAssetAssemblyDeclared()
    {
        var (result, consumerDir) = RunCase(declareAssetAssembly: true);
        Assert.True(result.ExitCode == 0, $"Consumer build should succeed (exit {result.ExitCode}).");
        Assert.DoesNotContain(UnresolvedSpinScript, result.Output);

        // The namespaced plugin's root asset compiles under its rooted URL; the consumer's own
        // package is not namespaced, so its assets stay bare.
        var dbDir = Path.Combine(consumerDir, "obj", "stride", "assetbuild", "data", "db");
        var index = File.ReadAllText(Directory.GetFiles(dbDir, "index.Consumer.*")[0]);
        Assert.Matches(@"(?m)^/StrideAssetPlugin/PluginPage ", index);
        Assert.Matches(@"(?m)^Page ", index);

        // The consumer's StrideAssetNamespaceUsings brings the plugin namespace into scope: the
        // deployed alias table maps its bare URL to the canonical one.
        var aliases = File.ReadAllText(Path.Combine(consumerDir, "bin", "Debug", "net10.0", "data", "db", "aliases"));
        Assert.Contains("PluginPage|/StrideAssetPlugin/PluginPage", aliases);
    }

    [Fact]
    public void ConsumerFailsWhenAssetAssemblyNotDeclared()
    {
        var (result, _) = RunCase(declareAssetAssembly: false);
        // Unresolved type is only a warning (IUnloadable substituted), so assert on the diagnostic
        // rather than the exit code.
        Assert.Contains(UnresolvedSpinScript, result.Output);
    }

    /// <summary>
    /// Pack the plugin into an isolated feed, then build the consumer against it. Each case runs in
    /// its own temp tree with its own NuGet cache so the fixed 1.0.0 plugin package never collides.
    /// </summary>
    private (ExecResult Result, string ConsumerDir) RunCase(bool declareAssetAssembly)
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
        return (ConsumerBuild.Run(Path.Combine(consumerDir, "Consumer.csproj"), consumerDir, output, timeoutMin: 10,
            $"-p:StrideEngineVersion={version}", $"-p:RestorePackagesPath={nugetCache}"), consumerDir);
    }
}
