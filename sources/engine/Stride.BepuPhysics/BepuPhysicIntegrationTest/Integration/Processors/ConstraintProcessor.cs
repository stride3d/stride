using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Constraints;
using Stride.Core.Annotations;
using Stride.Engine;
using BepuPhysicIntegrationTest.Integration.Components.ConstraintsV2;
using BepuPhysicIntegrationTest.Integration.Configurations;
using SharpFont.MultipleMasters;

namespace BepuPhysicIntegrationTest.Integration.Processors
{
    public class ConstraintProcessor : EntityProcessor<ConstraintComponent>
	{
		private BepuConfiguration _bepuConfig;

		public ConstraintProcessor()
        {
            Order = 10020;
        }

		protected override void OnSystemAdd()
		{
			_bepuConfig = Services.GetService<BepuConfiguration>();
		}

		protected override void OnEntityComponentAdding(Entity entity, [NotNull] ConstraintComponent component, [NotNull] ConstraintComponent data)
        {
            base.OnEntityComponentAdding(entity, component, data);
            component.ConstraintData = new(component, _bepuConfig.BepuSimulations[0]); //TODO : get Index from bodies
            component.ConstraintData.BuildConstraint();
        }
        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] ConstraintComponent component, [NotNull] ConstraintComponent data)
        {
            base.OnEntityComponentRemoved(entity, component, data);
            component.ConstraintData.DestroyConstraint();
            component.ConstraintData = null;
        }
    }

    //Will need rewrite and expose directly the constraint (AgularAxisGearMotor for example) from the component to allow edit.
    internal class ConstraintData
    {
        internal ConstraintComponent ConstraintComponent { get; }
        internal BepuSimulation BepuSimulation { get; }

        internal ConstraintHandle CHandle { get; set; } = new(-1);

        public ConstraintData(ConstraintComponent constraintComponent, BepuSimulation bepuSimulation)
        {
            ConstraintComponent = constraintComponent;
            BepuSimulation = bepuSimulation;
        }

        internal void BuildConstraint()
        {
            var bodies = new Span<BodyHandle>(ConstraintComponent.Bodies.Where(e => e.ContainerData != null).Select(e => e.ContainerData.BHandle).ToArray());
            switch (ConstraintComponent)
            {
                case BallSocketConstraintComponent _bscc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _bscc._bepuConstraint);
                    break;
                default:
                    break;
            }
        }
        internal void DestroyConstraint()
        {
            if (CHandle.Value != -1 && BepuSimulation.Simulation.Solver.ConstraintExists(CHandle))
            {
                BepuSimulation.Simulation.Solver.Remove(CHandle);
            }
        }
    }

}
