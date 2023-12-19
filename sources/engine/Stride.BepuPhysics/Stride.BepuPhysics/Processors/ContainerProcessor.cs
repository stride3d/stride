using System.ComponentModel;
using System.Diagnostics;
using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Components.Colliders;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Extensions;
using Stride.Core;
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
    public class ContainerProcessor : EntityProcessor<ContainerComponent>
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

        //protected override ContainerData GenerateComponentData(Entity entity, ContainerComponent component)
        //{
        //    if (_game == null)
        //        throw new NullReferenceException(nameof(_game));

        //    return new(component, _bepuConfiguration, _game);
        //}

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] ContainerComponent component, [NotNull] ContainerComponent data)
        {
            if (_game == null)
                throw new NullReferenceException(nameof(_game));

            component.ContainerData = new(component, _bepuConfiguration, _game);
            component.ContainerData.RebuildContainer();
            component.Services = Services;
            var parent = GetComponentsInParents<ContainerComponent>(entity).FirstOrDefault();
            if (parent != null)
            {
                parent.ContainerData?.RebuildContainer();
            }
        }
        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] ContainerComponent component, [NotNull] ContainerComponent data)
        {
            component.ContainerData?.DestroyContainer();
            component.ContainerData = null;

            var parent = GetComponentsInParents<ContainerComponent>(entity).FirstOrDefault();
            if (parent != null)
            {
                parent.ContainerData?.RebuildContainer();
            }
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

                var totalTimeStepInMillisec = dt * bepuSim.TimeWarp; //Calculate the theoretical time step of the simulation
                bepuSim.RemainingUpdateTime += totalTimeStepInMillisec; //Add it to the counter

                int stepCount = 0;
                while (bepuSim.RemainingUpdateTime >= bepuSim.SimulationFixedStep & stepCount < bepuSim.MaxStepPerFrame)
                {
                    var simTimeStepInSec = bepuSim.SimulationFixedStep / 1000f;
                    bepuSim.CallSimulationUpdate(simTimeStepInSec);//cal the SimulationUpdate with the real step time of the sim in secs
                    bepuSim.Simulation.Timestep(simTimeStepInSec, bepuSim.ThreadDispatcher); //perform physic simulation using bepuSim.SimulationFixedStep
                    bepuSim.ContactEvents.Flush(); //Fire events handlers stuffs.
                    bepuSim.RemainingUpdateTime -= bepuSim.SimulationFixedStep; //in millisec
                    stepCount++;
                    bepuSim.CallAfterSimulationUpdate(simTimeStepInSec);//cal the AfterSimulationUpdate with the real step time of the sim in secs
                }



#warning I don't think this should be user-controllable ? We don't provide control over the other parts of the engine when they run through the dispatcher and having it on or of doesn't (or rather shouldn't) actually change the result, just how fast it resolves
                // I guess it could make sense when running on a low power device, but at that point might as well make the change to Dispatcher itself
                //Nicogo : a performance test on a smallScene would be nice to be sure
                if (bepuSim.ParallelUpdate)
                {
                    Dispatcher.For(0, bepuSim.Simulation.Bodies.ActiveSet.Count, (i) => UpdateBodiesPositionFunction(bepuSim.Simulation.Bodies.ActiveSet.IndexToHandle[i], bepuSim));
                }
                else
                {
                    for (int i = 0; i < bepuSim.Simulation.Bodies.ActiveSet.Count; i++)
                    {
                        UpdateBodiesPositionFunction(bepuSim.Simulation.Bodies.ActiveSet.IndexToHandle[i], bepuSim);
                    }
                }
            }
        }
        private static void UpdateBodiesPositionFunction(BodyHandle handle, BepuSimulation bepuSim)
        {
            var bodyContainer = bepuSim.BodiesContainers[handle];
            var body = bepuSim.Simulation.Bodies[handle];

            var parentEntityPosition = new Vector3();
            var parentEntityRotation = Quaternion.Identity;
            var parent = bodyContainer.Entity.Transform.Parent;
            if (parent != null)
            {
                parent.WorldMatrix.Decompose(out Vector3 _, out parentEntityRotation, out parentEntityPosition);
            }

            var entityTransform = bodyContainer.Entity.Transform;

            Vector3 localPosition = (body.Pose.Position.ToStrideVector() - bodyContainer.CenterOfMass - parentEntityPosition);
            entityTransform.Position = Vector3.Transform(localPosition, Quaternion.Invert(parentEntityRotation));
            entityTransform.Rotation = body.Pose.Orientation.ToStrideQuaternion() * Quaternion.Invert(parentEntityRotation);

            if (bodyContainer.ChildsContainerComponent.Count > 0)
            {
                entityTransform.UpdateWorldMatrix(); //Warning this may cause threading-race issues (but i did large tests and never had issues)
                foreach (var item in bodyContainer.ChildsContainerComponent)
                {
                    //We need to call 
                    if (item.ContainerData != null && !item.ContainerData.IsStatic)
                        UpdateBodiesPositionFunction(item.ContainerData.BHandle, bepuSim);
                }
            }

        }

        private static IEnumerable<Entity> GetParents(Entity entity, bool includeMyself = false)
        {
            if (includeMyself)
                yield return entity;

            var parent = entity.GetParent();
            while (parent != null)
            {
                yield return parent;
                parent = parent.GetParent(); //Here
            }
        }
        private static IEnumerable<T> GetComponentsInParents<T>(Entity entity, bool includeMyself = false) where T : EntityComponent
        {
            foreach (var parent in GetParents(entity, includeMyself))
            {
                if (parent.Get<T>() is T component)
                    yield return component;
            }
        }

    }

    public class ContainerData
	{
        [DataMemberIgnore]
		public TypedIndex ShapeIndex { get; private set; }

		private readonly IGame _game;
        private readonly ContainerComponent _containerComponent;
        private BodyInertia _shapeInertia;
        private bool _isStatic;
        private bool _exist;

        private BepuConfiguration _config;

        internal BepuSimulation BepuSimulation => _config.BepuSimulations[_containerComponent.SimulationIndex];

        internal BodyHandle BHandle { get; set; } = new(-1);
        internal StaticHandle SHandle { get; set; } = new(-1);

        internal bool Exist => _exist;
        internal bool IsStatic => _isStatic;

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
                BepuSimulation.Simulation.Shapes.Remove(ShapeIndex);
                ShapeIndex = default;
            }

            _containerComponent.Entity.Transform.UpdateWorldMatrix();
            _containerComponent.Entity.Transform.WorldMatrix.Decompose(out Vector3 containerWorldScale, out Quaternion containerWorldRotation, out Vector3 containerWorldTranslation);

            var colliders = new List<ColliderComponent>();
            CollectComponentsInHierarchy<ColliderComponent, List<ColliderComponent>>(_containerComponent.Entity, _containerComponent, colliders);

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

                ShapeIndex = BepuSimulation.Simulation.Shapes.Add(mesh);
                _shapeInertia = meshContainer.Closed ? mesh.ComputeClosedInertia(meshContainer.Mass) : mesh.ComputeOpenInertia(meshContainer.Mass);

#warning check why it is not needed
                //Looks like it is not needed for both, static was working because it doesn't update stride position. By default both bepu & stride handle mesh position by 'centerOfMass'.
                //if (_containerComponent is BodyMeshContainerComponent _b)
                //{
                //    _containerComponent.CenterOfMass = (_b.Closed ? mesh.ComputeClosedCenterOfMass() : mesh.ComputeOpenCenterOfMass()).ToStrideVector();
                //}
                //else if (_containerComponent is StaticMeshContainerComponent _s)
                //{
                //    _containerComponent.CenterOfMass = (_s.Closed ? mesh.ComputeClosedCenterOfMass() : mesh.ComputeOpenCenterOfMass()).ToStrideVector();
                //}
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

                        var localTra = Vector3.Transform(colliderWorldTranslation - containerWorldTranslation, Quaternion.Invert(colliderWorldRotation));
                        var localRot = Quaternion.Invert(containerWorldRotation) * colliderWorldRotation;
                        var localPose = new RigidPose(localTra.ToNumericVector(), localRot.ToNumericQuaternion());

                        collider.AddToCompoundBuilder(_game, ref compoundBuilder, localPose);
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

            var ContainerPose = new RigidPose(containerWorldTranslation.ToNumericVector(), containerWorldRotation.ToNumericQuaternion());
            switch (_containerComponent)
            {
                case BodyContainerComponent _c:
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
                case StaticContainerComponent _c:
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
                BepuSimulation.Simulation.Shapes.Remove(ShapeIndex);
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

        static void CollectComponentsInHierarchy<T, T2>(Entity entity, ContainerComponent entityContainer, T2 collection) where T : EntityComponent where T2 : ICollection<T>
        {
            entityContainer.ChildsContainerComponent.Clear();
            var stack = new Stack<Entity>();
            stack.Push(entity);
            do
            {
                var descendant = stack.Pop();
                foreach (var child in descendant.Transform.Children)
                    stack.Push(child.Entity);

                if (entity != descendant) //if a child entity that is not the main Entity has a container, we don't Add it's colliders and we register it as a child.
                {
                    var childContainerComponent = descendant.Get<ContainerComponent>();
                    if (childContainerComponent != null)
                    {
                        entityContainer.ChildsContainerComponent.Add(childContainerComponent);
                        continue;
                    }
                }

                foreach (var component in descendant.GetAll<T>())
                {
                    collection.Add(component);
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
