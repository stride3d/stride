using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Input;
using System;
using System.Threading.Tasks;

namespace ##Namespace##
{
    /// <summary>
    /// The default script for the scene editor camera.
    /// </summary>
    public class ##Scriptname## : AsyncScript
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
        /// Useful when attached to a character controller, where the controller should be the source of motion
        /// </summary>
        public bool RotationOnly { get; set; }

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
            Component.UseCustomAspectRatio = true;
            Reset();

            var backBuffer = GraphicsDevice.Presenter.BackBuffer;
            if (backBuffer != null)
            {
                Component.AspectRatio = backBuffer.Width / (float)backBuffer.Height;
            }

            if (!Platform.IsWindowsDesktop)
            {
                Input.Gestures.Add(new GestureConfigDrag());
                Input.Gestures.Add(new GestureConfigTap { RequiredNumberOfTaps = 2 });
            }

            Input.LockMousePosition(true);
            Game.IsMouseVisible = false;

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

            desiredPitch =
                Pitch =
                    (float)
                        Math.Atan2(
                            2 * Entity.Transform.Rotation.X * Entity.Transform.Rotation.W -
                            2 * Entity.Transform.Rotation.Y * Entity.Transform.Rotation.Z,
                            1 - 2 * Entity.Transform.Rotation.X * Entity.Transform.Rotation.X -
                            2 * Entity.Transform.Rotation.Z * Entity.Transform.Rotation.Z);

            position = Entity.Transform.Position;
        }

        /// <summary>
        /// Update the camera parameters.
        /// </summary>
        protected virtual void UpdateCamera()
        {
            var rotationDelta = Input.MouseDelta;
            var doubleTapped = false;
            foreach (var gestureEvent in Input.GestureEvents)
            {
                switch (gestureEvent.Type)
                {
                    case GestureType.Drag:
                        {
                            var drag = (GestureEventDrag)gestureEvent;
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
                        {
                            doubleTapped = true;
                        }
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // Compute translation speed according to framerate and modifiers
            var translationSpeed = MoveSpeed * (float)Game.UpdateTime.Elapsed.TotalSeconds;

            var oldPitch = Pitch;

            // Take shortest path
            var deltaPitch = desiredPitch - Pitch;
            var deltaYaw = (desiredYaw - Yaw) % MathUtil.TwoPi;
            if (deltaYaw < 0)
                deltaYaw += MathUtil.TwoPi;
            if (deltaYaw > MathUtil.Pi)
                deltaYaw -= MathUtil.TwoPi;
            desiredYaw = Yaw + deltaYaw;

            // Perform orientation transition
            var rotationAdaptation = (float)Game.UpdateTime.Elapsed.TotalSeconds * RotationAdaptationSpeed;
            Yaw = Math.Abs(deltaYaw) < rotationAdaptation ? desiredYaw : Yaw + rotationAdaptation * Math.Sign(deltaYaw);
            Pitch = Math.Abs(deltaPitch) < rotationAdaptation ? desiredPitch : Pitch + rotationAdaptation * Math.Sign(deltaPitch);

            desiredYaw = Yaw -= 1.333f * rotationDelta.X * RotationSpeed; // We want to rotate faster Horizontally and Vertically
            desiredPitch = Pitch = MathUtil.Clamp(Pitch - rotationDelta.Y * RotationSpeed, -MathUtil.PiOverTwo, MathUtil.PiOverTwo);

            if (!RotationOnly)
            {
                // Compute base vectors for camera movement
                var rotation = Matrix.RotationYawPitchRoll(Yaw, Pitch, 0);
                var forward = Vector3.TransformNormal(ForwardVector, rotation);
                var up = Vector3.TransformNormal(UpVector, rotation);
                var right = Vector3.Cross(forward, up);

                // Update camera move: Dolly (WADS model/arrow keys)
                var movePosition = Vector3.Zero;
                if (Input.IsKeyDown(Keys.A) || Input.IsKeyDown(Keys.Left))
                    movePosition += -right;
                if (Input.IsKeyDown(Keys.D) || Input.IsKeyDown(Keys.Right))
                    movePosition += right;
                if (Input.IsKeyDown(Keys.S) || Input.IsKeyDown(Keys.Down))
                    movePosition += Component.Projection == CameraProjectionMode.Perspective ? -forward : -up;
                if (Input.IsKeyDown(Keys.W) || Input.IsKeyDown(Keys.Up) || doubleTapped)
                    movePosition += Component.Projection == CameraProjectionMode.Perspective ? forward : up;
                if (Input.IsKeyDown(Keys.Q))
                    movePosition += Component.Projection == CameraProjectionMode.Perspective ? -up : -forward;
                if (Input.IsKeyDown(Keys.E))
                    movePosition += Component.Projection == CameraProjectionMode.Perspective ? up : forward;

                position += (Vector3.Normalize(movePosition) * translationSpeed);

                if (doubleTapped)
                {
                    desiredPitch = Pitch = oldPitch;
                    desiredYaw = Yaw;

                    forward = -Vector3.Transform(ForwardVector, Quaternion.RotationYawPitchRoll(Yaw, Pitch, 0));
                    var projectedForward = Vector3.Normalize(new Vector3(forward.X, 0, forward.Z));
                    position -= projectedForward * translationSpeed * MouseMoveSpeedFactor;
                }
            }

            // Update the camera view matrix
            UpdateViewMatrix();
        }

        private void UpdateViewMatrix()
        {
            var camera = Component;
            if (camera == null) return;
            var rotation = Quaternion.RotationYawPitchRoll(Yaw, Pitch, 0);

            Entity.Transform.Position = position;
            Entity.Transform.Rotation = rotation;
        }
    }
}
