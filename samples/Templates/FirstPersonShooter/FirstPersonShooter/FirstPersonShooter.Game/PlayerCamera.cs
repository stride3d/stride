// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Physics;

namespace FirstPersonShooter.Game
{
    public enum CameraMode
    {
        FPS,
        TPS
    }

    public class PlayerCamera : SyncScript
    {
        // --- FPS Settings ---
        /// <summary>
        /// The default height of the FPS camera target relative to the character root
        /// </summary>
        public float FpsTargetHeight { get; set; } = 1.6f;

        // --- TPS Settings ---
        /// <summary>
        /// The default distance from the player to the TPS camera
        /// </summary>
        public float DefaultTpsDistance { get; set; } = 4.0f;

        /// <summary>
        /// The default height offset of the TPS camera target relative to the character root
        /// </summary>
        public float DefaultTpsHeightOffset { get; set; } = 1.8f; // Slightly above player head

        /// <summary>
        /// How far the TPS camera should stay from obstacles
        /// </summary>
        public float TpsCollisionMargin { get; set; } = 0.2f;

        // --- Common Settings ---
        /// <summary>
        /// The mouse sensitivity
        /// </summary>
        public float CameraSensitivity { get; set; } = 20.0f;

        /// <summary>
        /// The minimum rotation X in degrees
        /// </summary>
        public float RotationXMin { get; set; } = -70.0f;

        /// <summary>
        /// The maximum rotation X in degrees
        /// </summary>
        public float RotationXMax { get; set; } = 70.0f;

        /// <summary>
        /// The player this camera is attached to
        /// </summary>
        public Entity Player { get; set; }

        /// <summary>
        /// The input component for the player
        /// </summary>
        public PlayerInput PlayerInput { get; set; }

        private float yaw;
        private float pitch;
        private CameraMode currentMode = CameraMode.FPS;
        private Simulation simulation; // For raycasting
        private Vector2 currentCameraMovement; // Stores mouse/gamepad input from PlayerInput

        public override void Start()
        {
            // Default values
            yaw = 0.0f;
            pitch = 0.0f;
            currentCameraMovement = Vector2.Zero;

            // Hide the mouse cursor
            Game.IsMouseVisible = false;

            simulation = this.GetSimulation();

            // Initial camera setup
            UpdateCameraMode();

            // Subscribe to events from PlayerInput
            if (PlayerInput != null)
            {
                PlayerInput.SwitchCameraModeEventKey.AddListener(HandleSwitchCameraMode);
                PlayerInput.CameraDirectionEventKey.AddListener(HandleCameraInput);
            }
            else
            {
                Log.Error("PlayerInput is not assigned to PlayerCamera. Camera will not respond to input.");
            }
        }

        public override void Cancel()
        {
            // Unsubscribe from events to prevent memory leaks
            if (PlayerInput != null)
            {
                PlayerInput.SwitchCameraModeEventKey.RemoveListener(HandleSwitchCameraMode);
                PlayerInput.CameraDirectionEventKey.RemoveListener(HandleCameraInput);
            }
            base.Cancel();
        }

        private void HandleCameraInput(Vector2 cameraMovement)
        {
            currentCameraMovement = cameraMovement;
        }

        private void HandleSwitchCameraMode(EventKey sender, EventReceiver receiver)
        {
            currentMode = (currentMode == CameraMode.FPS) ? CameraMode.TPS : CameraMode.FPS;
            UpdateCameraMode();
        }

        public override void Update()
        {
            if (Player == null || PlayerInput == null)
                return;

            // --- Camera Mode Switching --- is now handled by HandleSwitchCameraMode via event

            // --- Mouse Input and Rotation ---
            // The PlayerInput script should broadcast CameraDirectionEventKey which contains the mouse delta.
            // However, PlayerCamera currently reads PlayerInput.MouseMovement directly.
            // This needs to be aligned. For now, we assume PlayerInput.MouseMovement is correctly populated
            // by PlayerInput itself from its CameraDirectionEventKey or similar mechanism if events are not used.
            // Now using currentCameraMovement populated by HandleCameraInput event listener.
            // Note: PlayerInput already scales gamepad input by delta time. Mouse input is inherently delta.
            // So, CameraSensitivity might need adjustment if it was tuned for unscaled mouse delta before.
            // For consistency, ensure PlayerInput provides raw delta for mouse, and scaled for gamepad, OR PlayerCamera handles scaling.
            // PlayerInput.cs currently scales gamepad input by Elapsed.TotalSeconds and adds raw mouse delta * MouseSensitivity.
            // This means CameraDirectionEventKey broadcasts a value that is already scaled by sensitivity for mouse, and by time for gamepad.
            // PlayerCamera.cs should use this value directly without further scaling by CameraSensitivity or DeltaTime here.
            
            // The CameraDirectionEventKey from PlayerInput already includes sensitivity and delta time scaling.
            yaw -= currentCameraMovement.X; // PlayerInput's CameraDirectionEventKey now provides the complete rotation delta
            pitch -= currentCameraMovement.Y; // PlayerInput's CameraDirectionEventKey now provides the complete rotation delta
            currentCameraMovement = Vector2.Zero; // Reset after use for this frame if input is event-driven and might not fire every frame

            // Clamp pitch
            pitch = MathUtil.Clamp(pitch, MathUtil.DegreesToRadians(RotationXMin), MathUtil.DegreesToRadians(RotationXMax));

            // Player orientation (only yaw, pitch is for camera only)
            Player.Transform.Rotation = Quaternion.RotationY(yaw);

            // Camera orientation
            var cameraRotation = Quaternion.RotationY(yaw) * Quaternion.RotationX(pitch);
            Entity.Transform.Rotation = cameraRotation;

            // --- Camera Positioning ---
            Vector3 cameraTargetPosition;
            Vector3 desiredCameraPosition;

            var playerWorldPosition = Player.Transform.WorldMatrix.TranslationVector; // Use world position

            if (currentMode == CameraMode.FPS)
            {
                cameraTargetPosition = playerWorldPosition + new Vector3(0, FpsTargetHeight, 0);
                desiredCameraPosition = cameraTargetPosition; 
                // Player model should be hidden here (or arms shown)
                // Example: Player.GetChild(0)?.Get<ModelComponent>()?.Enabled = false; // Assuming model is first child
            }
            else // TPS Mode
            {
                cameraTargetPosition = playerWorldPosition + new Vector3(0, DefaultTpsHeightOffset, 0);
                Vector3 offsetDirection = Vector3.Transform(-Vector3.UnitZ, cameraRotation); // Use camera rotation for offset
                desiredCameraPosition = cameraTargetPosition + offsetDirection * DefaultTpsDistance;

                // Basic Collision Detection
                var raycastStart = cameraTargetPosition;
                var raycastEnd = desiredCameraPosition;
                var hitResult = simulation.Raycast(raycastStart, raycastEnd);

                if (hitResult.Succeeded)
                {
                    desiredCameraPosition = hitResult.Point + (raycastStart - raycastEnd).Normalized() * TpsCollisionMargin;
                }
                // Player model should be visible here
                // Example: Player.GetChild(0)?.Get<ModelComponent>()?.Enabled = true;
            }

            Entity.Transform.Position = desiredCameraPosition;
        }

        private void UpdateCameraMode()
        {
            if (Player == null) return;

            if (Player == null) return;

            // Player model visibility logic
            var playerModel = Player.GetChild(0)?.Get<ModelComponent>(); // Assumes model is on the first child
            // Alternative: var playerModel = Player.Get<ModelComponent>(); // If model is directly on Player entity

            if (playerModel != null)
            {
                playerModel.Enabled = (currentMode == CameraMode.TPS);
            }
            else
            {
                Log.Warning("Player model not found. Cannot set visibility.");
            }

            if (currentMode == CameraMode.FPS)
            {
                // Player model should be hidden in FPS mode (handled above)
                // Optionally, show FPS arms model here if available
                Log.Info("Switched to FPS mode. Player model hidden.");
            }
            else // TPS Mode
            {
                // Player model should be visible in TPS mode (handled above)
                Log.Info("Switched to TPS mode. Player model visible.");
            }
        }
    }
}
