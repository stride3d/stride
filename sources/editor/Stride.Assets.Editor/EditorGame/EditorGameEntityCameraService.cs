// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Tools;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Core.Mathematics;
using Stride.Editor.EditorGame.Game;
using Stride.Editor.Engine;
using Stride.Engine;
using Stride.Input;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// The camera of the game of an entity hierarchy editor. It opens at the default position of the WPF scene editor,
/// looking at the origin, and moves with the mouse and the keyboard as in the WPF scene editor.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Right button: look around; with W, A, S, D, Q, E or the arrows (Shift: faster), move; with the wheel, change the speed.</item>
/// <item>Middle button: pan. Middle and right buttons: move forward and backward.</item>
/// <item>Wheel: zoom. Alt and right button: zoom. Alt and left button: orbit.</item>
/// <item>+ and -: change the speed.</item>
/// </list>
/// The key bindings are the defaults of the WPF scene editor settings; the settings are not read yet.
/// </remarks>
[EditorGameService(typeof(EntityHierarchyEditorController), Order = 110)]
public sealed class EditorGameEntityCameraService : EditorGameCameraService
{
    /// <summary>
    /// The movement speeds the speed keys step through, as in the WPF scene editor.
    /// </summary>
    private static readonly float[] AvailableMovementSpeeds = [0.05f, 0.1f, 0.5f, 1.0f, 1.5f, 2.5f, 5.0f, 10.0f, 15.0f, 30.0f, 45.0f, 60.0f, 80.0f, 100.0f, 130.0f, 180.0f, 250.0f, 400.0f, 500.0f, 700.0f, 1000.0f];

    // The default key bindings of the WPF scene editor (SceneEditorSettings)
    private const Keys MoveCamForward = Keys.W;
    private const Keys MoveCamBackward = Keys.S;
    private const Keys MoveCamLeft = Keys.A;
    private const Keys MoveCamRight = Keys.D;
    private const Keys MoveCamUpward = Keys.E;
    private const Keys MoveCamDownward = Keys.Q;
    private const Keys IncreaseCamSpeed = Keys.OemPlus;
    private const Keys DecreaseCamSpeed = Keys.OemMinus;
    private const bool InvertPanningAxis = true;

    private const float PanningSpeedModifier = 0.033f;

    private float revolutionRadius;
    private Vector3 targetPos;

    public EditorGameEntityCameraService(IEditorGameController controller)
        : base(controller)
    {
    }

    /// <inheritdoc />
    public override void ResetCamera(Vector3 viewDirection)
    {
        var isViewVertical = MathUtil.NearEqual(viewDirection.X, 0) && MathUtil.NearEqual(viewDirection.Z, 0);
        SetCurrentYaw(isViewVertical ? 0 : MathF.Atan2(-viewDirection.X, -viewDirection.Z));

        var horizontalViewDirection = new Vector2(viewDirection.X, viewDirection.Z);
        SetCurrentPitch(MathF.Atan2(viewDirection.Y, horizontalViewDirection.Length()));
    }

    /// <summary>
    /// Moves the camera to look at an entity (game thread).
    /// </summary>
    /// <param name="target">The entity to look at.</param>
    /// <param name="keepActualTargetDistance"><c>true</c> to stay at the current distance, <c>false</c> to frame the entity.</param>
    /// <param name="meshIndex">The mesh of the model of the entity to look at, or -1 for the whole entity.</param>
    /// <param name="deltaDistance">A distance to add to the current one, when it is kept.</param>
    /// <param name="targetOffset">An offset of the point to look at.</param>
    public void SetTarget(Entity target, bool keepActualTargetDistance, int meshIndex = -1, float deltaDistance = 0, Vector3? targetOffset = null)
    {
        var ypr = Quaternion.RotationYawPitchRoll(Yaw, Pitch, 0);
        var direction = Vector3.TransformNormal(ForwardVector, Matrix.RotationQuaternion(ypr));
        var sphere = meshIndex == -1 ? target.CalculateBoundSphere() : target.CalculateBoundSphere(false, model => [model.Meshes[meshIndex]]);
        targetPos = sphere.Center;

        if (targetOffset.HasValue)
        {
            targetPos += targetOffset.Value;
        }
        float distance;
        if (keepActualTargetDistance)
        {
            distance = (targetPos - Position).Length() + deltaDistance;
        }
        else
        {
            distance = 3 * (MathF.Abs(sphere.Radius) > MathUtil.ZeroTolerance ? sphere.Radius : 0.5f * SceneUnit);
        }

        SetCurrentPosition(targetPos - direction * distance);
        revolutionRadius = (targetPos - Position).Length();
    }

    /// <inheritdoc />
    protected override void Reset()
    {
        base.Reset();

        SetCurrentPitch(DefaultPitch);
        SetCurrentYaw(DefaultYaw);

        targetPos = Vector3.Zero;
        revolutionRadius = (targetPos - Position).Length();
    }

    /// <inheritdoc />
    protected override void UpdateCamera()
    {
        base.UpdateCamera();

        // Movement speed control
        if (Game.Input.IsKeyPressed(IncreaseCamSpeed) || Game.Input.IsKeyPressed(Keys.Add))
            ChangeMovementSpeed(+1);
        else if (Game.Input.IsKeyPressed(DecreaseCamSpeed) || Game.Input.IsKeyPressed(Keys.Subtract))
            ChangeMovementSpeed(-1);

        var duplicating = Game.Input.IsKeyDown(Keys.LeftCtrl) || Game.Input.IsKeyDown(Keys.RightCtrl);
        if (duplicating)
            return;

        if (!IsMouseAvailable)
            return;

        var yaw = Yaw;
        var pitch = Pitch;
        var position = Position;
        var input = GetInput();

        UpdateCameraBase(ref yaw, ref pitch, ref position, IsOrthographic, input);

        SetCurrentPitch(pitch);
        SetCurrentPosition(position);
        SetCurrentYaw(yaw);
        UpdateViewMatrix();

        var isAnyMouseButtonDown = Game.Input.IsMouseButtonDown(MouseButton.Left) || Game.Input.IsMouseButtonDown(MouseButton.Middle) || Game.Input.IsMouseButtonDown(MouseButton.Right);
        var shouldControlMouse = IsMouseAvailable && isAnyMouseButtonDown && (input.IsMoving || input.IsPanning || input.IsRotating || input.IsOrbiting || input.IsZooming);
        if (shouldControlMouse != IsControllingMouse)
        {
            IsControllingMouse = shouldControlMouse;

            if (IsControllingMouse)
            {
                Game.Input.LockMousePosition();
                Game.IsMouseVisible = false;
            }
            else
            {
                Game.Input.UnlockMousePosition();
                Game.IsMouseVisible = true;
            }
        }
    }

    /// <summary>
    /// Steps the movement speed up or down the speeds of the WPF scene editor.
    /// </summary>
    private void ChangeMovementSpeed(int step)
    {
        var index = Array.FindIndex(AvailableMovementSpeeds, x => MathUtil.NearEqual(x, MoveSpeed));
        // A speed out of the steps (the default one) starts from the third step, as in the WPF scene editor
        if (index < 0)
            index = 2;
        MoveSpeed = AvailableMovementSpeeds[Math.Clamp(index + step, 0, AvailableMovementSpeeds.Length - 1)];
    }

    private CameraInput GetInput()
    {
        // The active tool of the view may take the middle or right drags
        var tools = Game.EditorServices.Get<IEditorGameToolService>();
        var lbDown = Game.Input.IsMouseButtonDown(MouseButton.Left);
        var mbDown = Game.Input.IsMouseButtonDown(MouseButton.Middle) && (tools?.IsDefaultInputOn(EditorViewDefaultInputs.CameraMiddleDrag) ?? true);
        var rbDown = Game.Input.IsMouseButtonDown(MouseButton.Right) && (tools?.IsDefaultInputOn(EditorViewDefaultInputs.CameraRightDrag) ?? true);
        var isAltDown = Game.Input.IsKeyDown(Keys.LeftAlt) || Game.Input.IsKeyDown(Keys.RightAlt);

        return new CameraInput
        {
            IsShiftDown = Game.Input.IsKeyDown(Keys.LeftShift) || Game.Input.IsKeyDown(Keys.RightShift),
            IsPanning = mbDown && !rbDown,
            IsRotating = !isAltDown && !mbDown && rbDown,
            IsMoving = !isAltDown && mbDown && rbDown,
            IsZooming = (isAltDown && !lbDown && !mbDown && rbDown) || MathF.Abs(Game.Input.MouseWheelDelta) > MathUtil.ZeroTolerance,
            IsOrbiting = isAltDown && lbDown && !mbDown && !rbDown,
        };
    }

    private void UpdateCameraBase(ref float yaw, ref float pitch, ref Vector3 position, bool asOrthographic, CameraInput input)
    {
        // Get clamped delta time (more stable during lags)
        var dt = Math.Min((float)Game.UpdateTime.Elapsed.TotalSeconds, 1.0f);

        // Compute translation speed according to framerate and modifiers
        var baseSpeed = MoveSpeed * SceneUnit * (input.IsShiftDown ? 10 : 1) * (1f / 60f);

        var zoomDelta = 0f;

        // Update yaw and pitch first to keep dependencies on 'rotation' up to date with current frame changes
        if (input.IsMoving || input.IsRotating || input.IsOrbiting)
        {
            var rotationSpeed = RotationSpeed * (input.IsOrbiting ? 2 : 1); // we want to rotate faster when rotating around an object.
            yaw -= 1.333f * Game.Input.MouseDelta.X * rotationSpeed; // we want to rotate faster Horizontally and Vertically
            if (input.IsRotating || input.IsOrbiting)
                pitch = MathUtil.Clamp(pitch - Game.Input.MouseDelta.Y * rotationSpeed, -MathUtil.PiOverTwo, MathUtil.PiOverTwo);
        }

        var rotation = Quaternion.RotationYawPitchRoll(yaw, pitch, 0);

        // If scene has changed since last time
        if (asOrthographic && Game.ContentScene?.Entities != null)
        {
            // Position the camera outside the bounding volume
            var sceneBounds = BoundingSphere.Empty;
            foreach (var entity in Game.ContentScene.Entities)
            {
                var sphere = entity.CalculateBoundSphere();
                sceneBounds = BoundingSphere.Merge(sceneBounds, sphere);
            }
            revolutionRadius = Math.Max(10f, sceneBounds.Radius * 2f);

            position = targetPos - Vector3.Normalize(Vector3.Transform(ForwardVector, rotation)) * revolutionRadius;
        }

        // Dolly (WASD model/arrow keys)
        if (input.IsPanning || input.IsMoving || input.IsRotating)
        {
            var inputSystem = Game.Input;
            float x, y, z;
            x = y = z = 0f;

            if (inputSystem.IsKeyDown(MoveCamLeft) || inputSystem.IsKeyDown(Keys.Left))
                x -= 1f;
            if (inputSystem.IsKeyDown(MoveCamRight) || inputSystem.IsKeyDown(Keys.Right))
                x += 1f;
            if (inputSystem.IsKeyDown(MoveCamBackward) || inputSystem.IsKeyDown(Keys.Down))
                z -= 1f;
            if (inputSystem.IsKeyDown(MoveCamForward) || inputSystem.IsKeyDown(Keys.Up))
                z += 1f;
            if (inputSystem.IsKeyDown(MoveCamDownward) || inputSystem.IsKeyDown(Keys.PageDown))
                y -= 1f;
            if (inputSystem.IsKeyDown(MoveCamUpward) || inputSystem.IsKeyDown(Keys.PageUp))
                y += 1f;

            if (asOrthographic)
            {
                zoomDelta += y;
                y = z;
                z = 0f;
            }

            // Dynamically change MovementSpeed of Camera by using MouseWheel while moving around
            if (input.IsRotating)
            {
                if (Game.Input.MouseWheelDelta > 0.0f)
                    ChangeMovementSpeed(+1);
                if (Game.Input.MouseWheelDelta < 0.0f)
                    ChangeMovementSpeed(-1);
            }

            var localDirection = Vector3.Normalize(new Vector3(x, y, -z));
            position += Vector3.Transform(localDirection, rotation) * baseSpeed * dt * 60f;
        }

        // Pan
        if (input.IsPanning)
        {
            var panningSpeed = asOrthographic ? Component.OrthographicSize : revolutionRadius;
            panningSpeed *= MouseMoveSpeedFactor * baseSpeed * PanningSpeedModifier;
            if (InvertPanningAxis)
                panningSpeed = -panningSpeed;

            var localDirection = new Vector3(Game.Input.MouseDelta.X, -Game.Input.MouseDelta.Y, 0f);
            position += Vector3.Transform(localDirection, rotation) * panningSpeed;
        }

        // Move
        if (input.IsMoving)
        {
            if (asOrthographic)
            {
                zoomDelta -= MouseMoveSpeedFactor * Game.Input.MouseDelta.Y;
            }
            else
            {
                var forward = Vector3.Transform(ForwardVector, rotation);
                var projectedForward = Vector3.Normalize(new Vector3(forward.X, 0, forward.Z)); // camera forward vector project on the XZ plane
                position -= projectedForward * baseSpeed * MouseMoveSpeedFactor * Game.Input.MouseDelta.Y;
            }
        }

        // Forward/backward
        if (!input.IsRotating && input.IsZooming)
        {
            if (asOrthographic)
            {
                zoomDelta += MouseWheelZoomSpeedFactor * Game.Input.MouseWheelDelta;
                if (Game.Input.HasDownMouseButtons)
                {
                    zoomDelta += MouseMoveSpeedFactor * (Game.Input.MouseDelta.X + Game.Input.MouseDelta.Y);
                }
            }
            else
            {
                // Perspective
                var forward = Vector3.Transform(ForwardVector, rotation);
                position += forward * MouseWheelZoomSpeedFactor * Game.Input.MouseWheelDelta * 0.1f;    // Multiply by 0.1f so it matches the zoom "speed" of the orthographic mode.
                if (Game.Input.HasDownMouseButtons)
                {
                    position += forward * baseSpeed * MouseMoveSpeedFactor * (Game.Input.MouseDelta.X + Game.Input.MouseDelta.Y);
                }
                revolutionRadius = Vector3.Distance(targetPos, position);
            }
        }

        // Apply zoom
        if (asOrthographic)
        {
            var newOrthographicSize = Component.OrthographicSize - baseSpeed * zoomDelta;
            if (newOrthographicSize > 0)
                Component.OrthographicSize = newOrthographicSize;
        }
        else
        {
            var newFov = Component.VerticalFieldOfView - baseSpeed * zoomDelta;
            newFov = newFov > 120 ? 120 : newFov < 20 ? 20 : newFov;
            Component.VerticalFieldOfView = newFov;
        }

        // Orbit
        // The connection between position and target is pretty straight-forward
        var direction = Vector3.Transform(ForwardVector, rotation);
        if (input.IsOrbiting)
        {
            position = targetPos - direction * revolutionRadius;
        }
        else
        {
            targetPos = position + direction * revolutionRadius;
        }
    }

    /// <summary>
    /// What the mouse buttons and the keys ask the camera to do this frame.
    /// </summary>
    private struct CameraInput
    {
        public bool IsPanning;
        public bool IsRotating;
        public bool IsMoving;
        public bool IsZooming;
        public bool IsOrbiting;
        public bool IsShiftDown;
    }
}
