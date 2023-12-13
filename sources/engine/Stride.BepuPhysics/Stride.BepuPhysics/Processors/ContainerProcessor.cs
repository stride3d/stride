using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Components.Colliders;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Extensions;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using BufferPool = BepuUtilities.Memory.BufferPool;
using Mesh = BepuPhysics.Collidables.Mesh;

namespace Stride.BepuPhysics.Processors
{
    public class ContainerProcessor : EntityProcessor<ContainerComponent, ContainerData>
    {
        private BepuConfiguration _bepuConfiguration = new();
        private IGame? _game = null;

        public ContainerProcessor()
        {
            Order = 10000;
        }

        protected override void OnSystemAdd()
        {
            var configService = Services.GetService<IGameSettingsService>();
            _bepuConfiguration = configService.Settings.Configurations.Get<BepuConfiguration>();
            _game = Services.GetService<IGame>();

            if (_bepuConfiguration.BepuSimulations.Count == 0)
            {
                _bepuConfiguration.BepuSimulations.Add(new BepuSimulation());
            }

            Services.AddService(_bepuConfiguration);
        }

        protected override ContainerData GenerateComponentData(Entity entity, ContainerComponent component)
        {
            if (_game == null)
                throw new NullReferenceException(nameof(_game));

            return new(component, _bepuConfiguration, _game);
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] ContainerComponent component, [NotNull] ContainerData data)
        {
            if (_game == null)
                throw new NullReferenceException(nameof(_game));

            component.ContainerData = data;
            data.RebuildContainer();
            if (component.ContactEventHandler != null && !component.ContainerData.IsRegistered())
                component.ContainerData.RegisterContact();
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] ContainerComponent component, [NotNull] ContainerData data)
        {
            if (data.IsRegistered())
                data.UnregisterContact();

            data.DestroyContainer();
            component.ContainerData = null;
        }

        public override void Update(GameTime time)
        {
            base.Update(time);

            var dt = (float)time.Elapsed.TotalMilliseconds;
            if (dt == 0f)
                return;

            foreach (var bepuSim in _bepuConfiguration.BepuSimulations)
            {
                if (!bepuSim.Enabled)
                    continue;

                var SimTimeStep = dt * bepuSim.TimeWarp; //Calculate the theoretical time step of the simulation
                bepuSim.RemainingUpdateTime += SimTimeStep; //Add it to the counter

                int stepCount = 0;
                while (bepuSim.RemainingUpdateTime >= bepuSim.SimulationFixedStep & stepCount < bepuSim.MaxStepPerFrame)
                {
                    bepuSim.CallSimulationUpdate(bepuSim.SimulationFixedStep / 1000f);//cal the SimulationUpdate with the real step time of the sim in secs
                    bepuSim.Simulation.Timestep(bepuSim.SimulationFixedStep / 1000f, bepuSim.ThreadDispatcher); //perform physic simulation using bepuSim.SimulationFixedStep
                    bepuSim.ContactEvents.Flush(); //Fire events handlers stuffs.
                    bepuSim.RemainingUpdateTime -= bepuSim.SimulationFixedStep;
                    stepCount++;
                }

                #warning I don't think this should be user-controllable ? We don't provide control over the other parts of the engine when they run through the dispatcher and having it on or of doesn't (or rather shouldn't) actually change the result, just how fast it resolves
                // I guess it could make sense when running on a low power device, but at that point might as well make the change to Dispatcher itself
                if (bepuSim.ParallelUpdate)
                {
                    Dispatcher.For(0, bepuSim.Simulation.Bodies.ActiveSet.Count, (i) =>
                    {
                        var handle = bepuSim.Simulation.Bodies.ActiveSet.IndexToHandle[i];
                        var bodyContainer = bepuSim.BodiesContainers[handle];
                        var body = bepuSim.Simulation.Bodies[handle];

                        var parentEntityTransform = new Vector3();
                        var parent = bodyContainer.Entity.Transform.Parent;
                        if (parent != null)
                        {
                            parent.WorldMatrix.Decompose(out Vector3 _, out Quaternion _, out parentEntityTransform);
                        }

                        var entityTransform = bodyContainer.Entity.Transform;
                        #warning fix this part, conversion from local to world is just halfway done here
                        entityTransform.Position = body.Pose.Position.ToStrideVector() - bodyContainer.CenterOfMass - parentEntityTransform;
                        entityTransform.Rotation = body.Pose.Orientation.ToStrideQuaternion();
                        #warning this operation is not thread safe if multiple containers are in the same hierarchy, perhaps best to avoid calling this method and instead let the engine update world matrix itself, maybe by moving this processor right before that process
                        entityTransform.UpdateWorldMatrix();
                    });
                }
                else
                {
                    for (int i = 0; i < bepuSim.Simulation.Bodies.ActiveSet.Count; i++)
                    {
                        var handle = bepuSim.Simulation.Bodies.ActiveSet.IndexToHandle[i];
                        var bodyContainer = bepuSim.BodiesContainers[handle];
                        var body = bepuSim.Simulation.Bodies[handle];

                        var parentEntityTransform = new Vector3();
                        var parent = bodyContainer.Entity.Transform.Parent;
                        if (parent != null)
                        {
                            parent.WorldMatrix.Decompose(out Vector3 _, out Quaternion _, out parentEntityTransform);
                        }

                        var entityTransform = bodyContainer.Entity.Transform;
                        entityTransform.Position = body.Pose.Position.ToStrideVector() - bodyContainer.CenterOfMass - parentEntityTransform;
                        entityTransform.Rotation = body.Pose.Orientation.ToStrideQuaternion();
                        entityTransform.UpdateWorldMatrix();
                    }
                }
            }
        }
    }

    public class ContainerData
    {
        private readonly IGame _game;
        private readonly ContainerComponent _containerComponent;
        private TypedIndex _shapeIndex;
        private BodyInertia _shapeInertia;
        private bool _isStatic;
        private bool _exist;

        private BepuConfiguration _config;

        internal BepuSimulation BepuSimulation => _config.BepuSimulations[_containerComponent.SimulationIndex];

        internal BodyHandle BHandle { get; set; } = new(-1);
        internal StaticHandle SHandle { get; set; } = new(-1);


        public ContainerData(ContainerComponent containerComponent, BepuConfiguration config, IGame game)
        {
            _config = config;
            _containerComponent = containerComponent;
            _game = game;
        }

        internal void TryUpdateContainer()
        {
            if (_exist)
                RebuildContainer();
        }

        internal void RebuildContainer()
        {
            if (_shapeIndex.Exists)
                BepuSimulation.Simulation.Shapes.Remove(_shapeIndex);

            _containerComponent.Entity.Transform.UpdateWorldMatrix();
            _containerComponent.Entity.Transform.WorldMatrix.Decompose(out Vector3 containerWorldScale, out Quaternion containerWorldRotation, out Vector3 containerWorldTranslation);

            var colliders = new List<ColliderComponent>();
            CollectComponentsInHierarchy<ColliderComponent, List<ColliderComponent>>(_containerComponent.Entity, colliders);

            if (_containerComponent is IMeshContainerComponent meshContainer)
            {
                if (colliders.Count > 0)
                    throw new Exception("MeshContainer cannot have compound colliders.");

                if (meshContainer.Model == null)
                {
                    DestroyContainer();
                    return;
                }

                var pool = new BufferPool();
                var triangles = ExtractMeshDataSlow(meshContainer.Model, _game, pool);
                var mesh = new Mesh(triangles, meshContainer.Entity.Transform.Scale.ToNumericVector(), pool);

                _shapeIndex = BepuSimulation.Simulation.Shapes.Add(mesh);
                _shapeInertia = meshContainer.Closed ? mesh.ComputeClosedInertia(meshContainer.Mass) : mesh.ComputeOpenInertia(meshContainer.Mass);

                if (_containerComponent is BodyMeshContainerComponent _b)
                {
                    #warning check why it is not needed
                    //ContainerComponent.CenterOfMass = (_b.Closed ? mesh.ComputeClosedCenterOfMass() : mesh.ComputeOpenCenterOfMass()).ToStrideVector();
                }
                else if (_containerComponent is StaticMeshContainerComponent _s)
                {
                    _containerComponent.CenterOfMass = (_s.Closed ? mesh.ComputeClosedCenterOfMass() : mesh.ComputeOpenCenterOfMass()).ToStrideVector();
                }
            }
            else if (colliders.Count == 0)
            {
                DestroyContainer();
                return;
            }
            else
            {
                var compoundBuilder = new CompoundBuilder(BepuSimulation.BufferPool, BepuSimulation.Simulation.Shapes, colliders.Count);
                try
                {
                    foreach (var collider in colliders)
                    {
                        collider.Entity.Transform.UpdateWorldMatrix();
                        collider.Entity.Transform.WorldMatrix.Decompose(out Vector3 colliderWorldScale, out Quaternion colliderWorldRotation, out Vector3 colliderWorldTranslation);

                        var localTra = colliderWorldTranslation - containerWorldTranslation;
                        var localRot = Quaternion.Invert(containerWorldRotation) * colliderWorldRotation;
                        var localPose = new RigidPose(localTra.ToNumericVector(), localRot.ToNumericQuaternion());

                        collider.AddToCompoundBuilder(_game, ref compoundBuilder, localPose);
                        collider.Container = _containerComponent;
                    }

                    BepuUtilities.Memory.Buffer<CompoundChild> compoundChildren;
                    BodyInertia shapeInertia;
                    System.Numerics.Vector3 shapeCenter;
                    compoundBuilder.BuildDynamicCompound(out compoundChildren, out shapeInertia, out shapeCenter);

                    _shapeIndex = BepuSimulation.Simulation.Shapes.Add(new Compound(compoundChildren));
                    _shapeInertia = shapeInertia;
                    _containerComponent.CenterOfMass = shapeCenter.ToStrideVector();
                }
                finally
                {
                    compoundBuilder.Dispose();
                }
            }

            var ContainerPose = new RigidPose(containerWorldTranslation.ToNumericVector(), containerWorldRotation.ToNumericQuaternion());
            switch (_containerComponent)
            {
                case BodyContainerComponent _c:
                    _isStatic = false;
                    if (_c.Kinematic)
                    {
                        _shapeInertia = new BodyInertia();
                    }

                    var bDescription = BodyDescription.CreateDynamic(ContainerPose, _shapeInertia, _shapeIndex, new(_c.SleepThreshold, _c.MinimumTimestepCountUnderThreshold));

                    if (BHandle.Value != -1)
                    {
                        BepuSimulation.Simulation.Bodies[BHandle].GetDescription(out var tmpDesc);
                        bDescription.Velocity = tmpDesc.Velocity; //Keep velocity when updating
                        BepuSimulation.Simulation.Bodies.ApplyDescription(BHandle, bDescription);
                        BepuSimulation.CollidableMaterials[BHandle] = new() { SpringSettings = new(_c.SpringFrequency, _c.SpringDampingRatio), FrictionCoefficient = _c.FrictionCoefficient, MaximumRecoveryVelocity = _c.MaximumRecoveryVelocity, colliderGroupMask = _c.ColliderGroupMask };
                    }
                    else
                    {
                        BHandle = BepuSimulation.Simulation.Bodies.Add(bDescription);
                        BepuSimulation.BodiesContainers.Add(BHandle, _c);
                        BepuSimulation.CollidableMaterials.Allocate(BHandle) = new() { SpringSettings = new(_c.SpringFrequency, _c.SpringDampingRatio), FrictionCoefficient = _c.FrictionCoefficient, MaximumRecoveryVelocity = _c.MaximumRecoveryVelocity, colliderGroupMask = _c.ColliderGroupMask };
                        _exist = true;
                    }

                    break;
                case StaticContainerComponent _c:
                    _isStatic = true;

                    var sDescription = new StaticDescription(ContainerPose, _shapeIndex);

                    if (SHandle.Value != -1)
                    {
                        BepuSimulation.Simulation.Statics.ApplyDescription(SHandle, sDescription);
                        BepuSimulation.CollidableMaterials[SHandle] = new() { SpringSettings = new(_c.SpringFrequency, _c.SpringDampingRatio), FrictionCoefficient = _c.FrictionCoefficient, MaximumRecoveryVelocity = _c.MaximumRecoveryVelocity, colliderGroupMask = _c.ColliderGroupMask };
                    }
                    else
                    {
                        SHandle = BepuSimulation.Simulation.Statics.Add(sDescription);
                        BepuSimulation.StaticsContainers.Add(SHandle, _c);
                        BepuSimulation.CollidableMaterials.Allocate(SHandle) = new() { SpringSettings = new(_c.SpringFrequency, _c.SpringDampingRatio), FrictionCoefficient = _c.FrictionCoefficient, MaximumRecoveryVelocity = _c.MaximumRecoveryVelocity, colliderGroupMask = _c.ColliderGroupMask };
                        _exist = true;
                    }

                    break;
                default:
                    break;
            }
        }

        internal void DestroyContainer()
        {
            _containerComponent.CenterOfMass = new();

            if (IsRegistered())
            {
                UnregisterContact();
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

            if (_shapeIndex.Exists)
            {
                BepuSimulation.Simulation.Shapes.Remove(_shapeIndex);
                _shapeIndex = default;
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

        static void CollectComponentsInHierarchy<T, T2>(Entity entity, T2 collection) where T : EntityComponent where T2 : ICollection<T>
        {
            var stack = new Stack<Entity>();
            stack.Push(entity);
            do
            {
                var descendant = stack.Pop();
                foreach (var child in descendant.Transform.Children)
                    stack.Push(child.Entity);

                foreach (var component in descendant.Components)
                {
                    if (component is T t)
                        collection.Add(t);
                }
            } while (stack.Count > 0);
        }

        static unsafe BepuUtilities.Memory.Buffer<Triangle> ExtractMeshDataSlow(Model model, IGame game, BufferPool pool)
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
