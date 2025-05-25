// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
// Adapted for MySurvivalGame.
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Input;
using Stride.Core.Mathematics; // Required for Vector2
// using FirstPersonShooter.Core; // This namespace might not be needed if Utils class is not used or redefined

namespace MySurvivalGame.Game
{
    public class PlayerInput : SyncScript
    {
        /// <summary>
        /// Raised every frame with the intended direction of movement from the player.
        /// </summary>
        public static readonly EventKey<Vector3> MoveDirectionEventKey = new EventKey<Vector3>(); // MODIFIED: Uncommented

        public static readonly EventKey<Vector2> CameraDirectionEventKey = new EventKey<Vector2>();
        public static readonly EventKey SwitchCameraModeEventKey = new EventKey(); // MODIFIED: Uncommented
        public static readonly EventKey<int> HotbarSlotSelectedEventKey = new EventKey<int>(); // ADDED: For hotbar selection
        public static readonly EventKey InteractEventKey = new EventKey(); // ADDED: For interaction

        // public static readonly EventKey<bool> ShootEventKey = new EventKey<bool>();
        // public static readonly EventKey<bool> ReloadEventKey = new EventKey<bool>();
        // public static readonly EventKey ShootReleasedEventKey = new EventKey();
        // public static readonly EventKey ToggleBuildModeEventKey = new EventKey();
        // public static readonly EventKey RotateBuildActionLeftEventKey = new EventKey();
        // public static readonly EventKey RotateBuildActionRightEventKey = new EventKey();
        // public static readonly EventKey CycleBuildableNextEventKey = new EventKey();
        // public static readonly EventKey CycleBuildablePrevEventKey = new EventKey();
        // public static readonly EventKey DebugDestroyEventKey = new EventKey();

        public float DeadZone { get; set; } = 0.25f;

        // This CameraComponent reference is not strictly needed by PlayerInput anymore if MoveDirectionEventKey is removed,
        // but it might be useful for other inputs later or if the FPS template's Camera property was used by other systems.
        // For now, it's kept as it was in the original template PlayerInput.
        public CameraComponent? Camera { get; set; } 

        /// <summary>
        /// Multiplies mouse movement by this amount to apply aim rotations
        /// </summary>
        public float MouseSensitivity { get; set; } = 100.0f;

        // Key bindings for movement, shooting, etc., are commented out as their corresponding event keys are.
        public List<Keys> KeysLeft { get; set; } = new List<Keys>() { Keys.A, Keys.Left }; // MODIFIED: Uncommented
        public List<Keys> KeysRight { get; set; } = new List<Keys>() { Keys.D, Keys.Right }; // MODIFIED: Uncommented
        public List<Keys> KeysUp { get; set; } = new List<Keys>() { Keys.W, Keys.Up }; // MODIFIED: Uncommented
        public List<Keys> KeysDown { get; set; } = new List<Keys>() { Keys.S, Keys.Down }; // MODIFIED: Uncommented
        // public List<Keys> KeysReload { get; set; } = new List<Keys>() { Keys.R };
        public List<Keys> KeysSwitchCamera { get; set; } = new List<Keys>() { Keys.T }; // MODIFIED: Uncommented
        // public List<Keys> KeysToggleBuildMode { get; set; } = new List<Keys>() { Keys.B };
        // public List<Keys> KeysRotateBuildLeft { get; set; } = new List<Keys>() { Keys.OemComma };
        // public List<Keys> KeysRotateBuildRight { get; set; } = new List<Keys>() { Keys.OemPeriod };
        // public List<Keys> KeysCycleBuildableNext { get; set; } = new List<Keys>() { Keys.PageUp };
        // public List<Keys> KeysCycleBuildablePrev { get; set; } = new List<Keys>() { Keys.PageDown };
        // public List<Keys> KeysDebugDestroy { get; set; } = new List<Keys>() { Keys.K };

        public PlayerInput()
        {
            // Fix single frame input lag
            Priority = -1000;
        }

        public override void Update()
        {
            // Character movement
            //  The character movement can be controlled by a game controller or a keyboard
            //  The character receives input in 3D world space, so that it can be controlled by an AI script as well
            //  For this reason we map the 2D user input to a 3D movement using the current camera
            { // MODIFIED: Uncommented block
                // Game controller: left stick
                var moveDirection = Input.GetLeftThumbAny(DeadZone);
                var isDeadZoneLeft = moveDirection.Length() < DeadZone;
                if (isDeadZoneLeft)
                    moveDirection = Vector2.Zero;
                else
                    moveDirection.Normalize();

                // Keyboard
                if (KeysLeft.Any(key => Input.IsKeyDown(key)))
                    moveDirection += -Vector2.UnitX;
                if (KeysRight.Any(key => Input.IsKeyDown(key)))
                    moveDirection += +Vector2.UnitX;
                if (KeysUp.Any(key => Input.IsKeyDown(key)))
                    moveDirection += +Vector2.UnitY;
                if (KeysDown.Any(key => Input.IsKeyDown(key)))
                    moveDirection += -Vector2.UnitY;

                // Broadcast the movement vector as a world-space Vector3 to allow characters to be controlled
                // The Utils class will be needed here.
                var worldSpeed = (Camera != null)
                    ? MySurvivalGame.Game.Core.Utils.LogicDirectionToWorldDirection(moveDirection, Camera, Vector3.UnitY) // MODIFIED: Added Core namespace
                    : new Vector3(moveDirection.X, 0, moveDirection.Y); 

                MoveDirectionEventKey.Broadcast(worldSpeed);
            }

            // Camera rotation
            {
                // Game controller: right stick
                var cameraDirection = Input.GetRightThumbAny(DeadZone);
                var isDeadZoneRight = cameraDirection.Length() < DeadZone;
                if (isDeadZoneRight)
                    cameraDirection = Vector2.Zero;
                else
                    cameraDirection.Normalize();
                
                cameraDirection *= (float)Game.UpdateTime.Elapsed.TotalSeconds;

                // Mouse-based camera rotation.
                if (Input.IsMouseButtonDown(MouseButton.Left)) // Or any other button to lock mouse
                {
                    Input.LockMousePosition(true);
                    Game.IsMouseVisible = false;
                }
                if (Input.IsKeyPressed(Keys.Escape))
                {
                    Input.UnlockMousePosition();
                    Game.IsMouseVisible = true;
                }
                if (Input.IsMousePositionLocked)
                {
                    // Adjust sensitivity application if necessary. Original was just MouseSensitivity.
                    // The template multiplies by MouseSensitivity, but some prefer to divide by a factor.
                    // For now, keeping it as in the template.
                    cameraDirection += new Vector2(Input.MouseDelta.X, -Input.MouseDelta.Y) * (MouseSensitivity / 1000.0f); // Adjusted sensitivity scaling
                }

                CameraDirectionEventKey.Broadcast(cameraDirection);
            }

            // Shooting logic commented out
            /*
            {
                // ... (shooting logic) ...
                ShootEventKey.Broadcast(didShoot);
                // ... (shoot release logic) ...
                ShootReleasedEventKey.Broadcast();
            }
            */

            // Reload logic commented out
            /*
            {
                // ... (reload logic) ...
                ReloadEventKey.Broadcast(isReloading);
            }
            */

            // Camera mode switch logic
            {
                if (KeysSwitchCamera.Any(key => Input.IsKeyPressed(key))) // MODIFIED: Uncommented block
                {
                    SwitchCameraModeEventKey.Broadcast();
                }
            }

            // Building mode logic commented out
            /*
            {
                // ... (building mode logic) ...
            }
            */
            
            // Debug Destroy logic commented out
            /*
            {
                if (KeysDebugDestroy.Any(key => Input.IsKeyPressed(key)))
                {
                    DebugDestroyEventKey.Broadcast();
                }
            }
            */

            // ADDED: Hotbar Slot Selection Input
            if (Input.IsKeyPressed(Keys.D1)) HotbarSlotSelectedEventKey.Broadcast(0);
            if (Input.IsKeyPressed(Keys.D2)) HotbarSlotSelectedEventKey.Broadcast(1);
            if (Input.IsKeyPressed(Keys.D3)) HotbarSlotSelectedEventKey.Broadcast(2);
            if (Input.IsKeyPressed(Keys.D4)) HotbarSlotSelectedEventKey.Broadcast(3);
            if (Input.IsKeyPressed(Keys.D5)) HotbarSlotSelectedEventKey.Broadcast(4);
            if (Input.IsKeyPressed(Keys.D6)) HotbarSlotSelectedEventKey.Broadcast(5);
            if (Input.IsKeyPressed(Keys.D7)) HotbarSlotSelectedEventKey.Broadcast(6);
            if (Input.IsKeyPressed(Keys.D8)) HotbarSlotSelectedEventKey.Broadcast(7);

            // ADDED: Interaction Input
            if (Input.IsKeyPressed(Keys.E))
            {
                InteractEventKey.Broadcast();
            }
        }
    }
}
