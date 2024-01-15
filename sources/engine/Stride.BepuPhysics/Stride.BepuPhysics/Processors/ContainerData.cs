using System.Diagnostics;
using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Components.Containers.Interfaces;
using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Extensions;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using BufferPool = BepuUtilities.Memory.BufferPool;
using Mesh = BepuPhysics.Collidables.Mesh;

namespace Stride.BepuPhysics.Processors
{
    internal class ContainerData
    {
        private readonly BepuConfiguration _config;
        private readonly IGame _game;

        private bool _exist;

        #warning we should be able to get rid of that second condition in the summary by moving their container to a waiting queue and setting their ContainerData to null
        /// <summary>
        /// Can be null when the container is not of type <see cref="IBodyContainer"/> or when the body does not have any colliders
        /// </summary>
        internal BodyReference? BodyReference { get; private set; } = null;
        /// <summary>
        /// Can be null when the container is not of type <see cref="IStaticContainer"/> or when the body does not have any colliders
        /// </summary>
        internal StaticReference? StaticReference { get; private set; } = null;

        internal BepuSimulation BepuSimulation => _config.BepuSimulations[ContainerComponent.SimulationIndex];

        internal TypedIndex ShapeIndex { get; private set; }// = new(-1, -1);
        internal ContainerData? Parent { get; set; }
        internal ContainerComponent ContainerComponent { get; set; }

        internal ContainerData(ContainerComponent containerComponent, BepuConfiguration config, IGame game, ContainerData? parent)
        {
            ContainerComponent = containerComponent;
            _config = config;
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
            ref var mat = ref StaticReference is {} sRef ? ref BepuSimulation.CollidableMaterials[sRef.Handle] : ref BepuSimulation.CollidableMaterials[BodyReference!.Value];

            mat.SpringSettings = new(ContainerComponent.SpringFrequency, ContainerComponent.SpringDampingRatio);
            mat.FrictionCoefficient = ContainerComponent.FrictionCoefficient;
            mat.MaximumRecoveryVelocity = ContainerComponent.MaximumRecoveryVelocity;
            mat.IsTrigger = isTrigger;

            mat.ColliderGroupMask = ContainerComponent.ColliderGroupMask;
            mat.FilterByDistanceId = ContainerComponent.ColliderFilterByDistanceId;
            mat.FilterByDistanceX = ContainerComponent.ColliderFilterByDistanceX;
            mat.FilterByDistanceY = ContainerComponent.ColliderFilterByDistanceY;
            mat.FilterByDistanceZ = ContainerComponent.ColliderFilterByDistanceZ;

            mat.IgnoreGlobalGravity = ContainerComponent.IgnoreGlobalGravity;
        }

        internal void RebuildContainer()
        {
            if (ShapeIndex.Exists)
            {
                BepuSimulation.Simulation.Shapes.RemoveAndDispose(ShapeIndex, BepuSimulation.BufferPool);
                ShapeIndex = default;
            }

            ContainerComponent.Entity.Transform.UpdateWorldMatrix();
            ContainerComponent.Entity.Transform.WorldMatrix.Decompose(out Vector3 containerWorldScale, out Quaternion containerWorldRotation, out Vector3 containerWorldTranslation);

            BodyInertia shapeInertia;
            if (ContainerComponent is IContainerWithMesh meshContainer)
            {
                if (meshContainer.Model == null)
                {
                    DestroyContainer();
                    return;
                }

#warning maybe recycle mesh shapes themselves if possible ?
                var triangles = ExtractMeshDataSlow(meshContainer.Model, _game, BepuSimulation.BufferPool);
                var mesh = new Mesh(triangles, ContainerComponent.Entity.Transform.Scale.ToNumericVector(), BepuSimulation.BufferPool);

                ShapeIndex = BepuSimulation.Simulation.Shapes.Add(mesh);
                shapeInertia = meshContainer.Closed ? mesh.ComputeClosedInertia(meshContainer.Mass) : mesh.ComputeOpenInertia(meshContainer.Mass);

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
                }

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

                if (BodyReference is {} bRef)
                {
                    bRef.GetDescription(out var tmpDesc);
                    bDescription.Velocity = tmpDesc.Velocity; //Keep velocity when updating
                    BepuSimulation.Simulation.Bodies.ApplyDescription(bRef.Handle, bDescription);
                }
                else
                {
                    var bHandle = BepuSimulation.Simulation.Bodies.Add(bDescription);
                    BodyReference = BepuSimulation.Simulation.Bodies[bHandle];
                    BodyReference.Value.Collidable.Continuity = body.ContinuousDetection;
                    if (BepuSimulation.Bodies.Count == bHandle.Value)
                    {
                        BepuSimulation.Bodies.Add(body);
                    }
                    else
                    {
                        BepuSimulation.Bodies[bHandle.Value] = body;
                    }
                    BepuSimulation.CollidableMaterials.Allocate(bHandle) = new();
                }
            }
            else if (ContainerComponent is IStaticContainer @static)
            {
                var sDescription = new StaticDescription(containerPose, ShapeIndex);

                if (StaticReference is {} sRef)
                {
                    BepuSimulation.Simulation.Statics.ApplyDescription(sRef.Handle, sDescription);
                }
                else
                {
                    var sHandle = BepuSimulation.Simulation.Statics.Add(sDescription);
                    StaticReference = BepuSimulation.Simulation.Statics[sHandle];
                    if (BepuSimulation.Statics.Count == sHandle.Value)
                    {
                        BepuSimulation.Statics.Add(@static);
                    }
                    else
                    {
                        BepuSimulation.Statics[sHandle.Value] = @static;
                    }
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
        }

        internal void DestroyContainer()
        {
            ContainerComponent.CenterOfMass = new();

            if (IsRegistered())
            {
                UnregisterContact();
            }

            if (ShapeIndex.Exists)
            {
                BepuSimulation.Simulation.Shapes.RemoveAndDispose(ShapeIndex, BepuSimulation.BufferPool);
                ShapeIndex = default;
            }

            if (BodyReference is {} bRef)
            {
                BepuSimulation.Simulation.Bodies.Remove(bRef.Handle);
                BepuSimulation.Bodies[bRef.Handle.Value] = null;
                _exist = false;
                BodyReference = null;
            }

            if (StaticReference is {} sRef)
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

            if (StaticReference is {} sRef)
                BepuSimulation.ContactEvents.Register(sRef.Handle, ContainerComponent.ContactEventHandler);
            else if (BodyReference is {} bRef)
                BepuSimulation.ContactEvents.Register(bRef.Handle, ContainerComponent.ContactEventHandler);
        }
        internal void UnregisterContact()
        {
            if (StaticReference is {} sRef)
                BepuSimulation.ContactEvents.Unregister(sRef.Handle);
            else if (BodyReference is {} bRef)
                BepuSimulation.ContactEvents.Unregister(bRef.Handle);
        }
        internal bool IsRegistered()
        {
            if (StaticReference is {} sRef)
                return BepuSimulation.ContactEvents.IsListener(sRef.Handle);
            else if (BodyReference is {} bRef)
                return BepuSimulation.ContactEvents.IsListener(bRef.Handle);
            return false;
        }

        private static unsafe BepuUtilities.Memory.Buffer<Triangle> ExtractMeshDataSlow(Model model, IGame game, BufferPool pool)
        {
            int totalIndices = 0;
            foreach (var meshData in model.Meshes)
            {
                totalIndices += meshData.Draw.IndexBuffer.Count;
            }

            pool.Take<Triangle>(totalIndices / 3, out var triangles);
            var triangleAsV3 = triangles.As<Vector3>();
            int triangleV3Index = 0;

            foreach (var meshData in model.Meshes)
            {
                // Get mesh data from GPU or shared memory, this can be quite slow
                byte[] verticesBytes = meshData.Draw.VertexBuffers[0].Buffer.GetData<byte>(game.GraphicsContext.CommandList);
                byte[] indicesBytes = meshData.Draw.IndexBuffer.Buffer.GetData<byte>(game.GraphicsContext.CommandList);

                var vBindings = meshData.Draw.VertexBuffers[0];
                int vStride = vBindings.Declaration.VertexStride;
                var position = vBindings.Declaration.EnumerateWithOffsets().First(x => x.VertexElement.SemanticName == VertexElementUsage.Position);

                if (position.VertexElement.Format is PixelFormat.R32G32B32_Float or PixelFormat.R32G32B32A32_Float == false)
                    throw new ArgumentException($"{model}'s vertex position must be declared as float3 or float4");

                fixed (byte* vBuffer = &verticesBytes[vBindings.Offset])
                fixed (byte* iBuffer = indicesBytes)
                {
                    if (meshData.Draw.IndexBuffer.Is32Bit)
                    {
                        foreach (int i in new Span<int>(iBuffer + meshData.Draw.IndexBuffer.Offset, meshData.Draw.IndexBuffer.Count))
                        {
                            triangleAsV3[triangleV3Index++] = *(Vector3*)(vBuffer + vStride * i + position.Offset); // start of the buffer, move to the 'i'th vertex, and read from the position field of that vertex
                        }
                    }
                    else
                    {
                        foreach (ushort i in new Span<ushort>(iBuffer + meshData.Draw.IndexBuffer.Offset, meshData.Draw.IndexBuffer.Count))
                        {
                            triangleAsV3[triangleV3Index++] = *(Vector3*)(vBuffer + vStride * i + position.Offset);
                        }
                    }
                }
            }

            return triangles;
        }
    }
}
