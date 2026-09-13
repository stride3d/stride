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

    /// <summary>
    /// The editor package loads at the version its runtime package declares, which is the runtime's own version
    /// for the in-repo packages (the dev suffix included), and states the kind the runtime declares.
    /// </summary>
    private static void AssertDeclaredEditorPackage(PackageSession session, string runtimeName, Package editorPackage)
    {
        var runtime = session.Packages.Single(p => p.Meta.Name == runtimeName);
        var declaration = Assert.Single(runtime.CompanionPackages, c => c.Kind == PackageKind.Editor);
        Assert.Equal(editorPackage.Meta.Name, declaration.Name);
        Assert.Equal(runtime.Meta.Version, declaration.Version);
        Assert.Equal(runtime.Meta.Version, editorPackage.Meta.Version);
        Assert.Equal(PackageKind.Editor, editorPackage.Kind);
    }

    [Fact]
    public void AssetsPackageRestoredFromFeed()
    {
        using var c = new Case(output, "assets-feed");
        c.DeclareAssetsCompanion();
        c.PackPlugin();
        c.PackAssets();
        c.AddSpinAsset();
        c.AddTypedSpinConstantCheck();

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
        c.AddTypedSpinConstantCheck();

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

    [Fact]
    public void VideoCompilesThroughItsAssetsPackageAndOnlyTheEditorLoadsItsEditorPackage()
    {
        // The engine's own plugin with both companions: Stride.Video declares Stride.Video.Assets (asset,
        // compiler, ffmpeg) and Stride.Video.Editor (editor extensions)
        using var c = new Case(output, "assets-video");
        c.PackPlugin();
        c.ReferencePackage("Stride.Video");
        c.AddVideoAsset();
        // Typed from Stride.Video's [assembly: AssetFileExtension(".sdvid", ...)]
        c.AddTypedConstantCheck("Clip", "Stride.Video.Video");

        var result = c.BuildConsumer();
        Assert.True(result.ExitCode == 0, $"Consumer build should succeed (exit {result.ExitCode}).");
        Assert.Contains("Video Asset Compiler", result.Output);
        c.AssertContentCompiled("/Consumer/Clip");

        // A session without editor packages (the asset compiler's) has the Assets package only
        var compilerSession = c.LoadConsumerProjectSession();
        Assert.Contains(compilerSession.Packages, p => p.Meta.Name == "Stride.Video.Assets");
        Assert.DoesNotContain(compilerSession.Packages, p => p.Meta.Name == "Stride.Video.Editor");

        // The editor's session load asks for editor packages too. Stride.Video.Editor targets Windows only, as Game Studio does.
        if (!OperatingSystem.IsWindows())
            return;
        var editorSession = c.LoadConsumerProjectSession(loadEditorPackages: true);
        Assert.Contains(editorSession.Packages, p => p.Meta.Name == "Stride.Video.Assets");
        var editorPackage = Assert.Single(editorSession.Packages, p => p.Meta.Name == "Stride.Video.Editor");
        var editorContainer = (StandalonePackage)editorPackage.Container;
        Assert.True(editorContainer.IsCompanionPackage);
        var editorAssembly = Assert.Single(editorContainer.Assemblies);
        Assert.True(File.Exists(editorAssembly), $"Editor assembly [{editorAssembly}] should exist.");
        AssertDeclaredEditorPackage(editorSession, "Stride.Video", editorPackage);
    }

    [Fact]
    public void ColliderShapeCompilesThroughThePhysicsAssetsPackageAndOnlyTheEditorLoadsItsEditorPackage()
    {
        // Bullet physics: Stride.Physics declares Stride.Physics.Assets (collider shape, heightmap and navigation mesh
        // assets, compilers, templates) and Stride.Physics.Editor (gizmos, navigation overlay, ...)
        using var c = new Case(output, "assets-physics");
        c.PackPlugin();
        c.ReferencePackage("Stride.Physics");
        c.AddColliderShapeAsset();
        // Typed from Stride.Physics's [assembly: AssetFileExtension(".sdphy", ...)]
        c.AddTypedConstantCheck("BoxCollider", "Stride.Physics.PhysicsColliderShape");

        var result = c.BuildConsumer();
        Assert.True(result.ExitCode == 0, $"Consumer build should succeed (exit {result.ExitCode}).");
        c.AssertContentCompiled("/Consumer/BoxCollider");

        var compilerSession = c.LoadConsumerProjectSession();
        Assert.Contains(compilerSession.Packages, p => p.Meta.Name == "Stride.Physics.Assets");
        Assert.DoesNotContain(compilerSession.Packages, p => p.Meta.Name == "Stride.Physics.Editor");
        Assert.Contains(TemplateManager.FindTemplates(TemplateScope.Asset, compilerSession), t => t.Name == "Navigation mesh");

        // Stride.Physics.Editor targets Windows only, as Game Studio does
        if (!OperatingSystem.IsWindows())
            return;
        var editorSession = c.LoadConsumerProjectSession(loadEditorPackages: true);
        var editorPackage = Assert.Single(editorSession.Packages, p => p.Meta.Name == "Stride.Physics.Editor");
        Assert.True(((StandalonePackage)editorPackage.Container).IsCompanionPackage);
        AssertDeclaredEditorPackage(editorSession, "Stride.Physics", editorPackage);
    }

    [Fact]
    public void SpriteStudioSheetCompilesThroughItsAssetsPackageAndOnlyTheEditorLoadsItsEditorPackage()
    {
        // Stride.SpriteStudio.Runtime declares companions under other ids: Stride.SpriteStudio.Assets (sheet and
        // animation assets, compilers, importer, templates) and Stride.SpriteStudio.Editor (preview, thumbnails, ...)
        using var c = new Case(output, "assets-spritestudio");
        c.PackPlugin();
        c.ReferencePackage("Stride.SpriteStudio.Runtime");
        c.AddSpriteStudioSheetAsset();
        // Typed from Stride.SpriteStudio.Runtime's [assembly: AssetFileExtension(".sdss4s", ...)]
        c.AddTypedConstantCheck("Character", "Stride.SpriteStudio.Runtime.SpriteStudioSheet");

        var result = c.BuildConsumer();
        Assert.True(result.ExitCode == 0, $"Consumer build should succeed (exit {result.ExitCode}).");
        c.AssertContentCompiled("/Consumer/Character");

        var compilerSession = c.LoadConsumerProjectSession();
        Assert.Contains(compilerSession.Packages, p => p.Meta.Name == "Stride.SpriteStudio.Assets");
        Assert.DoesNotContain(compilerSession.Packages, p => p.Meta.Name == "Stride.SpriteStudio.Editor");
        Assert.Contains(TemplateManager.FindTemplates(TemplateScope.Asset, compilerSession), t => t.Name == "SpriteStudio® sheet");

        // Stride.SpriteStudio.Editor targets Windows only, as Game Studio does
        if (!OperatingSystem.IsWindows())
            return;
        var editorSession = c.LoadConsumerProjectSession(loadEditorPackages: true);
        var editorPackage = Assert.Single(editorSession.Packages, p => p.Meta.Name == "Stride.SpriteStudio.Editor");
        Assert.True(((StandalonePackage)editorPackage.Container).IsCompanionPackage);
        AssertDeclaredEditorPackage(editorSession, "Stride.SpriteStudio.Runtime", editorPackage);
    }

    [Fact]
    public void SoundCompilesThroughTheAudioAssetsPackageAndOnlyTheEditorLoadsItsEditorPackage()
    {
        // Stride.Audio declares Stride.Audio.Assets (sound asset, compiler, importer,
        // templates) and Stride.Audio.Editor (gizmos, preview, thumbnail)
        using var c = new Case(output, "assets-audio");
        c.PackPlugin();
        c.ReferencePackage("Stride.Audio");
        c.AddSoundAsset();
        // Typed from Stride.Audio's [assembly: AssetFileExtension(".sdsnd", ...)]
        c.AddTypedConstantCheck("Bip", "Stride.Audio.Sound");

        var result = c.BuildConsumer();
        Assert.True(result.ExitCode == 0, $"Consumer build should succeed (exit {result.ExitCode}).");
        c.AssertContentCompiled("/Consumer/Bip");

        var compilerSession = c.LoadConsumerProjectSession();
        Assert.Contains(compilerSession.Packages, p => p.Meta.Name == "Stride.Audio.Assets");
        Assert.DoesNotContain(compilerSession.Packages, p => p.Meta.Name == "Stride.Audio.Editor");
        Assert.Contains(TemplateManager.FindTemplates(TemplateScope.Asset, compilerSession), t => t.Name == "Spatialized sound");

        // Stride.Audio.Editor targets Windows only, as Game Studio does
        if (!OperatingSystem.IsWindows())
            return;
        var editorSession = c.LoadConsumerProjectSession(loadEditorPackages: true);
        var editorPackage = Assert.Single(editorSession.Packages, p => p.Meta.Name == "Stride.Audio.Editor");
        Assert.True(((StandalonePackage)editorPackage.Container).IsCompanionPackage);
        AssertDeclaredEditorPackage(editorSession, "Stride.Audio", editorPackage);

        // The sound preview's WPF view is a companion of Stride.Audio.Editor written for the Wpf toolkit: an editor
        // that names no toolkit leaves it out, the WPF one loads it
        var viewDeclaration = Assert.Single(editorPackage.CompanionPackages);
        Assert.Equal("Stride.Audio.Editor.Wpf", viewDeclaration.Name);
        Assert.Equal(PackageKind.Editor, viewDeclaration.Kind);
        Assert.Equal("Wpf", viewDeclaration.Toolkit);
        Assert.DoesNotContain(editorSession.Packages, p => p.Meta.Name == "Stride.Audio.Editor.Wpf");

        var wpfSession = c.LoadConsumerProjectSession(loadEditorPackages: true, editorToolkit: "Wpf");
        var viewPackage = Assert.Single(wpfSession.Packages, p => p.Meta.Name == "Stride.Audio.Editor.Wpf");
        Assert.Equal(PackageKind.Editor, viewPackage.Kind);
        Assert.Equal("Wpf", viewPackage.Toolkit);
        Assert.True(((StandalonePackage)viewPackage.Container).IsCompanionPackage);
    }

    [Fact]
    public void VoxelsHasAnEditorPackageOnlyAndShipsItsTemplate()
    {
        // Stride.Voxels ships no asset type, so it declares Stride.Voxels.Editor alone (gizmo, entity factories);
        // its compositor template comes with the runtime package itself
        using var c = new Case(output, "editor-voxels");
        c.PackPlugin();
        c.ReferencePackage("Stride.Voxels");

        var result = c.BuildConsumer();
        Assert.True(result.ExitCode == 0, $"Consumer build should succeed (exit {result.ExitCode}).");

        var compilerSession = c.LoadConsumerProjectSession();
        Assert.DoesNotContain(compilerSession.Packages, p => p.Meta.Name == "Stride.Voxels.Editor");
        Assert.Contains(TemplateManager.FindTemplates(TemplateScope.Asset, compilerSession), t => t.Name == "Graphics compositor (Voxel Cone Tracing)");

        // Stride.Voxels.Editor targets Windows only, as Game Studio does
        if (!OperatingSystem.IsWindows())
            return;
        var editorSession = c.LoadConsumerProjectSession(loadEditorPackages: true);
        var editorPackage = Assert.Single(editorSession.Packages, p => p.Meta.Name == "Stride.Voxels.Editor");
        Assert.True(((StandalonePackage)editorPackage.Container).IsCompanionPackage);
        AssertDeclaredEditorPackage(editorSession, "Stride.Voxels", editorPackage);
    }

    [Fact]
    public void ParticlesHasAnEditorPackageOnly()
    {
        // Stride.Particles ships no asset type, so it declares Stride.Particles.Editor alone (gizmo, entity factories,
        // property grid updater, preview render feature); the asset compiler never loads it
        using var c = new Case(output, "editor-particles");
        c.PackPlugin();
        c.ReferencePackage("Stride.Particles");

        var result = c.BuildConsumer();
        Assert.True(result.ExitCode == 0, $"Consumer build should succeed (exit {result.ExitCode}).");

        var compilerSession = c.LoadConsumerProjectSession();
        Assert.DoesNotContain(compilerSession.Packages, p => p.Meta.Name == "Stride.Particles.Editor");

        // Stride.Particles.Editor targets Windows only, as Game Studio does
        if (!OperatingSystem.IsWindows())
            return;
        var editorSession = c.LoadConsumerProjectSession(loadEditorPackages: true);
        var editorPackage = Assert.Single(editorSession.Packages, p => p.Meta.Name == "Stride.Particles.Editor");
        Assert.True(((StandalonePackage)editorPackage.Container).IsCompanionPackage);
        AssertDeclaredEditorPackage(editorSession, "Stride.Particles", editorPackage);
    }

    [Fact]
    public void BepuHullCompilesThroughItsAssetsPackage()
    {
        // The engine's own plugin: Stride.BepuPhysics declares Stride.BepuPhysics.Assets, which carries
        // the hull asset compiler and its native V-HACD dependency
        using var c = new Case(output, "assets-bepu");
        c.PackPlugin();
        c.ReferenceBepu();
        c.AddHullAssets();
        // Typed from Stride.BepuPhysics's [assembly: AssetFileExtension(".sdhull", ...)]
        c.AddTypedConstantCheck("Hull", "Stride.BepuPhysics.Definitions.DecomposedHulls");

        var result = c.BuildConsumer();
        Assert.True(result.ExitCode == 0, $"Consumer build should succeed (exit {result.ExitCode}).");
        Assert.Contains("convex hull", result.Output);
        c.AssertContentCompiled("/Consumer/Hull");
        // Saved by an older version: the companion's upgrader brings it to the current format
        c.AssertContentCompiled("/Consumer/HullV2");

        // The editor's path loads the companion the runtime package declares
        var session = c.LoadConsumerProjectSession();
        var companion = Assert.Single(session.Packages, p => p.Meta.Name == "Stride.BepuPhysics.Assets");
        Assert.Equal(PackageKind.Assets, companion.Kind);
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
            // An engine companion restores there at the engine version: only that version is dropped
            var engineCompanionDir = Path.Combine(globalPackages, "stride.bepuphysics.assets", version.ToLowerInvariant());
            if (Directory.Exists(engineCompanionDir))
                Directory.Delete(engineCompanionDir, recursive: true);
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

        public void AddTypedSpinConstantCheck() => AddTypedConstantCheck("Spin", "StrideAssetPlugin.SpinData");

        /// <summary>
        /// Adds game code that compiles only when the asset URL constant <paramref name="name"/> has type
        /// <paramref name="contentType"/>, set by the plugin runtime's [assembly: AssetFileExtension].
        /// </summary>
        public void AddTypedConstantCheck(string name, string contentType)
        {
            File.WriteAllText(Path.Combine(consumerDir, "Consumer.Game", $"TypedConstantCheck{name}.cs"), $$"""
                namespace Consumer;

                internal static class TypedConstantCheck{{name}}
                {
                    internal static readonly Stride.Core.Serialization.UrlReference<{{contentType}}> Value = Assets.{{name}};
                }
                """);
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

        public void ReferenceBepu() => ReferencePackage("Stride.BepuPhysics");

        /// <summary>Adds an engine package reference to the game project.</summary>
        public void ReferencePackage(string id)
        {
            const string uiReference = """<PackageReference Include="Stride.UI" Version="$(StrideEngineVersion)" />""";
            var text = File.ReadAllText(GameProject);
            Assert.Contains(uiReference, text);
            File.WriteAllText(GameProject, text.Replace(uiReference,
                uiReference + Indent + $"""<PackageReference Include="{id}" Version="$(StrideEngineVersion)" />"""));
        }

        public void AddVideoAsset()
        {
            var resources = Path.Combine(consumerDir, "Consumer.Game", "Resources");
            Directory.CreateDirectory(resources);
            File.Copy(Path.Combine(TestEnvironment.WorktreeRoot(), "sources", "data", "tests", "video", "clip.mp4"), Path.Combine(resources, "clip.mp4"));
            File.WriteAllText(Path.Combine(consumerDir, "Consumer.Game", "Assets", "Clip.sdvid"), """
                !Video
                Id: 9b2c3d4e-5f60-4a71-8b82-93a4b5c6d7e8
                SerializedVersion: {Stride: 2.1.0.0}
                Tags: []
                Source: ../Resources/clip.mp4
                """);
            AddRootAsset("9b2c3d4e-5f60-4a71-8b82-93a4b5c6d7e8:Clip");
        }

        public void AddColliderShapeAsset()
        {
            File.WriteAllText(Path.Combine(consumerDir, "Consumer.Game", "Assets", "BoxCollider.sdphy"), """
                !ColliderShapeAsset
                Id: 1c2d3e4f-5a6b-4c7d-8e9f-0a1b2c3d4e5f
                SerializedVersion: {Stride: 4.0.0.0}
                Tags: []
                ColliderShapes:
                    e803604e1edf064bb9d035b760568ca5: !BoxColliderShapeDesc
                        Size: {X: 1.0, Y: 1.0, Z: 1.0}
                """);
            AddRootAsset("1c2d3e4f-5a6b-4c7d-8e9f-0a1b2c3d4e5f:BoxCollider");
        }

        public void AddSpriteStudioSheetAsset()
        {
            // The sheet's source .ssae names its cell map .ssce, which names the texture
            var resources = Path.Combine(consumerDir, "Consumer.Game", "Resources");
            Directory.CreateDirectory(resources);
            var sampleResources = Path.Combine(TestEnvironment.WorktreeRoot(), "samples", "Graphics", "SpriteStudioDemo", "Resources");
            foreach (var file in new[] { "character_template_2head.ssae", "character_2head.ssce", "character_2head.png" })
                File.Copy(Path.Combine(sampleResources, file), Path.Combine(resources, file));
            File.WriteAllText(Path.Combine(consumerDir, "Consumer.Game", "Assets", "Character.sdss4s"), """
                !SpriteStudioSheetAsset
                Id: 7595b1d1-3f94-4cc5-bbfc-ec51687b96d6
                SerializedVersion: {Stride: 2.0.0.0}
                Tags: []
                Source: ../Resources/character_template_2head.ssae
                """);
            AddRootAsset("7595b1d1-3f94-4cc5-bbfc-ec51687b96d6:Character");
        }

        public void AddSoundAsset()
        {
            var resources = Path.Combine(consumerDir, "Consumer.Game", "Resources");
            Directory.CreateDirectory(resources);
            File.Copy(Path.Combine(TestEnvironment.WorktreeRoot(), "sources", "data", "tests", "audio", "90-bboc1.wav"), Path.Combine(resources, "bip.wav"));
            File.WriteAllText(Path.Combine(consumerDir, "Consumer.Game", "Assets", "Bip.sdsnd"), """
                !Sound
                Id: 2f3a4b5c-6d7e-4f80-9a1b-2c3d4e5f6a7b
                SerializedVersion: {Stride: 2.0.0.0}
                Tags: []
                Source: ../Resources/bip.wav
                Spatialized: false
                """);
            AddRootAsset("2f3a4b5c-6d7e-4f80-9a1b-2c3d4e5f6a7b:Bip");
        }

        public void AddHullAssets()
        {
            var assets = Path.Combine(consumerDir, "Consumer.Game", "Assets");
            File.WriteAllText(Path.Combine(assets, "Cube.sdpromodel"), """
                !ProceduralModelAsset
                Id: 7f0d6c3a-2b1e-4a5c-9d8e-0f1a2b3c4d5e
                SerializedVersion: {Stride: 2.0.0.0}
                Tags: []
                Type: !CubeProceduralModel
                    Size: {X: 1.0, Y: 1.0, Z: 1.0}
                """);
            File.WriteAllText(Path.Combine(assets, "Hull.sdhull"), """
                !HullAsset
                Id: 8a1e7d4b-3c2f-4b6d-8e9f-1a2b3c4d5e6f
                SerializedVersion: {Stride: 3.0.0.0}
                Tags: []
                ConvexHulls: null
                Model: 7f0d6c3a-2b1e-4a5c-9d8e-0f1a2b3c4d5e:Cube
                LocalOffset: {X: 0.0, Y: 0.0, Z: 0.0}
                LocalRotation: {X: 0.0, Y: 0.0, Z: 0.0, W: 1.0}
                Scaling: {X: 1.0, Y: 1.0, Z: 1.0}
                Decomposition:
                    Enabled: false
                """);
            AddRootAsset("8a1e7d4b-3c2f-4b6d-8e9f-1a2b3c4d5e6f:Hull");
            File.WriteAllText(Path.Combine(assets, "HullV2.sdhull"), """
                !HullAsset
                Id: 9b2f8e5c-4d3a-4c7e-9fa0-2b3c4d5e6f70
                SerializedVersion: {Stride: 2.0.0.0}
                Tags: []
                ConvexHulls: null
                Model: 7f0d6c3a-2b1e-4a5c-9d8e-0f1a2b3c4d5e:Cube
                LocalOffset: {X: 0.0, Y: 0.0, Z: 0.0}
                LocalRotation: {X: 0.0, Y: 0.0, Z: 0.0, W: 1.0}
                Scaling: {X: 1.0, Y: 1.0, Z: 1.0}
                Decomposition:
                    Enabled: false
                    Depth: 10
                    Threshold: 0.01
                """);
            AddRootAsset("9b2f8e5c-4d3a-4c7e-9fa0-2b3c4d5e6f70:HullV2");
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
        public PackageSession LoadConsumerProjectSession(bool loadEditorPackages = false, string? editorToolkit = null)
        {
            PackageSessionPublicHelper.FindAndSetMSBuildVersion();
            var result = new PackageSessionResult();
            PackageSession.Load(GameProject, result, new PackageLoadParameters { AutoCompileProjects = false, LoadAssemblyReferences = false, AutoLoadTemporaryAssets = false, LoadEditorPackages = loadEditorPackages, EditorToolkit = editorToolkit });
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
