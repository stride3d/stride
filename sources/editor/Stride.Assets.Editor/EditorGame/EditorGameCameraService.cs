// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Threading.Tasks;
using Stride.Assets.Presentation.AssetEditors;
using Stride.Core.Mathematics;
using Stride.Editor.EditorGame.Game;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Input;
using Stride.Rendering.Compositing;
using Stride.Assets.Editor.AssetEditors;
using Stride.Assets.Editor.AssetEditors.GameEditor.Game;
using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Game;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// The camera the game of an editor renders the scene with: an entity of the editor scene, given to the top-level
/// compositor of the game as its external camera.
/// </summary>
/// <remarks>
/// The view models of the editor reach it on the UI thread through <see cref="IEditorGameCameraViewModelService"/>: its
/// setters post their change to the game thread.
/// </remarks>
public abstract class EditorGameCameraService : EditorGameMouseServiceBase, IEditorGameCameraService, IEditorGameCameraViewModelService
{
    public static readonly Vector3 DefaultPosition = new(4, 2, 4);
    public static readonly float DefaultPitch = -MathUtil.Pi / 12.0f;
    public static readonly float DefaultYaw = MathUtil.Pi / 4.0f;
    public static readonly float DefaultMoveSpeed = 3.0f;
    protected static readonly Vector3 UpVector = new(0, 1, 0);
    protected static readonly Vector3 ForwardVector = new(0, 0, -1);

    private Entity? cameraEditorEntity;

    protected EditorGameCameraService(IEditorGameController controller)
    {
        Controller = controller;
    }

    public CameraComponent Component { get; private set; } = null!;

    public Matrix ViewMatrix => Component.ViewMatrix;

    public Matrix ProjectionMatrix
    {
        get
        {
            Component.Update();
            return Component.ProjectionMatrix;
        }
    }

    public bool IsMoving => Game.Input.IsMouseButtonDown(MouseButton.Left) || Game.Input.IsMousePositionLocked;

    public float AspectRatio => Component.AspectRatio;

    public float VerticalFieldOfView => Component.VerticalFieldOfView;

    public float NearPlane => Component.NearClipPlane;

    public float FarPlane => Component.FarClipPlane;

    public float SceneUnit { get; set; } = 1.0f;

    public bool IsOrthographic
    {
        get => Component.Projection == CameraProjectionMode.Orthographic;
        set => Component.Projection = value ? CameraProjectionMode.Orthographic : CameraProjectionMode.Perspective;
    }

    public float MoveSpeed { get; set; }

    public float RotationSpeed { get; set; }

    public float MouseMoveSpeedFactor { get; set; }

    public float MouseWheelZoomSpeedFactor { get; set; }

    public float RotationAdaptationSpeed { get; set; }

    public float Yaw { get; private set; }

    public float Pitch { get; private set; }

    public Vector3 Position { get; private set; }

    public override bool IsControllingMouse { get; protected set; }

    protected IEditorGameController Controller { get; }

    protected EntityHierarchyEditorGame Game { get; private set; } = null!;

    public virtual void ResetCamera(Vector3 viewDirection)
    {
        var isViewVertical = MathUtil.NearEqual(viewDirection.X, 0) && MathUtil.NearEqual(viewDirection.Z, 0);
        Yaw = isViewVertical ? 0 : MathF.Atan2(-viewDirection.X, -viewDirection.Z);

        var horizontalViewDirection = new Vector2(viewDirection.X, viewDirection.Z);
        Pitch = MathF.Atan2(viewDirection.Y, horizontalViewDirection.Length());
    }

    public void ResetCamera(CameraOrientation orientation)
    {
        var viewDirection = orientation switch
        {
            CameraOrientation.Front => Vector3.UnitZ,
            CameraOrientation.Back => -Vector3.UnitZ,
            CameraOrientation.Top => -Vector3.UnitY,
            CameraOrientation.Bottom => Vector3.UnitY,
            CameraOrientation.Left => Vector3.UnitX,
            CameraOrientation.Right => -Vector3.UnitX,
            _ => throw new ArgumentOutOfRangeException(nameof(orientation)),
        };
        ResetCamera(viewDirection);
    }

    /// <inheritdoc />
    public virtual void SwitchProjection()
    {
        IsOrthographic = !IsOrthographic;
    }

    /// <inheritdoc />
    public void LoadSettings(Vector3 position, Vector2 pitchYaw)
    {
        Controller.InvokeAsync(() =>
        {
            SetCurrentPosition(position);
            SetCurrentPitch(pitchYaw.X);
            SetCurrentYaw(pitchYaw.Y);
            // Also when the editor is hidden, and its camera inactive
            UpdateViewMatrix();
        });
    }

    /// <inheritdoc />
    public (Vector3 Position, Vector2 PitchYaw) SaveSettings()
    {
        return (Position, new Vector2(Pitch, Yaw));
    }

    void IEditorGameCameraViewModelService.SetOrthographicProjection(bool value) => Controller.InvokeAsync(() => IsOrthographic = value);

    void IEditorGameCameraViewModelService.SetOrthographicSize(float value) => Controller.InvokeAsync(() => Component.OrthographicSize = value);

    void IEditorGameCameraViewModelService.SetNearPlane(float value) => Controller.InvokeAsync(() => Component.NearClipPlane = value);

    void IEditorGameCameraViewModelService.SetFarPlane(float value) => Controller.InvokeAsync(() => Component.FarClipPlane = value);

    void IEditorGameCameraViewModelService.SetFieldOfView(float value) => Controller.InvokeAsync(() => Component.VerticalFieldOfView = value);

    void IEditorGameCameraViewModelService.ResetCamera() => Reset();

    void IEditorGameCameraViewModelService.ResetCameraOrientation(CameraOrientation orientation) => Controller.InvokeAsync(() => ResetCamera(orientation));

    /// <inheritdoc />
    protected override Task<bool> Initialize(EditorServiceGame editorGame)
    {
        Game = (EntityHierarchyEditorGame)editorGame;
        Component = new CameraComponent();
        cameraEditorEntity = new Entity("Camera Editor Entity") { Component };

        Game.EditorScene.Entities.Add(cameraEditorEntity);

        if (Game.EditorSceneSystem.GraphicsCompositor.Game is EditorTopLevelCompositor editorCameraTopLevel)
            editorCameraTopLevel.ExternalCamera = Component;

        // set the camera values
        Component.NearClipPlane = CameraComponent.DefaultNearClipPlane;
        Component.FarClipPlane = CameraComponent.DefaultFarClipPlane;
        Component.UseCustomViewMatrix = true;

        MoveSpeed = DefaultMoveSpeed;
        RotationSpeed = 0.75f * MathUtil.Pi;
        MouseMoveSpeedFactor = 100.0f;
        MouseWheelZoomSpeedFactor = 12.0f;
        RotationAdaptationSpeed = 5.0f;
        Component.UseCustomAspectRatio = true;
        Reset();

        // create the script
        Game.Script.AddTask(Update);

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public override void UpdateGraphicsCompositor(EditorServiceGame game)
    {
        base.UpdateGraphicsCompositor(game);

        if (Game.SceneSystem.GraphicsCompositor.Game is EditorTopLevelCompositor cameraTopLevel)
            cameraTopLevel.ExternalCamera = Component;
    }

    protected virtual void Reset()
    {
        Yaw = DefaultYaw;
        Pitch = DefaultPitch;
        Position = DefaultPosition;
    }

    protected virtual void SetCurrentPitch(float value)
    {
        Pitch = MathUtil.Clamp(value, -MathUtil.PiOverTwo, MathUtil.PiOverTwo);
    }

    protected virtual void SetCurrentPosition(Vector3 value)
    {
        Position = value;
    }

    protected virtual void SetCurrentYaw(float value)
    {
        Yaw = value;
    }

    protected virtual void UpdateCamera()
    {
        // Update camera ratio
        var backBuffer = Game.GraphicsDevice.Presenter.BackBuffer;
        if (backBuffer != null)
        {
            Component.AspectRatio = backBuffer.Width / (float)backBuffer.Height;
        }
    }

    protected void UpdateViewMatrix()
    {
        Component.ViewMatrix = ComputeViewMatrix(Position, Yaw, Pitch);
    }

    /// <summary>
    /// Gets the view matrix of a camera at <paramref name="position"/> that looks along <paramref name="yaw"/> and
    /// <paramref name="pitch"/>, as the editor camera does.
    /// </summary>
    internal static Matrix ComputeViewMatrix(Vector3 position, float yaw, float pitch)
    {
        var rotation = Quaternion.Invert(Quaternion.RotationYawPitchRoll(yaw, pitch, 0));
        return Matrix.Translation(-position) * Matrix.RotationQuaternion(rotation);
    }

    /// <summary>
    /// Gets the yaw and the pitch of a camera that looks along <paramref name="viewDirection"/>, as
    /// <see cref="ResetCamera(Vector3)"/> computes them.
    /// </summary>
    internal static (float Yaw, float Pitch) ToYawPitch(Vector3 viewDirection)
    {
        var isViewVertical = MathUtil.NearEqual(viewDirection.X, 0) && MathUtil.NearEqual(viewDirection.Z, 0);
        var yaw = isViewVertical ? 0 : MathF.Atan2(-viewDirection.X, -viewDirection.Z);
        var horizontalViewDirection = new Vector2(viewDirection.X, viewDirection.Z);
        var pitch = MathUtil.Clamp(MathF.Atan2(viewDirection.Y, horizontalViewDirection.Length()), -MathUtil.PiOverTwo, MathUtil.PiOverTwo);
        return (yaw, pitch);
    }

    /// <summary>
    /// Gets the state of the camera, to give it back later with <see cref="RestoreViewState"/>: the camera of one of the
    /// views of the editor (game thread).
    /// </summary>
    internal virtual EditorCameraViewState CaptureViewState()
    {
        return new EditorCameraViewState(Position, Yaw, Pitch, IsOrthographic, Component.OrthographicSize);
    }

    /// <summary>
    /// Moves the camera to a state <see cref="CaptureViewState"/> gave: another view of the editor became active (game
    /// thread).
    /// </summary>
    internal virtual void RestoreViewState(EditorCameraViewState state)
    {
        SetCurrentPosition(state.Position);
        SetCurrentPitch(state.Pitch);
        SetCurrentYaw(state.Yaw);
        IsOrthographic = state.IsOrthographic;
        Component.OrthographicSize = state.OrthographicSize;
        UpdateViewMatrix();
    }

    private async Task Update()
    {
        while (!IsDisposed)
        {
            if (IsActive)
            {
                UpdateCamera();
                UpdateViewMatrix();
            }

            await Game.Script.NextFrame();
        }
    }
}

/// <summary>
/// The state of the editor camera in one of the views of the editor (prototype of several views).
/// </summary>
/// <param name="Position">The position of the camera.</param>
/// <param name="Yaw">The yaw of the camera.</param>
/// <param name="Pitch">The pitch of the camera.</param>
/// <param name="IsOrthographic">Whether the projection is orthographic.</param>
/// <param name="OrthographicSize">The orthographic size.</param>
internal sealed record EditorCameraViewState(Vector3 Position, float Yaw, float Pitch, bool IsOrthographic, float OrthographicSize)
{
    /// <summary>
    /// Gets the point the camera orbits around, or <c>null</c> to keep the one of the camera.
    /// </summary>
    public Vector3? Target { get; init; }
}
