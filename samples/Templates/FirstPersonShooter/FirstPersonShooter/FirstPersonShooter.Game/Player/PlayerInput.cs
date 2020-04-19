// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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

        public float DeadZone { get; set; } = 0.25f;

        public CameraComponent Camera { get; set; }

        /// <summary>
        /// Multiplies move movement by this amount to apply aim rotations
        /// </summary>
        public float MouseSensitivity { get; set; } = 100.0f;

        public List<Keys> KeysLeft { get; } = new List<Keys>();

        public List<Keys> KeysRight { get; } = new List<Keys>();

        public List<Keys> KeysUp { get; } = new List<Keys>();

        public List<Keys> KeysDown { get; } = new List<Keys>();

        public List<Keys> KeysReload { get; } = new List<Keys>();

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
            }

            {
                // Reload weapon
                var isReloading = Input.IsGamePadButtonDownAny(GamePadButton.X);
                if (KeysReload.Any(key => Input.IsKeyDown(key)))
                    isReloading = true;

                ReloadEventKey.Broadcast(isReloading);
            }
        }
    }
}
