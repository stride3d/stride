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
/// A game assembly reloaded in the editor's process: the live objects of its types take the types of the new copy and keep
/// their values, and the reload is undone and redone as one change.
/// </summary>
public sealed class GameStudioAssemblyReloaderTests
{
    [Fact]
    public async Task AComponentOfAReloadedAssemblyTakesTheNewTypeAndKeepsItsValues()
    {
        await using var probe = new ProbeAssemblies();
        await using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, [("MainScene", new SceneAsset())]);
        Assert.False(fixture.Result.HasErrors, fixture.Log);
        var loadedAssembly = probe.LoadFirstVersion(fixture);

        var component = (EntityComponent)Activator.CreateInstance(loadedAssembly.Assembly!.GetType(ProbeAssemblies.TypeName)!)!;
        component.GetType().GetField("Speed")!.SetValue(component, 7);
        AddEntity(fixture, new Entity("Probe") { component });

        var undoRedo = fixture.Session.UndoRedoService;
        var log = new LoggerResult();
        using (undoRedo.CreateTransaction())
        {
            GameStudioAssemblyReloader.Reload(fixture.Session, log, () => { }, () => { }, new() { [loadedAssembly] = probe.SecondVersionPath });
        }
        Assert.DoesNotContain(log.Messages, x => x.Type >= LogMessageType.Warning);

        var reloaded = ProbeComponent(fixture);
        Assert.Same(loadedAssembly.Assembly, reloaded.GetType().Assembly);
        Assert.NotSame(probe.FirstVersion, reloaded.GetType().Assembly);
        Assert.NotNull(reloaded.GetType().GetField("Range"));
        Assert.Equal(7, reloaded.GetType().GetField("Speed")!.GetValue(reloaded));
        Assert.Equal(component.Id, reloaded.Id);

        undoRedo.Undo();
        var undone = ProbeComponent(fixture);
        Assert.Same(probe.FirstVersion, undone.GetType().Assembly);
        Assert.Same(probe.FirstVersion, loadedAssembly.Assembly);
        Assert.Equal(7, undone.GetType().GetField("Speed")!.GetValue(undone));

        undoRedo.Redo();
        var redone = ProbeComponent(fixture);
        Assert.NotSame(probe.FirstVersion, redone.GetType().Assembly);
        Assert.Equal(7, redone.GetType().GetField("Speed")!.GetValue(redone));
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
