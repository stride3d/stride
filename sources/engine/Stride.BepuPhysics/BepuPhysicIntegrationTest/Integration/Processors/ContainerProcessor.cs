using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Documents;
using BepuPhysicIntegrationTest.Integration.Components.Colliders;
using BepuPhysicIntegrationTest.Integration.Components.Containers;
using BepuPhysicIntegrationTest.Integration.Configurations;
using BepuPhysicIntegrationTest.Integration.Extensions;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using BepuUtilities.Collections;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Games;

namespace BepuPhysicIntegrationTest.Integration.Processors
{
    public class ContainerProcessor : EntityProcessor<ContainerComponent>
    {
        private BepuConfiguration _bepuConfiguration;
        private IGame _game;

        public ContainerProcessor()
        {
            Order = 10000;
        }

        protected override void OnSystemAdd()
        {
            var configService = Services.GetService<IGameSettingsService>();
            _bepuConfiguration = configService.Settings.Configurations.Get<BepuConfiguration>();
            if (_bepuConfiguration == null)
            {
                _bepuConfiguration = new BepuConfiguration();
                _bepuConfiguration.BepuSimulations.Add(new BepuSimulation());
            }

            _game = Services.GetService<IGame>();
            Services.AddService(_bepuConfiguration);
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] ContainerComponent component, [NotNull] ContainerComponent data)
        {
            component.ContainerData = new(component, _bepuConfiguration.BepuSimulations[component.SimulationIndex], _game);
            component.ContainerData.BuildShape();
        }
        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] ContainerComponent component, [NotNull] ContainerComponent data)
        {
            component.ContainerData.DestroyShape();
            component.ContainerData = null;
        }

        public override void Update(GameTime time)
        {
            var dt = (float)time.Elapsed.TotalSeconds;
            if (dt == 0f)
                return;

            var totalWatch = new Stopwatch();
            var simUpdWatch = new Stopwatch();
            var simStepWatch = new Stopwatch();
            var parForWatch = new Stopwatch();

            totalWatch.Start();

            foreach (var item in _bepuConfiguration.BepuSimulations)
            {
                if (!item.Enabled)
                    continue;

                var SimTimeStep = dt * item.TimeWarp; //calcul the timeStep of the simulation

                simUpdWatch.Start();
                item.CallSimulationUpdate(SimTimeStep); //cal the SimulationUpdate with simTimeStep
                simUpdWatch.Stop();

                simStepWatch.Start();
                item.Simulation.Timestep(SimTimeStep, item.ThreadDispatcher); //perform physic sim using simTimeStep
                simStepWatch.Stop();

                parForWatch.Start();
                if (item.Para)
                {
                    var a = Parallel.For(0, item.Simulation.Bodies.ActiveSet.Count, (i) =>
                    {
                        var handle = item.Simulation.Bodies.ActiveSet.IndexToHandle[i];
                        var entity = item.Bodies[handle];
                        var body = item.Simulation.Bodies[handle];

                        var entityTransform = entity.Transform;
                        entityTransform.Position = body.Pose.Position.ToStrideVector();
                        entityTransform.Rotation = body.Pose.Orientation.ToStrideQuaternion();
                        entityTransform.UpdateWorldMatrix();
                    });
                }
                else
                {
                    for (int i = 0; i < item.Simulation.Bodies.ActiveSet.Count; i++)
                    {
                        var handle = item.Simulation.Bodies.ActiveSet.IndexToHandle[i];
                        var entity = item.Bodies[handle];
                        var body = item.Simulation.Bodies[handle];

                        var entityTransform = entity.Transform;
                        entityTransform.Position = body.Pose.Position.ToStrideVector();
                        entityTransform.Rotation = body.Pose.Orientation.ToStrideQuaternion();
                        entityTransform.UpdateWorldMatrix();
                    }
                }
                parForWatch.Stop();
            }
            totalWatch.Stop();

            Debug.WriteLine($"total : {totalWatch.ElapsedMilliseconds} \n    sim update : {simUpdWatch.ElapsedMilliseconds}\n    sim step : {simStepWatch.ElapsedMilliseconds}\n    Position update : {parForWatch.ElapsedMilliseconds}");
            base.Update(time);
		}
	}

    internal class ContainerData
    {
        internal ContainerComponent ContainerComponent { get; }
        internal BepuSimulation BepuSimulation { get; }

        internal bool isStatic { get; set; } = false;

        internal BodyInertia ShapeInertia { get; set; }
        internal TypedIndex ShapeIndex { get; set; }

        internal BodyDescription BDescription { get; set; }
        internal BodyHandle BHandle { get; set; } = new(-1);

        internal StaticDescription SDescription { get; set; }
        internal StaticHandle SHandle { get; set; } = new(-1);

        private IGame _game;

        public ContainerData(ContainerComponent containerComponent, BepuSimulation bepuSimulation, IGame game)
        {
            ContainerComponent = containerComponent;
            BepuSimulation = bepuSimulation;
            _game = game;
        }

        internal void BuildShape()
        {
            if (BepuSimulation == null)
                throw new Exception("A container must be inside a BepuSimulation.");

            if (ShapeIndex.Exists)
                BepuSimulation.Simulation.Shapes.Remove(ShapeIndex);

            var colliders = ContainerComponent.Entity.GetAll<ColliderComponent>();

            if (colliders.Count() == 0)
            {
                return;
            }
            else if (colliders.Count() == 1)
            {
                switch (colliders.First())
                {
                    case BoxColliderComponent box:
                        var shapeB = new Box(box.Size.X, box.Size.Y, box.Size.Z);
                        ShapeInertia = shapeB.ComputeInertia(box.Mass);
                        ShapeIndex = BepuSimulation.Simulation.Shapes.Add(shapeB);
                        break;
                    case SphereColliderComponent sphere:
                        var shapeS = new Sphere(sphere.Radius);
                        ShapeInertia = shapeS.ComputeInertia(sphere.Mass);
                        ShapeIndex = BepuSimulation.Simulation.Shapes.Add(shapeS);
                        break;
                    case CapsuleColliderComponent capsule:
                        var shapeC = new Capsule(capsule.Radius, capsule.Length);
                        ShapeInertia = shapeC.ComputeInertia(capsule.Mass);
                        ShapeIndex = BepuSimulation.Simulation.Shapes.Add(shapeC);
                        break;
                    case ConvexHullColliderComponent convexHull:
                        //var shapeCh = new ConvexHull(GetMeshColliderShape(convexHull), new BufferPool(), out _);
                        //ShapeInertia = shapeCh.ComputeInertia(convexHull.Mass);
                        ShapeIndex = BepuSimulation.Simulation.Shapes.Add(GetMeshColliderShapeTest(convexHull));
                        break;
                    case CylinderColliderComponent cylinder:
                        var shapeCy = new Cylinder(cylinder.Radius, cylinder.Length);
                        ShapeInertia = shapeCy.ComputeInertia(cylinder.Mass);
                        ShapeIndex = BepuSimulation.Simulation.Shapes.Add(shapeCy);
                        break;
                    case TriangleColliderComponent triangle:
                        var shapeT = new Triangle(triangle.A.ToNumericVector(), triangle.B.ToNumericVector(), triangle.C.ToNumericVector());
                        ShapeInertia = shapeT.ComputeInertia(triangle.Mass);
                        ShapeIndex = BepuSimulation.Simulation.Shapes.Add(shapeT);
                        break;
                    default:
                        throw new Exception("Unknown Shape");
                }
            }
            else
            {
                BepuUtilities.Memory.Buffer<CompoundChild> compoundChildren;
                BodyInertia shapeInertia;
                Vector3 shapeCenter;

                using (var compoundBuilder = new CompoundBuilder(BepuSimulation.BufferPool, BepuSimulation.Simulation.Shapes, colliders.Count()))
                {
                    foreach (var collider in colliders)
                    {
                        switch (collider)
                        {
                            case BoxColliderComponent box:
                                compoundBuilder.Add(new Box(box.Size.X, box.Size.Y, box.Size.Z), collider.Entity.Transform.ToBepuPose(), collider.Mass);
                                break;
                            case SphereColliderComponent sphere:
                                compoundBuilder.Add(new Sphere(sphere.Radius), collider.Entity.Transform.ToBepuPose(), collider.Mass);
                                break;
                            case CapsuleColliderComponent capsule:
                                compoundBuilder.Add(new Capsule(capsule.Radius, capsule.Length), collider.Entity.Transform.ToBepuPose(), collider.Mass);
                                break;
                            case ConvexHullColliderComponent convexHull: //TODO
                                compoundBuilder.Add(new ConvexHull(), collider.Entity.Transform.ToBepuPose(), collider.Mass);
                                break;
                            case CylinderColliderComponent cylinder:
                                compoundBuilder.Add(new Cylinder(cylinder.Radius, cylinder.Length), collider.Entity.Transform.ToBepuPose(), collider.Mass);
                                break;
                            case TriangleColliderComponent triangle:
                                compoundBuilder.Add(new Triangle(triangle.A.ToNumericVector(), triangle.B.ToNumericVector(), triangle.C.ToNumericVector()), collider.Entity.Transform.ToBepuPose(), collider.Mass);
                                break;
                            default:
                                throw new Exception("Unknown Shape");
                        }
                    }

                    compoundBuilder.BuildDynamicCompound(out compoundChildren, out shapeInertia, out shapeCenter);
                }

                ShapeInertia = ShapeInertia;
                ShapeIndex = BepuSimulation.Simulation.Shapes.Add(new Compound(compoundChildren));
            }

            if (ShapeInertia.InverseMass == float.PositiveInfinity) //TODO : don't compute inertia (up) if kinematic or static
                ShapeInertia = new BodyInertia();

            var pose = ContainerComponent.Entity.Transform.ToBepuPose();
            switch (ContainerComponent)
            {
                case BodyContainerComponent _c:
                    isStatic = false;
                    if (_c.Kinematic)
                    {
                        ShapeInertia = new BodyInertia();
                    }

                    BDescription = BodyDescription.CreateDynamic(pose, ShapeInertia, ShapeIndex, .1f);

                    if (BHandle.Value != -1)
                    {
                        BepuSimulation.Simulation.Bodies.Remove(BHandle);
                        BepuSimulation.Bodies.Remove(BHandle);
                    }

                    BHandle = BepuSimulation.Simulation.Bodies.Add(BDescription);
                    BepuSimulation.Bodies.Add(BHandle, ContainerComponent.Entity);
                    break;
                case StaticContainerComponent _c:
                    isStatic = true;

                    SDescription = new StaticDescription(pose, ShapeIndex);

                    if (SHandle.Value != -1)
                    {
                        BepuSimulation.Simulation.Statics.Remove(SHandle);
                        BepuSimulation.Statics.Remove(SHandle);
                    }

                    SHandle = BepuSimulation.Simulation.Statics.Add(SDescription);
                    BepuSimulation.Statics.Add(SHandle, ContainerComponent.Entity);

                    break;
                default:
                    break;
            }
        }
        internal void DestroyShape()
        {
            if (BHandle.Value != -1 && BepuSimulation.Simulation.Bodies.BodyExists(BHandle))
            {
                BepuSimulation.Simulation.Bodies.Remove(BHandle);
                BepuSimulation.Bodies.Remove(BHandle);
            }

            if (SHandle.Value != -1 && BepuSimulation.Simulation.Statics.StaticExists(SHandle))
            {
                BepuSimulation.Simulation.Statics.Remove(SHandle);
                BepuSimulation.Statics.Remove(SHandle);
            }

            if (ShapeIndex.Exists)
                BepuSimulation.Simulation.Shapes.Remove(ShapeIndex);
		}

		private Span<Vector3> GetMeshColliderShape(ConvexHullColliderComponent collider)
		{
			// TODO: Create an extension that returns a numeric vectors instead of Stride Vector.
			(var verts, var indices) = collider.ModelData.Model.GetMeshVerticesAndIndices(_game);
			Vector3[] bepuVerts = new Vector3[indices.Count];

			for (int i = 0; i < indices.Count; i++)
			{
                bepuVerts[i] = verts[indices[i]].ToNumericVector();
			}

			return bepuVerts.AsSpan();
		}

        private Mesh GetMeshColliderShapeTest(ConvexHullColliderComponent collider)
        {
            (var verts, var indices) = collider.ModelData.Model.GetMeshVerticesAndIndices(_game);
            List<Triangle> bepuVerts = new(); //= new Triangle[(indices.Count + 3)/ 3];
            int a;
            int b;
            int c;
			for (int i = 0; i < indices.Count; i += 3)
            {
                a = indices[i];
                b = indices[i+1];
                c = indices[i+2];
                bepuVerts.Add( new(verts[a].ToNumericVector(), verts[b].ToNumericVector(), verts[c].ToNumericVector()));
			}

            var triSpan = bepuVerts.ToArray().AsSpan();

			return new Mesh(triSpan, Vector3.One, new BufferPool());
        }
	}

}
