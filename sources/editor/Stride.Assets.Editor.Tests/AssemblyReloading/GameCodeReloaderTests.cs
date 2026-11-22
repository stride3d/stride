// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.AssemblyReloading;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets;
using Stride.Core.Assets.Quantum;
using Stride.Core.Diagnostics;
using Stride.Engine;

namespace Stride.Assets.Editor.Tests.AssemblyReloading;

/// <summary>
/// The reload of a session's game code from its rebuilt projects: in place when it can, with the reason to open the session
/// again in a fresh editor when it can't.
/// </summary>
public sealed class GameCodeReloaderTests
{
    [Fact]
    public async Task TheBuiltAssemblyOfAProjectReloadsInPlaceAsOneUndoableChange()
    {
        await using var probe = new ProbeAssemblies();
        await using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, [("MainScene", new SceneAsset())]);
        Assert.False(fixture.Result.HasErrors, fixture.Log);
        var loadedAssembly = probe.LoadFirstVersion(fixture, withProject: true);
        var component = (EntityComponent)Activator.CreateInstance(loadedAssembly.Assembly!.GetType(ProbeAssemblies.TypeName)!)!;
        component.GetType().GetField("Speed")!.SetValue(component, 7);
        AddEntity(fixture, new Entity("Probe") { component });

        var log = new LoggerResult();
        var undone = 0;
        var result = await new GameCodeReloader().ReloadAsync(fixture.Session, new Dictionary<string, string> { [probe.ProjectPath] = probe.SecondVersionPath }, log, () => undone++);

        Assert.True(result.Reloaded, result.RestartReason);
        Assert.DoesNotContain(log.Messages, x => x.Type >= LogMessageType.Warning);
        var reloaded = ProbeComponent(fixture);
        Assert.NotSame(probe.FirstVersion, reloaded.GetType().Assembly);
        Assert.Equal(7, reloaded.GetType().GetField("Speed")!.GetValue(reloaded));

        var undoRedo = fixture.Session.UndoRedoService;
        Assert.Equal("Reload game assemblies", undoRedo.GetName(undoRedo.RetrieveAllTransactions().Last()));
        undoRedo.Undo();
        Assert.Same(probe.FirstVersion, ProbeComponent(fixture).GetType().Assembly);
        Assert.Equal(1, undone);
    }

    [Fact]
    public async Task AProjectTheSessionDoesNotLoadNeedsNoReload()
    {
        await using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, [("MainScene", new SceneAsset())]);
        var transactions = fixture.Session.UndoRedoService.RetrieveAllTransactions().Count();

        var result = await new GameCodeReloader().ReloadAsync(fixture.Session, new Dictionary<string, string> { [Path.Combine(Path.GetTempPath(), "Other.csproj")] = "Other.dll" }, new LoggerResult(), () => { });

        Assert.True(result.Reloaded);
        Assert.Equal(transactions, fixture.Session.UndoRedoService.RetrieveAllTransactions().Count());
    }

    [Fact]
    public async Task AProjectWhoseAssemblyTheSessionCouldNotLoadRestarts()
    {
        await using var probe = new ProbeAssemblies();
        await using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, [("MainScene", new SceneAsset())]);
        var loadedAssembly = probe.LoadFirstVersion(fixture, withProject: true);
        var notLoaded = new PackageLoadedAssembly(loadedAssembly.ProjectReference, probe.FirstVersionPath);
        fixture.Package.Package.LoadedAssemblies.Remove(loadedAssembly);
        fixture.Package.Package.LoadedAssemblies.Add(notLoaded);
        try
        {
            var result = await new GameCodeReloader().ReloadAsync(fixture.Session, new Dictionary<string, string> { [probe.ProjectPath] = probe.SecondVersionPath }, new LoggerResult(), () => { });

            Assert.False(result.Reloaded);
            Assert.Contains("is not loaded in this session", result.RestartReason);
        }
        finally
        {
            fixture.Package.Package.LoadedAssemblies.Remove(notLoaded);
            fixture.Package.Package.LoadedAssemblies.Add(loadedAssembly);
        }
    }

    [Fact]
    public async Task AnAssemblyTheEditorItselfUsesRestarts()
    {
        await using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, [("MainScene", new SceneAsset())]);
        // The editor's own copy of the engine, as a project of the session
        var engine = typeof(Entity).Assembly;
        var projectPath = Path.Combine(Path.GetTempPath(), "Stride.Engine.csproj");
        var loadedAssembly = new PackageLoadedAssembly(new ProjectReference(Guid.NewGuid(), projectPath, ProjectType.Library), engine.Location) { Assembly = engine };
        fixture.Package.Package.LoadedAssemblies.Add(loadedAssembly);
        var transactions = fixture.Session.UndoRedoService.RetrieveAllTransactions().Count();
        try
        {
            var result = await new GameCodeReloader().ReloadAsync(fixture.Session, new Dictionary<string, string> { [projectPath] = engine.Location }, new LoggerResult(), () => { });

            Assert.False(result.Reloaded);
            Assert.Contains("Stride.Engine", result.RestartReason);
            Assert.Contains("in use by Game Studio itself", result.RestartReason);
            Assert.Equal(transactions, fixture.Session.UndoRedoService.RetrieveAllTransactions().Count());
        }
        finally
        {
            fixture.Package.Package.LoadedAssemblies.Remove(loadedAssembly);
        }
    }

    private static void AddEntity(AssetsSessionFixture fixture, Entity entity)
    {
        var graph = (AssetCompositeHierarchyPropertyGraph<EntityDesign, Entity>)fixture.Asset<SceneViewModel>("MainScene").PropertyGraph;
        using (fixture.Session.UndoRedoService.CreateTransaction())
        {
            var design = new EntityDesign(entity);
            graph.AddPartToAsset(new AssetPartCollection<EntityDesign, Entity> { design }, design, null, 0);
        }
    }

    private static EntityComponent ProbeComponent(AssetsSessionFixture fixture)
    {
        var scene = (SceneAsset)fixture.Asset("MainScene").Asset;
        return scene.Hierarchy.Parts.Values.Single().Entity.Components.Single(x => x.GetType().FullName == ProbeAssemblies.TypeName);
    }
}
