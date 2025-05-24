// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Input;
using FirstPersonShooter.Core;

namespace FirstPersonShooter.Player
{
    public class PlayerInput : SyncScript
    {
        /// <summary>
        /// Raised every frame with the intended direction of movement from the player.
        /// </summary>
        public static readonly EventKey<Vector3> MoveDirectionEventKey = new EventKey<Vector3>();       // This can be made non-static and require specific binding to the scripts instead

        public static readonly EventKey<Vector2> CameraDirectionEventKey = new EventKey<Vector2>();     // This can be made non-static and require specific binding to the scripts instead

        public static readonly EventKey<bool> ShootEventKey = new EventKey<bool>();                     // This can be made non-static and require specific binding to the scripts instead

        public static readonly EventKey<bool> ReloadEventKey = new EventKey<bool>();                    // This can be made non-static and require specific binding to the scripts instead

        public static readonly EventKey SwitchCameraModeEventKey = new EventKey();               // Event for camera mode switch
        public static readonly EventKey ShootReleasedEventKey = new EventKey();                  // Event for when shooting input is released (e.g., for bows)
        public static readonly EventKey ToggleBuildModeEventKey = new EventKey();                // Event for toggling building mode
        public static readonly EventKey RotateBuildActionLeftEventKey = new EventKey();          // Event for rotating build preview left
        public static readonly EventKey RotateBuildActionRightEventKey = new EventKey();         // Event for rotating build preview right
        public static readonly EventKey CycleBuildableNextEventKey = new EventKey();             // Event for cycling to next buildable item
        public static readonly EventKey CycleBuildablePrevEventKey = new EventKey();             // Event for cycling to previous buildable item
        public static readonly EventKey DebugDestroyEventKey = new EventKey();                   // Event for debug destroying a building piece

        public float DeadZone { get; set; } = 0.25f;

        public CameraComponent Camera { get; set; }

        /// <summary>
        /// Multiplies move movement by this amount to apply aim rotations
        /// </summary>
        public float MouseSensitivity { get; set; } = 100.0f;

        public List<Keys> KeysLeft { get; set; } = new List<Keys>() { Keys.A, Keys.Left };

        public List<Keys> KeysRight { get; set; } = new List<Keys>() { Keys.D, Keys.Right };

        public List<Keys> KeysUp { get; set; } = new List<Keys>() { Keys.W, Keys.Up };

        public List<Keys> KeysDown { get; set; } = new List<Keys>() { Keys.S, Keys.Down };

        public List<Keys> KeysReload { get; set; } = new List<Keys>() { Keys.R };

        public List<Keys> KeysSwitchCamera { get; set; } = new List<Keys>() { Keys.T }; // Keys for switching camera
        
        public List<Keys> KeysToggleBuildMode { get; set; } = new List<Keys>() { Keys.B };
        public List<Keys> KeysRotateBuildLeft { get; set; } = new List<Keys>() { Keys.OemComma }; // ',' or '<' key
        public List<Keys> KeysRotateBuildRight { get; set; } = new List<Keys>() { Keys.OemPeriod }; // '.' or '>' key
        
        // Using Keys.PageUp and Keys.PageDown as example, can be changed. Mouse wheel handled separately.
        public List<Keys> KeysCycleBuildableNext { get; set; } = new List<Keys>() { Keys.PageUp }; 
        public List<Keys> KeysCycleBuildablePrev { get; set; } = new List<Keys>() { Keys.PageDown };
        public List<Keys> KeysDebugDestroy { get; set; } = new List<Keys>() { Keys.K };


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
            {
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
                var worldSpeed = (Camera != null)
                    ? Utils.LogicDirectionToWorldDirection(moveDirection, Camera, Vector3.UnitY)
                    : new Vector3(moveDirection.X, 0, moveDirection.Y); // If we don't have the correct camera attached we can send the directions anyway, but they probably won't match

                MoveDirectionEventKey.Broadcast(worldSpeed);
            }

            // Camera rotation
            //  Camera rotation is ALWAYS in camera space, so we don't need to account for View or Projection matrices
            {
                // Game controller: right stick
                var cameraDirection = Input.GetRightThumbAny(DeadZone);
                var isDeadZoneRight = cameraDirection.Length() < DeadZone;
                if (isDeadZoneRight)
                    cameraDirection = Vector2.Zero;
                else
                    cameraDirection.Normalize();
                
                // Contrary to a mouse, driving camera rotation from a stick must be scaled by delta time.
                // The amount of camera rotation with a stick is constant over time based on the tilt of the stick,
                // Whereas mouse driven rotation is already constrained by time, it is driven by the difference in position from last *time* to this *time*.
                cameraDirection *= (float)this.Game.UpdateTime.Elapsed.TotalSeconds;

                // Mouse-based camera rotation.
                //  Only enabled after you click the screen to lock your cursor, pressing escape will cancel it.
                if (Input.IsMouseButtonDown(MouseButton.Left))
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
                    cameraDirection += new Vector2(Input.MouseDelta.X, -Input.MouseDelta.Y) * MouseSensitivity;
                }

                // Broadcast the camera direction directly, as a screen-space Vector2
                CameraDirectionEventKey.Broadcast(cameraDirection);
            }

            {
                // Controller: Right trigger
                // Mouse: Left button, Tap events
                var didShoot = Input.GetRightTriggerAny(0.2f) > 0.2f;   // This will allow for continuous shooting

                if (Input.PointerEvents.Any(x => x.EventType == PointerEventType.Pressed))
                    didShoot = true;
                    
                if (Input.HasMouse && Input.IsMouseButtonDown(MouseButton.Left))                  // This will allow for continuous shooting
                    didShoot = true;

                ShootEventKey.Broadcast(didShoot);

                // Check for shoot release
                bool didShootRelease = false;
                if (Input.HasMouse && Input.IsMouseButtonReleased(MouseButton.Left))
                    didShootRelease = true;
                
                // Pointer events are not "released" in the same way as mouse buttons or keys.
                // A common pattern for touch is to check for PointerEventType.Released.
                // However, PointerEvents is a list of events that occurred *this frame*.
                // For simplicity, matching game controller trigger release.
                if (Input.IsTriggerReleasedAny(0.2f)) // Check if any trigger was released (using same threshold as GetRightTriggerAny)
                    didShootRelease = true;
                
                // If other input types are used for "didShoot", their release should be checked here too.
                // e.g. Tap events don't have a "release" in the same frame typically, they are discrete.

                if (didShootRelease)
                {
                    ShootReleasedEventKey.Broadcast();
                }
            }

            {
                // Reload weapon
                var isReloading = Input.IsGamePadButtonDownAny(GamePadButton.X);
                if (KeysReload.Any(key => Input.IsKeyDown(key)))
                    isReloading = true;

                ReloadEventKey.Broadcast(isReloading);
            }

            // Camera mode switch
            {
                if (KeysSwitchCamera.Any(key => Input.IsKeyPressed(key)))
                {
                    SwitchCameraModeEventKey.Broadcast();
                }
            }

            // Building mode toggle and rotation
            {
                if (KeysToggleBuildMode.Any(key => Input.IsKeyPressed(key)))
                {
                    ToggleBuildModeEventKey.Broadcast();
                }
                if (KeysRotateBuildLeft.Any(key => Input.IsKeyPressed(key)))
                {
                    RotateBuildActionLeftEventKey.Broadcast();
                }
                if (KeysRotateBuildRight.Any(key => Input.IsKeyPressed(key)))
                {
                    RotateBuildActionRightEventKey.Broadcast();
                }
                
                // Cycle buildable items with mouse wheel
                var mouseWheelDelta = Input.MouseWheelDelta;
                if (mouseWheelDelta > 0) // Positive delta for scroll up/forward
                {
                    CycleBuildableNextEventKey.Broadcast();
                }
                else if (mouseWheelDelta < 0) // Negative delta for scroll down/backward
                {
                    CycleBuildablePrevEventKey.Broadcast();
                }

                // Cycle buildable items with keys
                if (KeysCycleBuildableNext.Any(key => Input.IsKeyPressed(key)))
                {
                    CycleBuildableNextEventKey.Broadcast();
                }
                if (KeysCycleBuildablePrev.Any(key => Input.IsKeyPressed(key)))
                {
                    CycleBuildablePrevEventKey.Broadcast();
                }
            }
            
            // Debug Destroy
            {
                if (KeysDebugDestroy.Any(key => Input.IsKeyPressed(key)))
                {
                    DebugDestroyEventKey.Broadcast();
                }
            }
        }
    }
}
