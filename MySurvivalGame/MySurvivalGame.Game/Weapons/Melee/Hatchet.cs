using Stride.Engine;
// Potentially using MySurvivalGame.Game.Core for ITargetable or IDamageable later

namespace MySurvivalGame.Game.Weapons.Melee
{
    public class Hatchet : BaseMeleeWeapon
    {
        public override void PrimaryAction()
        {
            Log.Info($"{this.Entity?.Name ?? "Hatchet"}: Hatchet Swung.");
            // Future: Trigger animation, sound, collision check for damage/resource gathering
        }

        public override void SecondaryAction()
        {
            Log.Info($"{this.Entity?.Name ?? "Hatchet"}: Hatchet Secondary Action (e.g., block or stronger swing - TBD).");
        }

        public override void Reload()
        {
            // Typically no reload for a hatchet
            Log.Info($"{this.Entity?.Name ?? "Hatchet"}: Hatchet has no reload action.");
        }

        public override void OnEquip(Entity owner)
        {
            Log.Info($"{this.Entity?.Name ?? "Hatchet"}: Hatchet equipped by {owner?.Name}.");
            // Future: Change player animations, parent model to hand
        }

        public override void OnUnequip(Entity owner)
        {
            Log.Info($"{this.Entity?.Name ?? "Hatchet"}: Hatchet unequipped by {owner?.Name}.");
            // Future: Revert player animations
        }
    }
}
