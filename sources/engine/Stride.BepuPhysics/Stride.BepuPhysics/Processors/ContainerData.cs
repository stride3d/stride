using System.Diagnostics;
using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Components.Containers.Interfaces;
using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Extensions;
using Stride.Games;
using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Processors
{
    internal class ContainerData
    {
        private readonly IGame _game;
        private bool _exist;
        private BepuShapeCacheSystem.Cache? _cache;
        private ContainerProcessor _processor;

#warning we should be able to get rid of that second condition in the summary by moving their container to a waiting queue and setting their ContainerData to null
        /// <summary>
        /// Can be null when the container is not of type <see cref="IBodyContainer"/> or when the body does not have any colliders
        /// </summary>
        internal BodyReference? BodyReference { get; private set; } = null;
        /// <summary>
        /// Can be null when the container is not of type <see cref="IStaticContainer"/> or when the body does not have any colliders
        /// </summary>
        internal StaticReference? StaticReference { get; private set; } = null;

        internal BepuSimulation BepuSimulation => _processor.BepuConfiguration.BepuSimulations[ContainerComponent.SimulationIndex];

        internal TypedIndex ShapeIndex { get; private set; }// = new(-1, -1);
        internal ContainerData? Parent { get; set; }
        internal ContainerComponent ContainerComponent { get; set; }

        internal ContainerData(ContainerComponent containerComponent, ContainerProcessor processor, IGame game, ContainerData? parent)
        {
            ContainerComponent = containerComponent;
            _processor = processor;
            _game = game;
            Parent = parent;
        }

        internal void TryUpdateContainer()
        {
            if (_exist)
                RebuildContainer();
        }

        internal void UpdateMaterialProperties()
        {
            if (!_exist)
                return;

            Debug.Assert(BodyReference.HasValue || StaticReference.HasValue);

            var isTrigger = ContainerComponent is TriggerContainerComponent;
            ref var mat = ref StaticReference is { } sRef ? ref BepuSimulation.CollidableMaterials[sRef.Handle] : ref BepuSimulation.CollidableMaterials[BodyReference!.Value];

            mat.SpringSettings = new(ContainerComponent.SpringFrequency, ContainerComponent.SpringDampingRatio);
            mat.FrictionCoefficient = ContainerComponent.FrictionCoefficient;
            mat.MaximumRecoveryVelocity = ContainerComponent.MaximumRecoveryVelocity;
            mat.IsTrigger = isTrigger;

            mat.ColliderCollisionMask = ContainerComponent.CollisionMask;
            mat.FilterByDistance = ContainerComponent.FilterByDistance;

            mat.IgnoreGlobalGravity = ContainerComponent is IBodyContainer body && body.IgnoreGlobalGravity;
        }

        internal void RebuildContainer()
        {
            if (ShapeIndex.Exists)
            {
                BepuSimulation.Simulation.Shapes.RemoveAndDispose(ShapeIndex, BepuSimulation.BufferPool);
                ShapeIndex = default;
            }

            ContainerComponent.Entity.Transform.UpdateWorldMatrix();
            var matrix = ContainerComponent.Entity.Transform.WorldMatrix;
            matrix.Decompose(out _, out Quaternion containerWorldRotation, out Vector3 containerWorldTranslation);

            BodyInertia shapeInertia;
            if (ContainerComponent is IContainerWithMesh meshContainer)
            {
                if (meshContainer.Model == null)
                {
                    DestroyContainer();
                    return;
                }

                _processor.ShapeCache.GetModelCache(meshContainer.Model, out _cache);
                var mesh = _cache.GetBepuMesh(_processor.ShapeCache.ComputeMeshScale(meshContainer));

                ShapeIndex = BepuSimulation.Simulation.Shapes.Add(mesh);
                shapeInertia = meshContainer.Closed ? mesh.ComputeClosedInertia(meshContainer.Mass) : mesh.ComputeOpenInertia(meshContainer.Mass);
                ContainerComponent.CenterOfMass = Vector3.Zero;
                //if (_containerComponent is BodyMeshContainerComponent _b)
                //{
                //    _containerComponent.CenterOfMass = (_b.Closed ? mesh.ComputeClosedCenterOfMass() : mesh.ComputeOpenCenterOfMass()).ToStrideVector();
                //}
                //else if (_containerComponent is StaticMeshContainerComponent _s)
                //{
                //    _containerComponent.CenterOfMass = (_s.Closed ? mesh.ComputeClosedCenterOfMass() : mesh.ComputeOpenCenterOfMass()).ToStrideVector();
                //}
            }
            else if (ContainerComponent is IContainerWithColliders collidersContainer)
            {
                if (collidersContainer.Colliders.Count == 0)
                {
                    DestroyContainer();
                    return;
                    //Want to allow empty colliders ? // the previous 2 lines.
                    ShapeIndex = new TypedIndex();
                    shapeInertia = new Sphere(1).ComputeInertia(meshContainer.Mass);
                    ContainerComponent.CenterOfMass = Vector3.Zero;
                }
                else
                {
                    var compoundBuilder = new CompoundBuilder(BepuSimulation.BufferPool, BepuSimulation.Simulation.Shapes, collidersContainer.Colliders.Count);
                    try
                    {
                        foreach (var collider in collidersContainer.Colliders)
                        {
                            var localTranslation = collider.PositionLocal;
                            var localRotation = collider.RotationLocal;

                            var compoundChildLocalPose = new RigidPose(localTranslation.ToNumericVector(), localRotation.ToNumericQuaternion());
                            collider.AddToCompoundBuilder(_game, BepuSimulation, ref compoundBuilder, compoundChildLocalPose);
                            collider.Container = ContainerComponent;
                        }

                        BepuUtilities.Memory.Buffer<CompoundChild> compoundChildren;
                        System.Numerics.Vector3 shapeCenter;
                        compoundBuilder.BuildDynamicCompound(out compoundChildren, out shapeInertia, out shapeCenter);

                        ShapeIndex = BepuSimulation.Simulation.Shapes.Add(new Compound(compoundChildren));
                        ContainerComponent.CenterOfMass = shapeCenter.ToStrideVector();
                    }
                    finally
                    {
                        compoundBuilder.Dispose();
                    }
                }
            }
            else
            {
                throw new Exception($"Container type '{ContainerComponent.GetType()}' doesn't have any content");
            }

            _exist = true;

            var containerPose = new RigidPose((containerWorldTranslation + ContainerComponent.CenterOfMass).ToNumericVector(), containerWorldRotation.ToNumericQuaternion());
            if (ContainerComponent is IBodyContainer body)
            {
                if (body.Kinematic)
                {
                    shapeInertia = new BodyInertia();
                }

                var bDescription = BodyDescription.CreateDynamic(containerPose, shapeInertia, ShapeIndex, new(body.SleepThreshold, body.MinimumTimestepCountUnderThreshold));

                if (BodyReference is { } bRef)
                {
                    bRef.GetDescription(out var previousDesc);
                    bDescription.Velocity = previousDesc.Velocity; //Keep velocity when updating
                    bRef.ApplyDescription(bDescription);
                }
                else
                {
                    var bHandle = BepuSimulation.Simulation.Bodies.Add(bDescription);
                    BodyReference = BepuSimulation.Simulation.Bodies[bHandle];
                    BodyReference.Value.Collidable.Continuity = body.ContinuousDetection;

                    while (BepuSimulation.Bodies.Count <= bHandle.Value) // There may be more than one add if soft physics inserted a couple of bodies
                        BepuSimulation.Bodies.Add(null);
                    BepuSimulation.Bodies[bHandle.Value] = body;

                    BepuSimulation.CollidableMaterials.Allocate(bHandle) = new();
                }
            }
            else if (ContainerComponent is IStaticContainer @static)
            {
                var sDescription = new StaticDescription(containerPose, ShapeIndex);

                if (StaticReference is { } sRef)
                {
                    sRef.ApplyDescription(sDescription);
                }
                else
                {
                    var sHandle = BepuSimulation.Simulation.Statics.Add(sDescription);
                    StaticReference = BepuSimulation.Simulation.Statics[sHandle];

                    while (BepuSimulation.Statics.Count <= sHandle.Value) // There may be more than one add if soft physics inserted a couple of bodies
                        BepuSimulation.Statics.Add(null);
                    BepuSimulation.Statics[sHandle.Value] = @static;

                    BepuSimulation.CollidableMaterials.Allocate(sHandle) = new();
                }
            }
            else
            {
                throw new Exception($"Container type '{ContainerComponent.GetType()}' is not static nor body");
            }

            if (ContainerComponent.ContactEventHandler != null && !IsRegistered())
                RegisterContact();

            UpdateMaterialProperties();

            _processor.OnPostAdd?.Invoke(ContainerComponent);
        }

        internal void DestroyContainer()
        {
            if (_exist == false)
                return;

            _processor.OnPreRemove?.Invoke(ContainerComponent);

            ContainerComponent.CenterOfMass = new();

            if (IsRegistered())
            {
                UnregisterContact();
            }

            if (ShapeIndex.Exists)
            {
                if (ContainerComponent is IContainerWithMesh)
                {
                    BepuSimulation.Simulation.Shapes.Remove(ShapeIndex);
                    _cache = null; // Let GC collect the cache
                }
                else
                {
                    BepuSimulation.Simulation.Shapes.RemoveAndDispose(ShapeIndex, BepuSimulation.BufferPool);
                }

                ShapeIndex = default;
            }

            if (BodyReference is { } bRef)
            {
                BepuSimulation.Simulation.Bodies.Remove(bRef.Handle);
                BepuSimulation.Bodies[bRef.Handle.Value] = null;
                _exist = false;
                BodyReference = null;
            }

            if (StaticReference is { } sRef)
            {
                BepuSimulation.Simulation.Statics.Remove(sRef.Handle);
                BepuSimulation.Statics[sRef.Handle.Value] = null;
                _exist = false;
                StaticReference = null;
            }
        }

        internal void RegisterContact()
        {
            if (ContainerComponent.ContactEventHandler == null)
                return;

            if (StaticReference is { } sRef)
                BepuSimulation.ContactEvents.Register(sRef.Handle, ContainerComponent.ContactEventHandler);
            else if (BodyReference is { } bRef)
                BepuSimulation.ContactEvents.Register(bRef.Handle, ContainerComponent.ContactEventHandler);
        }
        internal void UnregisterContact()
        {
            if (StaticReference is { } sRef)
                BepuSimulation.ContactEvents.Unregister(sRef.Handle);
            else if (BodyReference is { } bRef)
                BepuSimulation.ContactEvents.Unregister(bRef.Handle);
        }
        internal bool IsRegistered()
        {
            if (StaticReference is { } sRef)
                return BepuSimulation.ContactEvents.IsListener(sRef.Handle);
            else if (BodyReference is { } bRef)
                return BepuSimulation.ContactEvents.IsListener(bRef.Handle);
            return false;
        }
    }
}
