// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.AssetEditors;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Core.Mathematics;
using Stride.Editor.EditorGame.Game;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Input;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// The camera the game of an editor renders the scene with: an entity of the editor scene, given to the top-level
/// compositor of the game as its external camera.
/// </summary>
/// <remarks>
/// The cross-platform counterpart of the WPF <c>EditorGameCameraService</c>, without the view model side yet (projection,
/// speed and the camera settings saved with the scene).
/// </remarks>
public abstract class EditorGameCameraService : EditorGameMouseServiceBase, IEditorGameCameraService
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
        var rotation = Quaternion.Invert(Quaternion.RotationYawPitchRoll(Yaw, Pitch, 0));
        var viewMatrix = Matrix.Translation(-Position) * Matrix.RotationQuaternion(rotation);
        Component.ViewMatrix = viewMatrix;
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
