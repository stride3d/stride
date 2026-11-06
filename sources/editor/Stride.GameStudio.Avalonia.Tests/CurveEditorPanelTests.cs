// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Animations;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Editor.Avalonia.Views.CurveEditor;
using Stride.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Assets.Editor.Services;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Editor.ViewModels.CurveEditor;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Panels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Engine;
using Stride.GameStudio.Avalonia.Docking;
using Stride.GameStudio.Avalonia.Settings;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;
using Stride.Particles;
using Stride.Particles.Components;
using Stride.Particles.Updaters;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The curve editor panel: the "Edit curve" button of a curve property shows it with the curve, with the asset view; a second curve
/// is added and the same curve selected; the wheel and the middle button change the view; it is hidden when the session opens again,
/// and its curves go with the session.
/// </summary>
/// <remarks>
/// The tests have no game (the previews are disabled): the scene editor is open for its entities only.
/// </remarks>
public sealed class CurveEditorPanelTests
{
    [AvaloniaFact]
    public async Task TheEditCurveButtonShowsThePanelWithTheCurve()
    {
        await using var scene = await OpenSceneAsync();
        var panels = scene.Fixture.Main.Panels;
        Assert.False(panels.IsVisible(CurveEditorPanelViewModel.PanelId));
        Assert.Contains(scene.Fixture.Main.ViewMenu.Items, x => x.PanelId == CurveEditorPanelViewModel.PanelId);

        // The property template of a curve: the editor of its type, with the button
        var size = await scene.CurvePropertyAsync(0);
        var button = await scene.ShowEditCurveButtonAsync(size);
        Assert.True(button.IsVisible);
        await ((Window)TopLevel.GetTopLevel(button)!).ClickAsync(button);

        var panel = await scene.WaitForPanelAsync();
        var curve = Assert.Single(panel.Editor.Curves);
        Assert.Equal("Size Animation > Main > Curve", curve.DisplayName);
        Assert.Same(curve, panel.Editor.SelectedCurve);
        // The panel is with the asset view, and its view draws the curve
        var view = await scene.WaitForViewAsync();
        Assert.Same(panel.Editor, view.DataContext);
        var canvas = view.GetVisualDescendants().OfType<DrawingCanvas>().Single();
        Assert.Same(panel.Editor, canvas.Model);
        Assert.True(canvas.Bounds.Width > 100 && canvas.Bounds.Height > 100, canvas.Bounds.ToString());
        Assert.False(panel.Editor.CurveArea.IsEmpty);
    }

    [AvaloniaFact]
    public async Task ASecondCurveIsAddedAndTheSameCurveIsSelectedAgain()
    {
        await using var scene = await OpenSceneAsync();
        var size = await scene.CurvePropertyAsync(0);
        var color = await scene.CurvePropertyAsync(1);

        size.GetCommand(EditCurveCommand.CommandName)!.Execute(null);
        color.GetCommand(EditCurveCommand.CommandName)!.Execute(null);
        var panel = await scene.WaitForPanelAsync();
        await WindowTestHelper.WaitUntilAsync(() => panel.Editor.Curves.Count == 2, "the second curve");
        Assert.Equal(["Size Animation > Main > Curve", "Color Animation > Main > Curve"], panel.Editor.Curves.Select(x => x.DisplayName));
        // The color curve is listed with its components
        Assert.Equal(4, panel.Editor.Curves.ElementAt(1).Children.Count);
        Assert.Same(panel.Editor.Curves.ElementAt(1), panel.Editor.SelectedCurve);

        size.GetCommand(EditCurveCommand.CommandName)!.Execute(null);
        await WindowTestHelper.PumpAsync();
        Assert.Equal(2, panel.Editor.Curves.Count);
        Assert.Same(panel.Editor.Curves.ElementAt(0), panel.Editor.SelectedCurve);
        // The list shows both, the selected one selected
        var view = await scene.WaitForViewAsync();
        var list = view.FindControl<TreeView>("CurveList")!;
        Assert.Same(panel.Editor.SelectedCurve, list.SelectedItem);
    }

    [AvaloniaFact]
    public async Task TheWheelZoomsAndTheMiddleButtonPansTheView()
    {
        await using var scene = await OpenSceneAsync();
        (await scene.CurvePropertyAsync(0)).GetCommand(EditCurveCommand.CommandName)!.Execute(null);
        var panel = await scene.WaitForPanelAsync();
        var view = await scene.WaitForViewAsync();
        var canvas = view.GetVisualDescendants().OfType<DrawingCanvas>().Single();
        var curve = panel.Editor.SelectedCurve!;
        var window = scene.Window;
        var center = canvas.TranslatePoint(new Point(canvas.Bounds.Width / 2, canvas.Bounds.Height / 2), window)!.Value;
        (double, double) Range(Stride.Assets.Editor.ViewModels.CurveEditor.Axes.AxisBase axis) => (axis.ActualMinimum, axis.ActualMaximum);

        // The wheel zooms both axes in around the pointer
        var (x0, y0) = (Range(curve.XAxis!), Range(curve.YAxis!));
        window.MouseMove(center);
        window.MouseWheel(center, new Vector(0, 1));
        await WindowTestHelper.PumpAsync();
        var (x1, y1) = (Range(curve.XAxis!), Range(curve.YAxis!));
        Assert.True(x1.Item2 - x1.Item1 < x0.Item2 - x0.Item1, $"{x0} {x1}");
        Assert.True(y1.Item2 - y1.Item1 < y0.Item2 - y0.Item1, $"{y0} {y1}");

        // With Control, the keys only
        window.MouseWheel(center, new Vector(0, -1), RawInputModifiers.Control);
        await WindowTestHelper.PumpAsync();
        var (x2, y2) = (Range(curve.XAxis!), Range(curve.YAxis!));
        Assert.True(x2.Item2 - x2.Item1 > x1.Item2 - x1.Item1, $"{x1} {x2}");
        Assert.Equal(y1, y2);

        // The middle button pans: dragging right shows smaller keys, dragging down shows larger values
        window.MouseDown(center, MouseButton.Middle);
        window.MouseMove(center + new Point(40, 30), RawInputModifiers.MiddleMouseButton);
        window.MouseUp(center + new Point(40, 30), MouseButton.Middle);
        await WindowTestHelper.PumpAsync();
        var (x3, y3) = (Range(curve.XAxis!), Range(curve.YAxis!));
        Assert.True(x3.Item1 < x2.Item1, $"{x2} {x3}");
        Assert.True(y3.Item1 > y2.Item1, $"{y2} {y3}");

        // Fit brings the whole curve back
        view.FindControl<Button>("FitButton")!.Command!.Execute(0);
        await WindowTestHelper.PumpAsync();
        Assert.True(curve.XAxis!.ActualMinimum <= 0 && curve.XAxis.ActualMaximum >= 1, Range(curve.XAxis).ToString());
    }

    [AvaloniaFact]
    public async Task ThePanelIsHiddenWhenTheSessionOpensAgainAndItsCurvesGoWithTheSession()
    {
        await using var scene = await OpenSceneAsync();
        (await scene.CurvePropertyAsync(0)).GetCommand(EditCurveCommand.CommandName)!.Execute(null);
        var panel = await scene.WaitForPanelAsync();
        var panels = scene.Fixture.Main.Panels;
        Assert.True(panels.IsVisible(CurveEditorPanelViewModel.PanelId));

        // The session opens again: the panel keeps its place but is hidden
        panels.SaveLayout();
        var reopened = new DockPanelService(scene.Fixture.ServiceProvider, panels.Registry, GameStudioInternalSettings.CreateSessionPanelLayouts());
        try
        {
            reopened.OpenSession(scene.Fixture.Session, scene.Fixture.PackagePath);
            Assert.False(reopened.IsVisible(CurveEditorPanelViewModel.PanelId));
            reopened.Show(CurveEditorPanelViewModel.PanelId);
            Assert.True(reopened.IsVisible(CurveEditorPanelViewModel.PanelId));
            // A new session's panel starts empty
            Assert.Empty(Assert.IsType<CurveEditorPanelViewModel>(reopened.GetPanel(CurveEditorPanelViewModel.PanelId)).Editor.Curves);
        }
        finally
        {
            reopened.Dispose();
        }

        // Closing the session destroys the panel with its curves
        Assert.True(await scene.Fixture.Main.CloseSessionAsync());
        scene.Fixture.Main.CloseSessionServices();
        Assert.Empty(panel.Editor.Curves);
        Assert.Null(panel.Editor.SelectedCurve);
    }

    [AvaloniaFact]
    public async Task TheCurveServiceOfTheSessionShowsAndHidesThePanel()
    {
        await using var scene = await OpenSceneAsync();
        var service = Assert.IsType<CurveEditorPanelService>(scene.Fixture.Session.ServiceProvider.Get<ICurveEditorService>());

        service.Open(scene.SizeCurve, "Size");
        await WindowTestHelper.PumpAsync();
        Assert.True(scene.Fixture.Main.Panels.IsVisible(CurveEditorPanelViewModel.PanelId));
        Assert.Equal(["Size"], service.Editor!.Curves.Select(x => x.DisplayName));

        service.Close();
        await WindowTestHelper.PumpAsync();
        Assert.False(scene.Fixture.Main.Panels.IsVisible(CurveEditorPanelViewModel.PanelId));
        // Hidden, it keeps its curves until the session closes
        Assert.Single(service.Editor.Curves);
    }

    private static async Task<SceneFixture> OpenSceneAsync()
    {
        var fixture = ShellSessionFixture.Create(("MainScene", new SceneAsset()));
        fixture.ReplaceDialogService();
        await fixture.OpenAsync();
        var window = new MainWindow { DataContext = fixture.Main };
        try
        {
            await window.ShowAsync();
            // After it opens, as the window restores its saved size then: room for the panel with the asset view
            window.Width = 1600;
            window.Height = 1000;
            var sceneAsset = (SceneViewModel)fixture.Asset("MainScene");
            fixture.Session.AssetCollection.SelectAssets([sceneAsset]);
            await WindowTestHelper.PumpAsync();
            fixture.Session.EditSelectedContentCommand.Execute();
            await WindowTestHelper.WaitUntilAsync(() => window.GetVisualDescendants().OfType<EntityHierarchyEditorView>().Any(), "the scene editor");
            var editor = Assert.IsType<SceneEditorViewModel>(fixture.Session.EditorCollection.ActiveEditor);
            await WindowTestHelper.WaitUntilAsync(() => editor.HierarchyRoot is not null, "the hierarchy of the scene");

            // A particle system with a size curve and a color curve, added through the property graph as the editor does
            var size = new ComputeAnimationCurveFloat();
            size.KeyFrames.Add(new AnimationKeyFrame<float> { Key = 0, Value = 1 });
            size.KeyFrames.Add(new AnimationKeyFrame<float> { Key = 0.5f, Value = 3 });
            size.KeyFrames.Add(new AnimationKeyFrame<float> { Key = 1, Value = 2 });
            var color = new ComputeAnimationCurveColor4();
            color.KeyFrames.Add(new AnimationKeyFrame<Core.Mathematics.Color4> { Key = 0, Value = Core.Mathematics.Color4.White });
            color.KeyFrames.Add(new AnimationKeyFrame<Core.Mathematics.Color4> { Key = 1, Value = new Core.Mathematics.Color4(1, 0, 0, 0) });
            var emitter = new ParticleEmitter();
            emitter.Updaters.Add(new UpdaterSizeOverTime { SamplerMain = new ComputeCurveSamplerFloat { Curve = size } });
            emitter.Updaters.Add(new UpdaterColorOverTime { SamplerMain = new ComputeCurveSamplerColor4 { Curve = color } });
            var component = new ParticleSystemComponent();
            component.ParticleSystem.Emitters.Add(emitter);
            var design = new EntityDesign(new Entity("Particles") { component });
            var graph = (AssetCompositeHierarchyPropertyGraph<EntityDesign, Entity>)sceneAsset.PropertyGraph!;
            using (fixture.Session.UndoRedoService.CreateTransaction())
            {
                graph.AddPartToAsset(new AssetPartCollection<EntityDesign, Entity> { design }, design, null, 0);
            }
            await WindowTestHelper.WaitUntilAsync(() => editor.HierarchyRoot.InnerSubEntities.Any(x => x.Name == "Particles"), "the particle entity");
            return new SceneFixture(fixture, window, editor);
        }
        catch
        {
            await window.CloseAsync();
            fixture.Dispose();
            throw;
        }
    }

    private sealed class SceneFixture(ShellSessionFixture fixture, MainWindow window, SceneEditorViewModel editor) : IAsyncDisposable
    {
        private readonly List<Window> windows = [];

        public ShellSessionFixture Fixture { get; } = fixture;

        public MainWindow Window { get; } = window;

        public SceneEditorViewModel Editor { get; } = editor;

        public EntityViewModel Particles => Editor.HierarchyRoot.InnerSubEntities.Single(x => x.Name == "Particles");

        /// <summary>
        /// The size curve, on the asset side.
        /// </summary>
        public ComputeAnimationCurveFloat SizeCurve => (ComputeAnimationCurveFloat)Particles.AssetSideEntity.Get<ParticleSystemComponent>()!.ParticleSystem.Emitters[0].Updaters.OfType<UpdaterSizeOverTime>().Single().SamplerMain.Curve;

        /// <summary>
        /// The curve property of the updater at the given index, in the properties of the particle entity.
        /// </summary>
        public async Task<NodeViewModel> CurvePropertyAsync(int updater)
        {
            var graph = await Fixture.Session.AssetViewProperties.GenerateSelectionPropertiesAsync([Particles]);
            var components = graph!.RootNode.GetChild(nameof(Entity.Components))!;
            var component = components.Children.Single(x => x.NodeValue is ParticleSystemComponent);
            var emitter = component.GetChild(nameof(ParticleSystemComponent.ParticleSystem))!.GetChild("Emitters")!.Children.Single();
            return emitter.GetChild("Updaters")!.Children.ElementAt(updater).GetChild("SamplerMain")!.GetChild("Curve")!;
        }

        /// <summary>
        /// Shows the property template of a curve property in a window of its own, and returns its "Edit curve" button.
        /// </summary>
        public async Task<Button> ShowEditCurveButtonAsync(NodeViewModel property)
        {
            // The editor of the property as the property grid picks it: the curve template first, then the editor of its type inside
            Assert.True(CurveEditorViewModel.CanEdit(property.Type));
            var content = new ContentControl { Content = property, ContentTemplate = PropertyViewHelper.EditorProviders };
            var window = new Window { Width = 400, Height = 60, Content = content };
            windows.Add(window);
            await window.ShowAsync();
            return content.GetVisualDescendants().OfType<Button>().Single(x => x.Command is not null && ToolTip.GetTip(x) is "Edit curve");
        }

        public async Task<CurveEditorPanelViewModel> WaitForPanelAsync()
        {
            var panels = Fixture.Main.Panels;
            await WindowTestHelper.WaitUntilAsync(() => panels.IsVisible(CurveEditorPanelViewModel.PanelId), "the curve editor panel");
            return Assert.IsType<CurveEditorPanelViewModel>(panels.GetPanel(CurveEditorPanelViewModel.PanelId));
        }

        public async Task<CurveEditorView> WaitForViewAsync()
        {
            await WindowTestHelper.WaitUntilAsync(() => Window.GetVisualDescendants().OfType<CurveEditorView>().Any(x => x.IsEffectivelyVisible && x.Bounds.Width > 0), "the view of the curve editor");
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            return Window.GetVisualDescendants().OfType<CurveEditorView>().Single(x => x.IsEffectivelyVisible);
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var window in windows)
                await window.CloseAsync();
            await Window.CloseAsync();
            Fixture.Dispose();
        }
    }
}
