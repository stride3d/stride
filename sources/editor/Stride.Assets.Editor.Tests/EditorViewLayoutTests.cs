// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Editor.Engine;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The layouts of the views of a scene editor (prototype): the panes each layout shows, the cameras of new panes, and the
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

        // A camera picked in the header of a pane goes to the game
        front.SetCameraCommand.Execute(EditorViewCamera.Right);
        Assert.Equal((front.Id, EditorViewCamera.Right), service.Cameras[^1]);
        Assert.Equal("Right", front.Title);
    }

    private sealed class FakeMultiViewService : IEditorGameMultiViewViewModelService
    {
        public List<(int Id, EditorViewCamera Camera)> Cameras { get; } = [];

        public List<int> Removed { get; } = [];

        public bool IsAvailable => true;

        public EditorGameViewCameraState? PrimaryViewCamera => null;

        public event EventHandler<EditorGameViewCameraInfo>? ActiveViewChanged;

        public void SetViewCamera(int viewId, EditorViewCamera camera) => Cameras.Add((viewId, camera));

        public void RemoveView(int viewId) => Removed.Add(viewId);

        public void RaiseActiveViewChanged(EditorGameViewCameraInfo info) => ActiveViewChanged?.Invoke(this, info);
    }
}
