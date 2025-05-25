using Stride.Engine; // Required for Entity

namespace MySurvivalGame.Game.Core
{
    public interface IDamageable
    {
        void TakeDamage(float amount, DamageType type);
        // Could also add other properties/methods like:
        // float CurrentHealth { get; }
        // bool IsDead { get; }
        // Stride.Engine.Entity Entity { get; } // To get the entity that was damaged
    }
}
