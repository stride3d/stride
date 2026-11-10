// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Assets.Editor.AssetEditors.GameEditor.ViewModels;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;
using Stride.Editor.Engine;

namespace Stride.Assets.Editor.ViewModels;

/// <summary>
/// How the views of a scene editor are laid out (prototype of several views).
/// </summary>
public enum EditorViewLayout
{
    /// <summary>One view.</summary>
    Single,

    /// <summary>Two views side by side.</summary>
    TwoColumns,

    /// <summary>Two views, one above the other.</summary>
    TwoRows,

    /// <summary>Four views: top, front, left and perspective.</summary>
    Quad,
}

/// <summary>
/// The views of the scene of a scene or prefab editor (prototype): a layout, the panes it shows, and the active pane, whose
/// camera is the editor camera. Each UI toolkit lays the panes out with its own views; the game keeps a camera per pane
/// (<see cref="IEditorGameMultiViewViewModelService"/>).
/// </summary>
public sealed class EditorViewLayoutViewModel : DispatcherViewModel
{
    private readonly IEditorGameMultiViewViewModelService service;
    private readonly EditorCameraViewModel? cameraOptions;
    private readonly ObservableCollection<EditorViewPaneViewModel> panes;
    private EditorViewLayout layout;
    private EditorViewPaneViewModel activePane;
    private EditorViewPaneViewModel? maximizedPane;
    private int nextId = IEditorGameMultiViewport.PrimaryView + 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditorViewLayoutViewModel"/> class, with one view.
    /// </summary>
    /// <param name="serviceProvider">The services of the editor.</param>
    /// <param name="service">The views of the game.</param>
    /// <param name="cameraOptions">The camera options of the toolbar, which show the camera of the active view.</param>
    public EditorViewLayoutViewModel(IViewModelServiceProvider serviceProvider, IEditorGameMultiViewViewModelService service, EditorCameraViewModel? cameraOptions)
        : base(serviceProvider)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
        this.cameraOptions = cameraOptions;
        activePane = new EditorViewPaneViewModel(serviceProvider, this, IEditorGameMultiViewport.PrimaryView, EditorViewCamera.Perspective) { IsActive = true };
        panes = [activePane];
        Panes = new ReadOnlyObservableCollection<EditorViewPaneViewModel>(panes);
        SetLayoutCommand = new AnonymousCommand<EditorViewLayout>(serviceProvider, value => Layout = value);
        ToggleMaximizeCommand = new AnonymousCommand(serviceProvider, () => MaximizedPane = MaximizedPane is null && panes.Count > 1 ? ActivePane : null);
        service.ActiveViewChanged += ServiceActiveViewChanged;
    }

    /// <summary>
    /// Gets the layouts the editor offers.
    /// </summary>
    public IReadOnlyList<EditorViewLayout> Layouts { get; } = Enum.GetValues<EditorViewLayout>();

    /// <summary>
    /// Gets or sets the layout of the views. A change keeps the primary view and as many other views as the layout has room
    /// for, with their cameras; a new view starts with the camera of its place in the layout.
    /// </summary>
    public EditorViewLayout Layout { get => layout; set => SetValue(ref layout, value, ApplyLayout); }

    /// <summary>
    /// Gets the panes of the layout, in reading order (left to right, then top to bottom).
    /// </summary>
    public ReadOnlyObservableCollection<EditorViewPaneViewModel> Panes { get; }

    /// <summary>
    /// Gets the pane whose camera is the editor camera: the last one the pointer came over.
    /// </summary>
    public EditorViewPaneViewModel ActivePane { get => activePane; private set => SetValue(ref activePane, value); }

    /// <summary>
    /// Gets or sets the pane that fills the whole view, or <c>null</c> when the layout shows every pane.
    /// </summary>
    public EditorViewPaneViewModel? MaximizedPane { get => maximizedPane; set => SetValue(ref maximizedPane, value is not null && panes.Contains(value) ? value : null); }

    /// <summary>
    /// Gets the command that changes the layout (an <see cref="EditorViewLayout"/>).
    /// </summary>
    public ICommandBase SetLayoutCommand { get; }

    /// <summary>
    /// Gets the command that makes the active pane fill the view, or shows every pane again.
    /// </summary>
    public ICommandBase ToggleMaximizeCommand { get; }

    internal IEditorGameMultiViewViewModelService Service => service;

    /// <inheritdoc/>
    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(EditorViewLayoutViewModel));
        service.ActiveViewChanged -= ServiceActiveViewChanged;
        base.Destroy();
    }

    private void ApplyLayout()
    {
        MaximizedPane = null;
        // The cameras of the places of each layout, in reading order; the primary view keeps its own camera
        EditorViewCamera?[] places = layout switch
        {
            EditorViewLayout.Single => [null],
            EditorViewLayout.TwoColumns => [null, EditorViewCamera.Top],
            EditorViewLayout.TwoRows => [null, EditorViewCamera.Front],
            EditorViewLayout.Quad => [EditorViewCamera.Top, EditorViewCamera.Front, EditorViewCamera.Left, null],
            _ => throw new ArgumentOutOfRangeException(nameof(Layout)),
        };

        var primary = panes.Single(x => x.Id == IEditorGameMultiViewport.PrimaryView);
        var others = new Queue<EditorViewPaneViewModel>(panes.Where(x => x != primary));
        var arranged = new List<EditorViewPaneViewModel>();
        foreach (var place in places)
        {
            if (place is not { } camera)
                arranged.Add(primary);
            else if (others.TryDequeue(out var kept))
                arranged.Add(kept);
            else
            {
                var pane = new EditorViewPaneViewModel(ServiceProvider, this, nextId++, camera);
                service.SetViewCamera(pane.Id, camera);
                arranged.Add(pane);
            }
        }

        // The views the layout has no room for are closed
        foreach (var removed in others)
            service.RemoveView(removed.Id);

        panes.Clear();
        foreach (var pane in arranged)
            panes.Add(pane);
    }

    private void ServiceActiveViewChanged(object? sender, EditorGameViewCameraInfo info)
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (IsDestroyed)
                return;
            if (panes.FirstOrDefault(x => x.Id == info.ViewId) is { } pane)
            {
                ActivePane.IsActive = false;
                ActivePane = pane;
                pane.IsActive = true;
            }
            // The camera options of the toolbar show the camera of the active view
            cameraOptions?.ShowProjection(info.IsOrthographic, info.OrthographicSize);
        });
    }
}

/// <summary>
/// One pane of the views of a scene editor: a view of the scene with a camera of its own (prototype).
/// </summary>
public sealed class EditorViewPaneViewModel : DispatcherViewModel
{
    private readonly EditorViewLayoutViewModel owner;
    private EditorViewCamera camera;
    private bool isActive;

    internal EditorViewPaneViewModel(IViewModelServiceProvider serviceProvider, EditorViewLayoutViewModel owner, int id, EditorViewCamera camera)
        : base(serviceProvider)
    {
        this.owner = owner;
        this.camera = camera;
        Id = id;
        SetCameraCommand = new AnonymousCommand<EditorViewCamera>(serviceProvider, value => Camera = value);
    }

    /// <summary>
    /// Gets the identifier of the view of this pane in the viewport of the game.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the cameras a pane can start from.
    /// </summary>
    public IReadOnlyList<EditorViewCamera> Cameras { get; } = Enum.GetValues<EditorViewCamera>();

    /// <summary>
    /// Gets or sets the camera the pane started from, which its title shows; setting it moves the camera of the pane.
    /// </summary>
    public EditorViewCamera Camera
    {
        get => camera;
        set => SetValue(ref camera, value, () => owner.Service.SetViewCamera(Id, value), nameof(Camera), nameof(Title));
    }

    /// <summary>
    /// Gets the title of the pane.
    /// </summary>
    public string Title => camera.ToString();

    /// <summary>
    /// Gets whether the camera of this pane is the editor camera.
    /// </summary>
    public bool IsActive { get => isActive; internal set => SetValue(ref isActive, value); }

    /// <summary>
    /// Gets the command that gives this pane another camera (an <see cref="EditorViewCamera"/>).
    /// </summary>
    public ICommandBase SetCameraCommand { get; }
}
