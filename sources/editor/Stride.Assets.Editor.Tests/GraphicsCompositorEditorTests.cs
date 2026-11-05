// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Editor.ViewModels.GraphicsCompositorEditor;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Rendering;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Presentation.Services;
using Stride.Core.Reflection;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The graphics compositor editor, shared with the WPF editor: its blocks and links, its lists of render stages, render
/// features and camera slots, and the commands that edit them. Each change is one entry of the undo history, with the names
/// of the WPF editor.
/// </summary>
public sealed class GraphicsCompositorEditorTests
{
    [Fact]
    public async Task TheEditorShowsTheEntryPointsTheSharedRenderersAndTheirLinks()
    {
        using var test = await CompositorTest.OpenAsync();
        var editor = test.Editor;

        Assert.Equal(3, editor.Blocks.Count);
        var entryPoints = Assert.IsType<EntryPointBlockViewModel>(editor.Blocks[0]);
        Assert.Equal("Entry points", entryPoints.Title);
        Assert.Equal(["Editor", "Game", "SingleView"], entryPoints.OutputSlots.Select(x => x.Name).Order());
        Assert.Empty(entryPoints.InputSlots);

        // The Game entry point links to the forward renderer, the others are free
        var forward = test.Block(test.Forward);
        var game = Slot(entryPoints, "Game");
        var link = Assert.Single(game.Links);
        Assert.Same(game, link.SourceSlot);
        Assert.Same(forward.InputSlots.Single(), link.TargetSlot);
        Assert.Same(link, Assert.Single(forward.InputSlots.Single().Links));
        Assert.Empty(Slot(entryPoints, "SingleView").Links);
        Assert.Empty(Slot(entryPoints, "Editor").Links);

        Assert.Equal(["Opaque", "Transparent"], editor.RenderStages.Select(x => x.Name));
        Assert.Equal(["Main"], editor.CameraSlots.Select(x => x.Name));
        Assert.Equal([typeof(MeshRenderFeature)], editor.RenderFeatures.Select(x => x.RenderFeature.GetType()));
    }

    [Fact]
    public async Task TheFactoriesListTheInstantiableSharedRenderersByName()
    {
        using var test = await CompositorTest.OpenAsync();
        var factories = test.Editor.SharedRendererFactories;

        Assert.Contains(factories, x => x.Type == typeof(ForwardRenderer));
        Assert.Contains(factories, x => x.Type == typeof(DebugRenderer));
        Assert.Equal(factories.Select(x => x.Name).Order(StringComparer.Ordinal), factories.Select(x => x.Name));
        Assert.All(factories, x => Assert.Null(TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<ObsoleteAttribute>(x.Type)));
        Assert.All(factories, x => Assert.Null(TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<Stride.Core.Annotations.NonInstantiableAttribute>(x.Type)));
        Assert.Contains(test.Editor.RenderFeatureTypes, x => x.Type == typeof(MeshRenderFeature));
    }

    [Fact]
    public async Task AFactoryAddsASharedRendererBlockAndUndoRemovesIt()
    {
        using var test = await CompositorTest.OpenAsync();
        var editor = test.Editor;
        var names = test.RecordUndoNames();

        editor.SharedRendererFactories.Single(x => x.Type == typeof(ForwardRenderer)).CreateCommand.Execute(null);

        Assert.Equal(["Create renderer"], names);
        Assert.Equal(3, test.Asset.Asset.SharedRenderers.Count);
        var added = test.Asset.Asset.SharedRenderers[2];
        Assert.IsType<ForwardRenderer>(added);
        Assert.Equal(4, editor.Blocks.Count);
        Assert.Same(added, ((SharedRendererBlockViewModel)editor.Blocks[3]).GetSharedRenderer());

        test.Undo();
        Assert.Equal(2, test.Asset.Asset.SharedRenderers.Count);
        Assert.Equal(3, editor.Blocks.Count);
    }

    [Fact]
    public async Task LinkingAnEntryPointSetsItsRendererAndClearingTheLinkResetsIt()
    {
        using var test = await CompositorTest.OpenAsync();
        var editor = test.Editor;
        var names = test.RecordUndoNames();
        var entryPoints = (EntryPointBlockViewModel)editor.Blocks[0];
        var singleView = Slot(entryPoints, "SingleView");
        var debugInput = test.Block(test.Debug).InputSlots.Single();

        Assert.True(singleView.CanLinkTo((GraphicsCompositorSlotViewModel)debugInput));
        Assert.False(debugInput.CanLinkTo((GraphicsCompositorSlotViewModel)singleView));
        singleView.LinkTo(debugInput);

        Assert.Same(test.Debug, test.Asset.Asset.SingleView);
        Assert.Same(debugInput, Assert.Single(singleView.Links).TargetSlot);

        singleView.ClearLink();
        Assert.Null(test.Asset.Asset.SingleView);
        Assert.Empty(singleView.Links);
        Assert.Empty(debugInput.Links);
        Assert.Equal(["Connect SingleView to ", "Clear link"], names);

        test.Undo();
        Assert.Same(test.Debug, test.Asset.Asset.SingleView);
        Assert.Single(Slot((EntryPointBlockViewModel)editor.Blocks[0], "SingleView").Links);
    }

    [Fact]
    public async Task DeletingASelectedLinkClearsItsSlot()
    {
        using var test = await CompositorTest.OpenAsync();
        var editor = test.Editor;
        var names = test.RecordUndoNames();
        var game = Slot((EntryPointBlockViewModel)editor.Blocks[0], "Game");

        editor.SelectedRendererLinks.Add(game.Links.Single());
        test.WaitForProperties();
        editor.DeleteSelectionCommand.Execute(null);

        Assert.Null(test.Asset.Asset.Game);
        Assert.Empty(game.Links);
        Assert.Empty(editor.SelectedRendererLinks);
        Assert.Contains("Delete link(s)", names);

        test.Undo();
        Assert.Same(test.Forward, test.Asset.Asset.Game);
    }

    [Fact]
    public async Task DeletingASelectedSharedRendererRemovesItsBlockAndTheReferencesToIt()
    {
        using var test = await CompositorTest.OpenAsync();
        var editor = test.Editor;
        var names = test.RecordUndoNames();
        var forward = test.Block(test.Forward);

        editor.SelectedSharedRenderers.Add(forward);
        test.WaitForProperties();
        editor.DeleteSelectionCommand.Execute(null);

        Assert.Equal(["Delete renderer(s)"], names);
        Assert.DoesNotContain(test.Forward, test.Asset.Asset.SharedRenderers);
        Assert.Null(test.Asset.Asset.Game);
        Assert.Equal(2, editor.Blocks.Count);
        Assert.DoesNotContain(forward, editor.Blocks);
        Assert.Empty(Slot((EntryPointBlockViewModel)editor.Blocks[0], "Game").Links);

        test.Undo();
        Assert.Contains(test.Forward, test.Asset.Asset.SharedRenderers);
        Assert.Same(test.Forward, test.Asset.Asset.Game);
        Assert.Equal(3, editor.Blocks.Count);
        Assert.Single(Slot((EntryPointBlockViewModel)editor.Blocks[0], "Game").Links);
    }

    [Fact]
    public async Task AddingAndRemovingRenderStagesIsOneUndoEntryEach()
    {
        using var test = await CompositorTest.OpenAsync();
        var editor = test.Editor;
        var names = test.RecordUndoNames();

        editor.AddNewRenderStageCommand.Execute(null);
        Assert.Equal(3, editor.RenderStages.Count);
        Assert.Same(test.Asset.Asset.RenderStages[2], editor.RenderStages[2].RenderStage);

        editor.SelectedRenderStages.Add(editor.RenderStages[0]);
        test.WaitForProperties();
        editor.RemoveSelectedRenderStagesCommand.Execute(null);
        Assert.Equal(["Transparent", "RenderStage"], editor.RenderStages.Select(x => x.Name));
        Assert.Empty(editor.SelectedRenderStages);
        Assert.Equal(["Create new render stage", "Delete render stage(s)"], names);

        test.Undo();
        Assert.Equal(["Opaque", "Transparent", "RenderStage"], editor.RenderStages.Select(x => x.Name));
        test.Undo();
        Assert.Equal(["Opaque", "Transparent"], editor.RenderStages.Select(x => x.Name));
    }

    [Fact]
    public async Task RenamingARenderStageEditsTheAsset()
    {
        using var test = await CompositorTest.OpenAsync();
        var editor = test.Editor;

        editor.RenderStages[1].Name = "Glass";
        editor.RenderStages[1].EffectSlotName = "GlassSlot";

        Assert.Equal("Glass", test.Asset.Asset.RenderStages[1].Name);
        Assert.Equal("GlassSlot", test.Asset.Asset.RenderStages[1].EffectSlotName);
        test.Undo();
        Assert.Equal("Transparent", editor.RenderStages[1].EffectSlotName);
    }

    [Fact]
    public async Task AddingAndRemovingRenderFeaturesAndCameraSlotsIsOneUndoEntryEach()
    {
        using var test = await CompositorTest.OpenAsync();
        var editor = test.Editor;
        var names = test.RecordUndoNames();

        editor.AddNewRenderFeatureCommand.Execute(editor.RenderFeatureTypes.Single(x => x.Type == typeof(MeshRenderFeature)));
        Assert.Equal(2, editor.RenderFeatures.Count);
        editor.SelectedRenderFeatures.Add(editor.RenderFeatures[0]);
        test.WaitForProperties();
        editor.RemoveSelectedRenderFeaturesCommand.Execute(null);
        Assert.Single(editor.RenderFeatures);
        Assert.Single(test.Asset.Asset.RenderFeatures);

        editor.AddNewCameraSlotCommand.Execute(null);
        Assert.Equal(2, editor.CameraSlots.Count);
        editor.SelectedCameraSlots.Add(editor.CameraSlots[0]);
        test.WaitForProperties();
        editor.RemoveSelectedCameraSlotsCommand.Execute(null);
        Assert.Equal(["CameraSlot"], editor.CameraSlots.Select(x => x.Name));

        Assert.Equal(["Create new render feature", "Delete render feature(s)", "Create new camera slot", "Delete camera slot(s)"], names);
        test.Undo();
        Assert.Equal(["Main", "CameraSlot"], editor.CameraSlots.Select(x => x.Name));
    }

    [Fact]
    public async Task SelectingAnItemShowsItsPropertiesUnderItsKind()
    {
        using var test = await CompositorTest.OpenAsync();
        var editor = test.Editor;
        var properties = editor.EditorProperties;

        editor.SelectedSharedRenderers.Add(test.Block(test.Forward));
        test.WaitForProperties();
        Assert.Equal("Shared renderer", properties.TypeDescription);
        Assert.True(properties.CanDisplayProperties);

        editor.SelectedSharedRenderers.Clear();
        editor.SelectedRenderStages.Add(editor.RenderStages[0]);
        editor.SelectedRenderStages.Add(editor.RenderStages[1]);
        test.WaitForProperties();
        Assert.Equal("Render stage", properties.TypeDescription);
        Assert.Equal("2 render stages selected", properties.Name);
    }

    [Fact]
    public async Task ThePackageFeaturesNeedAnOpaqueAndATransparentStage()
    {
        using var test = await CompositorTest.OpenAsync();
        var editor = test.Editor;
        editor.RenderStages[0].Name = "Solid";
        var dialogs = (FakeEditorDialogService)test.Fixture.ServiceProvider.Get<IDialogService>();

        // The dialog service of the test answers at once, so the command ends before Execute returns
        editor.AddPackageRenderFeaturesCommand.Execute(null);

        Assert.Single(dialogs.Messages);
        Assert.Single(editor.RenderFeatures);
    }

    private static IGraphicsCompositorSlotViewModel Slot(IGraphicsCompositorBlockViewModel block, string name)
    {
        return block.OutputSlots.Single(x => x.Name == name);
    }

    private sealed class CompositorTest : IDisposable
    {
        private CompositorTest(AssetsSessionFixture fixture, GraphicsCompositorEditorViewModel editor, ISharedRenderer forward, ISharedRenderer debug)
        {
            Fixture = fixture;
            Editor = editor;
            Forward = forward;
            Debug = debug;
        }

        public AssetsSessionFixture Fixture { get; }

        public GraphicsCompositorEditorViewModel Editor { get; }

        public GraphicsCompositorViewModel Asset => Editor.Asset;

        /// <summary>
        /// The forward renderer of the loaded asset, which the Game entry point references.
        /// </summary>
        public ISharedRenderer Forward { get; }

        /// <summary>
        /// The debug renderer of the loaded asset, which nothing references.
        /// </summary>
        public ISharedRenderer Debug { get; }

        public static async Task<CompositorTest> OpenAsync()
        {
            var forward = new ForwardRenderer();
            var asset = new GraphicsCompositorAsset();
            asset.RenderStages.Add(new RenderStage("Opaque", "Main"));
            asset.RenderStages.Add(new RenderStage("Transparent", "Transparent"));
            asset.RenderFeatures.Add(new MeshRenderFeature());
            asset.Cameras.Add(new SceneCameraSlot { Name = "Main" });
            asset.SharedRenderers.Add(forward);
            asset.SharedRenderers.Add(new DebugRenderer());

            var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: true, ("Compositor", asset));
            Assert.False(fixture.Result.HasErrors, fixture.Log);
            var compositor = fixture.Asset<GraphicsCompositorViewModel>("Compositor");
            var loaded = compositor.Asset;
            var loadedForward = loaded.SharedRenderers.OfType<ForwardRenderer>().Single();

            // The Game entry point references the shared forward renderer. Set through the property graph, which keeps it a
            // reference (the package of the test is written without the asset's reference metadata, which would make a copy)
            using (fixture.Session.UndoRedoService.CreateTransaction())
            {
                fixture.Session.AssetNodeContainer.GetNode(loaded)[nameof(GraphicsCompositorAsset.Game)].Update(loadedForward);
            }
            Assert.Same(loadedForward, loaded.Game);

            var editor = new GraphicsCompositorEditorViewModel(compositor);
            Assert.True(await editor.Initialize());
            return new CompositorTest(fixture, editor, loadedForward, loaded.SharedRenderers.OfType<DebugRenderer>().Single());
        }

        public SharedRendererBlockViewModel Block(ISharedRenderer renderer) => (SharedRendererBlockViewModel)Editor.GetBlock(renderer);

        public List<string?> RecordUndoNames()
        {
            WaitForProperties();
            var undoRedo = Fixture.Session.UndoRedoService;
            var names = new List<string?>();
            // The stack drops the transactions that change nothing (the Delete command opens one per kind of item)
            undoRedo.Done += (_, e) =>
            {
                if (e.Transaction.Operations.Any(x => x.HasEffect))
                    names.Add(undoRedo.GetName(e.Transaction));
            };
            return names;
        }

        public void Undo()
        {
            WaitForProperties();
            Fixture.Session.UndoRedoService.Undo();
        }

        /// <summary>
        /// Waits for the property grid of the selection: it builds its nodes on another thread, which must not run while the
        /// test changes the asset (the dispatcher of the test session has no thread of its own).
        /// </summary>
        public void WaitForProperties()
        {
            var properties = Editor.EditorProperties;
            for (var i = 0; i < 250 && !properties.CanDisplayProperties && properties.FallbackMessage == "Loading properties..."; ++i)
            {
                Thread.Sleep(20);
            }
        }

        public void Dispose()
        {
            WaitForProperties();
            Editor.Destroy();
            Fixture.Dispose();
        }
    }
}
