using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BepuPhysicIntegrationTest.Integration.Components.Colliders;
using BepuPhysicIntegrationTest.Integration.Components.Containers;
using BepuPhysicIntegrationTest.Integration.Components.Simulations;
using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Games;
using static BulletSharp.Dbvt;

namespace BepuPhysicIntegrationTest.Integration.Processors
{
    public class ContainerProcessor : EntityProcessor<ContainerComponent>
    {
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

    public class ContainerData
    {
        internal ContainerComponent ContainerComponent;

        internal BepuUtilities.Memory.Buffer<CompoundChild> CompoundChildren;
        internal BodyInertia CompoundInertia;
        internal Vector3 CompoundCenter;
        internal TypedIndex ShapeIndex;

        internal BodyDescription Description;
        internal BodyHandle Handle = new(-1);

        public ContainerData(ContainerComponent containerComponent)
        {
            ContainerComponent = containerComponent;
        }

        internal void BuildShape()
        {
            if (ContainerComponent.BepuSimulation == null)
                throw new Exception("Container must be inside a simulationBepu or be linked to one from the editor.");

            var colliders = ContainerComponent.Entity.GetAll<ColliderComponent>();

            if (colliders.Count() == 0)
            {
                return;
            }
            else
            {
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
                            //case Capsule capsule:
                            //    compoundBuilder.Add(capsule, collider.Entity.Transform.ToBepuPose(), collider.Mass);
                            //    break;
                            //case ConvexHull convexHull:
                            //    compoundBuilder.Add(convexHull, collider.Entity.Transform.ToBepuPose(), collider.Mass);
                            //    break;
                            //case Cylinder cylinder:
                            //    compoundBuilder.Add(cylinder, collider.Entity.Transform.ToBepuPose(), collider.Mass);
                            //    break;
                            //case Triangle triangle:
                            //    compoundBuilder.Add(triangle, collider.Entity.Transform.ToBepuPose(), collider.Mass);
                            //    break;
                            default:
                                throw new Exception("Empty Shape");
                        }
                    }
                    compoundBuilder.BuildDynamicCompound(out CompoundChildren, out CompoundInertia, out CompoundCenter);
                }
                if (CompoundInertia.InverseMass == float.PositiveInfinity)
                    CompoundInertia = new BodyInertia();
                if (ShapeIndex.Exists == true)
                    ContainerComponent.BepuSimulation.Simulation.Shapes.Remove(ShapeIndex);
                ShapeIndex = ContainerComponent.BepuSimulation.Simulation.Shapes.Add(new Compound(CompoundChildren));
            }

            switch (ContainerComponent)
            {
                case BodyContainerComponent _c:


                    if (_c.Kinematic)
                    {
                        CompoundInertia = new BodyInertia();
                    }

                    var pose = _c.Entity.Transform.ToBepuPose();
                    Description = BodyDescription.CreateDynamic(pose, CompoundInertia, ShapeIndex, .1f);

                    if (Handle.Value != -1)
                    {
                        ContainerComponent.BepuSimulation.Simulation.Bodies.Remove(Handle);
                        ContainerComponent.BepuSimulation.Bodies.RemoveAll(e => e.handle == Handle);
                    }

                    Handle = _c.BepuSimulation.Simulation.Bodies.Add(Description);
                    ContainerComponent.BepuSimulation.Bodies.Add((Handle, _c.Entity));
                    break;
                case StaticContainerComponent _c:

                    break;
                default:
                    break;
            }

        }
        internal void DestroyShape()
        {
            if (ShapeIndex.Exists == true)
                ContainerComponent.BepuSimulation.Simulation.Shapes.Remove(ShapeIndex);

            if (Handle.Value != -1)
            {
                ContainerComponent.BepuSimulation.Simulation.Bodies.Remove(Handle);
                ContainerComponent.BepuSimulation.Bodies.RemoveAll(e => e.handle == Handle);
            }
        }
    }

}
