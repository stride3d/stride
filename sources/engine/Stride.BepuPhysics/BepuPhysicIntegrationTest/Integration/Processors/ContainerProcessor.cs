using System;
using System.Linq;
using System.Numerics;
using BepuPhysicIntegrationTest.Integration.Components.Colliders;
using BepuPhysicIntegrationTest.Integration.Components.Constraints;
using BepuPhysicIntegrationTest.Integration.Components.Containers;
using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.Core.Annotations;
using Stride.Engine;
using static BulletSharp.Dbvt;

namespace BepuPhysicIntegrationTest.Integration.Processors
{
    public class ContainerProcessor : EntityProcessor<ContainerComponent>
    {
        //public ConstraintProcessor ConstraintProcessor { get; }

        public ContainerProcessor()
        {
            Order = 10010;
            //ConstraintProcessor = EntityManager.Processors.Get<ConstraintProcessor>();
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] ContainerComponent component, [NotNull] ContainerComponent data)
        {
            base.OnEntityComponentAdding(entity, component, data);
            component.ContainerData = new(component);
            component.ContainerData.BuildShape();
        }
        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] ContainerComponent component, [NotNull] ContainerComponent data)
        {
            base.OnEntityComponentRemoved(entity, component, data);
            component.ContainerData.DestroyShape();
            component.ContainerData = null;
        }
    }

    internal class ContainerData
    {
        internal ContainerComponent ContainerComponent;

        internal bool isStatic { get; set; } = false;

        internal BodyInertia ShapeInertia { get; set; }
        internal TypedIndex ShapeIndex { get; set; }

        internal BodyDescription BDescription { get; set; }
        internal BodyHandle BHandle { get; set; } = new(-1);

        internal StaticDescription SDescription { get; set; }
        internal StaticHandle SHandle { get; set; } = new(-1);

        public ContainerData(ContainerComponent containerComponent)
        {
            ContainerComponent = containerComponent;
        }

        internal void BuildShape()
        {
            if (ContainerComponent.BepuSimulation == null)
                throw new Exception("Container must be inside a simulationBepu or be linked to one from the editor.");

            if (ShapeIndex.Exists == true)
                ContainerComponent.BepuSimulation.Simulation.Shapes.Remove(ShapeIndex);

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
                        ShapeIndex = ContainerComponent.BepuSimulation.Simulation.Shapes.Add(shapeB);
                        break;
                    case SphereColliderComponent sphere:
                        var shapeS = new Sphere(sphere.Radius);
                        ShapeInertia = shapeS.ComputeInertia(sphere.Mass);
                        ShapeIndex = ContainerComponent.BepuSimulation.Simulation.Shapes.Add(shapeS);
                        break;
                    case CapsuleColliderComponent capsule:
                        var shapeC = new Capsule(capsule.Radius, capsule.Length);
                        ShapeInertia = shapeC.ComputeInertia(capsule.Mass);
                        ShapeIndex = ContainerComponent.BepuSimulation.Simulation.Shapes.Add(shapeC);
                        break;
                    case ConvexHullColliderComponent convexHull: //TODO
                        var shapeCh = new ConvexHull();
                        ShapeInertia = shapeCh.ComputeInertia(convexHull.Mass);
                        ShapeIndex = ContainerComponent.BepuSimulation.Simulation.Shapes.Add(shapeCh);
                        break;
                    case CylinderColliderComponent cylinder:
                        var shapeCy = new Cylinder(cylinder.Radius, cylinder.Length);
                        ShapeInertia = shapeCy.ComputeInertia(cylinder.Mass);
                        ShapeIndex = ContainerComponent.BepuSimulation.Simulation.Shapes.Add(shapeCy);
                        break;
                    case TriangleColliderComponent triangle:
                        var shapeT = new Triangle(triangle.A.ToNumericVector(), triangle.B.ToNumericVector(), triangle.C.ToNumericVector());
                        ShapeInertia = shapeT.ComputeInertia(triangle.Mass);
                        ShapeIndex = ContainerComponent.BepuSimulation.Simulation.Shapes.Add(shapeT);
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

                using (var compoundBuilder = new CompoundBuilder(ContainerComponent.BepuSimulation.BufferPool, ContainerComponent.BepuSimulation.Simulation.Shapes, colliders.Count()))
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
                ShapeIndex = ContainerComponent.BepuSimulation.Simulation.Shapes.Add(new Compound(compoundChildren));
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
                        ContainerComponent.BepuSimulation.Simulation.Bodies.Remove(BHandle);
                        ContainerComponent.BepuSimulation.Bodies.Remove(BHandle);
                    }

                    BHandle = ContainerComponent.BepuSimulation.Simulation.Bodies.Add(BDescription);
                    ContainerComponent.BepuSimulation.Bodies.Add(BHandle, ContainerComponent.Entity);
                    break;
                case StaticContainerComponent _c:
                    isStatic = true;

                    SDescription = new StaticDescription(pose, ShapeIndex);

                    if (SHandle.Value != -1)
                    {
                        ContainerComponent.BepuSimulation.Simulation.Statics.Remove(SHandle);
                        ContainerComponent.BepuSimulation.Statics.Remove(SHandle);
                    }

                    SHandle = ContainerComponent.BepuSimulation.Simulation.Statics.Add(SDescription);
                    ContainerComponent.BepuSimulation.Statics.Add(SHandle, ContainerComponent.Entity);

                    break;
                default:
                    break;
            }
        }
        internal void DestroyShape()
        {
            if (ContainerComponent.BepuSimulation.Destroyed) return;
                
            if (BHandle.Value != -1)
            {
                ContainerComponent.BepuSimulation.Simulation.Bodies.Remove(BHandle);
                ContainerComponent.BepuSimulation.Bodies.Remove(BHandle);
            }

            if (SHandle.Value != -1)
            {
                ContainerComponent.BepuSimulation.Simulation.Statics.Remove(SHandle);
                ContainerComponent.BepuSimulation.Statics.Remove(SHandle);
            }

            if (ShapeIndex.Exists == true)
                ContainerComponent.BepuSimulation.Simulation.Shapes.Remove(ShapeIndex);
        }
    }

}
