// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq; // For FirstOrDefault
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;
using FirstPersonShooter.Core;    // For MaterialType
using FirstPersonShooter.World;   // For IResourceNode, SurfaceMaterial
using FirstPersonShooter.Audio;   // For SoundManager

namespace FirstPersonShooter.Weapons.Projectiles
{
    public class ArrowProjectile : ScriptComponent
    {
        public float Damage { get; set; } = 10f;
        public float InitialSpeed { get; set; } = 20f; // Speed to set on Rigidbody
        public float Lifespan { get; set; } = 5f;     // Seconds before self-destruct if no collision

        private float lifeTimer = 0f;
        private RigidbodyComponent rigidbody;
        private bool hasHit = false; // To prevent multiple hit processing

        public override void Start()
        {
            rigidbody = Entity.Get<RigidbodyComponent>();
            if (rigidbody == null)
            {
                Log.Error($"ArrowProjectile on Entity '{Entity.Name}' requires a RigidbodyComponent.");
                DestroyProjectile(); // Self-destruct if no rigidbody
                return;
            }

            // Ensure the Rigidbody is not kinematic and has appropriate collision settings.
            // These should be set on the prefab.
            // rigidbody.IsKinematic = false; 
            // rigidbody.Restitution = 0.1f; // Little bounce
            // rigidbody.Friction = 0.8f;

            Entity.Transform.GetWorldTransformation(out Vector3 position, out Quaternion rotation, out Vector3 scale);
            rigidbody.LinearVelocity = Vector3.Transform(Vector3.UnitZ, rotation) * InitialSpeed; // Assumes arrow model Z is forward

            rigidbody.CollisionStarted += OnCollision;
            Log.Info($"ArrowProjectile '{Entity.Name}' started. Speed: {InitialSpeed}, Lifespan: {Lifespan}");
        }

        public override void Update()
        {
            if (hasHit) return; // Don't update lifespan if already hit and processing destruction

            lifeTimer += (float)Game.UpdateTime.Elapsed.TotalSeconds;
            if (lifeTimer >= Lifespan)
            {
                Log.Info($"ArrowProjectile '{Entity.Name}' lifespan expired.");
                DestroyProjectile();
            }
        }

        private void OnCollision(object sender, Collision args)
        {
            if (hasHit) return;
            hasHit = true; // Process hit only once

            var otherEntity = args.ColliderA == rigidbody ? args.ColliderB.Entity : args.ColliderA.Entity;
            Log.Info($"ArrowProjectile '{Entity.Name}' hit {otherEntity?.Name ?? "something"}.");

            MaterialType surfaceMatType = MaterialType.Default;
            var resourceNode = otherEntity?.Get<IResourceNode>();
            if (resourceNode != null)
            {
                surfaceMatType = resourceNode.HitMaterial;
                // Optionally, arrows could do small damage to resource nodes or specific types
                // resourceNode.Harvest(this.Damage * 0.1f, null); // Example: Arrow does 10% of its damage to node
            }
            else
            {
                var surfaceMaterialComponent = otherEntity?.Get<SurfaceMaterial>();
                if (surfaceMaterialComponent != null)
                {
                    surfaceMatType = surfaceMaterialComponent.Type;
                }
            }

            // Assuming arrow is made of Wood for sound purposes. This could be a property.
            SoundManager.PlayImpactSound(args.Contacts.FirstOrDefault()?.Position ?? Entity.Transform.Position, MaterialType.Wood, surfaceMatType);

            // Conceptual: Apply damage to otherEntity if it's damageable
            // var damageable = otherEntity?.Get<IDamageable>();
            // if (damageable != null) { damageable.TakeDamage(this.Damage, null /* Attacker unknown or from arrow's owner */); }
            
            // Stick the arrow into the target by making it kinematic and parenting (optional)
            // This requires more complex logic, e.g., unparenting if already parented,
            // setting local transform relative to hit point, etc.
            // For now, just destroy.
            // Example:
            // rigidbody.IsKinematic = true;
            // rigidbody.LinearVelocity = Vector3.Zero;
            // rigidbody.AngularVelocity = Vector3.Zero;
            // this.Entity.Transform.Parent = otherEntity?.Transform; // May cause issues if otherEntity is destroyed

            DestroyProjectile();
        }

        private void DestroyProjectile()
        {
            if (rigidbody != null)
            {
                rigidbody.CollisionStarted -= OnCollision; // Unsubscribe
            }
            
            // Check if Entity is already removed or being removed
            if (this.Entity.Scene != null)
            {
                this.Entity.Scene = null; // Removes entity from scene
                Log.Info($"ArrowProjectile '{Entity.Name}' destroyed.");
            }
        }
    }
}
