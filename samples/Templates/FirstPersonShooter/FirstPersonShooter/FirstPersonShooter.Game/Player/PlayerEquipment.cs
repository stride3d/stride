// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using Stride.Engine.Events; // For EventReceiver
using FirstPersonShooter.Weapons; // For BaseWeapon
// Assuming PlayerInput is in FirstPersonShooter.Player namespace
// using FirstPersonShooter.Player; 

namespace FirstPersonShooter.Player
{
    /// <summary>
    /// Manages the equipment of a player, specifically their current weapon.
    /// Also handles relaying input actions to the equipped weapon.
    /// </summary>
    public class PlayerEquipment : ScriptComponent
    {
        /// <summary>
        /// Gets the currently equipped weapon.
        /// </summary>
        public BaseWeapon CurrentWeapon { get; private set; }

        private EventReceiver<bool> shootEventReceiver;
        private EventReceiver<bool> reloadEventReceiver;
        private EventReceiver shootReleasedEventReceiver; 
        // Event receivers for building mode
        private EventReceiver toggleBuildModeEventReceiver;
        private EventReceiver rotateBuildLeftEventReceiver;
        private EventReceiver rotateBuildRightEventReceiver;
        private EventReceiver cycleBuildableNextEventReceiver;
        private EventReceiver cycleBuildablePrevEventReceiver;
        private EventReceiver debugDestroyEventReceiver;

        private BuildingPlacementController buildingPlacementController; // Reference to the building controller

        public override void Start()
        {
            base.Start(); // Good practice

            // Initialize event receivers for weapon actions
            shootEventReceiver = new EventReceiver<bool>(PlayerInput.ShootEventKey);
            reloadEventReceiver = new EventReceiver<bool>(PlayerInput.ReloadEventKey);
            shootReleasedEventReceiver = new EventReceiver(PlayerInput.ShootReleasedEventKey);
            
            // Initialize event receivers for building actions
            toggleBuildModeEventReceiver = new EventReceiver(PlayerInput.ToggleBuildModeEventKey);
            rotateBuildLeftEventReceiver = new EventReceiver(PlayerInput.RotateBuildActionLeftEventKey);
            rotateBuildRightEventReceiver = new EventReceiver(PlayerInput.RotateBuildActionRightEventKey);
            cycleBuildableNextEventReceiver = new EventReceiver(PlayerInput.CycleBuildableNextEventKey);
            cycleBuildablePrevEventReceiver = new EventReceiver(PlayerInput.CycleBuildablePrevEventKey);
            debugDestroyEventReceiver = new EventReceiver(PlayerInput.DebugDestroyEventKey);
            
            // Get BuildingPlacementController, assuming it's on the same entity
            buildingPlacementController = Entity.Get<BuildingPlacementController>();
            if (buildingPlacementController == null)
            {
                Log.Warning("PlayerEquipment: BuildingPlacementController not found on this entity. Building mode will not function.");
            }
        }

        public override void Update()
        {
            // Handle Building Mode Toggle and Rotation Inputs
            if (buildingPlacementController != null)
            {
                if (toggleBuildModeEventReceiver.TryReceive())
                {
                    buildingPlacementController.ToggleBuildingMode();
                }

                // Only process rotation if building mode is active
                if (buildingPlacementController.IsBuildingModeActive)
                {
                    if (rotateBuildLeftEventReceiver.TryReceive())
                    {
                        buildingPlacementController.RotateGhost(false); // false for counter-clockwise / left
                    }
                    if (rotateBuildRightEventReceiver.TryReceive())
                    {
                        buildingPlacementController.RotateGhost(true); // true for clockwise / right
                    }

                    // Handle cycling buildable items
                    if (cycleBuildableNextEventReceiver.TryReceive())
                    {
                        buildingPlacementController.CycleBuildableItem(true); // true for next
                    }
                    if (cycleBuildablePrevEventReceiver.TryReceive())
                    {
                        buildingPlacementController.CycleBuildableItem(false); // false for previous
                    }
                }
            }

            // Handle Primary Action (Shoot / Place Building)
            if (shootEventReceiver.TryReceive(out bool shootPressed) && shootPressed)
            {
                // TriggerCurrentWeaponPrimary will check if in build mode first
                TriggerCurrentWeaponPrimary();
            }

            // Handle Shoot Release (for Bows, only if not in build mode)
            if (shootReleasedEventReceiver.TryReceive()) 
            {
                if (buildingPlacementController == null || !buildingPlacementController.IsBuildingModeActive)
                {
                    if (CurrentWeapon is Weapons.Ranged.BaseBowWeapon bowWeapon)
                    {
                        bowWeapon.OnPrimaryActionReleased();
                    }
                }
            }

            // Handle Reload (only if not in build mode)
            if (reloadEventReceiver.TryReceive(out bool reloadPressed) && reloadPressed)
            {
                if (buildingPlacementController == null || !buildingPlacementController.IsBuildingModeActive)
                {
                    if (CurrentWeapon != null && !CurrentWeapon.IsBroken) 
                    {
                        CurrentWeapon.Reload();
                    }
                    else if (CurrentWeapon != null && CurrentWeapon.IsBroken)
                    {
                         Log.Info($"PlayerEquipment: Cannot reload, {CurrentWeapon.GetEntity()?.Name ?? "Current weapon"} is broken.");
                    }
                }
            }

            // Handle Debug Destroy action
            if (debugDestroyEventReceiver.TryReceive())
            {
                if (buildingPlacementController == null || !buildingPlacementController.IsBuildingModeActive)
                {
                    DebugAttemptDestroyTarget();
                }
                else
                {
                    Log.Info("Debug Destroy action ignored: In building mode.");
                }
            }
        }

        private void DebugAttemptDestroyTarget()
        {
            var playerInput = Entity.Get<PlayerInput>(); // Need this to get the camera
            if (playerInput == null || playerInput.Camera == null)
            {
                Log.Error("PlayerEquipment.DebugAttemptDestroyTarget: PlayerInput or Camera not found.");
                return;
            }

            var camera = playerInput.Camera;
            var simulation = this.GetSimulation();
            if (simulation == null)
            {
                Log.Error("PlayerEquipment.DebugAttemptDestroyTarget: Physics simulation not found.");
                return;
            }

            Matrix cameraWorldMatrix = camera.Entity.Transform.WorldMatrix;
            Vector3 raycastStart = cameraWorldMatrix.TranslationVector;
            Vector3 raycastForward = cameraWorldMatrix.Forward;
            float raycastDistance = 15f; // Max distance for debug destroy

            var hitResult = simulation.Raycast(raycastStart, raycastStart + raycastForward * raycastDistance);

            if (hitResult.Succeeded && hitResult.Collider != null)
            {
                var hitEntity = hitResult.Collider.Entity;
                var pieceToDestroy = hitEntity?.Get<FirstPersonShooter.Building.Pieces.BaseBuildingPiece>(); // Fully qualify

                if (pieceToDestroy != null)
                {
                    Log.Info($"Debug: Attempting to force destroy {hitEntity.Name}.");
                    pieceToDestroy.Debug_ForceDestroy();
                }
                else
                {
                    Log.Info($"Debug: Raycast hit {hitEntity?.Name ?? "Unknown"}, but it's not a BaseBuildingPiece.");
                }
            }
            else
            {
                Log.Info("Debug: Destroy raycast hit nothing.");
            }
        }


        /// <summary>
        /// Equips a new weapon. If a weapon is already equipped, it will be unequipped first.
        /// </summary>
        /// <param name="newWeapon">The new weapon to equip. Can be null to unequip.</param>
        public void EquipWeapon(BaseWeapon newWeapon)
        {
            // Unequip the current weapon if one exists
            if (CurrentWeapon != null)
            {
                CurrentWeapon.OnUnequip(this.Entity);
                // Optional: Unparent CurrentWeapon.GetEntity() from the player's hand/attachment point.
                // Log.Info($"Unequipped {CurrentWeapon.GetEntity()?.Name}");
            }

            CurrentWeapon = newWeapon;

            // Equip the new weapon if it's not null
            if (CurrentWeapon != null)
            {
                CurrentWeapon.OnEquip(this.Entity);
                // Optional: Parent CurrentWeapon.GetEntity() to a specific hand/attachment point on this.Entity.
                // For example, find a child entity named "WeaponSlot" and parent the weapon's entity to it.
                // var weaponSlot = this.Entity.FindChild("WeaponSlot");
                // if (weaponSlot != null && CurrentWeapon.GetEntity() != null)
                // {
                //     CurrentWeapon.GetEntity().Transform.Parent = weaponSlot.Transform;
                //     CurrentWeapon.GetEntity().Transform.Position = Vector3.Zero; // Reset local position
                //     CurrentWeapon.GetEntity().Transform.Rotation = Quaternion.Identity; // Reset local rotation
                // }
                // Log.Info($"Equipped {CurrentWeapon.GetEntity()?.Name}");
            }
        }

        // Example of how this might be used (e.g., driven by PlayerInput or an inventory system):
        // public override void Update()
        // {
        //     // Example: Press '1' to equip a hypothetical weapon (this would need a weapon instance)
        //     if (Input.IsKeyPressed(Keys.D1) && someWeaponInstance != null)
        //     {
        //         EquipWeapon(someWeaponInstance); 
        //     }
        //     // Example: Press 'Q' to unequip
        //     if (Input.IsKeyPressed(Keys.Q))
        //     {
        //         EquipWeapon(null);
        //     }
        //
        //     // Example: Use the equipped weapon
        //     if (CurrentWeapon != null && Input.IsMouseButtonDown(MouseButton.Left)) // Assuming ShootEventKey is true
        //     {
        //          // This would ideally be driven by an event from PlayerInput, similar to ShootEventKey
        //          // And the cooldown based on AttackRate would be handled within the weapon itself or here.
        //         CurrentWeapon.PrimaryAction();
        //     }
        // }

        /// <summary>
        /// Triggers the primary action of the currently equipped weapon or places a building.
        /// </summary>
        public void TriggerCurrentWeaponPrimary()
        {
            // If building mode is active and controller exists, attempt to place building.
            if (buildingPlacementController != null && buildingPlacementController.IsBuildingModeActive)
            {
                buildingPlacementController.TryPlaceBuilding();
                return; // Do not proceed to weapon actions if in build mode.
            }

            // If not in build mode, or no controller, proceed with weapon action.
            if (CurrentWeapon == null)
            {
                // Log.Warning("PlayerEquipment: No weapon equipped to trigger primary action."); // Optional
                return;
            }

            if (CurrentWeapon.IsBroken)
            {
                Log.Info($"PlayerEquipment: Cannot use primary action, {CurrentWeapon.GetEntity()?.Name ?? "Current weapon"} is broken.");
                return;
            }

            CurrentWeapon.PrimaryAction();
        }

        /// <summary>
        /// Triggers the secondary action of the currently equipped weapon.
        /// </summary>
        public void TriggerCurrentWeaponSecondary()
        {
            if (CurrentWeapon == null)
            {
                // Log.Warning("PlayerEquipment: No weapon equipped to trigger secondary action.");
                return;
            }

            if (CurrentWeapon.IsBroken)
            {
                Log.Info($"PlayerEquipment: Cannot use secondary action, {CurrentWeapon.GetEntity()?.Name ?? "Current weapon"} is broken.");
                return;
            }

            CurrentWeapon.SecondaryAction();
        }
    }
}
