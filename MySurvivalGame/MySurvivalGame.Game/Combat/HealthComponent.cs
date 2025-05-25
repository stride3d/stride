using Stride.Engine;
using Stride.Core; // For [DataMember]
using MySurvivalGame.Game.Core; // For IDamageable, DamageType
using MySurvivalGame.Game.Audio; // ADDED for GameSoundManager

namespace MySurvivalGame.Game.Combat
{
    public class HealthComponent : ScriptComponent, IDamageable 
    {
        [DataMember(0)] 
        public float MaxHealth { get; set; } = 100f;

        private float _currentHealth;
        [DataMember(1)] 
        public float CurrentHealth 
        { 
            get => _currentHealth;
            set
            {
                _currentHealth = System.Math.Clamp(value, 0f, MaxHealth);
                IsDead = _currentHealth <= 0;
                // Optional: Broadcast health changed event here
            }
        }

        public bool IsDead { get; private set; } = false;
        
        // Implementation of IDamageable.Entity
        // This is automatically available because ScriptComponent inherits from EntityComponent,
        // which has an 'Entity' property. So, no explicit implementation is needed if
        // IDamageable's Entity property is 'Stride.Engine.Entity Entity { get; }'
        // If IDamageable.Entity was 'new Entity Entity { get; }', then explicit implementation would be:
        // Entity IDamageable.Entity => this.Entity; 

        public override void Start()
        {
            CurrentHealth = MaxHealth; // Initialize health
            IsDead = false;
        }

        public void TakeDamage(float amount, DamageType type)
        {
            if (IsDead) return;

            CurrentHealth -= amount;
            Log.Info($"{Entity.Name} took {amount} damage of type {type}. Current Health: {CurrentHealth}/{MaxHealth}");
            GameSoundManager.PlaySound("Damage_Taken", this.Entity.Transform.WorldMatrix.TranslationVector); // ADDED

            if (IsDead)
            {
                Log.Warning($"{Entity.Name} has died.");
                GameSoundManager.PlaySound("Entity_Died", this.Entity.Transform.WorldMatrix.TranslationVector); // ADDED
                // Future: Trigger death animations, drop loot, notify game systems, etc.
                // For now, could disable the entity:
                // Entity.Enabled = false; 
            }
        }
    }
}
