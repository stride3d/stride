using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BepuPhysicIntegrationTest.Integration.Components.ConstrraintsV2;
using BepuPhysics;
using BepuPhysics.Constraints;
using Stride.Core.Annotations;
using Stride.Engine;

namespace BepuPhysicIntegrationTest.Integration.Processors
{
    public class ConstraintProcessorV2 : EntityProcessor<ConstraintComponentV2>
    {
        public ConstraintProcessorV2()
        {
            Order = 10030;
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] ConstraintComponentV2 component, [NotNull] ConstraintComponentV2 data)
        {
            base.OnEntityComponentAdding(entity, component, data);
            component.ConstraintData = new(component);
            component.ConstraintData.BuildConstraint();
        }
        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] ConstraintComponentV2 component, [NotNull] ConstraintComponentV2 data)
        {
            base.OnEntityComponentRemoved(entity, component, data);
            component.ConstraintData.DestroyConstraint();
            component.ConstraintData = null;
        }
    }

    //Will need rewrite and expose directly the constraint (AgularAxisGearMotor for example) from the component to allow edit.
    internal class ConstraintDataV2
    {
        internal ConstraintComponentV2 ConstraintComponent;

        internal ConstraintHandle CHandle { get; set; } = new(-1);

        public ConstraintDataV2(ConstraintComponentV2 constraintComponent)
        {
            ConstraintComponent = constraintComponent;
        }

        internal void BuildConstraint()
        {
            switch (ConstraintComponent)
            {       
                case BallSocketConstraintComponentV2 _bscc:
                    CHandle = ConstraintComponent.BepuSimulation.Simulation.Solver.Add(new Span<BodyHandle>(ConstraintComponent.Bodies.Where(e => e.ContainerData != null).Select(e => e.ContainerData.BHandle).ToArray()), _bscc._bepuConstraint);
                    break;
                default:
                    break;
            }
        }
        internal void DestroyConstraint()
        {
            if (ConstraintComponent.BepuSimulation.Destroyed) return;

            if (CHandle.Value != -1 && ConstraintComponent.BepuSimulation.Simulation.Solver.ConstraintExists(CHandle))
            {
                ConstraintComponent.BepuSimulation.Simulation.Solver.Remove(CHandle);
            }
        }
    }

}
