// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Engine;
using FirstPersonShooter.Core; // For IDamageable and ITargetable

namespace FirstPersonShooter.Tests
{
    public class MockDamageableTarget : ScriptComponent, ITargetable, IDamageable
    {
        public Vector3 TargetOffset { get; set; } = Vector3.Zero;
        public string TargetName { get; set; }
        public float Health { get; set; } = 100f;
        public int TimesDamaged { get; private set; }
        public float LastDamageAmount { get; private set; }
        public Entity LastDamageSource { get; private set; }

        // Default constructor for Stride serialization if attached in editor, though for tests we'll use the other one.
        public MockDamageableTarget() { TargetName = "UnnamedMock"; } 
        public MockDamageableTarget(string name) { TargetName = name; }

        public Vector3 GetTargetPosition() => Entity.Transform.Position + TargetOffset;
        
        // GetEntity() from ITargetable (if it were defined there, good practice for interfaces needing the entity)
        // public Entity GetEntity() => Entity; 

        public void TakeDamage(float amount, Entity source)
        {
            Health -= amount;
            TimesDamaged++;
            LastDamageAmount = amount;
            LastDamageSource = source;
            // Log.Info($"{TargetName} ({Entity?.Name}) took {amount} damage from {source?.Name}. Health: {Health}");
        }

        public void ResetDamageState(float initialHealth = 100f)
        {
            Health = initialHealth;
            TimesDamaged = 0;
            LastDamageAmount = 0f;
            LastDamageSource = null;
        }

        public override string ToString() => TargetName;
    }
}
