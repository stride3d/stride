// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Avalonia.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Editor.Avalonia.Engine;
using Stride.Editor.Engine;

namespace Stride.Assets.Editor.Avalonia.Tests;

/// <summary>
/// The panel that lays the views of a scene editor out (prototype of several views): a view of the viewport per pane, and
/// the view under the pointer asked to become active.
/// </summary>
public sealed class EditorViewLayoutPanelTests
{
    [AvaloniaFact]
    public void TheQuadLayoutShowsFourViewsOfTheViewportAndOneLeavesThePrimaryViewAlone()
    {
        var dispatcher = new DispatcherService(Dispatcher.UIThread);
        var viewport = new OffscreenGameViewport(dispatcher, new LoggerResult());
        var layout = new EditorViewLayoutViewModel(new ViewModelServiceProvider([dispatcher]), new FakeMultiViewService(), null);
        var panel = new EditorViewLayoutPanel { GameView = viewport.View, Viewport = viewport, Layout = layout };
        var window = new Window { Width = 400, Height = 300, Content = panel };
        window.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            Assert.Equal([viewport.View], panel.GetVisualDescendants().OfType<OffscreenGameView>());

            layout.Layout = EditorViewLayout.Quad;
            Dispatcher.UIThread.RunJobs();

            var views = panel.GetVisualDescendants().OfType<OffscreenGameView>().ToList();
            Assert.Equal(4, views.Count);
            Assert.Equal(4, viewport.Views.Count);
            // The primary view is the perspective view, bottom right
            Assert.Same(viewport.View, views[3]);
            var headers = panel.GetVisualDescendants().OfType<Button>().Where(x => x.Name == "PaneHeader").ToList();
            Assert.Equal(4, headers.Count);
            Assert.All(headers, x => Assert.True(x.IsVisible));

            // The view the pointer comes over is asked to become active
            var topLeft = views[0].TranslatePoint(new Point(20, 40), window)!.Value;
            window.MouseMove(topLeft);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(layout.Panes[0].Id, viewport.RequestedActiveView);

            // Back to one view: the other views leave the viewport, and the pointer goes back to the primary view
            layout.Layout = EditorViewLayout.Single;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal([viewport.View], panel.GetVisualDescendants().OfType<OffscreenGameView>());
            Assert.Equal([viewport.View], viewport.Views);
            Assert.Equal(IEditorGameMultiViewport.PrimaryView, viewport.RequestedActiveView);
        }
        finally
        {
            window.Close();
            viewport.Dispose();
        }
    }

    [AvaloniaFact]
    public void AMaximizedPaneFillsThePanelAndTheOthersAreNotDrawn()
    {
        var dispatcher = new DispatcherService(Dispatcher.UIThread);
        var viewport = new OffscreenGameViewport(dispatcher, new LoggerResult());
        var layout = new EditorViewLayoutViewModel(new ViewModelServiceProvider([dispatcher]), new FakeMultiViewService(), null);
        var panel = new EditorViewLayoutPanel { GameView = viewport.View, Viewport = viewport, Layout = layout };
        var window = new Window { Width = 400, Height = 300, Content = panel };
        window.Show();
        try
        {
            layout.Layout = EditorViewLayout.TwoColumns;
            Dispatcher.UIThread.RunJobs();
            var infos = new List<EditorGameViewInfo>();
            viewport.GetViews(infos);
            var other = infos.Single(x => x.Id != IEditorGameMultiViewport.PrimaryView);
            Assert.True(other.Width > 0 && other.Height > 0);

            layout.MaximizedPane = layout.Panes.Single(x => x.Id == IEditorGameMultiViewport.PrimaryView);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal([viewport.View], panel.GetVisualDescendants().OfType<OffscreenGameView>());
            // The game draws no frame for a view out of the visual tree
            viewport.GetViews(infos);
            other = infos.Single(x => x.Id != IEditorGameMultiViewport.PrimaryView);
            Assert.Equal((0, 0), (other.Width, other.Height));
        }
        finally
        {
            window.Close();
            viewport.Dispose();
        }
    }

    private sealed class FakeMultiViewService : IEditorGameMultiViewViewModelService
    {
        public bool IsAvailable => true;

        public EditorGameViewCameraState? PrimaryViewCamera => null;

        public event EventHandler<EditorGameViewCameraInfo>? ActiveViewChanged
        {
            add { }
            remove { }
        }

        public void SetViewCamera(int viewId, EditorViewCamera camera)
        {
        }

        public IReadOnlyDictionary<int, EditorGameViewCameraState> GetViewCameras() => new Dictionary<int, EditorGameViewCameraState>();

        public void SetViewCamera(int viewId, EditorGameViewCameraState state)
        {
        }

        public void RemoveView(int viewId)
        {
        }
    }
}
