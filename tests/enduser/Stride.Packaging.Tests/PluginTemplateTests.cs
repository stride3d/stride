// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using Stride.Assets.Templates;
using Stride.Core.Assets;
using Stride.Core.Assets.Templates;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Xunit;
using Xunit.Abstractions;

namespace Stride.Packaging.Tests;

/// <summary>
/// The stride-plugin template added to a game's solution by <see cref="AddPluginGenerator"/>: its projects join the
/// session and the game builds with the plugin.
/// </summary>
[Collection("Packaging")]
public class PluginTemplateTests
{
    private readonly ITestOutputHelper output;
    public PluginTemplateTests(ITestOutputHelper output) => this.output = output;

    private static readonly Guid NewGameTemplateId = new("81d2adea-37b1-4711-834c-0d73a05c206c");
    private static readonly Guid PluginTemplateId = new("2D5E7F1A-8C3B-4E6A-9F0D-1B2C3D4E5F60");

    [Fact]
    public void AddPluginJoinsTheSolutionAndTheGameBuildsWithIt()
    {
        const string gameName = "PluginHost";
        const string pluginName = "MyPlugin";
        var solutionPath = TestEnvironment.GenerateSample(NewGameTemplateId, gameName);
        var solutionDir = Path.GetDirectoryName(solutionPath)!;
        var logger = new LoggerResult();
        logger.MessageLogged += (_, e) => output.WriteLine(e.Message.ToString());

        // Assemblies are not loaded: the plugin's would be a second copy next to the ones the other cases load
        var loadParameters = PackageLoadParameters.Default();
        loadParameters.AutoCompileProjects = false;
        loadParameters.LoadAssemblyReferences = false;
        var result = new PackageSessionResult();
        PackageSession.Load(solutionPath, result, loadParameters);
        AssertNoPluginError(result);
        using var session = result.Session;

        var description = TemplateManager.FindTemplates(session).First(t => t.Id == PluginTemplateId);
        var parameters = new SessionTemplateGeneratorParameters
        {
            Session = session,
            Unattended = true,
            Description = description,
            Name = pluginName,
            Namespace = pluginName,
            OutputDirectory = new UDirectory(solutionDir),
            Logger = logger,
        };
        var gitignore = File.ReadAllText(Path.Combine(solutionDir, ".gitignore"));
        var generator = new AddPluginGenerator(null, loadParameters);
        Assert.True(generator.PrepareForRun(parameters).Result, "PrepareForRun returned false.");
        Assert.True(generator.Generate(parameters), "Generate returned false.");
        Assert.False(logger.HasErrors, "The generator reported errors.");

        // The three projects are in the session, the game library references the runtime only, the template's own
        // solution file is gone
        foreach (var name in new[] { pluginName, pluginName + ".Assets", pluginName + ".Editor" })
            Assert.Contains(session.Projects.OfType<SolutionProject>(), p => p.Name == name);
        var gameProject = File.ReadAllText(Path.Combine(solutionDir, $"{gameName}.Game", $"{gameName}.Game.csproj"));
        Assert.Contains($"{pluginName}.csproj", gameProject);
        Assert.DoesNotContain($"{pluginName}.Assets.csproj", gameProject);
        Assert.DoesNotContain($"{pluginName}.Editor.csproj", gameProject);
        Assert.False(File.Exists(Path.Combine(solutionDir, pluginName + ".slnx")), "The template's solution file must not stay next to the game's.");
        // The game's own files stay as they were
        Assert.Equal(gitignore, File.ReadAllText(Path.Combine(solutionDir, ".gitignore")));
        session.Save(logger);
        Assert.False(logger.HasErrors, "Saving the session reported errors.");

        // The game builds: the executable builds the Assets companion for the asset compiler, and does not ship it
        var platformSuffix = TestEnvironment.HostPlatform == "linux" ? "Linux"
                           : TestEnvironment.HostPlatform == "macos" ? "macOS"
                           : "Windows";
        var executableDir = Path.Combine(solutionDir, $"{gameName}.{platformSuffix}");
        NuGetConsumerFeed.WriteStrictNuGetConfig(solutionDir);
        var build = ConsumerBuild.Run(Path.Combine(executableDir, $"{gameName}.{platformSuffix}.csproj"), solutionDir, output, timeoutMin: 10);
        Assert.True(build.ExitCode == 0, $"Game build should succeed (exit {build.ExitCode}).");
        Assert.DoesNotContain("was not found", build.Output);
        // The runtime declares the asset file extension its Assets companion's asset type compiles to
        Assert.DoesNotContain("STRDIAG014", build.Output);
        Assert.True(File.Exists(Path.Combine(solutionDir, pluginName + ".Assets", "bin", "Debug", "net10.0", pluginName + ".Assets.dll")), "The executable must build the Assets companion.");
        // The game's outputs go under the solution's Bin folder
        Assert.Empty(Directory.GetFiles(Path.Combine(solutionDir, "Bin"), pluginName + ".Assets.dll", SearchOption.AllDirectories));
        Assert.NotEmpty(Directory.GetFiles(Path.Combine(solutionDir, "Bin"), pluginName + ".dll", SearchOption.AllDirectories));

        // The reloaded solution: the runtime declares both companions, found as solution projects
        var reloaded = new PackageSessionResult();
        loadParameters.LoadEditorPackages = true;
        PackageSession.Load(solutionPath, reloaded, loadParameters);
        AssertNoPluginError(reloaded);
        using var reloadedSession = reloaded.Session;
        var runtime = Assert.Single(reloadedSession.Packages, p => p.Meta.Name == pluginName);
        Assert.Equal(new[] { pluginName + ".Assets", pluginName + ".Editor" }, runtime.CompanionPackages.Select(c => c.Name).OrderBy(n => n));
        Assert.Contains(runtime.CompanionPackages, c => c.Kind == PackageKind.Assets);
        Assert.Contains(runtime.CompanionPackages, c => c.Kind == PackageKind.Editor);
        Assert.IsType<SolutionProject>(Assert.Single(reloadedSession.Packages, p => p.Meta.Name == pluginName + ".Editor").Container);
        // The Assets companion's asset template, listed by its .sdpkg, is what Add asset offers
        Assert.Contains(TemplateManager.FindTemplates(TemplateScope.Asset, reloadedSession), t => t.Name == pluginName && t is TemplateAssetDescription);
    }

    /// <summary>
    /// A headless session over a game without its assemblies reports unloadable engine types and missing engine
    /// assets; only an error about the plugin fails the test.
    /// </summary>
    private void AssertNoPluginError(PackageSessionResult result)
    {
        foreach (var message in result.Messages)
            output.WriteLine(message.ToString());
        Assert.NotNull(result.Session);
        Assert.DoesNotContain(result.Messages, m => m.Type == LogMessageType.Error && m.Text.Contains("MyPlugin", StringComparison.Ordinal));
    }
}
