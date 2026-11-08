// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Settings;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// What the Plugins panel will list (<see cref="PluginCatalog"/>, <see cref="PluginInfo"/>), gathered from the session's
/// packages, their <c>.nuspec</c> and the plugin host; and the service that switches a package off (<see cref="PluginSwitchService"/>).
/// </summary>
public sealed class TestPluginInfo : IDisposable
{
    private static readonly PackageVersion Version = new("1.2.3");

    private readonly string directory = Path.Combine(Path.GetTempPath(), "Stride.PluginInfo." + Guid.NewGuid().ToString("N"));

    public TestPluginInfo() => Directory.CreateDirectory(directory);

    public void Dispose()
    {
        Directory.Delete(directory, true);
    }

    [Fact]
    public void TheNuspecGivesTheDescriptionOfAPackage()
    {
        var root = WriteRestoredPackage("Plugin.Editor", title: "My plugin");

        var fields = PluginCatalog.ReadNuspec(Path.Combine(root, "plugin.editor.nuspec"));

        Assert.NotNull(fields);
        Assert.Equal("Plugin.Editor", fields.Id);
        Assert.Equal("1.2.3", fields.Version);
        Assert.Equal("My plugin", fields.Title);
        Assert.Equal("Someone, Someone else", fields.Authors);
        Assert.Equal("Edits the plugin's assets.", fields.Description);
        Assert.Equal(Path.Combine(root, "icon.png"), fields.IconPath);
        Assert.Equal("https://example.com/plugin", fields.ProjectUrl);
        Assert.Equal("MIT", fields.License);
        Assert.Null(PluginCatalog.ReadNuspec(Path.Combine(directory, "missing.nuspec")));
    }

    [Theory]
    [InlineData("file:///C:/Windows/System32/calc.exe")]
    [InlineData(@"\\server\share\setup.exe")]
    [InlineData("/usr/bin/xterm")]
    [InlineData("ftp://example.com/plugin")]
    [InlineData("example.com/plugin")]
    public void AProjectUrlThatIsNotAWebPageIsLeftOut(string projectUrl)
    {
        var root = WriteRestoredPackage("Plugin.Editor", title: "My plugin", projectUrl: projectUrl);

        var fields = PluginCatalog.ReadNuspec(Path.Combine(root, "plugin.editor.nuspec"));

        Assert.NotNull(fields);
        Assert.Null(fields.ProjectUrl);
    }

    [Fact]
    public void ARestoredPackageIsListedWithItsNuspecFieldsItsPluginsAndWhatTheyRegistered()
    {
        var plugin = new TestPluginContributions.PanelPlugin();
        var host = new RecordingEditorPluginHost([plugin]) { PluginViewBaseType = typeof(object) };
        host.DeclareRegistry<TestPluginContributions.Widget>();
        try
        {
            using var session = new PackageSession { EditorToolkit = EditorToolkits.Avalonia };
            var root = WriteRestoredPackage("Plugin.Editor", title: "My plugin");
            var declaring = AddDeclaringPackage(session, "Plugin", new CompanionPackage { Name = "Plugin.Editor", Version = Version, Kind = PackageKind.Editor });
            var companion = new Package { Meta = { Name = "Plugin.Editor", Version = Version }, Kind = PackageKind.Editor, FullPath = Path.Combine(root, "stride", "Plugin.Editor.sdpkg") };
            // The plugin type is in the package's assembly: here, this test assembly, built against this editor
            var container = new StandalonePackage(companion) { IsDependencyPackage = true, IsCompanionPackage = true };
            container.Assemblies.Add(typeof(TestPluginInfo).Assembly.Location);
            session.Projects.Add(container);
            var fake = new FakeSession();
            fake.ServiceProvider.RegisterService(host);
            host.RegisterSession(fake, new LoggerResult());

            var info = Assert.Single(PluginCatalog.Collect(host, session), x => x.Id == "Plugin.Editor");

            Assert.Equal(Version, info.Version);
            Assert.Equal("My plugin", info.DisplayName);
            Assert.Equal("Someone, Someone else", info.Authors);
            Assert.Equal("Edits the plugin's assets.", info.Description);
            Assert.Equal(Path.Combine(root, "icon.png"), info.IconPath);
            Assert.Equal("https://example.com/plugin", info.ProjectUrl);
            Assert.Equal("MIT", info.License);
            Assert.Equal(PackageKind.Editor, info.Kind);
            Assert.Null(info.Toolkit);
            Assert.Equal(PluginSource.Package, info.Source);
            Assert.True(info.CanSwitchOff);
            Assert.Equal(BuiltInPlugins.EditorVersion, info.EditorApiVersion);
            Assert.Equal(PluginState.Joined, info.State);
            Assert.Same(plugin, Assert.Single(info.Statuses).Plugin);
            Assert.Empty(info.Errors);
            Assert.Equal(new Dictionary<string, int> { ["Panels"] = 1, ["ViewTypes"] = 1, ["Widget"] = 1 }, info.Contributions);
            Assert.NotNull(declaring);
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public void ASwitchedOffPackageIsListedOffWithTheNuspecItWasRestoredWith()
    {
        var host = new RecordingEditorPluginHost([]) { SafeMode = true };
        try
        {
            using var session = new PackageSession { EditorToolkit = EditorToolkits.Avalonia };
            var declaration = new CompanionPackage { Name = "Plugin.Editor", Version = Version, Kind = PackageKind.Editor };
            var declaring = AddDeclaringPackage(session, "Plugin", declaration);
            Assert.False(host.GetCompanionPackageFilter(null).ShouldLoad(declaring, declaration, new LoggerResult()));

            var info = Assert.Single(PluginCatalog.Collect(host, session));

            Assert.Equal("Plugin.Editor", info.Id);
            Assert.Equal("Plugin.Editor", info.DisplayName);
            Assert.Equal(PluginState.Off, info.State);
            Assert.Equal(PluginOffReason.SafeMode, info.OffReason);
            Assert.Equal(PluginSource.Package, info.Source);
            Assert.Null(info.EditorApiVersion);
            Assert.Empty(info.Contributions);
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public void TheSourceOfAPackageIsItsContainer()
    {
        var host = new RecordingEditorPluginHost([]);
        try
        {
            using var session = new PackageSession { EditorToolkit = EditorToolkits.Avalonia };
            var editorVersion = new PackageVersion($"{BuiltInPlugins.EditorVersion.Major}.{BuiltInPlugins.EditorVersion.Minor}.0");
            AddDeclaringPackage(session, "Game.Plugin",
                new CompanionPackage { Name = "Game.Plugin.Editor", Version = Version, Kind = PackageKind.Editor },
                new CompanionPackage { Name = "Dev.Plugin.Editor", Version = Version, Kind = PackageKind.Editor },
                new CompanionPackage { Name = "Stride.UI.Editor", Version = editorVersion, Kind = PackageKind.Editor },
                new CompanionPackage { Name = "Game.Plugin.Editor.Avalonia", Version = Version, Kind = PackageKind.Editor, Toolkit = EditorToolkits.Avalonia },
                // Not this editor's: not listed
                new CompanionPackage { Name = "Game.Plugin.Editor.Wpf", Version = Version, Kind = PackageKind.Editor, Toolkit = EditorToolkits.Wpf },
                // Not loaded and not left out by the editor (it failed to restore): not listed
                new CompanionPackage { Name = "Missing.Editor", Version = Version, Kind = PackageKind.Editor });
            session.Projects.Add(new SolutionProject(new Package { Meta = { Name = "Game.Plugin.Editor" } }, Guid.NewGuid(), Path.Combine(directory, "MyPlugin.Editor.csproj")));
            session.Projects.Add(new StandalonePackage(new Package { Meta = { Name = "Dev.Plugin.Editor", Version = Version } }) { IsDevRedirectPackage = true });
            session.Projects.Add(new StandalonePackage(new Package { Meta = { Name = "Stride.UI.Editor", Version = editorVersion } }) { IsDependencyPackage = true });
            session.Projects.Add(new StandalonePackage(new Package { Meta = { Name = "Game.Plugin.Editor.Avalonia", Version = Version } }) { IsDependencyPackage = true });

            var infos = PluginCatalog.Collect(host, session);

            Assert.Equal(["Game.Plugin.Editor", "Dev.Plugin.Editor", "Stride.UI.Editor", "Game.Plugin.Editor.Avalonia"], infos.Select(x => x.Id));
            var project = infos[0];
            Assert.Equal((PluginSource.SolutionProject, "MyPlugin.Editor", Version), (project.Source, project.DisplayName, project.Version));
            Assert.Equal(PluginSource.DevRedirect, infos[1].Source);
            Assert.Equal(PluginSource.BuiltIn, infos[2].Source);
            Assert.False(infos[2].CanSwitchOff);
            Assert.Equal((PluginSource.Package, EditorToolkits.Avalonia), (infos[3].Source, infos[3].Toolkit));
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public void TheEditorsOwnPluginsAreBuiltIn()
    {
        var plugin = new TestPluginContributions.PanelPlugin();
        var host = new RecordingEditorPluginHost([plugin]) { PluginViewBaseType = typeof(object) };
        host.DeclareRegistry<TestPluginContributions.Widget>();
        try
        {
            var fake = new FakeSession();
            fake.ServiceProvider.RegisterService(host);
            host.RegisterSession(fake, new LoggerResult());

            var info = Assert.Single(PluginCatalog.Collect(host, session: null));

            Assert.Equal(typeof(TestPluginInfo).Assembly.GetName().Name, info.Id);
            Assert.Equal(PluginSource.Editor, info.Source);
            Assert.True(info.IsBuiltIn);
            Assert.False(info.CanSwitchOff);
            Assert.Equal(PluginState.Joined, info.State);
            Assert.Equal(3, info.Contributions.Values.Sum());
            // The build version, not the assembly version pinned to major.minor.0; no toolkit referenced: any toolkit
            var informational = typeof(TestPluginInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
            Assert.Equal(PluginCatalog.ParseBuildVersion(informational), info.Version);
            Assert.NotNull(info.Version);
            Assert.Null(info.Toolkit);
        }
        finally
        {
            host.Dispose();
        }
    }

    [Theory]
    [InlineData("4.5.1-dev", "4.5.1-dev")]
    [InlineData("4.5.1-dev+g1a2b3c4d", "4.5.1-dev")]
    [InlineData("4.5.1-beta+g1a2b3c4d", "4.5.1-beta")]
    [InlineData("4.5.1.0", "4.5.1.0")]
    [InlineData("", null)]
    [InlineData("+g1a2b3c4d", null)]
    [InlineData("not a version", null)]
    public void TheBuildVersionOfAnAssemblyDropsItsBuildMetadata(string text, string? expected)
    {
        Assert.Equal(expected, PluginCatalog.ParseBuildVersion(text)?.ToString());
    }

    [Theory]
    [InlineData(new[] { "System.Runtime", "Avalonia.Base", "Stride.Core" }, EditorToolkits.Avalonia)]
    [InlineData(new[] { "Avalonia" }, EditorToolkits.Avalonia)]
    [InlineData(new[] { "System.Runtime", "PresentationFramework" }, EditorToolkits.Wpf)]
    [InlineData(new[] { "PresentationCore", "WindowsBase" }, EditorToolkits.Wpf)]
    [InlineData(new[] { "System.Runtime", "Stride.Core.Assets.Editor", "Stride.Avalonian" }, null)]
    public void TheToolkitOfAnEditorAssemblyComesFromTheToolkitItReferences(string[] references, string? expected)
    {
        Assert.Equal(expected, PluginCatalog.ReadToolkit(references));
    }

    [Fact]
    public void TheSwitchServiceWritesAndClearsTheEntryOfASession()
    {
        var container = new SettingsContainer();
        var key = new SettingsKey<List<DisabledEditorCompanions>>(InternalSettings.DisabledEditorCompanions.Name, container, () => new List<DisabledEditorCompanions>());
        var profile = container.CreateSettingsProfile(true);
        var service = new PluginSwitchService(new DisabledEditorCompanionStore(() => profile, key, () => { }));
        var game = new UFile("/Projects/Game/Game.sln");
        var plugin = new PluginInfo { Id = "Plugin.Editor", DisplayName = "My plugin", Version = Version, Source = PluginSource.Package };

        Assert.True(service.SwitchOff(game, plugin));
        Assert.False(service.SwitchOff(game, plugin));
        Assert.True(service.IsSwitchedOff(game, "plugin.editor"));
        Assert.Equal(["Plugin.Editor"], service.GetSwitchedOff(game));
        Assert.Equal("Plugin.Editor", Assert.Single(Assert.Single(key.GetValue(profile, true)).Packages));

        Assert.True(service.SwitchOn(game, "Plugin.Editor"));
        Assert.False(service.IsSwitchedOff(game, "Plugin.Editor"));
        Assert.Empty(key.GetValue(profile, true));

        // A built-in package can't be
        var builtIn = new PluginInfo { Id = "Stride.Particles.Editor", DisplayName = "Stride.Particles.Editor", Version = new PackageVersion($"{BuiltInPlugins.EditorVersion.Major}.{BuiltInPlugins.EditorVersion.Minor}.0"), Source = PluginSource.BuiltIn };
        Assert.Throws<InvalidOperationException>(() => service.SwitchOff(game, builtIn));
        Assert.Empty(key.GetValue(profile, true));
    }

    [Fact]
    public void TheConfirmationSaysWhatGoes()
    {
        var plugin = new PluginInfo
        {
            Id = "Plugin.Editor",
            DisplayName = "My plugin",
            Source = PluginSource.Package,
            Contributions = new Dictionary<string, int> { ["AssetEditorViewModelTypes"] = 2, ["AssetPreviewViewModelTypes"] = 1, ["TemplateProviders"] = 3, ["NodePresenterCommands"] = 1, ["Panels"] = 1 },
        };

        var summary = PluginSwitchService.DescribeSwitchOff(plugin);

        Assert.Equal("Plugin.Editor", summary.PackageName);
        Assert.Equal(
            ["its asset editors (2)", "its asset previews (1)", "its gizmos in the scene editor", "its additions to the property grid (4)", "its render features: what its components draw may no longer show in the scene editor", "its panels (1)"],
            summary.WhatGoes);
        Assert.StartsWith("Switching off My plugin takes away, from the next opening of this project:", summary.Message);
        Assert.EndsWith("Its assets still open, with the generic property grid and no editor of their own.", summary.Message);
    }

    // A package as NuGet restores it: its .nuspec (named in lower case, as the restore writes it) and icon at the root, its
    // sdpkg under stride/
    private string WriteRestoredPackage(string id, string title, string projectUrl = "https://example.com/plugin")
    {
        var root = Path.Combine(directory, id.ToLowerInvariant(), Version.ToString());
        Directory.CreateDirectory(Path.Combine(root, "stride"));
        File.WriteAllBytes(Path.Combine(root, "icon.png"), [0x89, 0x50, 0x4E, 0x47]);
        File.WriteAllText(Path.Combine(root, id.ToLowerInvariant() + ".nuspec"), $"""
            <?xml version="1.0" encoding="utf-8"?>
            <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
              <metadata>
                <id>{id}</id>
                <version>{Version}</version>
                <title>{title}</title>
                <authors>Someone, Someone else</authors>
                <license type="expression">MIT</license>
                <licenseUrl>https://licenses.nuget.org/MIT</licenseUrl>
                <icon>icon.png</icon>
                <projectUrl>{projectUrl}</projectUrl>
                <description>Edits the plugin's assets.</description>
              </metadata>
            </package>
            """);
        return root;
    }

    private static Package AddDeclaringPackage(PackageSession session, string name, params CompanionPackage[] companions)
    {
        var package = new Package { Meta = { Name = name, Version = Version } };
        package.CompanionPackages.AddRange(companions);
        session.Projects.Add(new StandalonePackage(package) { IsDependencyPackage = true });
        return package;
    }
}
