using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Constraints;
using Stride.Core.Annotations;
using Stride.Engine;
using BepuPhysicIntegrationTest.Integration.Components.Constraints;
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

    internal class ConstraintData
    {
        internal ConstraintComponent ConstraintComponent { get; }
        internal BepuSimulation BepuSimulation { get; }

        internal ConstraintHandle CHandle { get; set; } = new(-1);


        public bool Exist => CHandle.Value != -1 && BepuSimulation.Simulation.Solver.ConstraintExists(CHandle);

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
                case AngularAxisGearMotorConstraintComponent _aagmcc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _aagmcc._bepuConstraint);
                    break;
                case AngularAxisMotorConstraintComponent _aamcc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _aamcc._bepuConstraint);
                    break;
                case AngularHingeConstraintComponent _ahcc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _ahcc._bepuConstraint);
                    break;


                case BallSocketConstraintComponent _bscc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _bscc._bepuConstraint);
                    break;
                case WeldConstraintComponent _wcc:
                    CHandle = BepuSimulation.Simulation.Solver.Add(bodies, _wcc._bepuConstraint);
                    break;
                default:
                    break;
            }
        }
        internal void DestroyConstraint()
        {
            if (Exist)
            {
                BepuSimulation.Simulation.Solver.Remove(CHandle);
            }
        }
    }

}
