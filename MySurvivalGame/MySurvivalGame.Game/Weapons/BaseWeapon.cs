// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
// Adapted for MySurvivalGame.

using Stride.Engine;
// Potentially using MySurvivalGame.Game.Core for ITargetable if needed later
// Potentially using MySurvivalGame.Game.Items for MaterialType if needed later

namespace MySurvivalGame.Game.Weapons
{
    public abstract class BaseWeapon : ScriptComponent // Or EntityComponent, or just a class if not a component itself
    {
        public virtual bool IsBroken { get; protected set; } = false;
        public virtual float Durability { get; protected set; } = 1.0f; // Example: 1.0 = 100%

        public abstract void PrimaryAction();
        public abstract void SecondaryAction();
        public abstract void Reload();
        public abstract void OnEquip(Entity owner);
        public abstract void OnUnequip(Entity owner);

        // Placeholder for potential bow functionality, if BaseBowWeapon is to exist
        public virtual void OnPrimaryActionReleased() { } 
    }

    // Placeholder for BaseBowWeapon if referenced by PlayerEquipment
    // (PlayerEquipment.cs was modified to use MySurvivalGame.Game.Weapons.BaseBowWeapon)
    public abstract class BaseBowWeapon : BaseWeapon 
    {
        // Specific bow logic would go here
        // For example:
        // public abstract void ChargeShot(float chargeTime);
        // public override void OnPrimaryActionReleased() { /* Fire Arrow based on charge */ }
    }
}
