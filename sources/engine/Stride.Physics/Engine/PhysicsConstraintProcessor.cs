// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Diagnostics;
using Stride.Engine;
using Stride.Games;

namespace Stride.Physics.Engine
{
    public class PhysicsConstraintProcessor : EntityProcessor<PhysicsConstraintComponent>
    {
        private static readonly Logger logger = GlobalLogger.GetLogger(nameof(PhysicsConstraintProcessor));

        private readonly FastList<PhysicsConstraintComponent> detachedComponents = new FastList<PhysicsConstraintComponent>();

        public PhysicsConstraintProcessor()
        {
            Order = 0xFFFF; // After PhysicsProcessor
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] PhysicsConstraintComponent component, [NotNull] PhysicsConstraintComponent data)
        {
            component.ConstraintProcessor = this;
            component.Detached = false;

            // this is mostly required for the game studio gizmos
            if (Simulation.DisableSimulation)
            {
                return;
            }

            Recreate(component, skipUninitializedComponents: true);
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] PhysicsConstraintComponent component, [NotNull] PhysicsConstraintComponent data)
        {
            DisposeOf(component);
        }

        public override void Update(GameTime time)
        {
            // this is mostly required for the game studio gizmos
            if (Simulation.DisableSimulation)
                return;

            detachedComponents.Clear();

            foreach (var datas in ComponentDatas)
            {
                var component = datas.Key;

                if (component.Constraint != null)
                {
                    if (component.Constraint.InternalConstraint != null)
                    {
                        component.Constraint.Enabled = component.Enabled;
                    }
                    else
                    {
                        // the constraint is set but also disposed, let's clear it
                        DisposeOf(component);
                        detachedComponents.Add(component);
                    }
                }
                else if (component.Constraint == null && component.Enabled)
                {
                    Recreate(component, component.Detached);
                }
            }

            foreach (var component in detachedComponents)
            {
                component.OnDetach();
            }
        }

        /// <summary>
        /// Recreate the constraint according to the description in the <paramref name="component"/>.
        /// </summary>
        /// <param name="component">Constraint component.</param>
        /// <param name="skipUninitializedComponents">If <c>true</c> and rigidbody internals have not been initialized no exception will be thrown.</param>
        public void Recreate(PhysicsConstraintComponent component, bool skipUninitializedComponents = false)
        {
            if (component.Constraint != null)
            {
                DisposeOf(component);
            }
            
            if (component.Enabled)
            {
                if (component.Description == null || component.BodyA == null)
                {
                    logger.Warning("ConstraintComponent with an empty description or missing required body. Skipping constraint creation.");
                    return;
                }

                // this can happen when the constraint component is added to the processor
                // before the rigidbodies are added
                if (component.BodyA?.InternalRigidBody == null || component.BodyB != null && component.BodyB.InternalRigidBody == null)
                {
                    if (skipUninitializedComponents)
                        return;
                    else
                        throw new InvalidOperationException("Attempt was made to create a physics constraint, but one of the rigidbodies has not been initialized by the PhysicsProcessor.");
                }

                component.Constraint = component.Description.Build(
                    component.BodyA,
                    component.BodyB);
                component.Simulation = component.BodyA.Simulation;
                component.Simulation.AddConstraint(component.Constraint, component.DisableCollisionsBetweenBodies);
                component.Detached = false;
            }
        }

        private void DisposeOf(PhysicsConstraintComponent component)
        {
            // A disposed constraint will have internal == null
            if (component.Constraint != null && component.Constraint.InternalConstraint != null)
            {
                component.Constraint.Dispose();
            }

            component.Constraint = null;
            component.Detached = true;
        }
    }
}
