// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
// Adapted for MySurvivalGame.

using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Physics;
using Stride.Engine.Events; // Required for EventKey and EventReceiver

namespace MySurvivalGame.Game // MODIFIED: Namespace updated
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
        /// The mouse sensitivity. Note: PlayerInput script might also apply sensitivity.
        /// This value might need to be 1.0 if PlayerInput already handles it.
        /// </summary>
        public float CameraSensitivity { get; set; } = 1.0f; // MODIFIED: Default to 1.0 assuming PlayerInput handles sensitivity

        /// <summary>
        /// The minimum rotation X in degrees
        /// </summary>
        public float RotationXMin { get; set; } = -70.0f;

        /// <summary>
        /// The maximum rotation X in degrees
        /// </summary>
        public float RotationXMax { get; set; } = 70.0f;

        /// <summary>
        /// The player entity this camera is attached to. This should be the root player entity.
        /// </summary>
        public Entity Player { get; set; }

        /// <summary>
        /// The input component for the player. This should be the PlayerInput script instance on the Player entity.
        /// </summary>
        public PlayerInput PlayerInput { get; set; }

        private float yaw;
        private float pitch;
        private CameraMode currentMode = CameraMode.FPS;
        private Simulation simulation; 
        private Vector2 currentCameraInputDelta; // Stores mouse/gamepad input from PlayerInput

        // Event listeners
        private EventReceiver<Vector2> cameraDirectionEventListener;
        private EventListener<EventKey> switchCameraModeEventListener;


        public override void Start()
        {
            // Default values
            yaw = 0.0f; // Initialize yaw from player's current orientation if needed
            pitch = 0.0f;
            currentCameraInputDelta = Vector2.Zero;

            // Initialize from Player's current rotation to ensure camera starts facing where player is.
            if (Player != null)
            {
                Player.Transform.Rotation.ToYawPitchRoll(out yaw, out var initialPitch, out _);
                // Use initialPitch for the camera's pitch, respecting constraints.
                // This assumes player's forward is along Z. If not, adjust yaw offset.
                pitch = MathUtil.Clamp(initialPitch, MathUtil.DegreesToRadians(RotationXMin), MathUtil.DegreesToRadians(RotationXMax));

            }


            Game.IsMouseVisible = false; // Consider managing this globally or based on UI state

            simulation = this.GetSimulation();

            if (PlayerInput == null && Player != null)
            {
                // Try to get PlayerInput from the Player entity if not set
                PlayerInput = Player.Get<PlayerInput>();
            }
            
            // Subscribe to events from PlayerInput
            if (PlayerInput != null)
            {
                // It's important that CameraDirectionEventKey is static in PlayerInput
                cameraDirectionEventListener = new EventReceiver<Vector2>(PlayerInput.CameraDirectionEventKey);
                // It's important that SwitchCameraModeEventKey is static in PlayerInput
                switchCameraModeEventListener = new EventListener<EventKey>(PlayerInput.SwitchCameraModeEventKey, HandleSwitchCameraMode);

            }
            else
            {
                Log.Error("PlayerInput is not assigned or found on Player entity for PlayerCamera. Camera will not respond to input.");
            }
            
            UpdateCameraModeVisuals(); // Initial setup for player model visibility
        }

        public override void Cancel()
        {
            // Unsubscribe from events to prevent memory leaks
            PlayerInput?.SwitchCameraModeEventKey.RemoveListener(HandleSwitchCameraMode);
            // EventReceiver does not need explicit removal like this, it's managed by its lifetime.
            // if (cameraDirectionEventListener != null) { /* clean up if necessary, usually not for EventReceiver */ }

            base.Cancel();
        }
        
        private void HandleSwitchCameraMode(EventKey sender) // MODIFIED: Parameter changed to EventKey
        {
            currentMode = (currentMode == CameraMode.FPS) ? CameraMode.TPS : CameraMode.FPS;
            UpdateCameraModeVisuals();
        }

        private void UpdateCameraModeVisuals()
        {
            if (Player == null) return;

            // Player model visibility logic
            // Attempt to find a ModelComponent on a child named "PlayerModelPlaceholder" or the first child.
            var playerModelEntity = Player.GetChild("PlayerModelPlaceholder") ?? (Player.GetChildren().Count > 0 ? Player.GetChild(0) : null);
            var playerModel = playerModelEntity?.Get<ModelComponent>();

            if (playerModel != null)
            {
                playerModel.Enabled = (currentMode == CameraMode.TPS);
            }
            else
            {
                Log.Warning("Player model (or placeholder) not found as a child of Player. Cannot set visibility for FPS/TPS switch.");
            }

            if (currentMode == CameraMode.FPS)
            {
                Log.Info("Switched to FPS mode.");
            }
            else // TPS Mode
            {
                Log.Info("Switched to TPS mode.");
            }
        }

        public override void Update()
        {
            if (Player == null || PlayerInput == null || Entity == null) // Entity is the camera entity itself
                return;

            // Receive camera input delta from PlayerInput event
            if (cameraDirectionEventListener != null && cameraDirectionEventListener.TryReceive(out var newCameraInput))
            {
                currentCameraInputDelta = newCameraInput;
            }

            // Apply sensitivity (assuming PlayerInput sends raw/normalized delta, and this script scales it)
            // PlayerInput.cs (from previous task) scales mouse delta by (MouseSensitivity / 1000.0f)
            // and gamepad delta by DeltaTime.
            // So, CameraDirectionEventKey effectively carries already-scaled rotation values.
            // The CameraSensitivity here should ideally be 1.0f if PlayerInput handles all scaling.
            // If PlayerInput.MouseSensitivity is e.g. 100, and here CameraSensitivity is 20, the net effect is 2000x.
            // For now, assuming PlayerInput's CameraDirectionEventKey provides the final intended rotation delta.
            yaw -= currentCameraInputDelta.X * CameraSensitivity; 
            pitch -= currentCameraInputDelta.Y * CameraSensitivity; 
            currentCameraInputDelta = Vector2.Zero; // Reset after use for this frame

            // Clamp pitch
            pitch = MathUtil.Clamp(pitch, MathUtil.DegreesToRadians(RotationXMin), MathUtil.DegreesToRadians(RotationXMax));

            // Player orientation (only yaw, pitch is for camera only)
            // The Player entity itself should only rotate around the Y axis (yaw).
            Player.Transform.Rotation = Quaternion.RotationY(yaw);

            // Camera's local rotation (pitch) relative to the player's yaw.
            // The camera entity (Entity) is a child of Player, so its world rotation will be Player's world rotation * local camera rotation.
            // However, this script is attached to the Camera Entity itself, which might not be a child of Player yet
            // as per BasicScene.sdscene structure. The scene setup in step 3 will make Camera a child of Player.
            // For FPS, camera is at player's head. For TPS, it's offset.
            // The camera entity's rotation should be the full yaw and pitch.
            Entity.Transform.Rotation = Quaternion.RotationY(yaw) * Quaternion.RotationX(pitch);


            // --- Camera Positioning ---
            Vector3 cameraTargetPosition; // The point the camera looks at or originates from for raycasting
            Vector3 desiredCameraPosition; // The final position the camera should move to

            var playerWorldPosition = Player.Transform.WorldMatrix.TranslationVector;

            if (currentMode == CameraMode.FPS)
            {
                // Camera is positioned at the FpsTargetHeight on the Player entity.
                // Player.Transform.Position is the base of the player.
                // The Camera entity (this.Entity) is what needs to be positioned.
                // If Camera is a child of Player, its local position is relative to Player.
                desiredCameraPosition = Player.Transform.WorldMatrix.TranslationVector + Vector3.Transform(new Vector3(0, FpsTargetHeight, 0), Player.Transform.Rotation);
                // More simply, if camera is child of player and player rotation is already set:
                // Entity.Transform.LocalPosition = new Vector3(0, FpsTargetHeight, 0);
                // Entity.Transform.Position will be automatically calculated.
                // For FPS, the camera entity should be directly at the eye height, and share player's yaw, and have its own pitch.
                // The Player entity rotates with yaw. The Camera entity (this script is on) also rotates with yaw AND pitch.
                // So, the camera's position is relative to the already rotated Player.
                cameraTargetPosition = playerWorldPosition + Vector3.UnitY * FpsTargetHeight; // More accurate if player is base
                Entity.Transform.Position = cameraTargetPosition;
                Entity.Transform.Rotation = Quaternion.RotationY(yaw) * Quaternion.RotationX(pitch);


            }
            else // TPS Mode
            {
                // TPS camera orbits around a point slightly above the player's root.
                cameraTargetPosition = playerWorldPosition + Vector3.UnitY * DefaultTpsHeightOffset;
                
                // Offset direction is based on the camera's current full rotation (yaw and pitch)
                Vector3 offsetDirection = Vector3.Transform(-Vector3.UnitZ, Entity.Transform.Rotation);
                desiredCameraPosition = cameraTargetPosition + offsetDirection * DefaultTpsDistance;

                // Basic Collision Detection
                var raycastStart = cameraTargetPosition;
                var raycastEnd = desiredCameraPosition;
                var hitResult = simulation.Raycast(raycastStart, raycastEnd, CollisionFilterGroups.DefaultFilter, CollisionFilterGroupFlags.DefaultFilter); // Specify filter groups

                if (hitResult.Succeeded)
                {
                    // Move camera to hit point, plus a margin so it doesn't clip into the geometry
                    desiredCameraPosition = hitResult.Point + (raycastStart - raycastEnd).Normalized() * TpsCollisionMargin;
                }
                Entity.Transform.Position = desiredCameraPosition;
                Entity.Transform.Rotation = Quaternion.RotationY(yaw) * Quaternion.RotationX(pitch);
            }
        }
    }
}
