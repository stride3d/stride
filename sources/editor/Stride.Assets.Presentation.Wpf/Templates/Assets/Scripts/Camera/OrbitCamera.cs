using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using System;
using System.Threading.Tasks;
using Stride.Core;

namespace Stride.Scripts
{
    /// <summary>
    /// The default script for the scene editor camera.
    /// </summary>
    public class OrbitCamera : AsyncScript
    {
        private static readonly Vector3 ForwardVector = new Vector3(0, 0, -1);

        private float desiredYaw;
        private Vector3 position;
        private Vector3 targetPos;

        /// <summary>
        /// Gets the camera component used to visualized the scene.
        /// </summary>
        private CameraComponent Component => Entity?.Get<CameraComponent>();

        /// <summary>
        /// Gets or sets the rotation speed of the camera (in radian/screen units)
        /// </summary>
        public float RotationSpeed { get; set; } = 2.355f;

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
            // Set the camera values
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

            if (!Platform.IsWindowsDesktop)
            {
                Input.Gestures.Add(new GestureConfigDrag());
            }

            while (true)
            {
                UpdateCamera();

                await Script.NextFrame();
            }
        }

        public void Reset()
        {
            desiredYaw =
                Yaw =
                    (float)
                        Math.Asin(2 * Entity.Transform.Rotation.X * Entity.Transform.Rotation.Y +
                                  2 * Entity.Transform.Rotation.Z * Entity.Transform.Rotation.W);
            ;
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
            var rotationDelta = Input.MouseDelta;

            if (Input.IsKeyReleased(Keys.LeftAlt) || Input.IsKeyReleased(Keys.RightAlt) || Input.IsKeyReleased(Keys.Escape))
            {
                if (rotate)
                {
                    Input.UnlockMousePosition();
                    rotate = false;
                }
                else
                {
                    Input.LockMousePosition(true);
                    rotate = true;
                }
            }

            foreach (var gestureEvent in Input.GestureEvents)
            {
                switch (gestureEvent.Type)
                {
                    case GestureType.Drag:
                        {
                            var drag = (GestureEventDrag)gestureEvent;
                            rotate = true;
                            rotationDelta = drag.DeltaTranslation;
                        }
                        break;

                    case GestureType.Flick:
                        break;

                    case GestureType.LongPress:
                        break;

                    case GestureType.Composite:
                        break;

                    case GestureType.Tap:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (rotate)
            {
                // Take shortest path
                var deltaYaw = (desiredYaw - Yaw) % MathUtil.TwoPi;
                if (deltaYaw < 0)
                    deltaYaw += MathUtil.TwoPi;
                if (deltaYaw > MathUtil.Pi)
                    deltaYaw -= MathUtil.TwoPi;
                desiredYaw = Yaw + deltaYaw;

                desiredYaw = Yaw -= 1.333f * rotationDelta.X * RotationSpeed;
                // We want to rotate faster Horizontally and Vertically
                Pitch = MathUtil.Clamp(Pitch - rotationDelta.Y * RotationSpeed, -MathUtil.PiOverTwo, MathUtil.PiOverTwo);
            }

            // Compute base vectors for camera movement
            var rotation = Matrix.RotationYawPitchRoll(Yaw, Pitch, 0);
            var direction = Vector3.Normalize(Vector3.TransformNormal(ForwardVector, rotation));

            if (Math.Abs(Input.MouseWheelDelta) > MathUtil.ZeroTolerance)
            {
                DistanceFromTarget -= MouseWheelZoomSpeedFactor * Input.MouseWheelDelta;
                if (DistanceFromTarget < 0.0f)
                {
                    DistanceFromTarget = 0.0f;
                }
            }

            targetPos = Target.Transform.WorldMatrix.TranslationVector;
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

            var rotation = Quaternion.Invert(Quaternion.RotationYawPitchRoll(Yaw, Pitch, 0));
            var viewMatrix = Matrix.Translation(-position) * Matrix.RotationQuaternion(rotation);
            camera.ViewMatrix = viewMatrix;
        }
    }
}
