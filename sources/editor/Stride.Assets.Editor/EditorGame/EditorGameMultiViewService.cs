// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Assets.Editor.AssetEditors;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Editor.AssetEditors.GameEditor.Game;
using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Editor.EditorGame.Game;
using Stride.Editor.Engine;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// Several views of one scene editor: the game keeps one camera per view of its viewport, draws the active view as usual
/// and the other views into textures of their own, which the viewport shows.
/// </summary>
/// <remarks>
/// <para>
/// The editor camera (<see cref="EditorGameCameraService"/>) is the camera of the active view: every other service (picking,
/// gizmos, navigation, drop under the pointer) works in the active view without knowing about the others. When another view
/// becomes active, this service keeps the state of the camera for the view that was active, and moves the camera to the state
/// of the new one, before the scripts of the frame run.
/// </para>
/// <para>
/// The other views are drawn after the scene and the gizmos of the frame, with the same graphics compositors and a camera of
/// their own, so they show the same scene, gizmos and grid. The renderers that work for the pointer (the pickers) skip these
/// draws (<see cref="IsSecondaryViewKey"/>). Before each of these views, the services that draw something fitted to the
/// view (<see cref="IEditorGameViewDependent"/>: the gizmo sizes, the grid plane, the orientation cube) fit it to that
/// view's camera, and to the active view's camera once they are drawn.
/// </para>
/// <para>
/// The other views are drawn at a lower rate than the active one (<see cref="InactiveViewSchedule.FramesPerSecond"/>), and
/// at once when one has nothing to show yet, or when its size or its camera changed.
/// </para>
/// <para>
/// The service does nothing with a viewport that shows one view only.
/// </para>
/// </remarks>
[EditorGameService(typeof(IEntityHierarchyEditorController), Order = 400)]
[Experimental("STREXP009")]
public sealed class EditorGameMultiViewService : EditorGameServiceBase, IEditorGameMultiViewViewModelService
{
    /// <summary>
    /// A tag of the render context, <c>true</c> while the game draws a view other than the active one: the renderers that
    /// work for the pointer skip it.
    /// </summary>
    public static readonly PropertyKey<bool> IsSecondaryViewKey = new("EditorGameMultiViewService.IsSecondaryView", typeof(EditorGameMultiViewService));

    /// <summary>
    /// How far from the point it looks at an orthographic view starts.
    /// </summary>
    private const float OrthographicDistance = 50.0f;

    /// <summary>
    /// The orthographic size an orthographic view starts with (that of a new camera component).
    /// </summary>
    private const float DefaultOrthographicSize = 10.0f;

    private readonly IEditorGameController controller;
    private readonly IEditorGameMultiViewport? viewport;
    private readonly Dictionary<int, ViewState> views = [];
    private readonly List<EditorGameViewInfo> viewInfos = [];
    private readonly List<IEditorGameViewDependent> viewDependents = [];
    private readonly InactiveViewSchedule schedule = new();
    private EntityHierarchyEditorGame? game;
    private EditorGameCameraService? camera;
    private ViewRenderer? renderer;
    // The cameras of the views other than the active one, for the settings of the scene (written on the game thread)
    private readonly ConcurrentDictionary<int, EditorGameViewCameraState> savedCameras = new();
    private volatile int activeView = IEditorGameMultiViewport.PrimaryView;
    private bool failed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditorGameMultiViewService"/> class.
    /// </summary>
    /// <param name="controller">The controller of the game; its viewport says whether it shows several views.</param>
    public EditorGameMultiViewService(IEditorGameController controller)
    {
        this.controller = controller ?? throw new ArgumentNullException(nameof(controller));
        viewport = controller.Viewport as IEditorGameMultiViewport;
    }

    /// <inheritdoc/>
    public bool IsAvailable => viewport is not null;

    /// <inheritdoc/>
    public override IEnumerable<Type> Dependencies { get { yield return typeof(IEditorGameCameraService); } }

    /// <inheritdoc/>
    public event EventHandler<EditorGameViewCameraInfo>? ActiveViewChanged;

    /// <inheritdoc/>
    public EditorGameViewCameraState? PrimaryViewCamera
        => activeView != IEditorGameMultiViewport.PrimaryView && savedCameras.TryGetValue(IEditorGameMultiViewport.PrimaryView, out var state) ? state : null;

    /// <inheritdoc/>
    public IReadOnlyDictionary<int, EditorGameViewCameraState> GetViewCameras()
    {
        var cameras = new Dictionary<int, EditorGameViewCameraState>(savedCameras);
        // The active view's camera is the editor camera, as it is now
        var active = activeView;
        cameras.Remove(active);
        if (camera is { } editorCamera)
            cameras[active] = ToSavedState(editorCamera.CaptureViewState());
        return cameras;
    }

    /// <inheritdoc/>
    public void SetViewCamera(int viewId, EditorGameViewCameraState state)
    {
        controller.InvokeAsync(() =>
        {
            if (camera is null || IsDisposed)
                return;
            SetViewState(viewId, FromSavedState(state));
        });
    }

    /// <inheritdoc/>
    public void SetViewCamera(int viewId, EditorViewCamera viewCamera)
    {
        controller.InvokeAsync(() =>
        {
            if (camera is null || IsDisposed)
                return;
            SetViewState(viewId, CreateState(viewCamera));
        });
    }

    /// <summary>
    /// Moves the camera of a view (game thread).
    /// </summary>
    private void SetViewState(int viewId, EditorCameraViewState state)
    {
        if (viewId == activeView)
        {
            camera!.RestoreViewState(state);
            ActiveViewChanged?.Invoke(this, new EditorGameViewCameraInfo(viewId, state.IsOrthographic, state.OrthographicSize));
        }
        else
        {
            SetInactiveState(viewId, GetOrCreateView(viewId), state);
        }
    }

    private void SetInactiveState(int viewId, ViewState view, EditorCameraViewState state)
    {
        // A camera the user changed shows at once, whatever the schedule
        view.IsDirty = true;
        view.Camera = state;
        savedCameras[viewId] = ToSavedState(state);
    }

    private static EditorGameViewCameraState ToSavedState(EditorCameraViewState state)
        => new(state.Position, new Vector2(state.Pitch, state.Yaw), state.IsOrthographic, state.OrthographicSize) { Target = state.Target };

    private static EditorCameraViewState FromSavedState(EditorGameViewCameraState state)
        => new(state.Position, state.PitchYaw.Y, state.PitchYaw.X, state.IsOrthographic, state.OrthographicSize) { Target = state.Target };

    /// <inheritdoc/>
    public void RemoveView(int viewId)
    {
        controller.InvokeAsync(() =>
        {
            savedCameras.TryRemove(viewId, out _);
            if (views.Remove(viewId, out var view))
                view.Dispose();
        });
    }

    /// <inheritdoc/>
    public override ValueTask DisposeAsync()
    {
        if (renderer is not null)
        {
            game?.GameSystems.Remove(renderer);
            renderer = null;
        }
        foreach (var view in views.Values)
            view.Dispose();
        views.Clear();
        return base.DisposeAsync();
    }

    /// <inheritdoc/>
    protected override Task<bool> Initialize(EditorServiceGame editorGame)
    {
        game = (EntityHierarchyEditorGame)editorGame;
        camera = Services.Get<IEditorGameCameraService>() as EditorGameCameraService;
        if (viewport is null || camera is null)
            return Task.FromResult(true);

        // After the scene and the gizmos of the frame; before the scripts, which move the camera of the active view
        renderer = new ViewRenderer(this, game)
        {
            UpdateOrder = -10000,
            DrawOrder = game.EditorSceneSystem.DrawOrder + 1,
        };
        game.GameSystems.Add(renderer);
        return Task.FromResult(true);
    }

    /// <summary>
    /// Makes the view the user points at active, between two frames, once no mouse button is down (game thread).
    /// </summary>
    private void UpdateActiveView()
    {
        if (viewport is null || camera is null || game is null)
            return;

        var requested = viewport.RequestedActiveView;
        if (requested == activeView)
            return;
        // A drag (a camera move, a gizmo) stays in the view it started in
        if (game.Input.HasDownMouseButtons || camera.IsControllingMouse)
            return;
        // A view that was closed meanwhile
        viewport.GetViews(viewInfos);
        if (!viewInfos.Any(x => x.Id == requested))
            return;

        // The settings of the scene save the camera of every view, the primary one too while another view is active
        SetInactiveState(activeView, GetOrCreateView(activeView), camera.CaptureViewState());
        var next = GetOrCreateView(requested);
        next.Camera ??= CreateState(EditorViewCamera.Perspective);
        camera.RestoreViewState(next.Camera);
        activeView = requested;
        savedCameras.TryRemove(requested, out _);
        viewport.ActiveView = requested;
        ActiveViewChanged?.Invoke(this, new EditorGameViewCameraInfo(requested, next.Camera.IsOrthographic, next.Camera.OrthographicSize));
    }

    /// <summary>
    /// Draws the views other than the active one, and hands them to the viewport (game thread, while the game draws).
    /// </summary>
    private void DrawOtherViews(RenderDrawContext drawContext, TimeSpan time)
    {
        if (viewport is null || camera is null || game is null || failed)
            return;

        viewport.GetViews(viewInfos);
        // At a lower rate than the active view; a view that has nothing to show yet, or whose size or camera changed, at once
        var isDue = schedule.IsDue(time);
        // The gizmos, the grid and the orientation cube fit each view's camera, then the active view's camera again
        viewDependents.Clear();
        viewDependents.AddRange(game.EditorServices.Services.OfType<IEditorGameViewDependent>());
        var drewAny = false;
        try
        {
            foreach (var info in viewInfos)
            {
                if (info.Id == activeView || info.Width <= 0 || info.Height <= 0)
                    continue;
                var view = GetOrCreateView(info.Id);
                if (view.Camera is null)
                    SetInactiveState(info.Id, view, CreateState(EditorViewCamera.Perspective));
                if (!isDue && !view.NeedsDraw(info.Width, info.Height))
                    continue;
                view.IsDirty = false;
                drewAny = true;
                DrawView(drawContext, view, info.Width, info.Height);
                viewport.PresentView(info.Id, drawContext.CommandList, view.Target!);
            }
        }
        catch (Exception e)
        {
            // The other views stop, the active one keeps working
            failed = true;
            controller.Logger.Error("The other views of the scene could not be drawn; they stop until the editor is opened again.", e);
        }
        finally
        {
            if (drewAny)
                PrepareViewDependents(new EditorGameViewCamera(camera.Component, game.GraphicsDevice.Presenter.BackBuffer.Height, true));
        }
    }

    private void PrepareViewDependents(EditorGameViewCamera view)
    {
        for (var i = viewDependents.Count - 1; i >= 0; i--)
        {
            var dependent = viewDependents[i];
            try
            {
                dependent.PrepareView(view);
            }
            catch (Exception e)
            {
                // Not called again this frame
                viewDependents.RemoveAt(i);
                controller.Logger.Error($"{dependent.GetType().Name} could not fit what it draws to a view of the scene.", e);
            }
        }
    }

    private void DrawView(RenderDrawContext drawContext, ViewState view, int width, int height)
    {
        var game = this.game!;
        var commandList = drawContext.CommandList;
        var backBuffer = game.GraphicsDevice.Presenter.BackBuffer;
        var depthFormat = game.GraphicsDevice.Presenter.DepthStencilBuffer?.Format ?? PixelFormat.D24_UNorm_S8_UInt;
        view.EnsureTargets(game.GraphicsDevice, width, height, backBuffer.Format, depthFormat);
        UpdateViewCamera(view, width, height);

        using (drawContext.PushRenderTargetsAndRestore())
        {
            commandList.ResourceBarrierTransition(view.Target!, BarrierLayout.RenderTarget);
            commandList.ResourceBarrierTransition(view.Depth!, BarrierLayout.DepthStencilWrite);
            var background = game.GraphicsDevice.ColorSpace == ColorSpace.Linear ? EditorServiceGame.EditorBackgroundColorHdr : EditorServiceGame.EditorBackgroundColorLdr;
            commandList.Clear(view.Target!, background);
            commandList.Clear(view.Depth!, DepthStencilClearOptions.DepthBuffer | DepthStencilClearOptions.Stencil);
            commandList.SetRenderTargetAndViewport(view.Depth, view.Target);

            var renderContext = drawContext.RenderContext;
            using (renderContext.PushTagAndRestore(IsSecondaryViewKey, true))
            {
                // The scene, then the gizmos over it, as the two scene systems draw the active view. The gizmos, the grid and
                // the orientation cube are fitted to this view's camera first, so the processors of the editor scene run again.
                PrepareViewDependents(new EditorGameViewCamera(view.Component, height, false));
                DrawSceneSystem(drawContext, game.SceneSystem, view.Component, false);
                DrawSceneSystem(drawContext, game.EditorSceneSystem, view.Component, true);
            }
        }
    }

    private static void DrawSceneSystem(RenderDrawContext drawContext, SceneSystem sceneSystem, CameraComponent viewCamera, bool runProcessors)
    {
        var compositor = sceneSystem.GraphicsCompositor;
        if (compositor is null || sceneSystem.SceneInstance is null)
            return;

        var topLevel = compositor.Game as EditorTopLevelCompositor;
        var previousCamera = topLevel?.ExternalCamera;
        if (topLevel is not null)
            topLevel.ExternalCamera = viewCamera;
        try
        {
            // As SceneSystem.Draw does. The processors of the game's scene don't run again; those of the editor scene do, to
            // take the gizmos fitted to this view
            drawContext.RenderContext.Reset();
            if (runProcessors)
            {
                using (drawContext.RenderContext.PushTagAndRestore(GraphicsCompositor.Current, compositor))
                {
                    sceneSystem.SceneInstance.Draw(drawContext.RenderContext);
                }
            }
            drawContext.ResourceGroupAllocator.Flush();
            drawContext.QueryManager.Flush();
            using (drawContext.RenderContext.PushTagAndRestore(SceneInstance.Current, sceneSystem.SceneInstance))
            {
                compositor.Draw(drawContext);
            }
        }
        finally
        {
            if (topLevel is not null)
                topLevel.ExternalCamera = previousCamera;
        }
    }

    private void UpdateViewCamera(ViewState view, int width, int height)
    {
        var main = camera!.Component;
        var state = view.Camera!;
        var component = view.Component;
        component.NearClipPlane = main.NearClipPlane;
        component.FarClipPlane = main.FarClipPlane;
        component.VerticalFieldOfView = main.VerticalFieldOfView;
        component.Projection = state.IsOrthographic ? CameraProjectionMode.Orthographic : CameraProjectionMode.Perspective;
        component.OrthographicSize = state.OrthographicSize;
        component.AspectRatio = width / (float)height;
        component.ViewMatrix = EditorGameCameraService.ComputeViewMatrix(state.Position, state.Yaw, state.Pitch);
        component.Update();
    }

    /// <summary>
    /// Gets the camera a view starts with (game thread).
    /// </summary>
    private EditorCameraViewState CreateState(EditorViewCamera viewCamera)
    {
        // A perspective view starts where the primary view is; an orthographic view looks at the same point
        var primary = activeView == IEditorGameMultiViewport.PrimaryView || !views.TryGetValue(IEditorGameMultiViewport.PrimaryView, out var primaryView) || primaryView.Camera is null
            ? camera!.CaptureViewState()
            : primaryView.Camera;
        if (viewCamera == EditorViewCamera.Perspective)
            return primary with { IsOrthographic = false };

        var direction = viewCamera switch
        {
            EditorViewCamera.Top => -Vector3.UnitY,
            EditorViewCamera.Bottom => Vector3.UnitY,
            EditorViewCamera.Front => Vector3.UnitZ,
            EditorViewCamera.Back => -Vector3.UnitZ,
            EditorViewCamera.Left => Vector3.UnitX,
            EditorViewCamera.Right => -Vector3.UnitX,
            _ => throw new ArgumentOutOfRangeException(nameof(viewCamera)),
        };
        var target = primary.Target ?? Vector3.Zero;
        var (yaw, pitch) = EditorGameCameraService.ToYawPitch(direction);
        return new EditorCameraViewState(target - direction * OrthographicDistance, yaw, pitch, true, DefaultOrthographicSize) { Target = target };
    }

    private ViewState GetOrCreateView(int viewId)
    {
        if (!views.TryGetValue(viewId, out var view))
            views.Add(viewId, view = new ViewState());
        return view;
    }

    /// <summary>
    /// The camera of a view, and what it is drawn into while it is not active.
    /// </summary>
    private sealed class ViewState : IDisposable
    {
        public EditorCameraViewState? Camera { get; set; }

        public CameraComponent Component { get; } = new() { UseCustomViewMatrix = true, UseCustomAspectRatio = true };

        public Texture? Target { get; private set; }

        public Texture? Depth { get; private set; }

        /// <summary>
        /// Gets or sets whether the camera of the view changed since it was last drawn.
        /// </summary>
        public bool IsDirty { get; set; }

        /// <summary>
        /// Gets whether the view must be drawn at this frame whatever the schedule: it was never drawn, or its size or
        /// camera changed.
        /// </summary>
        public bool NeedsDraw(int width, int height) => IsDirty || Target is not { } target || target.Width != width || target.Height != height;

        public void EnsureTargets(GraphicsDevice device, int width, int height, PixelFormat format, PixelFormat depthFormat)
        {
            if (Target is { } target && target.Width == width && target.Height == height && target.Format == format)
                return;
            Dispose();
            Target = Texture.New2D(device, width, height, format, TextureFlags.RenderTarget | TextureFlags.ShaderResource);
            Depth = Texture.New2D(device, width, height, depthFormat, TextureFlags.DepthStencil);
        }

        public void Dispose()
        {
            Target?.Dispose();
            Target = null;
            Depth?.Dispose();
            Depth = null;
        }
    }

    /// <summary>
    /// The game system that swaps the camera of the active view before the scripts run, and draws the other views after the
    /// scene and the gizmos.
    /// </summary>
    private sealed class ViewRenderer : GameSystemBase
    {
        private readonly EditorGameMultiViewService service;
        private RenderDrawContext? drawContext;

        public ViewRenderer(EditorGameMultiViewService service, EntityHierarchyEditorGame game)
            : base(game.Services)
        {
            this.service = service;
            Enabled = true;
            Visible = true;
        }

        public override void Update(GameTime gameTime)
        {
            service.UpdateActiveView();
        }

        public override void Draw(GameTime gameTime)
        {
            var game = (Game)Game;
            drawContext ??= new RenderDrawContext(Services, RenderContext.GetShared(Services), game.GraphicsContext);
            service.DrawOtherViews(drawContext, gameTime.Total);
        }
    }
}
