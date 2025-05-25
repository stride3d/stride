// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using Stride.Engine.Events; 
using MySurvivalGame.Game.Weapons; 
using MySurvivalGame.Game.Player;   
using MySurvivalGame.Game.Items; 
using MySurvivalGame.Game.World; // ADDED: For ResourceNodeComponent
using Stride.Physics; // ADDED: For Raycasting
using Stride.Core.Mathematics; // ADDED: For Matrix and Vector3

namespace MySurvivalGame.Game.Player 
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
        private MySurvivalGame.Game.Items.WeaponToolData currentlyEquippedItemData; 

        private EventReceiver<bool> shootEventReceiver;
        private EventReceiver<bool> reloadEventReceiver;
        private EventReceiver shootReleasedEventReceiver; 
        private EventReceiver interactReceiver; // ADDED

        // REMOVED: Building related event receivers
        // private EventReceiver toggleBuildModeEventReceiver;
        // private EventReceiver rotateBuildLeftEventReceiver;
        // private EventReceiver rotateBuildRightEventReceiver;
        // private EventReceiver cycleBuildableNextEventReceiver;
        // private EventReceiver cycleBuildablePrevEventReceiver;
        // private EventReceiver debugDestroyEventReceiver;

        // REMOVED: Building controller reference
        // private BuildingPlacementController buildingPlacementController; 

        public override void Start()
        {
            base.Start(); 

            // Initialize event receivers for weapon actions
            shootEventReceiver = new EventReceiver<bool>(PlayerInput.ShootEventKey);
            reloadEventReceiver = new EventReceiver<bool>(PlayerInput.ReloadEventKey);
            shootReleasedEventReceiver = new EventReceiver(PlayerInput.ShootReleasedEventKey);
            interactReceiver = new EventReceiver(PlayerInput.InteractEventKey); // ADDED
            
            // REMOVED: Initialization of building event receivers
            // REMOVED: Getting BuildingPlacementController
        }

        public override void Update()
        {
            // ADDED: Interaction logic
            if (interactReceiver.TryReceive())
            {
                AttemptResourceGather();
            }

            // Handle Primary Action (Shoot)
            if (shootEventReceiver.TryReceive(out bool shootPressed) && shootPressed)
            {
                // MODIFIED: Direct weapon action, removed TriggerCurrentWeaponPrimary call
                if (CurrentWeapon == null)
                {
                    // Log.Warning("PlayerEquipment: No weapon equipped."); // Optional
                }
                else if (CurrentWeapon.IsBroken) 
                {
                    Log.Info($"PlayerEquipment: Cannot use primary action, {CurrentWeapon.Entity?.Name ?? "Current weapon"} is broken.");
                }
                else
                {
                    CurrentWeapon.PrimaryAction();
                }
            }

            // Handle Shoot Release (for Bows)
            if (shootReleasedEventReceiver.TryReceive()) 
            {
                // REMOVED: Building mode check
                if (CurrentWeapon is BaseBowWeapon bowWeapon) // MODIFIED: Namespace for BaseBowWeapon
                {
                    bowWeapon.OnPrimaryActionReleased();
                }
            }

            // Handle Reload
            if (reloadEventReceiver.TryReceive(out bool reloadPressed) && reloadPressed)
            {
                // REMOVED: Building mode check
                if (CurrentWeapon != null && !CurrentWeapon.IsBroken) 
                {
                    CurrentWeapon.Reload();
                }
                else if (CurrentWeapon != null && CurrentWeapon.IsBroken)
                {
                     Log.Info($"PlayerEquipment: Cannot reload, {CurrentWeapon.Entity?.Name ?? "Current weapon"} is broken.");
                }
            }

            // REMOVED: Debug Destroy action and its call
        }

        // REMOVED: DebugAttemptDestroyTarget() method

        /// <summary>
        /// Equips a new weapon. If a weapon is already equipped, it will be unequipped first.
        /// </summary>
        /// <param name="newWeapon">The new weapon to equip. Can be null to unequip.</param>
        public void EquipWeapon(BaseWeapon newWeapon) 
        {
            if (CurrentWeapon != null)
            {
                CurrentWeapon.OnUnequip(this.Entity);
                // Potentially destroy the old weapon entity if it was spawned
            }

            CurrentWeapon = newWeapon;

            if (CurrentWeapon != null)
            {
                CurrentWeapon.OnEquip(this.Entity);
                // Potentially attach the new weapon model to the player
            }
        }

        public void EquipItem(MySurvivalGame.Game.Items.MockInventoryItem itemToEquip)
        {
            // Unequip current item's logic (conceptual for now)
            if (currentlyEquippedItemData != null)
            {
                Log.Info($"PlayerEquipment: Unequipping '{currentlyEquippedItemData.Name}'.");
                // Future: Call OnUnequip on the actual weapon entity if one was spawned.
                // CurrentWeapon (BaseWeapon script instance) might be set to null here if it represented this item.
            }

            currentlyEquippedItemData = null; // Clear it first

            if (itemToEquip != null)
            {
                if (itemToEquip.CurrentEquipmentType == EquipmentType.Weapon || 
                    itemToEquip.CurrentEquipmentType == EquipmentType.Tool)
                {
                    if (itemToEquip is WeaponToolData castedItem)
                    {
                        currentlyEquippedItemData = castedItem;
                        Log.Info($"PlayerEquipment: Equipped '{currentlyEquippedItemData.Name}' (Type: {currentlyEquippedItemData.CurrentEquipmentType}, Damage: {currentlyEquippedItemData.Damage}, Durability: {currentlyEquippedItemData.DurabilityPoints}/{currentlyEquippedItemData.MaxDurabilityPoints}).");
                        // Future: Spawn actual weapon entity, get its BaseWeapon script, call EquipWeapon(baseWeaponScript)
                    }
                    else
                    {
                        Log.Error($"PlayerEquipment: Item '{itemToEquip.Name}' is a Weapon/Tool but could not be cast to WeaponToolData. Check item creation.");
                    }
                }
                else
                {
                    Log.Info($"PlayerEquipment: Item '{itemToEquip.Name}' is not a Weapon or Tool. Cannot equip in main weapon slot. (Type: {itemToEquip.CurrentEquipmentType})");
                }
            }
            else
            {
                Log.Info("PlayerEquipment: No item equipped (itemToEquip was null).");
                // Future: Call EquipWeapon(null) to clear any actual BaseWeapon script instance.
            }
        }
        
        /// <summary>
        /// Triggers the primary action of the currently equipped weapon.
        /// </summary>
        public void TriggerCurrentWeaponPrimary()
        {
            if (currentlyEquippedItemData == null)
            {
                // Log.Info("PlayerEquipment: No item equipped to use."); // Optional, can be verbose
                return;
            }

            if (currentlyEquippedItemData.IsBroken)
            {
                Log.Info($"PlayerEquipment: Item '{currentlyEquippedItemData.Name}' is broken. Cannot use.");
                return;
            }

            // --- Durability Consumption ---
            // For now, consume a fixed amount. This could vary by item or action later.
            float durabilityCost = 1.0f; 
            currentlyEquippedItemData.DurabilityPoints -= durabilityCost;

            // Ensure durability doesn't go below zero before updating IsBroken & base Durability
            if (currentlyEquippedItemData.DurabilityPoints < 0)
            {
                currentlyEquippedItemData.DurabilityPoints = 0;
            }
            
            // Call the UpdateDurability method in WeaponToolData to correctly update IsBroken and sync base.Durability
            currentlyEquippedItemData.UpdateDurability(currentlyEquippedItemData.DurabilityPoints);

            Log.Info($"PlayerEquipment: Used '{currentlyEquippedItemData.Name}'. Durability: {currentlyEquippedItemData.DurabilityPoints}/{currentlyEquippedItemData.MaxDurabilityPoints}. Broken: {currentlyEquippedItemData.IsBroken}");

            if (currentlyEquippedItemData.IsBroken)
            {
                Log.Warning($"PlayerEquipment: Item '{currentlyEquippedItemData.Name}' just broke!");
                // Future: Play a 'broken item' sound or visual effect.
            }
            // --- End Durability Consumption ---

            // Call the actual weapon's action (if a BaseWeapon script instance is equipped)
            // This part remains for future integration with actual weapon scripts.
            if (CurrentWeapon != null) // CurrentWeapon is the BaseWeapon script instance
            {
                // CurrentWeapon.PrimaryAction(); // This will be called on the actual weapon script
            }
            else
            {
                Log.Info($"PlayerEquipment: Primary action triggered for item data '{currentlyEquippedItemData.Name}', but no specific weapon script (CurrentWeapon) is active.");
            }
        }

        /// <summary>
        /// Triggers the secondary action of the currently equipped weapon.
        /// </summary>
        public void TriggerCurrentWeaponSecondary()
        {
            if (CurrentWeapon == null)
            {
                return;
            }

            if (CurrentWeapon.IsBroken)
            {
                Log.Info($"PlayerEquipment: Cannot use secondary action, {CurrentWeapon.Entity?.Name ?? "Current weapon"} is broken.");
                return;
            }

            CurrentWeapon.SecondaryAction();
        }

        private void AttemptResourceGather()
        {
            // The condition for hand gathering "!(this.Entity.Get<PlayerInput>()?.Camera?.Get<PlayerCamera>()?.IsFPSCrouched ?? false)"
            // was an example and is removed for clarity. We will rely on the ResourceNodeComponent's ToolCategory.
            // If currentlyEquippedItemData is null, ResourceNodeComponent.HitNode will handle it (e.g. if ToolCategory is Hand).

            var playerInput = this.Entity.Get<PlayerInput>();
            if (playerInput == null || playerInput.Camera == null)
            {
                Log.Error("PlayerEquipment.AttemptResourceGather: PlayerInput or Camera not found.");
                return;
            }

            var camera = playerInput.Camera; // This is the CameraComponent
            var simulation = this.GetSimulation();
            if (simulation == null)
            {
                Log.Error("PlayerEquipment.AttemptResourceGather: Physics simulation not found.");
                return;
            }

            Matrix cameraWorldMatrix = camera.Entity.Transform.WorldMatrix; // Camera's entity transform
            Vector3 raycastStart = cameraWorldMatrix.TranslationVector;
            Vector3 raycastForward = cameraWorldMatrix.Forward;
            float gatherRange = 2.0f; // Max distance for gathering

            // Perform raycast
            var hitResult = simulation.Raycast(raycastStart, raycastStart + raycastForward * gatherRange);

            if (hitResult.Succeeded && hitResult.Collider != null)
            {
                var hitEntity = hitResult.Collider.Entity;
                var resourceNode = hitEntity?.Get<MySurvivalGame.Game.World.ResourceNodeComponent>();
                var playerInventory = this.Entity.Get<PlayerInventoryComponent>();

                if (resourceNode != null && playerInventory != null)
                {
                    Log.Info($"PlayerEquipment: Interacted with '{hitEntity.Name}' which has a ResourceNodeComponent.");
                    
                    // Try to hit the node with the currently equipped tool (which might be null)
                    var harvestedItem = resourceNode.HitNode(currentlyEquippedItemData, playerInventory);

                    if (harvestedItem != null) // HitNode returns an item if harvest was successful
                    {
                        Log.Info($"PlayerEquipment: Successfully harvested '{harvestedItem.Name}' using '{currentlyEquippedItemData?.Name ?? "Hands (conceptual)"}'.");
                        
                        // If a tool was used (not null) and harvest was successful, consume durability
                        if (currentlyEquippedItemData != null)
                        {
                            if (currentlyEquippedItemData.IsBroken) // Check if tool was already broken
                            {
                                Log.Info($"PlayerEquipment: Tool '{currentlyEquippedItemData.Name}' is broken. Cannot use further for gathering.");
                                // Note: HitNode might still allow harvest if tool isn't strictly required or if it just broke.
                                // If HitNode returned an item, it means harvest occurred. We just log tool status here.
                                return; 
                            }

                            float durabilityCost = 1.0f; // Specific to gathering action
                            currentlyEquippedItemData.DurabilityPoints -= durabilityCost;
                            // No need to clamp here as UpdateDurability will handle it.
                            
                            currentlyEquippedItemData.UpdateDurability(currentlyEquippedItemData.DurabilityPoints); // This updates IsBroken and base.Durability

                            Log.Info($"PlayerEquipment: Tool '{currentlyEquippedItemData.Name}' durability: {currentlyEquippedItemData.DurabilityPoints}/{currentlyEquippedItemData.MaxDurabilityPoints}. Broken: {currentlyEquippedItemData.IsBroken}");
                            if (currentlyEquippedItemData.IsBroken)
                            {
                                Log.Warning($"PlayerEquipment: Tool '{currentlyEquippedItemData.Name}' just broke from gathering!");
                                // Future: Player notification, potentially unequip, etc.
                            }
                        }
                    }
                    // If harvestedItem is null, HitNode already logged why (e.g. wrong tool, depleted, inventory full)
                }
                else
                {
                    // Log.Info($"PlayerEquipment: Interacted with '{hitEntity.Name}', but it's not a resource node or player inventory is missing.");
                }
            }
            else
            {
                // Log.Info("PlayerEquipment: Interaction raycast hit nothing in range.");
            }
        }
    }
}
