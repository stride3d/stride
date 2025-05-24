// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Engine;
using FirstPersonShooter.Core; // For MaterialType
using Stride.Particles;
using Stride.Audio;
using Stride.Physics;
using Stride.Core.Mathematics; // Required for Vector3 for forces

// No direct reference to SnapPoint here as it's used by derived classes in their InitializeSnapPoints or by BuildingPlacementController

namespace FirstPersonShooter.Building.Pieces
{
    public abstract class BaseBuildingPiece : ScriptComponent
    {
        /// <summary>
        /// The health of this building piece. Must be implemented by derived classes.
        /// </summary>
        public abstract float Health { get; set; }

        /// <summary>
        /// The material type of this structure, for impact sounds or damage calculations. Must be implemented by derived classes.
        /// </summary>
        public abstract MaterialType StructureMaterialType { get; set; }

        /// <summary>
        /// Prefab for the particle effect to play upon destruction.
        /// </summary>
        public ParticleSystemComponent DestructionParticlePrefab { get; set; }

        /// <summary>
        /// Sound to play when a wood piece is destroyed.
        /// </summary>
        public Sound WoodDestroySound { get; set; }

        /// <summary>
        /// Sound to play when a stone piece is destroyed.
        /// </summary>
        public Sound StoneDestroySound { get; set; }

        /// <summary>
        /// Sound to play when a metal piece is destroyed.
        /// </summary>
        public Sound MetalDestroySound { get; set; }


        /// <summary>
        /// List of points where other building pieces can snap to this one.
        /// Populated by InitializeSnapPoints in derived classes.
        /// </summary>
        public List<SnapPoint> SnapPoints { get; protected set; } = new List<SnapPoint>();

        /// <summary>
        /// Abstract method to be implemented by derived classes to define their specific snap points.
        /// </summary>
        public abstract void InitializeSnapPoints();

        /// <summary>
        /// Indicates if this building piece is currently considered anchored to the ground or a ground-anchored structure.
        /// </summary>
        public bool IsAnchored { get; set; } = false;

        /// <summary>
        /// Defines if this type of piece can be considered a ground anchor if placed on terrain.
        /// E.g., Foundations are ground pieces, Walls are not.
        /// </summary>
        public bool IsGroundPiece { get; set; } = false;

        public List<BaseBuildingPiece> ConnectedPieces { get; private set; } = new List<BaseBuildingPiece>();

        public override void Start()
        {
            // Ensure SnapPoints list is initialized if derived class didn't do it (though it should)
            if (SnapPoints == null)
            {
                SnapPoints = new List<SnapPoint>();
            }
            InitializeSnapPoints();
            // Log.Info($"{GetType().Name} '{Entity?.Name ?? "Unnamed"}' initialized with {SnapPoints.Count} snap points.");
        }

        public void AddConnection(BaseBuildingPiece otherPiece)
        {
            if (otherPiece != null && !ConnectedPieces.Contains(otherPiece))
            {
                ConnectedPieces.Add(otherPiece);
                Log.Info($"{this.Entity?.Name ?? GetType().Name} connected to {otherPiece.Entity?.Name ?? otherPiece.GetType().Name}.");
            }
        }

        public void RemoveConnection(BaseBuildingPiece otherPiece)
        {
            if (otherPiece != null && ConnectedPieces.Contains(otherPiece))
            {
                ConnectedPieces.Remove(otherPiece);
                Log.Info($"{this.Entity?.Name ?? GetType().Name} disconnected from {otherPiece.Entity?.Name ?? otherPiece.GetType().Name}.");
            }
        }

        public virtual void OnPieceDestroyed()
        {
            Log.Info($"{this.Entity?.Name ?? GetType().Name} OnPieceDestroyed called. Notifying {ConnectedPieces.Count} connected pieces.");
            var piecesToNotify = new List<BaseBuildingPiece>(ConnectedPieces); // Iterate over a copy

            foreach (var piece in piecesToNotify)
            {
                piece.RemoveConnection(this);
                // Note: piece.CheckStructuralIntegrity() was a future comment, now handled by UpdateAnchorStatusForStructure
            }
            ConnectedPieces.Clear();

            // After notifying direct connections they are no longer connected to *this* piece,
            // trigger a structure integrity check for each of them starting from themselves.
            // This will re-evaluate their anchor status and potentially lead to collapses.
            Log.Info($"OnPieceDestroyed for {this.Entity?.Name ?? GetType().Name}: Triggering integrity checks for {piecesToNotify.Count} former neighbors.");
            foreach (var neighborPiece in piecesToNotify)
            {
                if (neighborPiece != null && neighborPiece.Entity != null && neighborPiece.Entity.Scene != null)
                {
                    // Ensure StructureIntegrityManager.Instance is available.
                    // This might not be the case if the scene is shutting down or manager not set up.
                    if (Building.StructureIntegrityManager.Instance != null)
                    {
                        Log.Info($"Triggering integrity check for neighbor {neighborPiece.Entity.Name} after {this.Entity?.Name ?? "OriginalPiece"} was destroyed.");
                        Building.StructureIntegrityManager.Instance.UpdateAnchorStatusForStructure(neighborPiece);
                    }
                    else
                    {
                        Log.Warning($"StructureIntegrityManager.Instance is null. Cannot trigger integrity check for neighbor {neighborPiece.Entity.Name}.");
                    }
                }
                else
                {
                    Log.Info($"Neighbor {neighborPiece?.Entity?.Name ?? "Unknown"} no longer valid or in scene, skipping integrity check for it.");
                }
            }
            // Future: This is where structural integrity checks would begin if this piece was a support.
            // The logic above now handles this by re-checking neighbors.

            // --- Destruction Effects ---

            // Particle Effects
            if (DestructionParticlePrefab != null)
            {
                var particleEntity = DestructionParticlePrefab.Entity.Clone();
                particleEntity.Transform.Position = this.Entity.Transform.WorldMatrix.TranslationVector;
                Entity.Scene.Entities.Add(particleEntity);
                // Assuming the particle system is set to play on creation or has an explicit play component.
                // If it needs to be started manually:
                // var particleComponent = particleEntity.Get<ParticleSystemComponent>();
                // particleComponent?.ParticleSystem?.Play(); // This depends on how Stride handles manually starting cloned particle systems
            }
            else
            {
                Log.Warning($"DestructionParticlePrefab is not set for {this.Entity?.Name ?? GetType().Name}. No particle effect will play.");
            }

            // Material-Specific Sounds
            var audioEmitter = Entity.Get<AudioEmitterComponent>();
            if (audioEmitter == null)
            {
                // Ensure there's an AudioEmitterComponent. If not, add one.
                // This is often better handled by ensuring the prefab has one.
                Log.Warning($"No AudioEmitterComponent found on {this.Entity?.Name ?? GetType().Name}. Adding one for destruction sound. Consider adding it to the prefab.");
                audioEmitter = new AudioEmitterComponent();
                Entity.Add(audioEmitter);
            }
            
            Sound soundToPlay = null;
            switch (StructureMaterialType)
            {
                case MaterialType.Wood:
                    soundToPlay = WoodDestroySound;
                    if (soundToPlay == null) Log.Warning($"WoodDestroySound not set for {this.Entity?.Name ?? GetType().Name}.");
                    break;
                case MaterialType.Stone:
                    soundToPlay = StoneDestroySound;
                    if (soundToPlay == null) Log.Warning($"StoneDestroySound not set for {this.Entity?.Name ?? GetType().Name}.");
                    break;
                case MaterialType.Metal:
                    soundToPlay = MetalDestroySound;
                    if (soundToPlay == null) Log.Warning($"MetalDestroySound not set for {this.Entity?.Name ?? GetType().Name}.");
                    break;
                default:
                    Log.Warning($"No specific destruction sound defined for material type {StructureMaterialType} on {this.Entity?.Name ?? GetType().Name}.");
                    break;
            }

            if (soundToPlay != null && audioEmitter != null)
            {
                audioEmitter.Play(soundToPlay); // Play the Sound object directly
            }


            // Physics/Crumbling Effect
            // Placeholder for swapping to a pre-fractured model:
            // if (HasPreFracturedModel) { SpawnPreFracturedModel(); Entity.Scene = null; return; }

            var modelComponents = new List<ModelComponent>();
            Entity.GetAll(modelComponents); // Gets all ModelComponents from this entity and its children

            if (modelComponents.Count > 0)
            {
                Log.Info($"Found {modelComponents.Count} ModelComponents for crumbling effect on {Entity.Name}.");
                foreach (var modelComp in modelComponents)
                {
                    var partEntity = modelComp.Entity; // The entity that owns this ModelComponent

                    // Ensure the partEntity is not the root entity itself if we are just disabling/removing the root later
                    // Or, handle it such that these parts become independent.
                    // For simplicity, we'll assume child entities for parts.
                    // If the ModelComponent is on the root entity, this logic might need adjustment
                    // depending on whether the root entity itself should become a physics object or just its children.

                    if (partEntity != this.Entity) // Apply physics to children, not the main entity that will be removed
                    {
                        var rigidBody = partEntity.Get<RigidbodyComponent>();
                        if (rigidBody == null)
                        {
                            rigidBody = new RigidbodyComponent();
                            partEntity.Add(rigidBody);
                            Log.Info($"Added RigidbodyComponent to {partEntity.Name}.");
                        }
                        rigidBody.IsKinematic = false; // Make it dynamic

                        // Ensure the RigidbodyComponent has a collider shape
                        if (rigidBody.ColliderShapes.Count == 0)
                        {
                            var existingColliderComponent = partEntity.Get<ColliderComponent>(); // Could be StaticColliderComponent or other
                            if (existingColliderComponent != null && existingColliderComponent.ColliderShapes.Count > 0)
                            {
                                Log.Info($"Copying shapes from existing ColliderComponent on {partEntity.Name} to RigidbodyComponent.");
                                foreach(var shape in existingColliderComponent.ColliderShapes)
                                {
                                    rigidBody.ColliderShapes.Add(shape); // This might share shape instances, which is usually fine
                                }
                                // If the existing collider was static, and we now have a dynamic rigidbody,
                                // we might want to disable or remove the original StaticColliderComponent to avoid issues.
                                if (existingColliderComponent is StaticColliderComponent)
                                {
                                    Log.Info($"Disabling StaticColliderComponent on {partEntity.Name} as dynamic RigidbodyComponent is now primary.");
                                    existingColliderComponent.Enabled = false;
                                }
                            }
                            else if (modelComp.Model != null)
                            {
                                // Create a new box collider shape from the model's bounding box
                                var boundingBox = modelComp.Model.BoundingBox;
                                var shapeDesc = new BoxColliderShapeDesc
                                {
                                    Size = boundingBox.Extent * 2, // Extent is half-size
                                    // LocalOffset can be used if the model's origin is not its center
                                };
                                var colliderShape = new BoxColliderShape(shapeDesc);
                                rigidBody.ColliderShapes.Add(colliderShape);
                                Log.Info($"Added new BoxColliderShape to RigidbodyComponent on {partEntity.Name} based on ModelComponent's BoundingBox.");
                            }
                            else
                            {
                                Log.Warning($"Cannot automatically create a collider shape for RigidbodyComponent on {partEntity.Name} as Model or Model.BoundingBox is null, and no existing ColliderComponent with shapes found.");
                            }
                        }
                        else
                        {
                            Log.Info($"RigidbodyComponent on {partEntity.Name} already has {rigidBody.ColliderShapes.Count} shapes.");
                        }

                        // Apply a small outward force
                        // Calculate direction from main entity center to part center
                        var direction = partEntity.Transform.WorldMatrix.TranslationVector - this.Entity.Transform.WorldMatrix.TranslationVector;
                        if (direction.LengthSquared() < 0.001f) // If part is at the same position, pick a random direction
                        {
                            direction = new Vector3(Stride.Core.Mathematics.RandomUtil.NextFloat(-1,1), Stride.Core.Mathematics.RandomUtil.NextFloat(0,1), Stride.Core.Mathematics.RandomUtil.NextFloat(-1,1) );
                        }
                        direction.Normalize();
                        rigidBody.ApplyImpulse(direction * (Stride.Core.Mathematics.RandomUtil.NextFloat(0.5f,2.0f)) + new Vector3(0, Stride.Core.Mathematics.RandomUtil.NextFloat(1.0f,3.0f),0)); // Apply some upward and outward force
                        rigidBody.ApplyTorqueImpulse(new Vector3(Stride.Core.Mathematics.RandomUtil.NextFloat(-5,5),Stride.Core.Mathematics.RandomUtil.NextFloat(-5,5),Stride.Core.Mathematics.RandomUtil.NextFloat(-5,5)));
                    
                        // Detach from parent so it can fly freely
                        partEntity.Transform.Parent = null; 
                        // Optionally, schedule for removal after some time
                        // partEntity.RunDelayed(5.0f, () => partEntity.Scene = null);
                    }
                }
            } else {
                 Log.Info($"No ModelComponents found for crumbling effect on {Entity.Name}.");
            }


            // Original entity removal - This should happen after a delay if children are now physics objects
            // or if the children are detached and the main entity is just a controller.
            // For now, let's assume the main entity itself is mostly a container/script host
            // and its visual/physical parts are children.
            // If the main entity had its own ModelComponent, it should be handled above too.

            // We disable the main entity's components rather than removing it immediately,
            // to allow sounds to play and physics parts to be properly detached and simulated.
            Log.Info($"Disabling components of {this.Entity?.Name ?? GetType().Name} after destruction effects initiated.");
            this.Enabled = false; // Disable this script
            foreach(var component in this.Entity.Components)
            {
                if(component is ScriptComponent || component is ModelComponent || component is ColliderComponentBase) // ColliderComponentBase for Stride 4.1+
                {
                    ((ActivableEntityComponent)component).Enabled = false;
                }
            }
            // Or, if all parts are detached and made independent, we can remove the main entity:
            // this.Entity.Scene = null; 
            // However, sounds might be cut off. A better approach for sound is to play it from a new temporary entity
            // or ensure the audio emitter is on a part that persists for a bit.
            // For now, the audio emitter is on this entity, so disabling components is safer.
            // Consider removing the entity after a delay:
            // Entity.RunDelayed(5.0f, () => { if (Entity != null) Entity.Scene = null; });


        }

        public void Debug_ForceDestroy()
        {
            Log.Info($"Debug_ForceDestroy called on {this.Entity?.Name ?? GetType().Name}.");
            // Simulate health depletion or set a flag
            // For simplicity, we'll just call OnPieceDestroyed.
            // In a full system, setting Health = 0 would ideally trigger TakeDamage -> OnDestroyed.
            
            OnPieceDestroyed(); // Trigger destruction effects and notify connections

            // The removal/disabling is now handled within OnPieceDestroyed
            // if (this.Entity != null && this.Entity.Scene != null)
            // {
            //    Log.Info($"Removing {this.Entity.Name} from scene due to Debug_ForceDestroy.");
            //    this.Entity.Scene = null; 
            // }
            // Or disable components:
            // this.Enabled = false;
            // this.Entity.Get<ModelComponent>()?.Enabled = false;
            // this.Entity.Get<ColliderComponent>()?.Enabled = false; // Use ColliderComponentBase for Stride 4.1+
        }

        // Future common methods for all building pieces:
        // public virtual void TakeDamage(float amount, Entity attacker) { /* ... Health -= amount; if (Health <=0) OnPieceDestroyed(); ... */ }
        // public virtual void Repair(float amount) { /* ... */ }
    }
}
