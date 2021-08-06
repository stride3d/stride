// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Engine;
using Stride.Games;

namespace Stride.Physics.Engine
{
    public class PhysicsConstraintProcessor : EntityProcessor<PhysicsConstraintComponent>
    {
        private static readonly Logger logger = GlobalLogger.GetLogger(nameof(PhysicsConstraintProcessor));

        public PhysicsConstraintProcessor()
        {
            Order = 0xFFFE;
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] PhysicsConstraintComponent component, [NotNull] PhysicsConstraintComponent data)
        {
            //this is mostly required for the game studio gizmos
            if (Simulation.DisableSimulation)
            {
                return;
            }

            Recreate(component);
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] PhysicsConstraintComponent component, [NotNull] PhysicsConstraintComponent data)
        {
            if (component.Constraint != null)
            {
                component.Simulation.RemoveConstraint(component.Constraint);
            }
        }

        public override void Update(GameTime time)
        {
            //this is mostly required for the game studio gizmos
            if (Simulation.DisableSimulation)
                return;

            foreach (var datas in ComponentDatas)
            {
                var component = datas.Key;

                if (component.Constraint != null)
                {
                    component.Constraint.Enabled = component.Enabled;
                }
                else if (component.Constraint == null && component.Enabled)
                {
                    Recreate(component);
                }
            }
        }

        public void Recreate(PhysicsConstraintComponent component)
        {
            if (component.Constraint != null)
            {
                component.Simulation.RemoveConstraint(component.Constraint);
            }
            
            if (component.Enabled)
            {
                if (component.Description == null || component.BodyA == null)
                {
                    logger.Warning("ConstraintComponent with an empty description or missing required body. Skipping constrain creation.");
                    return;
                }

                if (component.BodyB != null && component.BodyB.Simulation != component.BodyA.Simulation)
                    return; // simulation mismatch - may happen when first loading the scene
                if (component.BodyA?.InternalRigidBody == null || component.BodyB != null && component.BodyB.InternalRigidBody == null)
                    return; // constraint processing ran before rigidbodies were initialized by the PhysicsProcessor

                component.Constraint = component.Description.Build(
                    component.BodyA,
                    component.BodyB);
                component.Simulation = component.BodyA.Simulation;
                component.Simulation.AddConstraint(component.Constraint, component.DisableCollisionsBetweenBodies);
            }
        }
    }
}
