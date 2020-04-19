// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace ThirdPersonPlatformer
{
    /// <summary>
    /// A script that allows to move and rotate an entity through keyboard, mouse and touch input to provide basic camera navigation.
    /// </summary>
    /// <remarks>
    /// The entity can be moved using W, A, S, D, Q and E, arrow keys or dragging/scaling using multi-touch.
    /// Rotation is achieved using the Numpad, the mouse while holding the right mouse button, or dragging using single-touch.
    /// </remarks>
    public class BasicCameraController : SyncScript
    {
        private const float MaximumPitch = MathUtil.PiOverTwo * 0.99f;

        private Vector3 upVector;
        private Vector3 translation;
        private float yaw;
        private float pitch;

        public Vector3 KeyboardMovementSpeed { get; set; } = new Vector3(5.0f);

        public Vector3 TouchMovementSpeed { get; set; } = new Vector3(40, 40, 20);

        public float SpeedFactor { get; set; } = 5.0f;

        public Vector2 KeyboardRotationSpeed { get; set; } = new Vector2(3.0f);

        public Vector2 MouseRotationSpeed { get; set; } = new Vector2(90.0f, 60.0f);

        public Vector2 TouchRotationSpeed { get; set; } = new Vector2(60.0f, 40.0f);

        public override void Start()
        {
            base.Start();

            // Default up-direction
            upVector = Vector3.UnitY;

            // Configure touch input
            if (!Platform.IsWindowsDesktop)
            {
                Input.Gestures.Add(new GestureConfigDrag());
                Input.Gestures.Add(new GestureConfigComposite());
            }
        }

        public override void Update()
        {
            ProcessInput();
            UpdateTransform();
        }

        private void ProcessInput()
        {
            translation = Vector3.Zero;
            yaw = 0;
            pitch = 0;

            // Move with keyboard
            if (Input.IsKeyDown(Keys.W) || Input.IsKeyDown(Keys.Up))
            {
                translation.Z = -KeyboardMovementSpeed.Z;
            }
            else if (Input.IsKeyDown(Keys.S) || Input.IsKeyDown(Keys.Down))
            {
                translation.Z = KeyboardMovementSpeed.Z;
            }

            if (Input.IsKeyDown(Keys.A) || Input.IsKeyDown(Keys.Left))
            {
                translation.X = -KeyboardMovementSpeed.X;
            }
            else if (Input.IsKeyDown(Keys.D) || Input.IsKeyDown(Keys.Right))
            {
                translation.X = KeyboardMovementSpeed.X;
            }

            if (Input.IsKeyDown(Keys.Q))
            {
                translation.Y = -KeyboardMovementSpeed.Y;
            }
            else if (Input.IsKeyDown(Keys.E))
            {
                translation.Y = KeyboardMovementSpeed.Y;
            }

            // Alternative translation speed
            if (Input.IsKeyDown(Keys.LeftShift) || Input.IsKeyDown(Keys.RightShift))
            {
                translation *= SpeedFactor;
            }

            // Rotate with keyboard
            if (Input.IsKeyDown(Keys.NumPad2))
            {
                pitch = KeyboardRotationSpeed.X;
            }
            else if (Input.IsKeyDown(Keys.NumPad8))
            {
                pitch = -KeyboardRotationSpeed.X;
            }

            if (Input.IsKeyDown(Keys.NumPad4))
            {
                yaw = KeyboardRotationSpeed.Y;
            }
            else if (Input.IsKeyDown(Keys.NumPad6))
            {
                yaw = -KeyboardRotationSpeed.Y;
            }

            // Rotate with mouse
            if (Input.IsMouseButtonDown(MouseButton.Right))
            {
                Input.LockMousePosition();
                Game.IsMouseVisible = false;

                yaw = -Input.MouseDelta.X * MouseRotationSpeed.X;
                pitch = -Input.MouseDelta.Y * MouseRotationSpeed.Y;
            }
            else
            {
                Input.UnlockMousePosition();
                Game.IsMouseVisible = true;
            }

            // Handle gestures
            foreach (var gestureEvent in Input.GestureEvents)
            {
                switch (gestureEvent.Type)
                {
                // Rotate by dragging
                case GestureType.Drag:
                    var drag = (GestureEventDrag)gestureEvent;
                    var dragDistance = drag.DeltaTranslation;
                    yaw = -dragDistance.X * TouchRotationSpeed.X;
                    pitch = -dragDistance.Y * TouchRotationSpeed.Y;
                    break;

                // Move along z-axis by scaling and in xy-plane by multi-touch dragging
                case GestureType.Composite:
                    var composite = (GestureEventComposite)gestureEvent;
                    translation.X = -composite.DeltaTranslation.X * TouchMovementSpeed.X;
                    translation.Y = -composite.DeltaTranslation.Y * TouchMovementSpeed.Y;
                    translation.Z = -(float)Math.Log(composite.DeltaScale + 1) * TouchMovementSpeed.Z;
                    break;
                }
            }
        }

        private void UpdateTransform()
        {
            var elapsedTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;

            translation *= elapsedTime;
            yaw *= elapsedTime;
            pitch *= elapsedTime;

            // Get the local coordinate system
            var rotation = Matrix.RotationQuaternion(Entity.Transform.Rotation);

            // Enforce the global up-vector by adjusting the local x-axis
            var right = Vector3.Cross(rotation.Forward, upVector);
            var up = Vector3.Cross(right, rotation.Forward);

            // Stabilize
            right.Normalize();
            up.Normalize();

            // Adjust pitch. Prevent it from exceeding up and down facing. Stabilize edge cases.
            var currentPitch = MathUtil.PiOverTwo - (float)Math.Acos(Vector3.Dot(rotation.Forward, upVector));
            pitch = MathUtil.Clamp(currentPitch + pitch, -MaximumPitch, MaximumPitch) - currentPitch;

            // Move in local coordinates
            Entity.Transform.Position += Vector3.TransformCoordinate(translation, rotation);

            // Yaw around global up-vector, pitch and roll in local space
            Entity.Transform.Rotation *= Quaternion.RotationAxis(right, pitch) * Quaternion.RotationAxis(upVector, yaw);
        }
    }
}
