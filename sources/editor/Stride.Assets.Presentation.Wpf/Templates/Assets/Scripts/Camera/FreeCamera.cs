using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Input;
using System;
using System.Threading.Tasks;
using Stride.Core;

namespace Stride.Scripts
{
    /// <summary>
    /// The default script for the scene editor camera.
    /// </summary>
    public class FreeCamera : AsyncScript
    {
        private static readonly Vector3 UpVector = new Vector3(0, 1, 0);
        private static readonly Vector3 ForwardVector = new Vector3(0, 0, -1);

        private float desiredYaw;
        private float desiredPitch;
        private Vector3 position;

        /// <summary>
        /// Gets the camera component used to visualized the scene.
        /// </summary>
        private CameraComponent Component => Entity?.Get<CameraComponent>();

        /// <summary>
        /// Gets or sets the moving speed of the camera (in units/second).
        /// </summary>
        public float MoveSpeed { get; set; } = 3.0f;

        /// <summary>
        /// Gets or sets the rotation speed of the camera (in radian/screen units)
        /// </summary>
        public float RotationSpeed { get; set; } = 2.355f;

        /// <summary>
        /// Gets or sets the mouse move speed factor.
        /// </summary>
        /// <value>
        /// The mouse move speed factor.
        /// </value>
        public float MouseMoveSpeedFactor { get; set; } = 100.0f;

        /// <summary>
        /// Gets or sets the mouse wheel zoom speed factor.
        /// </summary>
        /// <value>
        /// The mouse wheel zoom speed factor.
        /// </value>
        public float MouseWheelZoomSpeedFactor { get; set; } = 0.1f;

        /// <summary>
        /// Gets or sets the rate at which orientation is adapted to a target value.
        /// </summary>
        /// <value>
        /// The adaptation rate.
        /// </value>
        public float RotationAdaptationSpeed { get; set; } = 5.0f;

        /// <summary>
        /// Gets or sets the Yaw rotation of the camera.
        /// </summary>
        private float Yaw { get; set; }

        /// <summary>
        /// Gets or sets the Pitch rotation of the camera.
        /// </summary>
        private float Pitch { get; set; }

        public override async Task Execute()
        {
            // set the camera values
            Component.NearClipPlane = CameraComponent.DefaultNearClipPlane;
            Component.FarClipPlane = CameraComponent.DefaultFarClipPlane;
            Component.UseCustomViewMatrix = true;
            Component.UseCustomAspectRatio = true;
            Reset();

            if (!Platform.IsWindowsDesktop)
            {
                Input.Gestures.Add(new GestureConfigDrag());
                Input.Gestures.Add(new GestureConfigDrag {RequiredNumberOfFingers = 2});
                Input.Gestures.Add(new GestureConfigTap {RequiredNumberOfTaps = 2});
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
            desiredPitch =
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
            // Update camera ratio in the update
            var backBuffer = GraphicsDevice.Presenter.BackBuffer;
            if (backBuffer != null)
            {
                Component.AspectRatio = backBuffer.Width / (float)backBuffer.Height;
            }

            // Capture/release mouse when the button is pressed/released
            if ((Input.IsMouseButtonPressed(MouseButton.Left)) || Input.IsMouseButtonPressed(MouseButton.Right) || Input.IsMouseButtonPressed(MouseButton.Middle))
            {
                Input.LockMousePosition(true);
            }
            else if (Input.IsMouseButtonReleased(MouseButton.Right) || Input.IsMouseButtonReleased(MouseButton.Left) || Input.IsMouseButtonReleased(MouseButton.Middle))
            {
                if (!Input.IsMouseButtonDown(MouseButton.Right) && !Input.IsMouseButtonDown(MouseButton.Left) && !Input.IsMouseButtonDown(MouseButton.Middle))
                    Input.UnlockMousePosition();
            }

            var rotationDelta = Input.MouseDelta;
            var touchingFingers = 0;
            var doubleTapped = false;
            foreach (var gestureEvent in Input.GestureEvents)
            {
                switch (gestureEvent.Type)
                {
                    case GestureType.Drag:
                        {
                            var drag = (GestureEventDrag)gestureEvent;
                            rotationDelta = drag.DeltaTranslation;
                            touchingFingers = drag.NumberOfFinger;
                            if (touchingFingers == 1) rotationDelta = -rotationDelta; // Reverse the delta in the case of a single finger camera move
                        }
                        break;

                    case GestureType.Flick:
                        break;

                    case GestureType.LongPress:
                        break;

                    case GestureType.Composite:
                        break;

                    case GestureType.Tap:
                        {
                            doubleTapped = true;
                        }
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // Compute translation speed according to framerate and modifiers
            float translationSpeed = MoveSpeed * (float)Game.UpdateTime.Elapsed.TotalSeconds;
            if (Input.IsKeyDown(Keys.LeftShift) || Input.IsKeyDown(Keys.RightShift))
                translationSpeed *= 10;

            var oldYaw = Yaw;
            var oldPitch = Pitch;

            // Take shortest path
            var deltaPitch = desiredPitch - Pitch;
            var deltaYaw = (desiredYaw - Yaw) % MathUtil.TwoPi;
            if (deltaYaw < 0)
                deltaYaw += MathUtil.TwoPi;
            if (deltaYaw > MathUtil.Pi)
                deltaYaw -= MathUtil.TwoPi;
            desiredYaw = Yaw + deltaYaw;

            // Revolve around current target or origin
            var isAdaptingOrientation = !MathUtil.NearEqual(deltaPitch, 0) || !MathUtil.NearEqual(deltaYaw, 0);

            // Perform orientation transition
            var rotationAdaptation = (float)Game.UpdateTime.Elapsed.TotalSeconds * RotationAdaptationSpeed;
            Yaw = Math.Abs(deltaYaw) < rotationAdaptation ? desiredYaw : Yaw + rotationAdaptation * Math.Sign(deltaYaw);
            Pitch = Math.Abs(deltaPitch) < rotationAdaptation ? desiredPitch : Pitch + rotationAdaptation * Math.Sign(deltaPitch);

            // Update rotation according to mouse deltas
            var rotateCamera = Input.IsMouseButtonDown(MouseButton.Left) || Input.IsMouseButtonDown(MouseButton.Right) || touchingFingers == 1;
            if (rotateCamera)
            {
                desiredYaw = Yaw -= 1.333f * rotationDelta.X * RotationSpeed; // We want to rotate faster Horizontally and Vertically
                desiredPitch = Pitch = MathUtil.Clamp(Pitch - rotationDelta.Y * RotationSpeed, -MathUtil.PiOverTwo, MathUtil.PiOverTwo);
            }

            // Compute base vectors for camera movement
            var rotation = Matrix.RotationYawPitchRoll(Yaw, Pitch, 0);
            var forward = Vector3.TransformNormal(ForwardVector, rotation);
            var up = Vector3.TransformNormal(UpVector, rotation);
            var right = Vector3.Cross(forward, up);

            // Update camera move: Dolly (WADS model/arrow keys)
            var movePosition = Vector3.Zero;
            if ((Input.HasDownMouseButtons() && Input.IsKeyDown(Keys.A)) || Input.IsKeyDown(Keys.Left))
                movePosition += -right;
            if ((Input.HasDownMouseButtons() && Input.IsKeyDown(Keys.D)) || Input.IsKeyDown(Keys.Right))
                movePosition += right;
            if ((Input.HasDownMouseButtons() && Input.IsKeyDown(Keys.S)) || Input.IsKeyDown(Keys.Down))
                movePosition += Component.Projection == CameraProjectionMode.Perspective ? -forward : -up;
            if ((Input.HasDownMouseButtons() && Input.IsKeyDown(Keys.W)) || Input.IsKeyDown(Keys.Up) || doubleTapped)
                movePosition += Component.Projection == CameraProjectionMode.Perspective ? forward : up;
            // Dolly (forward and backward)
            if (Input.HasDownMouseButtons() && Input.IsKeyDown(Keys.Q))
                movePosition += Component.Projection == CameraProjectionMode.Perspective ? -up : -forward;
            if (Input.HasDownMouseButtons() && Input.IsKeyDown(Keys.E))
                movePosition += Component.Projection == CameraProjectionMode.Perspective ? up : forward;

            position += (Vector3.Normalize(movePosition) * translationSpeed);

            if (doubleTapped)
            {
                desiredPitch = Pitch = oldPitch;
                desiredYaw = Yaw;

                forward = -Vector3.Transform(ForwardVector, Quaternion.RotationYawPitchRoll(Yaw, Pitch, 0));
                var projectedForward = Vector3.Normalize(new Vector3(forward.X, 0, forward.Z)); // Camera forward vector project on the XZ plane
                position -= projectedForward * translationSpeed * MouseMoveSpeedFactor;
            }

            // Dolly (top, bottom, left and right)
            if (Input.IsMouseButtonDown(MouseButton.Middle) || touchingFingers > 1)
            {
                desiredYaw = Yaw;
                desiredPitch = Pitch;

                position += -right * rotationDelta.X * MouseMoveSpeedFactor * translationSpeed;
                position += up * rotationDelta.Y * MouseMoveSpeedFactor * translationSpeed;
            }
            // Dolly (forward and backward)
            else if (Math.Abs(Input.MouseWheelDelta) > MathUtil.ZeroTolerance)
            {
                desiredPitch = Pitch = oldPitch;
                desiredYaw = Yaw;

                forward = Vector3.Transform(ForwardVector, Quaternion.RotationYawPitchRoll(Yaw, Pitch, 0));
                var projectedForward = Vector3.Normalize(new Vector3(forward.X, 0, forward.Z)); // Camera forward vector project on the XZ plane
                position -= projectedForward * translationSpeed * MouseWheelZoomSpeedFactor * -Input.MouseWheelDelta;
            }
            // Dolly
            else if (!isAdaptingOrientation)
            {
                if (Input.IsMouseButtonDown(MouseButton.Left) && !Input.IsMouseButtonDown(MouseButton.Right))
                {
                    desiredPitch = Pitch = oldPitch;
                    desiredYaw = Yaw;

                    forward = Vector3.Transform(ForwardVector, Quaternion.RotationYawPitchRoll(Yaw, Pitch, 0));
                    var projectedForward = Vector3.Normalize(new Vector3(forward.X, 0, forward.Z)); // Camera forward vector project on the XZ plane
                    position -= projectedForward * translationSpeed * MouseMoveSpeedFactor * rotationDelta.Y;
                }
                else if (Input.IsMouseButtonDown(MouseButton.Left) && Input.IsMouseButtonDown(MouseButton.Right))
                {
                    desiredYaw = Yaw = oldYaw;
                    desiredPitch = Pitch = oldPitch;

                    position += right * translationSpeed * MouseMoveSpeedFactor * rotationDelta.X;
                    position -= Vector3.UnitY * translationSpeed * MouseMoveSpeedFactor * rotationDelta.Y;
                }
            }
            else
            {
                var inverseOldRotation = Matrix.Invert(Matrix.RotationYawPitchRoll(oldYaw, oldPitch, 0));
                var deltaRotation = inverseOldRotation * rotation;

                var adaptationTargetOffset = position;
                Vector3.TransformNormal(ref adaptationTargetOffset, ref deltaRotation, out adaptationTargetOffset);
                position = adaptationTargetOffset;
            }

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
