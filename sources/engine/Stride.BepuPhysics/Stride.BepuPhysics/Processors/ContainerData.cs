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
        private readonly ContainerComponent _containerComponent;
        private readonly BepuConfiguration _config;
        private readonly IGame _game;

        private BodyInertia _shapeInertia;
        private bool _isStatic;
        private bool _exist;

        internal BepuSimulation BepuSimulation => _config.BepuSimulations[_containerComponent.SimulationIndex];

        internal BodyHandle BHandle { get; private set; } = new(-1);
        internal StaticHandle SHandle { get; private set; } = new(-1);
        internal TypedIndex ShapeIndex { get; private set; }// = new(-1, -1);
        internal ContainerComponent? Parent { get; set; }

        internal bool Exist => _exist;
        internal bool IsStatic => _isStatic;

        internal ContainerData(ContainerComponent containerComponent, BepuConfiguration config, IGame game, ContainerComponent? parent)
        {
            _containerComponent = containerComponent;
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

            var isTrigger = _containerComponent is TriggerContainerComponent;
            MaterialProperties mat;

            if (_isStatic)
                mat = BepuSimulation.CollidableMaterials[SHandle];
            else
                mat = BepuSimulation.CollidableMaterials[BHandle];

            mat.SpringSettings = new(_containerComponent.SpringFrequency, _containerComponent.SpringDampingRatio);
            mat.FrictionCoefficient = _containerComponent.FrictionCoefficient;
            mat.MaximumRecoveryVelocity = _containerComponent.MaximumRecoveryVelocity;
            mat.IsTrigger = isTrigger;

            mat.ColliderGroupMask = _containerComponent.ColliderGroupMask;
            mat.FilterByDistanceId = _containerComponent.ColliderFilterByDistanceId;
            mat.FilterByDistanceX = _containerComponent.ColliderFilterByDistanceX;
            mat.FilterByDistanceY = _containerComponent.ColliderFilterByDistanceY;
            mat.FilterByDistanceZ = _containerComponent.ColliderFilterByDistanceZ;

            mat.IgnoreGlobalGravity = _containerComponent.IgnoreGlobalGravity;

            if (_isStatic)
                BepuSimulation.CollidableMaterials[SHandle] = mat;
            else
                BepuSimulation.CollidableMaterials[BHandle] = mat;
        }
        internal void RebuildContainer()
        {
            if (ShapeIndex.Exists)
            {
                BepuSimulation.Simulation.Shapes.RemoveAndDispose(ShapeIndex, BepuSimulation.BufferPool);
                ShapeIndex = default;
            }

            _containerComponent.Entity.Transform.UpdateWorldMatrix();
            _containerComponent.Entity.Transform.WorldMatrix.Decompose(out Vector3 containerWorldScale, out Quaternion containerWorldRotation, out Vector3 containerWorldTranslation);

            if (_containerComponent is IContainerWithMesh meshContainer)
            {
                if (meshContainer.Model == null)
                {
                    DestroyContainer();
                    return;
                }

#warning maybe recycle mesh shapes themselves if possible ?
                var triangles = ExtractMeshDataSlow(meshContainer.Model, _game, BepuSimulation.BufferPool);
                var mesh = new Mesh(triangles, _containerComponent.Entity.Transform.Scale.ToNumericVector(), BepuSimulation.BufferPool);

                ShapeIndex = BepuSimulation.Simulation.Shapes.Add(mesh);
                _shapeInertia = meshContainer.Closed ? mesh.ComputeClosedInertia(meshContainer.Mass) : mesh.ComputeOpenInertia(meshContainer.Mass);

                //if (_containerComponent is BodyMeshContainerComponent _b)
                //{
                //    _containerComponent.CenterOfMass = (_b.Closed ? mesh.ComputeClosedCenterOfMass() : mesh.ComputeOpenCenterOfMass()).ToStrideVector();
                //}
                //else if (_containerComponent is StaticMeshContainerComponent _s)
                //{
                //    _containerComponent.CenterOfMass = (_s.Closed ? mesh.ComputeClosedCenterOfMass() : mesh.ComputeOpenCenterOfMass()).ToStrideVector();
                //}
            }
            else if (_containerComponent is IContainerWithColliders collidersContainer)
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
                        collider.Container = _containerComponent;
                    }

                    BepuUtilities.Memory.Buffer<CompoundChild> compoundChildren;
                    BodyInertia shapeInertia;
                    System.Numerics.Vector3 shapeCenter;
                    compoundBuilder.BuildDynamicCompound(out compoundChildren, out shapeInertia, out shapeCenter);

                    ShapeIndex = BepuSimulation.Simulation.Shapes.Add(new Compound(compoundChildren));
                    _shapeInertia = shapeInertia;
                    _containerComponent.CenterOfMass = shapeCenter.ToStrideVector();
                }
                finally
                {
                    compoundBuilder.Dispose();
                }
            }

            var ContainerPose = new RigidPose((containerWorldTranslation + _containerComponent.CenterOfMass).ToNumericVector(), containerWorldRotation.ToNumericQuaternion());
            switch (_containerComponent)
            {
                case IBodyContainer _c:
                    _isStatic = false;
                    if (_c.Kinematic)
                    {
                        _shapeInertia = new BodyInertia();
                    }

                    var bDescription = BodyDescription.CreateDynamic(ContainerPose, _shapeInertia, ShapeIndex, new(_c.SleepThreshold, _c.MinimumTimestepCountUnderThreshold));

                    if (BHandle.Value != -1)
                    {
                        BepuSimulation.Simulation.Bodies[BHandle].GetDescription(out var tmpDesc);
                        bDescription.Velocity = tmpDesc.Velocity; //Keep velocity when updating
                        BepuSimulation.Simulation.Bodies.ApplyDescription(BHandle, bDescription);
                    }
                    else
                    {
                        BHandle = BepuSimulation.Simulation.Bodies.Add(bDescription);
                        BepuSimulation.BodiesContainers.Add(BHandle, _c);
                        BepuSimulation.CollidableMaterials.Allocate(BHandle) = new();
                        _exist = true;
                    }

                    break;
                case IStaticContainer _c:
                    _isStatic = true;

                    var sDescription = new StaticDescription(ContainerPose, ShapeIndex);
                    var isTrigger = _c is TriggerContainerComponent;

                    if (SHandle.Value != -1)
                    {
                        BepuSimulation.Simulation.Statics.ApplyDescription(SHandle, sDescription);
                    }
                    else
                    {
                        SHandle = BepuSimulation.Simulation.Statics.Add(sDescription);
                        BepuSimulation.StaticsContainers.Add(SHandle, _c);
                        BepuSimulation.CollidableMaterials.Allocate(SHandle) = new();
                        _exist = true;
                    }

                    break;
                default:
                    break;
            }

            if (_containerComponent.ContactEventHandler != null && !IsRegistered())
                RegisterContact();

            UpdateMaterialProperties();
        }
        internal void DestroyContainer()
        {
            _containerComponent.CenterOfMass = new();

            if (IsRegistered())
            {
                UnregisterContact();
            }

            if (ShapeIndex.Exists)
            {
                BepuSimulation.Simulation.Shapes.RemoveAndDispose(ShapeIndex, BepuSimulation.BufferPool);
                ShapeIndex = default;
            }

            if (BHandle.Value != -1 && BepuSimulation.Simulation.Bodies.BodyExists(BHandle))
            {
                BepuSimulation.Simulation.Bodies.Remove(BHandle);
                BepuSimulation.BodiesContainers.Remove(BHandle);
                BHandle = new(-1);
                _exist = false;
            }

            if (SHandle.Value != -1 && BepuSimulation.Simulation.Statics.StaticExists(SHandle))
            {
                BepuSimulation.Simulation.Statics.Remove(SHandle);
                BepuSimulation.StaticsContainers.Remove(SHandle);
                SHandle = new(-1);
                _exist = false;
            }
        }

        internal void RegisterContact()
        {
            if (_exist == false || _containerComponent.ContactEventHandler == null)
                return;

            if (_isStatic)
                BepuSimulation.ContactEvents.Register(SHandle, _containerComponent.ContactEventHandler);
            else
                BepuSimulation.ContactEvents.Register(BHandle, _containerComponent.ContactEventHandler);
        }
        internal void UnregisterContact()
        {
            if (_exist == false)
                return;

            if (_isStatic)
                BepuSimulation.ContactEvents.Unregister(SHandle);
            else
                BepuSimulation.ContactEvents.Unregister(BHandle);
        }
        internal bool IsRegistered()
        {
            if (_exist == false)
                return false;

            if (_isStatic)
                return BepuSimulation.ContactEvents.IsListener(SHandle);
            else
                return BepuSimulation.ContactEvents.IsListener(BHandle);
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
