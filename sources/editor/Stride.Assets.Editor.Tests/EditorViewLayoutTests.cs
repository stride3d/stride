// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Editor.Engine;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The layouts of the views of a scene editor: the panes each layout shows, the cameras of new panes, and the
/// active pane that follows the game.
/// </summary>
public sealed class EditorViewLayoutTests
{
    [Fact]
    public void TheQuadLayoutShowsTopFrontLeftAndThePrimaryViewInPerspective()
    {
        var service = new FakeMultiViewService();
        var layout = new EditorViewLayoutViewModel(new ViewModelServiceProvider([new NullDispatcherService()]), service, null);
        Assert.Equal([IEditorGameMultiViewport.PrimaryView], layout.Panes.Select(x => x.Id));

        layout.SetLayoutCommand.Execute(EditorViewLayout.Quad);

        Assert.Equal([EditorViewCamera.Top, EditorViewCamera.Front, EditorViewCamera.Left, EditorViewCamera.Perspective], layout.Panes.Select(x => x.Camera));
        // The primary view keeps its camera, in the bottom right corner; each new view gets the camera of its place
        Assert.Equal(IEditorGameMultiViewport.PrimaryView, layout.Panes[3].Id);
        Assert.Equal(layout.Panes.Take(3).Select(x => (x.Id, x.Camera)), service.Cameras);
    }

    [Fact]
    public void AnotherLayoutKeepsTheViewsItHasRoomForAndClosesTheOthers()
    {
        var service = new FakeMultiViewService();
        var layout = new EditorViewLayoutViewModel(new ViewModelServiceProvider([new NullDispatcherService()]), service, null);
        layout.Layout = EditorViewLayout.Quad;
        var top = layout.Panes[0];

        layout.Layout = EditorViewLayout.TwoColumns;

        Assert.Equal([IEditorGameMultiViewport.PrimaryView, top.Id], layout.Panes.Select(x => x.Id));
        Assert.Equal(EditorViewCamera.Top, layout.Panes[1].Camera);
        Assert.Equal(2, service.Removed.Count);

        layout.Layout = EditorViewLayout.Single;
        Assert.Equal([IEditorGameMultiViewport.PrimaryView], layout.Panes.Select(x => x.Id));
        Assert.Contains(top.Id, service.Removed);
    }

    [Fact]
    public void EachLayoutShowsTheCamerasOfItsPicture()
    {
        var service = new FakeMultiViewService();
        var layout = new EditorViewLayoutViewModel(new ViewModelServiceProvider([new NullDispatcherService()]), service, null);
        layout.Layout = EditorViewLayout.TwoColumns;
        var top = layout.Panes[1];

        // Perspective over Front: the second view becomes a Front view, where a Front camera starts
        layout.Layout = EditorViewLayout.TwoRows;
        Assert.Equal([EditorViewCamera.Perspective, EditorViewCamera.Front], layout.Panes.Select(x => x.Camera));
        Assert.Same(top, layout.Panes[1]);
        Assert.Equal((top.Id, EditorViewCamera.Front), service.Cameras[^1]);
        var front = layout.Panes[1];

        // The quad keeps the Front view in its place, and opens Top and Left
        layout.Layout = EditorViewLayout.Quad;
        Assert.Equal([EditorViewCamera.Top, EditorViewCamera.Front, EditorViewCamera.Left, EditorViewCamera.Perspective], layout.Panes.Select(x => x.Camera));
        Assert.Same(front, layout.Panes[1]);
    }

    [Fact]
    public void APickedLayoutPutsItsViewsBackWhereTheyStartButNotThePrimaryView()
    {
        var service = new FakeMultiViewService();
        var layout = new EditorViewLayoutViewModel(new ViewModelServiceProvider([new NullDispatcherService()]), service, null);
        layout.SetLayoutCommand.Execute(EditorViewLayout.Quad);
        var top = layout.Panes[0];

        // The user moved the cameras, then goes to two views and back to the quad: every view other than the primary one
        // starts again from the camera of its place
        service.Cameras.Clear();
        layout.SetLayoutCommand.Execute(EditorViewLayout.TwoColumns);
        Assert.Equal([(top.Id, EditorViewCamera.Top)], service.Cameras);
        service.Cameras.Clear();
        layout.SetLayoutCommand.Execute(EditorViewLayout.Quad);
        Assert.Equal(layout.Panes.Take(3).Select(x => (x.Id, x.Camera)), service.Cameras);
        Assert.Same(top, layout.Panes[0]);

        // The quad picked again does it too
        service.Cameras.Clear();
        layout.SetLayoutCommand.Execute(EditorViewLayout.Quad);
        Assert.Equal(layout.Panes.Take(3).Select(x => (x.Id, x.Camera)), service.Cameras);
        Assert.DoesNotContain(service.Cameras, x => x.Id == IEditorGameMultiViewport.PrimaryView);
    }

    [Fact]
    public void TheActivePaneFollowsTheGameAndCanBeMaximized()
    {
        var service = new FakeMultiViewService();
        var layout = new EditorViewLayoutViewModel(new ViewModelServiceProvider([new NullDispatcherService()]), service, null);
        layout.Layout = EditorViewLayout.Quad;
        var front = layout.Panes[1];

        service.RaiseActiveViewChanged(new EditorGameViewCameraInfo(front.Id, true, 10));

        Assert.Same(front, layout.ActivePane);
        Assert.True(front.IsActive);
        Assert.Single(layout.Panes, x => x.IsActive);

        layout.ToggleMaximizeCommand.Execute();
        Assert.Same(front, layout.MaximizedPane);
        layout.ToggleMaximizeCommand.Execute();
        Assert.Null(layout.MaximizedPane);
        // The button of a pane maximizes that pane, and restores it
        var top = layout.Panes[0];
        top.ToggleMaximizeCommand.Execute();
        Assert.Same(top, layout.MaximizedPane);
        Assert.True(top.IsMaximized);
        Assert.False(front.IsMaximized);
        top.ToggleMaximizeCommand.Execute();
        Assert.Null(layout.MaximizedPane);
        Assert.False(top.IsMaximized);

        // A camera picked in the header of a pane goes to the game
        front.SetCameraCommand.Execute(EditorViewCamera.Right);
        Assert.Equal((front.Id, EditorViewCamera.Right), service.Cameras[^1]);
        Assert.Equal("Right", front.Title);
    }

    [Fact]
    public void ACameraPickedAgainResetsAnOrthographicViewButNotAPerspectiveOne()
    {
        var service = new FakeMultiViewService();
        var layout = new EditorViewLayoutViewModel(new ViewModelServiceProvider([new NullDispatcherService()]), service, null);
        layout.Layout = EditorViewLayout.Quad;
        var (top, perspective) = (layout.Panes[0], layout.Panes[3]);
        service.Cameras.Clear();

        // The user turned the camera of the Top view, then picks Top again in its header: it looks down again
        top.SetCameraCommand.Execute(EditorViewCamera.Top);
        Assert.Equal([(top.Id, EditorViewCamera.Top)], service.Cameras);
        Assert.Equal(EditorViewCamera.Top, top.Camera);

        // Perspective picked again keeps the camera where the user left it
        perspective.SetCameraCommand.Execute(EditorViewCamera.Perspective);
        Assert.Single(service.Cameras);
    }

    private sealed class FakeMultiViewService : IEditorGameMultiViewViewModelService
    {
        public List<(int Id, EditorViewCamera Camera)> Cameras { get; } = [];

        public List<int> Removed { get; } = [];

        public bool IsAvailable => true;

        public EditorGameViewCameraState? PrimaryViewCamera => null;

        public event EventHandler<EditorGameViewCameraInfo>? ActiveViewChanged;

        public void SetViewCamera(int viewId, EditorViewCamera camera) => Cameras.Add((viewId, camera));

        public IReadOnlyDictionary<int, EditorGameViewCameraState> GetViewCameras() => new Dictionary<int, EditorGameViewCameraState>();

        public void SetViewCamera(int viewId, EditorGameViewCameraState state)
        {
        }

        public void RemoveView(int viewId) => Removed.Add(viewId);

        public void RaiseActiveViewChanged(EditorGameViewCameraInfo info) => ActiveViewChanged?.Invoke(this, info);
    }
}
