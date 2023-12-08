using System;
using System.Linq;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
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

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] ContainerComponent component, [NotNull] ContainerComponent data)
        {
            if (_game == null)
                throw new Exception("Game is null");

            component.ContainerData = new(component, _bepuConfiguration, _game);
            component.ContainerData.BuildOrUpdateContainer();
            if (component.ContactEventHandler != null && !component.IsRegistered())
                component.RegisterContact();
        }
        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] ContainerComponent component, [NotNull] ContainerComponent data)
        {
            if (component.IsRegistered())
                component.UnregisterContact();

            component.ContainerData?.DestroyContainer();
            component.ContainerData = null;
        }

        public override void Update(GameTime time)
        {
            var dt = (float)time.Elapsed.TotalMilliseconds;
            if (dt == 0f)
                return;

            //var totalWatch = new Stopwatch();
            //var simUpdWatch = new Stopwatch();
            //var simStepWatch = new Stopwatch();
            //var parForWatch = new Stopwatch();

            //totalWatch.Start();
            //Debug.WriteLine($"Start");

            foreach (var bepuSim in _bepuConfiguration.BepuSimulations)
            {
                if (!bepuSim.Enabled)
                    continue;

                var SimTimeStep = dt * bepuSim.TimeWarp; //Calculate the theoretical time step of the simulation
                bepuSim.RemainingUpdateTime += SimTimeStep; //Add it to the counter

                //Debug.WriteLine($"    SimTimeStepSinceLastFrame : {SimTimeStep}\n    realSimTimeStep : {realSimTimeStepInSec*1000}");

                //simStepWatch.Start();
                int stepCount = 0;
                while (bepuSim.RemainingUpdateTime >= bepuSim.SimulationFixedStep & stepCount < bepuSim.MaxStepPerFrame)
                {
                    bepuSim.CallSimulationUpdate(bepuSim.SimulationFixedStep / 1000f);//cal the SimulationUpdate with the real step time of the sim in secs
                    bepuSim.Simulation.Timestep(bepuSim.SimulationFixedStep / 1000f, bepuSim.ThreadDispatcher); //perform physic simulation using bepuSim.SimulationFixedStep
                    bepuSim.ContactEvents.Flush(); //Fire events handlers stuffs.
                    bepuSim.RemainingUpdateTime -= bepuSim.SimulationFixedStep;
                    stepCount++;
                }
                //simStepWatch.Stop();

                //parForWatch.Start();

                if (bepuSim.ParallelUpdate)
                {
                    Dispatcher.For(0, bepuSim.Simulation.Bodies.ActiveSet.Count, (i) =>
                    {
                        var handle = bepuSim.Simulation.Bodies.ActiveSet.IndexToHandle[i];
                        var BodyContainer = bepuSim.BodiesContainers[handle];
                        var body = bepuSim.Simulation.Bodies[handle];

                        var entityTransform = BodyContainer.Entity.Transform;
                        entityTransform.WorldMatrix.Decompose(out Vector3 _, out Quaternion _, out Vector3 ContainerWorldTranslation);
                        var ParentEntityTransform = new Vector3();
                        var parent = BodyContainer.Entity.GetParent();
                        if (parent != null)
                        {
                            parent.Transform.WorldMatrix.Decompose(out Vector3 _, out Quaternion _, out ParentEntityTransform);
                        }

                        entityTransform.Position = body.Pose.Position.ToStrideVector() - BodyContainer.CenterOfMass - ParentEntityTransform;
                        entityTransform.Rotation = body.Pose.Orientation.ToStrideQuaternion();
                        entityTransform.UpdateWorldMatrix();
                    });
                }
                else
                {
                    for (int i = 0; i < bepuSim.Simulation.Bodies.ActiveSet.Count; i++)
                    {
                        var handle = bepuSim.Simulation.Bodies.ActiveSet.IndexToHandle[i];
                        var BodyContainer = bepuSim.BodiesContainers[handle];
                        var body = bepuSim.Simulation.Bodies[handle];

                        var entityTransform = BodyContainer.Entity.Transform;
                        entityTransform.WorldMatrix.Decompose(out Vector3 _, out Quaternion _, out Vector3 ContainerWorldTranslation);
                        var ParentEntityTransform = new Vector3();
                        var parent = BodyContainer.Entity.GetParent();
                        if (parent != null)
                        {
                            parent.Transform.WorldMatrix.Decompose(out Vector3 _, out Quaternion _, out ParentEntityTransform);
                        }

                        entityTransform.Position = body.Pose.Position.ToStrideVector() - BodyContainer.CenterOfMass - ParentEntityTransform;
                        entityTransform.Rotation = body.Pose.Orientation.ToStrideQuaternion();
                        entityTransform.UpdateWorldMatrix();
                    }
                }
                //parForWatch.Stop();
                //Debug.WriteLine($"    stepCount : {stepCount}\n    SimulationFixedStep : {bepuSim.SimulationFixedStep}\n    RemainingUpdateTime : {bepuSim.RemainingUpdateTime}");
            }
            //totalWatch.Stop();
            //Debug.WriteLine($"-   Sim update function call : {simUpdWatch.ElapsedMilliseconds}\n-   Sim timestep : {simStepWatch.ElapsedMilliseconds}\n-   Position update : {parForWatch.ElapsedMilliseconds}\nEnd in : {totalWatch.ElapsedMilliseconds}");
            base.Update(time);
        }
    }

    internal class ContainerData
    {
        private readonly IGame _game;

        internal ContainerComponent ContainerComponent { get; }
        internal BepuConfiguration BepuConfiguration { get; }
        internal BepuSimulation BepuSimulation => BepuConfiguration.BepuSimulations[ContainerComponent.SimulationIndex];

        internal bool isStatic { get; set; } = false;

        internal TypedIndex ShapeIndex { get; set; }
        internal BodyInertia ShapeInertia { get; set; }

        internal BodyHandle BHandle { get; set; } = new(-1);
        internal StaticHandle SHandle { get; set; } = new(-1);

        public bool Exist => isStatic ? BepuSimulation.Simulation.Statics.StaticExists(SHandle) : BepuSimulation.Simulation.Bodies.BodyExists(BHandle);

        public ContainerData(ContainerComponent containerComponent, BepuConfiguration bepuConfiguration, IGame game)
        {
            ContainerComponent = containerComponent;
            BepuConfiguration = bepuConfiguration;
            _game = game;
        }

        internal void BuildOrUpdateContainer()
        {
            if (BepuSimulation == null)
                throw new Exception("A container must be inside a BepuSimulation.");

            if (ShapeIndex.Exists)
                BepuSimulation.Simulation.Shapes.Remove(ShapeIndex);

            ContainerComponent.Entity.Transform.UpdateWorldMatrix();
            ContainerComponent.Entity.Transform.WorldMatrix.Decompose(out Vector3 containerWorldScale, out Quaternion containerWorldRotation, out Vector3 containerWorldTranslation);

            var colliders = ContainerComponent.Entity.GetComponentsInDescendants<ColliderComponent>(true).ToList();

            if (ContainerComponent is BodyMeshContainerComponent _b)
            {
                if (colliders.Count > 0)
                    throw new Exception("MeshContainer cannot have compound colliders.");

                if (_b.ModelData == null)
                {
                    DestroyContainer();
                    return;
                }

                var meshTriangles = GetMeshTriangles(_b.ModelData);
                var pool = new BufferPool();
                pool.Take<Triangle>(meshTriangles.Length, out var triangles);
                for (int i = 0; i < meshTriangles.Length; ++i)
                {
                    triangles[i] = new Triangle(meshTriangles[i].A, meshTriangles[i].B, meshTriangles[i].C);
                }
                var mesh = new Mesh(triangles, _b.Entity.Transform.Scale.ToNumericVector(), pool);

                ShapeIndex = BepuSimulation.Simulation.Shapes.Add(mesh);
                ShapeInertia = _b.Closed ? mesh.ComputeClosedInertia(_b.Mass) : mesh.ComputeOpenInertia(_b.Mass);
                //ContainerComponent.CenterOfMass = (_b.Closed ? mesh.ComputeClosedCenterOfMass() : mesh.ComputeOpenCenterOfMass()).ToStrideVector(); //TODO : check why it is not needed 
            }
            else if (ContainerComponent is StaticMeshContainerComponent _s)
            {
                if (colliders.Count > 0)
                    throw new Exception("MeshContainer cannot have compound colliders.");

                if (_s.ModelData == null)
                {
                    DestroyContainer();
                    return;
                }

                var meshTriangles = GetMeshTriangles(_s.ModelData);
                var pool = new BufferPool();
                pool.Take<Triangle>(meshTriangles.Length, out var triangles);
                for (int i = 0; i < meshTriangles.Length; ++i)
                {
                    triangles[i] = new Triangle(meshTriangles[i].A, meshTriangles[i].B, meshTriangles[i].C);
                }
                var mesh = new Mesh(triangles, _s.Entity.Transform.Scale.ToNumericVector(), pool);

                ShapeIndex = BepuSimulation.Simulation.Shapes.Add(mesh);
                ShapeInertia = _s.Closed ? mesh.ComputeClosedInertia(_s.Mass) : mesh.ComputeOpenInertia(_s.Mass);
                ContainerComponent.CenterOfMass = (_s.Closed ? mesh.ComputeClosedCenterOfMass() : mesh.ComputeOpenCenterOfMass()).ToStrideVector();
            }
            else
            {
                if (colliders.Count() == 0)
                {
                    DestroyContainer();
                    return;
                }
                else
                {
                    using (var compoundBuilder = new CompoundBuilder(BepuSimulation.BufferPool, BepuSimulation.Simulation.Shapes, colliders.Count()))
                    {
                        Buffer<CompoundChild> compoundChildren;
                        BodyInertia shapeInertia;
                        System.Numerics.Vector3 shapeCenter;

                        foreach (var collider in colliders)
                        {
                            collider.Entity.Transform.UpdateWorldMatrix();
                            collider.Entity.Transform.WorldMatrix.Decompose(out Vector3 colliderWorldScale, out Quaternion colliderWorldRotation, out Vector3 colliderWorldTranslation);

                            var localTra = colliderWorldTranslation - containerWorldTranslation;
                            var localRot = Quaternion.Invert(containerWorldRotation) * colliderWorldRotation;
                            var localPose = new RigidPose(localTra.ToNumericVector(), localRot.ToNumericQuaternion());

                            switch (collider)
                            {
                                case BoxColliderComponent box:
                                    compoundBuilder.Add(new Box(box.Size.X, box.Size.Y, box.Size.Z), localPose, collider.Mass);
                                    break;

                                case CapsuleColliderComponent capsule:
                                    compoundBuilder.Add(new Capsule(capsule.Radius, capsule.Length), localPose, collider.Mass);
                                    break;
                                case ConvexHullColliderComponent convexHull:
                                    compoundBuilder.Add(new ConvexHull(GetMeshPoints(convexHull), new BufferPool(), out _), localPose, collider.Mass);
                                    break;
                                case CylinderColliderComponent cylinder:
                                    compoundBuilder.Add(new Cylinder(cylinder.Radius, cylinder.Length), localPose, collider.Mass);
                                    break;
                                case SphereColliderComponent sphere:
                                    compoundBuilder.Add(new Sphere(sphere.Radius), localPose, collider.Mass);
                                    break;
                                case TriangleColliderComponent triangle:
                                    compoundBuilder.Add(new Triangle(triangle.A.ToNumericVector(), triangle.B.ToNumericVector(), triangle.C.ToNumericVector()), localPose, collider.Mass);
                                    break;
                                default:
                                    throw new Exception("Unknown Shape");
                            }
                        }

                        compoundBuilder.BuildDynamicCompound(out compoundChildren, out shapeInertia, out shapeCenter);

                        ShapeIndex = BepuSimulation.Simulation.Shapes.Add(new Compound(compoundChildren));
                        ShapeInertia = shapeInertia;
                        ContainerComponent.CenterOfMass = shapeCenter.ToStrideVector();
                    }
                }
            }


            var ContainerPose = new RigidPose(containerWorldTranslation.ToNumericVector(), containerWorldRotation.ToNumericQuaternion());
            switch (ContainerComponent)
            {
                case BodyContainerComponent _c:
                    isStatic = false;
                    if (_c.Kinematic)
                    {
                        ShapeInertia = new BodyInertia();
                    }

                    var bDescription = BodyDescription.CreateDynamic(ContainerPose, ShapeInertia, ShapeIndex, new(_c.SleepThreshold, _c.MinimumTimestepCountUnderThreshold));

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
                    }

                    break;
                case StaticContainerComponent _c:
                    isStatic = true;

                    var sDescription = new StaticDescription(ContainerPose, ShapeIndex);

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
                    }

                    break;
                default:
                    break;
            }
        }
        internal void DestroyContainer()
        {
            ContainerComponent.CenterOfMass = new();

            if (ContainerComponent.IsRegistered())
            {
                ContainerComponent.UnregisterContact();
            }

            if (BHandle.Value != -1 && BepuSimulation.Simulation.Bodies.BodyExists(BHandle))
            {
                BepuSimulation.Simulation.Bodies.Remove(BHandle);
                BepuSimulation.BodiesContainers.Remove(BHandle);
            }

            if (SHandle.Value != -1 && BepuSimulation.Simulation.Statics.StaticExists(SHandle))
            {
                BepuSimulation.Simulation.Statics.Remove(SHandle);
                BepuSimulation.StaticsContainers.Remove(SHandle);
            }

            if (ShapeIndex.Exists)
                BepuSimulation.Simulation.Shapes.Remove(ShapeIndex);
        }

        private Span<Triangle> GetMeshTriangles(ModelComponent model)
        {
            (var verts, var indices) = model.Model.GetMeshVerticesAndIndices(_game);

            // Create an array to hold the triangles
            Triangle[] triangles = new Triangle[indices.Count / 3];

            // Loop through the indices to form triangles directly
            for (int i = 0; i < indices.Count; i += 3)
            {
                triangles[i / 3] = new Triangle(
                    verts[indices[i]].ToNumericVector(),
                    verts[indices[i + 1]].ToNumericVector(),
                    verts[indices[i + 2]].ToNumericVector()
                );
            }

            return triangles.AsSpan();
        }
        private Span<System.Numerics.Vector3> GetMeshPoints(ConvexHullColliderComponent collider)
        {
            if (collider.ModelData == null)
                return new();

            (var verts, var indices) = collider.ModelData.Model.GetMeshVerticesAndIndices(_game);
            System.Numerics.Vector3[] bepuVerts = new System.Numerics.Vector3[indices.Count];

            for (int i = 0; i < indices.Count; i++)
            {
                bepuVerts[i] = (verts[indices[i]] * collider.Entity.Transform.Scale).ToNumericVector(); //Warning, convexHull is the only collider to be scaled.
            }

            return bepuVerts.AsSpan();
        }

    }

}
