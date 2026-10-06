// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stride.Core.Assets;
using Stride.Core.Assets.Templates;
using Xunit;
using Xunit.Abstractions;

namespace Stride.Packaging.Tests;

/// <summary>
/// A runtime plugin package records its companion packages (id, version, kind); a game references the runtime only,
/// and sessions load the companions from that record.
/// </summary>
[Collection("Packaging")]
public class CompanionPackageTests
{
    private readonly ITestOutputHelper output;
    public CompanionPackageTests(ITestOutputHelper output) => this.output = output;

    private const string AssetsPackageId = "StrideAssetPlugin.Assets";
    private const string CustomAssetsPackageId = "StrideAssetPlugin.CustomAssets";
    private const string ExtAssetsPackageId = "AcmeSpinPlugin.Assets";
    private static readonly Guid SpinTemplateId = new("9C2B6F1E-4D3A-4E5B-8C7D-0A1B2C3D4E5F");

    [Fact]
    public void AssetsPackageRestoredFromFeed()
    {
        using var c = new Case(output, "assets-feed");
        c.DeclareAssetsCompanion();
        c.PackPlugin();
        c.PackAssets();
        c.AddSpinAsset();

        var result = c.BuildConsumer();
        Assert.True(result.ExitCode == 0, $"Consumer build should succeed (exit {result.ExitCode}).");
        Assert.DoesNotContain("is referenced directly", result.Output);

        // The Assets package is a dependency of the session and ships the plugin's template
        var session = c.LoadConsumerSession();
        c.AssertContentCompiled("/Consumer/Spin");
        var assets = Assert.Single(session.Packages, p => p.Meta.Name == AssetsPackageId);
        Assert.True(((StandalonePackage)assets.Container).IsDependencyPackage);
        Assert.Equal("1.0.0", assets.Meta.Version.ToString());
        Assert.Equal(PackageKind.Assets, assets.Kind);
        Assert.Contains(TemplateManager.FindTemplates(TemplateScope.Asset, session), t => t.Id == SpinTemplateId);

        // The runtime's packed declaration carries the kind read from the companion project
        var plugin = Assert.Single(session.Packages, p => p.Meta.Name == "StrideAssetPlugin");
        var declaration = Assert.Single(plugin.CompanionPackages);
        Assert.Equal(AssetsPackageId, declaration.Name);
        Assert.Equal(PackageKind.Assets, declaration.Kind);

        // The project-based load (the editor's path) resolves it too
        var projectSession = c.LoadConsumerProjectSession();
        Assert.Contains(projectSession.Packages, p => p.Meta.Name == AssetsPackageId);
        Assert.Contains(TemplateManager.FindTemplates(TemplateScope.Asset, projectSession), t => t.Id == SpinTemplateId);
    }

    [Fact]
    public void MissingAssetsPackageFailsTheBuild()
    {
        using var c = new Case(output, "assets-missing");
        c.DeclareAssetsCompanion();
        c.PackPlugin();
        c.AddSpinAsset();

        var result = c.BuildConsumer();
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains($"Assets package [{AssetsPackageId}] version [1.0.0]", result.Output);
    }

    [Fact]
    public void AssetsPackageVersionMustMatchTheDeclaredVersion()
    {
        using var c = new Case(output, "assets-version");
        c.DeclareAssetsCompanion();
        c.PackPlugin();
        c.PackAssets(version: "1.0.1");
        c.AddSpinAsset();

        // The runtime was packed with the Assets project at 1.0.0: only that version satisfies the restore
        var result = c.BuildConsumer();
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains($"Assets package [{AssetsPackageId}] version [1.0.0]", result.Output);
    }

    [Fact]
    public void AssetsPackageCarriesItsOwnVersion()
    {
        using var c = new Case(output, "assets-own-version");
        c.DeclareAssetsCompanion();
        // The runtime (1.0.0) records the version of the Assets project it is packed with (1.0.1)
        c.PackPlugin(extraArg: "-p:PluginAssetsVersion=1.0.1");
        c.PackAssets(version: "1.0.1");
        c.AddSpinAsset();

        var result = c.BuildConsumer();
        Assert.True(result.ExitCode == 0, $"Consumer build should succeed (exit {result.ExitCode}).");
        c.AssertContentCompiled("/Consumer/Spin");
        // Session without assembly loading (the feed case already loads the plugin assemblies in this process)
        var session = c.LoadConsumerProjectSession();
        var assets = Assert.Single(session.Packages, p => p.Meta.Name == AssetsPackageId);
        Assert.Equal("1.0.1", assets.Meta.Version.ToString());
    }

    [Fact]
    public void CompanionProjectWithoutKindFailsThePack()
    {
        using var c = new Case(output, "assets-no-kind");
        c.DeclareAssetsCompanion();
        c.RemoveAssetsKind();

        // The companion must state what it carries: the runtime's pack cannot record a kind for it
        var result = c.TryPackPlugin();
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("must declare StridePackageKind", result.Output);
    }

    [Fact]
    public void DirectAssetsPackageReferenceWarns()
    {
        using var c = new Case(output, "assets-direct");
        c.DeclareAssetsCompanion();
        c.PackPlugin();
        c.PackAssets();
        c.AddSpinAsset();
        c.ReferenceAssetsPackageDirectly();

        var result = c.BuildConsumer();
        Assert.True(result.ExitCode == 0, $"Consumer build should succeed (exit {result.ExitCode}).");
        Assert.Contains($"[{AssetsPackageId}] is referenced directly", result.Output);
        c.AssertContentCompiled("/Consumer/Spin");
    }

    [Fact]
    public void AssetsPackageDependsOnAnotherPluginsAssetsPackage()
    {
        using var c = new Case(output, "assets-plugin-on-plugin");
        c.DeclareAssetsCompanion();
        c.PackPlugin();
        c.PackAssets();
        c.PackExtensionPlugin();
        c.ReferenceExtensionPluginOnly();
        c.AddDoubleSpinAsset();

        // StrideAssetPlugin.Assets comes as a companion and as a dependency of AcmeSpinPlugin.Assets, which the restore reads first
        var result = c.BuildConsumer();
        Assert.True(result.ExitCode == 0, $"Consumer build should succeed (exit {result.ExitCode}).");
        Assert.DoesNotContain("is referenced directly", result.Output);
        c.AssertContentCompiled("/Consumer/DoubleSpin");

        var session = c.LoadConsumerProjectSession();
        foreach (var id in new[] { AssetsPackageId, ExtAssetsPackageId })
        {
            var assets = Assert.Single(session.Packages, p => p.Meta.Name == id);
            Assert.True(((StandalonePackage)assets.Container).IsCompanionPackage, $"{id} should be loaded as a companion.");
        }
    }

    [Fact]
    public void AssetsPackageViaProjectReference()
    {
        // In-solution plugins: the game references both projects; the Assets project is found in the
        // session (no restore, no warning)
        using var c = new Case(output, "assets-project");
        c.DeclareAssetsCompanion();
        c.UseProjectReferences();
        c.AddSpinAsset();

        var result = c.BuildConsumer();
        Assert.True(result.ExitCode == 0, $"Consumer build should succeed (exit {result.ExitCode}).");
        Assert.DoesNotContain("is referenced directly", result.Output);
        Assert.DoesNotContain("Could not restore package", result.Output);
        c.AssertContentCompiled("/Consumer/Spin");
    }

    [Fact]
    public void PluginProjectsInSolution()
    {
        // The game references the plugin's runtime project only: the executable builds the Assets project, the game does not ship it
        using var c = new Case(output, "assets-solution");
        c.DeclareAssetsCompanion();
        c.ReferenceRuntimeProject();
        c.WriteSolution();
        c.AddSpinAsset();

        var result = c.BuildConsumer();
        Assert.True(result.ExitCode == 0, $"Consumer build should succeed (exit {result.ExitCode}).");
        Assert.DoesNotContain("was not found", result.Output);
        Assert.DoesNotContain("Could not restore package", result.Output);
        Assert.DoesNotContain("is referenced directly", result.Output);
        c.AssertContentCompiled("/Consumer/Spin");
        Assert.False(File.Exists(Path.Combine(c.ConsumerBinDir, AssetsPackageId + ".dll")), "The Assets companion must not ship with the game.");

        // The executable's manifest lists the companion's; the companion joins the session at the project's version
        var session = c.LoadConsumerExecutableSession();
        var assets = Assert.Single(session.Packages, p => p.Meta.Name == AssetsPackageId);
        Assert.Equal(PackageKind.Assets, assets.Kind);
        Assert.False(((StandalonePackage)assets.Container).IsDependencyPackage);
        Assert.Contains(TemplateManager.FindTemplates(TemplateScope.Asset, session), t => t.Id == SpinTemplateId);

        // The editor's path: the solution's project is the companion, nothing to restore
        var solutionSession = c.LoadConsumerSolutionSession();
        var project = Assert.Single(solutionSession.Packages, p => p.Meta.Name == AssetsPackageId);
        Assert.IsType<SolutionProject>(project.Container);
        var plugin = Assert.Single(solutionSession.Packages, p => p.Meta.Name == "StrideAssetPlugin");
        var declaration = Assert.Single(plugin.CompanionPackages);
        Assert.Equal(AssetsPackageId, declaration.Name);
        Assert.Equal(PackageKind.Assets, declaration.Kind);
    }

    [Fact]
    public void GameReplacesTheAssetsCompanion()
    {
        // The runtime declares StrideAssetPlugin.Assets, which is never packed into the feed: the game declares a
        // companion project of its own that replaces it, so the declared one is neither restored nor loaded
        using var c = new Case(output, "assets-replaced");
        c.DeclareAssetsCompanion();
        c.PackPlugin();
        c.AddCustomAssetsProject();
        c.AddSpinAsset();

        var result = c.BuildConsumer();
        Assert.True(result.ExitCode == 0, $"Consumer build should succeed (exit {result.ExitCode}).");
        Assert.DoesNotContain("was not found", result.Output);
        Assert.DoesNotContain("Could not restore package", result.Output);
        c.AssertContentCompiled("/Consumer/Spin");

        // The editor's path reads the game's declaration from its build manifest
        var session = c.LoadConsumerProjectSession();
        Assert.Contains(session.Packages, p => p.Meta.Name == CustomAssetsPackageId);
        Assert.DoesNotContain(session.Packages, p => p.Meta.Name == AssetsPackageId);
        var game = Assert.Single(session.Packages, p => p.Meta.Name == "Consumer.Game");
        var declaration = Assert.Single(game.CompanionPackages);
        Assert.Equal(CustomAssetsPackageId, declaration.Name);
        Assert.Equal(PackageKind.Assets, declaration.Kind);
        Assert.Equal(new[] { AssetsPackageId }, declaration.Replaces);
    }

    /// <summary>
    /// One temp tree per case: the plugin packed into a feed and a consumer built against it.
    /// Old copies of the fixed-version packages are deleted from the global packages folder first.
    /// </summary>
    private sealed class Case : IDisposable
    {
        private readonly ITestOutputHelper output;
        private readonly List<PackageSession> sessions = [];
        private readonly string version;
        private readonly string caseDir;
        private readonly string pluginDir;
        private readonly string assetsDir;
        private readonly string consumerDir;
        private readonly string feedDir;
        private readonly string nugetCache;

        public Case(ITestOutputHelper output, string name)
        {
            this.output = output;
            version = TestEnvironment.ResolveStrideVersion();
            var fixtures = TestEnvironment.FixturesDir();
            caseDir = Path.Combine(Path.GetTempPath(), "stride-packaging-tests", $"{name}-{Guid.NewGuid():N}");
            pluginDir = Path.Combine(caseDir, "Plugin");
            assetsDir = Path.Combine(caseDir, "PluginAssets");
            consumerDir = Path.Combine(caseDir, "consumer");
            feedDir = Path.Combine(caseDir, "feed");
            nugetCache = Path.Combine(caseDir, "nuget");
            TestEnvironment.CopyDirectory(Path.Combine(fixtures, "Plugin"), pluginDir);
            TestEnvironment.CopyDirectory(Path.Combine(fixtures, "PluginAssets"), assetsDir);
            TestEnvironment.CopyDirectory(Path.Combine(fixtures, "Consumer"), consumerDir);
            Directory.CreateDirectory(feedDir);
            NuGetConsumerFeed.WriteStrictNuGetConfig(pluginDir);
            NuGetConsumerFeed.WriteStrictNuGetConfig(assetsDir);

            var globalPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
            foreach (var id in new[] { "strideassetplugin", "strideassetplugin.assets", "acmespinplugin", "acmespinplugin.assets" })
            {
                var dir = Path.Combine(globalPackages, id);
                if (Directory.Exists(dir))
                    Directory.Delete(dir, recursive: true);
            }
        }

        private const string PluginReference = """<PackageReference Include="StrideAssetPlugin" Version="1.0.0" />""";
        private const string Indent = "\n    ";
        private string PluginProject => Path.Combine(pluginDir, "StrideAssetPlugin.csproj");
        private string AssetsProject => Path.Combine(assetsDir, "StrideAssetPlugin.Assets.csproj");
        private string GameProject => Path.Combine(consumerDir, "Consumer.Game", "Consumer.Game.csproj");
        private string GamePackage => Path.Combine(consumerDir, "Consumer.Game", "Consumer.Game.sdpkg");

        public void DeclareAssetsCompanion()
        {
            const string engineReference = """<PackageReference Include="Stride.Engine" Version="$(StrideEngineVersion)" />""";
            File.WriteAllText(PluginProject, File.ReadAllText(PluginProject).Replace(engineReference,
                """<StrideCompanionProject Include="..\PluginAssets\StrideAssetPlugin.Assets.csproj" />""" + Indent + engineReference));
            Assert.Contains("StrideCompanionProject", File.ReadAllText(PluginProject));
        }

        public void RemoveAssetsKind()
        {
            File.WriteAllText(AssetsProject, File.ReadAllText(AssetsProject).Replace("<StridePackageKind>Assets</StridePackageKind>", ""));
            Assert.DoesNotContain("StridePackageKind", File.ReadAllText(AssetsProject));
        }

        public void PackPlugin(string? extraArg = null)
        {
            var pack = TryPackPlugin(extraArg);
            Assert.True(pack.ExitCode == 0, $"Pack of the plugin failed with exit {pack.ExitCode}");
        }

        public ExecResult TryPackPlugin(string? extraArg = null) => Pack(PluginProject, pluginDir, extraArg);

        public void PackAssets(string? version = null)
        {
            var pack = Pack(AssetsProject, assetsDir, version is not null ? $"-p:PluginAssetsVersion={version}" : null);
            Assert.True(pack.ExitCode == 0, $"Pack of the Assets project failed with exit {pack.ExitCode}");
        }

        private ExecResult Pack(string project, string workingDir, string? extraArg = null)
        {
            var args = new List<string> { "pack", project, "-c", "Debug", "-v:m", $"-p:StrideEngineVersion={version}", $"-p:RestorePackagesPath={nugetCache}", "-o", feedDir };
            if (extraArg is not null)
                args.Add(extraArg);
            return Dotnet.Exec(args, workingDir, output, timeoutMin: 10);
        }

        public void AddSpinAsset()
        {
            File.WriteAllText(Path.Combine(consumerDir, "Consumer.Game", "Assets", "Spin.sdspin"), """
                !SpinAsset
                Id: 5d0c1f2e-3a4b-4c5d-8e6f-7a8b9c0d1e2f
                SerializedVersion: {StrideAssetPlugin: 1.0.0.0}
                Tags: []
                Speed: 3.0
                """);
            AddRootAsset("5d0c1f2e-3a4b-4c5d-8e6f-7a8b9c0d1e2f:Spin");
        }

        /// <summary>
        /// Packs AcmeSpinPlugin, a plugin built on this one: its runtime references StrideAssetPlugin, its Assets
        /// companion references StrideAssetPlugin.Assets (both from the feed). Pack this plugin first.
        /// </summary>
        public void PackExtensionPlugin()
        {
            var extDir = Path.Combine(caseDir, "PluginExt");
            var extAssetsDir = Path.Combine(caseDir, "PluginExtAssets");
            TestEnvironment.CopyDirectory(Path.Combine(TestEnvironment.FixturesDir(), "PluginExt"), extDir);
            TestEnvironment.CopyDirectory(Path.Combine(TestEnvironment.FixturesDir(), "PluginExtAssets"), extAssetsDir);
            NuGetConsumerFeed.WriteStrictNuGetConfig(extDir, [new ExtraFeed("plugin-feed", feedDir, "StrideAssetPlugin")]);
            NuGetConsumerFeed.WriteStrictNuGetConfig(extAssetsDir, [new ExtraFeed("plugin-feed", feedDir, "StrideAssetPlugin", "StrideAssetPlugin.Assets")]);

            var pack = Pack(Path.Combine(extDir, "AcmeSpinPlugin.csproj"), extDir);
            Assert.True(pack.ExitCode == 0, $"Pack of the extension plugin failed with exit {pack.ExitCode}");
            pack = Pack(Path.Combine(extAssetsDir, "AcmeSpinPlugin.Assets.csproj"), extAssetsDir);
            Assert.True(pack.ExitCode == 0, $"Pack of the extension's Assets project failed with exit {pack.ExitCode}");
        }

        public void ReferenceExtensionPluginOnly()
        {
            File.WriteAllText(GameProject, File.ReadAllText(GameProject).Replace(PluginReference,
                """<PackageReference Include="AcmeSpinPlugin" Version="1.0.0" />"""));
        }

        public void AddDoubleSpinAsset()
        {
            File.WriteAllText(Path.Combine(consumerDir, "Consumer.Game", "Assets", "DoubleSpin.sddspin"), """
                !DoubleSpinAsset
                Id: 6e1d2a3f-4b5c-4d6e-9f7a-8b9c0d1e2f3a
                SerializedVersion: {AcmeSpinPlugin: 1.0.0.0}
                Tags: []
                Speed: 6.0
                """);
            AddRootAsset("6e1d2a3f-4b5c-4d6e-9f7a-8b9c0d1e2f3a:DoubleSpin");
        }

        public void ReferenceAssetsPackageDirectly()
        {
            File.WriteAllText(GameProject, File.ReadAllText(GameProject).Replace(PluginReference,
                PluginReference + Indent + """<PackageReference Include="StrideAssetPlugin.Assets" Version="1.0.0" />"""));
        }

        public void UseProjectReferences()
        {
            File.WriteAllText(GameProject, File.ReadAllText(GameProject).Replace(PluginReference,
                """<ProjectReference Include="..\..\Plugin\StrideAssetPlugin.csproj" />""" + Indent +
                """<ProjectReference Include="..\..\PluginAssets\StrideAssetPlugin.Assets.csproj" />"""));
        }

        /// <summary>The game references the plugin's runtime project alone; the companion is not referenced.</summary>
        public void ReferenceRuntimeProject()
        {
            File.WriteAllText(GameProject, File.ReadAllText(GameProject).Replace(PluginReference,
                """<ProjectReference Include="..\..\Plugin\StrideAssetPlugin.csproj" />"""));
        }

        /// <summary>A solution holding the game, its executable and the plugin's projects, as an in-solution plugin's.</summary>
        public void WriteSolution()
        {
            File.WriteAllText(Path.Combine(consumerDir, "Consumer.slnx"), """
                <Solution>
                  <Project Path="Consumer.csproj" />
                  <Project Path="Consumer.Game/Consumer.Game.csproj" />
                  <Project Path="../Plugin/StrideAssetPlugin.csproj" />
                  <Project Path="../PluginAssets/StrideAssetPlugin.Assets.csproj" />
                </Solution>
                """);
        }

        public string ConsumerBinDir => Path.Combine(consumerDir, "bin", "Debug", "net10.0");

        /// <summary>
        /// A copy of the Assets fixture under another id, taking the plugin from the feed: the game references it
        /// and declares it as replacing the Assets package the plugin declares.
        /// </summary>
        public void AddCustomAssetsProject()
        {
            var customDir = Path.Combine(caseDir, "PluginCustomAssets");
            TestEnvironment.CopyDirectory(Path.Combine(TestEnvironment.FixturesDir(), "PluginAssets"), customDir);
            File.Move(Path.Combine(customDir, "StrideAssetPlugin.Assets.csproj"), Path.Combine(customDir, "StrideAssetPlugin.CustomAssets.csproj"));
            File.Move(Path.Combine(customDir, "StrideAssetPlugin.Assets.sdpkg"), Path.Combine(customDir, "StrideAssetPlugin.CustomAssets.sdpkg"));
            foreach (var file in new[] { "StrideAssetPlugin.CustomAssets.csproj", "StrideAssetPlugin.CustomAssets.sdpkg" })
            {
                var path = Path.Combine(customDir, file);
                File.WriteAllText(path, File.ReadAllText(path)
                    .Replace(AssetsPackageId, CustomAssetsPackageId)
                    .Replace("""<ProjectReference Include="..\Plugin\StrideAssetPlugin.csproj" />""", PluginReference));
            }
            NuGetConsumerFeed.WriteStrictNuGetConfig(customDir, [new ExtraFeed("plugin-feed", feedDir, "StrideAssetPlugin")]);

            File.WriteAllText(GameProject, File.ReadAllText(GameProject).Replace(PluginReference,
                PluginReference + Indent +
                """<ProjectReference Include="..\..\PluginCustomAssets\StrideAssetPlugin.CustomAssets.csproj" />""" + Indent +
                """<StrideCompanionProject Include="..\..\PluginCustomAssets\StrideAssetPlugin.CustomAssets.csproj" Replaces="StrideAssetPlugin.Assets" />"""));
        }

        private void AddRootAsset(string reference)
        {
            File.AppendAllText(GamePackage, $"    - {reference}\n");
        }

        public ExecResult BuildConsumer()
        {
            NuGetConsumerFeed.WriteStrictNuGetConfig(consumerDir, [new ExtraFeed("plugin-feed", feedDir, "StrideAssetPlugin", "StrideAssetPlugin.Assets", "AcmeSpinPlugin", "AcmeSpinPlugin.Assets")]);
            return ConsumerBuild.Run(Path.Combine(consumerDir, "Consumer.csproj"), consumerDir, output, timeoutMin: 10,
                $"-p:StrideEngineVersion={version}", $"-p:RestorePackagesPath={nugetCache}");
        }

        /// <summary>The asset is in the compiled index and loads from the deployed database.</summary>
        public void AssertContentCompiled(string url)
        {
            var dbDir = Path.Combine(consumerDir, "obj", "stride", "assetbuild", "data", "db");
            var index = File.ReadAllText(Directory.GetFiles(dbDir, "index.Consumer.*").Single(f => !f.EndsWith(".closure")));
            Assert.Matches($@"(?m)^{url} ", index);

            if (!OperatingSystem.IsWindows())
                return;
            var binDir = Path.Combine(consumerDir, "bin", "Debug", "net10.0");
            var run = Dotnet.Exec(["exec", Path.Combine(binDir, "Consumer.dll"), url], binDir, output, timeoutMin: 2);
            Assert.True(run.ExitCode == 0, $"Consumer runtime content check failed (exit {run.ExitCode}).");
            Assert.Contains($"CONTENT OK {url}", run.Output);
        }

        /// <summary>Headless session over the built consumer's build manifest, the asset compiler's path.</summary>
        public PackageSession LoadConsumerSession()
        {
            PackageSessionPublicHelper.FindAndSetMSBuildVersion();
            var manifest = Path.Combine(consumerDir, "Consumer.Game", "obj", "Debug", "net10.0", "Consumer.Game.sdbuild");
            var result = new PackageSessionResult();
            PackageSession.LoadFromBuildManifest(manifest, result);
            return CheckSession(result);
        }

        /// <summary>
        /// Headless session over the executable's build manifest, the asset compiler's path.
        /// It loads no assemblies: a second copy of the plugin's in the test process breaks its serializers.
        /// </summary>
        public PackageSession LoadConsumerExecutableSession()
        {
            PackageSessionPublicHelper.FindAndSetMSBuildVersion();
            var manifest = Path.Combine(consumerDir, "obj", "Debug", "net10.0", "Consumer.sdbuild");
            var result = new PackageSessionResult();
            PackageSession.LoadFromBuildManifest(manifest, result, new PackageLoadParameters { LoadAssemblyReferences = false, AutoLoadTemporaryAssets = false });
            return CheckSession(result);
        }

        /// <summary>Headless session over the consumer's solution (see <see cref="WriteSolution"/>), the editor's path.</summary>
        public PackageSession LoadConsumerSolutionSession()
        {
            PackageSessionPublicHelper.FindAndSetMSBuildVersion();
            var result = new PackageSessionResult();
            PackageSession.Load(Path.Combine(consumerDir, "Consumer.slnx"), result, new PackageLoadParameters { AutoCompileProjects = false, LoadAssemblyReferences = false, AutoLoadTemporaryAssets = false });
            return CheckSession(result);
        }

        /// <summary>
        /// Headless session over the consumer's project, the editor's path. Assemblies are not loaded: the
        /// manifest session already loaded this plugin's assemblies into the test process.
        /// </summary>
        public PackageSession LoadConsumerProjectSession()
        {
            PackageSessionPublicHelper.FindAndSetMSBuildVersion();
            var result = new PackageSessionResult();
            PackageSession.Load(GameProject, result, new PackageLoadParameters { AutoCompileProjects = false, LoadAssemblyReferences = false, AutoLoadTemporaryAssets = false });
            return CheckSession(result);
        }

        private PackageSession CheckSession(PackageSessionResult result)
        {
            foreach (var message in result.Messages)
                output.WriteLine(message.ToString());
            if (result.Session is not null)
                sessions.Add(result.Session);
            Assert.False(result.HasErrors, "Session load reported errors.");
            return result.Session;
        }

        public void Dispose()
        {
            for (var i = sessions.Count - 1; i >= 0; i--)
                sessions[i].Dispose();
            sessions.Clear();
        }
    }
}
