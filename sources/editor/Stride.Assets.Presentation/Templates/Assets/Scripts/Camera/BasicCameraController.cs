using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace ##Namespace##
{
    /// <summary>
    /// A script that allows to move and rotate an entity through keyboard, mouse and touch input to provide basic camera navigation.
    /// </summary>
    /// <remarks>
    /// The entity can be moved using W, A, S, D, Q and E, arrow keys, a gamepad's left stick or dragging/scaling using multi-touch.
    /// Rotation is achieved using the Numpad, the mouse while holding the right mouse button, a gamepad's right stick, or dragging using single-touch.
    /// </remarks>
    public class ##Scriptname## : SyncScript
    {
        private const float MaximumPitch = MathUtil.PiOverTwo * 0.99f;

        private Vector3 upVector;
        private Vector3 translation;
        private float yaw;
        private float pitch;

        public bool Gamepad { get; set; } = false;

        public Vector3 KeyboardMovementSpeed { get; set; } = new Vector3(5.0f);

        public Vector3 TouchMovementSpeed { get; set; } = new Vector3(0.7f, 0.7f, 0.3f);

        public float SpeedFactor { get; set; } = 5.0f;

        public Vector2 KeyboardRotationSpeed { get; set; } = new Vector2(3.0f);

        public Vector2 MouseRotationSpeed { get; set; } = new Vector2(1.0f, 1.0f);

        public Vector2 TouchRotationSpeed { get; set; } = new Vector2(1.0f, 0.7f);

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
            float deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;
            translation = Vector3.Zero;
            yaw = 0f;
            pitch = 0f;

            // Keyboard and Gamepad based movement
            {
                // Our base speed is: one unit per second:
                //    deltaTime contains the duration of the previous frame, let's say that in this update
                //    or frame it is equal to 1/60, that means that the previous update ran 1/60 of a second ago
                //    and the next will, in most cases, run in around 1/60 of a second from now. Knowing that,
                //    we can move 1/60 of a unit on this frame so that in around 60 frames(1 second)
                //    we will have travelled one whole unit in a second.
                //    If you don't use deltaTime your speed will be dependant on the amount of frames rendered
                //    on screen which often are inconsistent, meaning that if the player has performance issues,
                //    this entity will move around slower.
                float speed = 1f * deltaTime;

                Vector3 dir = Vector3.Zero;

                if (Gamepad && Input.HasGamePad)
                {
                    GamePadState padState = Input.DefaultGamePad.State;
                    // LeftThumb can be positive or negative on both axis (pushed to the right or to the left)
                    dir.Z += padState.LeftThumb.Y;
                    dir.X += padState.LeftThumb.X;

                    // Triggers are always positive, in this case using one to increase and the other to decrease
                    dir.Y -= padState.LeftTrigger;
                    dir.Y += padState.RightTrigger;

                    // Increase speed when pressing A, LeftShoulder or RightShoulder
                    // Here:does the enum flag 'Buttons' has one of the flag ('A','LeftShoulder' or 'RightShoulder') set
                    if ((padState.Buttons & (GamePadButton.A | GamePadButton.LeftShoulder | GamePadButton.RightShoulder)) != 0)
                    {
                        speed *= SpeedFactor;
                    }
                }

                if (Input.HasKeyboard)
                {
                    // Move with keyboard
                    // Forward/Backward
                    if (Input.IsKeyDown(Keys.W) || Input.IsKeyDown(Keys.Up))
                    {
                        dir.Z += 1;
                    }
                    if (Input.IsKeyDown(Keys.S) || Input.IsKeyDown(Keys.Down))
                    {
                        dir.Z -= 1;
                    }

                    // Left/Right
                    if (Input.IsKeyDown(Keys.A) || Input.IsKeyDown(Keys.Left))
                    {
                        dir.X -= 1;
                    }
                    if (Input.IsKeyDown(Keys.D) || Input.IsKeyDown(Keys.Right))
                    {
                        dir.X += 1;
                    }

                    // Down/Up
                    if (Input.IsKeyDown(Keys.Q))
                    {
                        dir.Y -= 1;
                    }
                    if (Input.IsKeyDown(Keys.E))
                    {
                        dir.Y += 1;
                    }

                    // Increase speed when pressing shift
                    if (Input.IsKeyDown(Keys.LeftShift) || Input.IsKeyDown(Keys.RightShift))
                    {
                        speed *= SpeedFactor;
                    }

                    // If the player pushes down two or more buttons, the direction and ultimately the base speed
                    // will be greater than one (vector(1, 1) is farther away from zero than vector(0, 1)),
                    // normalizing the vector ensures that whichever direction the player chooses, that direction
                    // will always be at most one unit in length.
                    // We're keeping dir as is if isn't longer than one to retain sub unit movement:
                    // a stick not entirely pushed forward should make the entity move slower.
                    if (dir.Length() > 1f)
                    {
                        dir = Vector3.Normalize(dir);
                    }
                }

                // Finally, push all of that to the translation variable which will be used within UpdateTransform()
                translation += dir * KeyboardMovementSpeed * speed;
            }

            // Keyboard and Gamepad based Rotation
            {
                // See Keyboard & Gamepad translation's deltaTime usage
                float speed = 1f * deltaTime;
                Vector2 rotation = Vector2.Zero;
                if (Gamepad && Input.HasGamePad)
                {
                    GamePadState padState = Input.DefaultGamePad.State;
                    rotation.X += padState.RightThumb.Y;
                    rotation.Y += -padState.RightThumb.X;
                }

                if (Input.HasKeyboard)
                {
                    if (Input.IsKeyDown(Keys.NumPad2))
                    {
                        rotation.X += 1;
                    }
                    if (Input.IsKeyDown(Keys.NumPad8))
                    {
                        rotation.X -= 1;
                    }

                    if (Input.IsKeyDown(Keys.NumPad4))
                    {
                        rotation.Y += 1;
                    }
                    if (Input.IsKeyDown(Keys.NumPad6))
                    {
                        rotation.Y -= 1;
                    }

                    // See Keyboard & Gamepad translation's Normalize() usage
                    if (rotation.Length() > 1f)
                    {
                        rotation = Vector2.Normalize(rotation);
                    }
                }

                // Modulate by speed
                rotation *= KeyboardRotationSpeed * speed;

                // Finally, push all of that to pitch & yaw which are going to be used within UpdateTransform()
                pitch += rotation.X;
                yaw += rotation.Y;
            }

            // Mouse movement and gestures
            {
                // This type of input should not use delta time at all, they already are frame-rate independent.
                //    Lets say that you are going to move your finger/mouse for one second over 40 units, it doesn't matter
                //    the amount of frames occuring within that time frame, each frame will receive the right amount of delta:
                //    a quarter of a second -> 10 units, half a second -> 20 units, one second -> your 40 units.

                if (Input.HasMouse)
                {
                    // Rotate with mouse
                    if (Input.IsMouseButtonDown(MouseButton.Right))
                    {
                        Input.LockMousePosition();
                        Game.IsMouseVisible = false;

                        yaw -= Input.MouseDelta.X * MouseRotationSpeed.X;
                        pitch -= Input.MouseDelta.Y * MouseRotationSpeed.Y;
                    }
                    else
                    {
                        Input.UnlockMousePosition();
                        Game.IsMouseVisible = true;
                    }
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
                            translation.Z = (float)Math.Log(composite.DeltaScale + 1) * TouchMovementSpeed.Z;
                            break;
                    }
                }
            }
        }

        private void UpdateTransform()
        {
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

            Vector3 finalTranslation = translation;
            finalTranslation.Z = -finalTranslation.Z;
            finalTranslation = Vector3.TransformCoordinate(finalTranslation, rotation);

            // Move in local coordinates
            Entity.Transform.Position += finalTranslation;

            // Yaw around global up-vector, pitch and roll in local space
            Entity.Transform.Rotation *= Quaternion.RotationAxis(right, pitch) * Quaternion.RotationAxis(upVector, yaw);
        }
    }
}
