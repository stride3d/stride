using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using System;
using System.Threading.Tasks;
using Stride.Core;
using System.Numerics;

namespace ##Namespace##;

/// <summary>
/// The default script for the scene editor camera.
/// </summary>
public class ##Scriptname## : AsyncScript
{
    private static readonly Vector3 ForwardVector = new Vector3(0, 0, -1);

    private Vector3 position;
    private Vector3 targetPos;

    /// <summary>
    /// Gets the camera component used to visualized the scene.
    /// </summary>
    private CameraComponent Component => Entity?.Get<CameraComponent>();

    /// <summary>
    /// Gets or sets the mouse wheel zoom speed factor.
    /// </summary>
    /// <value>
    /// The mouse wheel zoom speed factor.
    /// </value>
    public float MouseWheelZoomSpeedFactor { get; set; } = 0.01f;

    /// <summary>
    /// Gets or sets the Yaw rotation of the camera.
    /// </summary>
    private float Yaw { get; set; }

    /// <summary>
    /// Gets or sets the Pitch rotation of the camera.
    /// </summary>
    private float Pitch { get; set; }

    public Entity Target { get; set; }

    public float DistanceFromTarget { get; set; } = 10.0f;

    public Vector3 OffsetFromTarget { get; set; }

    public override async Task Execute()
    {
        // set the camera values
        var backBuffer = GraphicsDevice.Presenter.BackBuffer;
        if (backBuffer != null)
        {
            Component.AspectRatio = backBuffer.Width / (float)backBuffer.Height;
        }
        Component.NearClipPlane = CameraComponent.DefaultNearClipPlane;
        Component.FarClipPlane = CameraComponent.DefaultFarClipPlane;
        Component.UseCustomViewMatrix = true;
        Component.UseCustomAspectRatio = true;
        Reset();

        Input.LockMousePosition(true);

        while (true)
        {
            UpdateCamera();

            await Script.NextFrame();
        }
    }

    public void Reset()
    {
        Pitch =
            (float)
                Math.Atan2(
                    2 * Entity.Transform.Rotation.X * Entity.Transform.Rotation.W -
                    2 * Entity.Transform.Rotation.Y * Entity.Transform.Rotation.Z,
                    1 - 2 * Entity.Transform.Rotation.X * Entity.Transform.Rotation.X -
                    2 * Entity.Transform.Rotation.Z * Entity.Transform.Rotation.Z);
        ;
        position = Entity.Transform.Position;
    }

    /// <summary>
    /// Update the camera parameters.
    /// </summary>
    protected virtual void UpdateCamera()
    {
        if (Target == null) return;

        var rotate = Input.IsMousePositionLocked;

        if (Input.IsKeyReleased(Keys.LeftAlt) || Input.IsKeyReleased(Keys.RightAlt) || Input.IsKeyReleased(Keys.Escape))
        {
            if (rotate)
            {
                Input.UnlockMousePosition();
            }
            else
            {
                Input.LockMousePosition(true);
            }
        }

        // Compute base vectors for camera movement
        var rotation = Matrix4x4.CreateFromYawPitchRoll(Yaw, Pitch, 0);
        var direction = Vector3.Normalize(Vector3.TransformNormal(ForwardVector, rotation));

        if (Math.Abs(Input.MouseWheelDelta) > MathUtil.ZeroTolerance)
        {
            DistanceFromTarget -= MouseWheelZoomSpeedFactor * Input.MouseWheelDelta;
            if (DistanceFromTarget < 0.0f)
            {
                DistanceFromTarget = 0.0f;
            }
        }

        targetPos = new Vector3(Target.Transform.WorldMatrix.Translation.X, Entity.Transform.WorldMatrix.Translation.Y, Entity.Transform.WorldMatrix.Translation.Z);
        targetPos += OffsetFromTarget;
        position = targetPos - direction * DistanceFromTarget;
        (targetPos - position).Length();

        // Update the camera view matrix
        UpdateViewMatrix();
    }

    private void UpdateViewMatrix()
    {
        var camera = Component;
        if (camera == null) return;

        var rotation = Quaternion.Inverse(Quaternion.CreateFromYawPitchRoll(Yaw, Pitch, 0));
        var viewMatrix = Matrix4x4.CreateTranslation(-position) * Matrix4x4.CreateFromQuaternion(rotation);
        camera.ViewMatrix = viewMatrix;
    }
}
