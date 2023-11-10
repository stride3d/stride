using System;
using System.Linq;
using System.Numerics;
using BepuPhysicIntegrationTest.Integration.Components.Colliders;
using BepuPhysicIntegrationTest.Integration.Components.Constraints;
using BepuPhysicIntegrationTest.Integration.Configurations;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using Stride.Core.Annotations;
using Stride.Engine;
using static BulletSharp.Dbvt;

namespace BepuPhysicIntegrationTest.Integration.Processors
{
    public class ConstraintProcessor : EntityProcessor<ConstraintComponent>
    {
        private BepuConfiguration _bepuConfig;

        public ConstraintProcessor()
        {
            Order = 10040;
        }

		protected override void OnSystemAdd()
		{
			_bepuConfig = Services.GetService<BepuConfiguration>();
		}

		protected override void OnEntityComponentAdding(Entity entity, [NotNull] ConstraintComponent component, [NotNull] ConstraintComponent data)
        {
            base.OnEntityComponentAdding(entity, component, data);
            component.ConstraintData = new(component);
            component.BepuSimulation = _bepuConfig.BepuSimulations[0];
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
        internal ConstraintComponent ConstraintComponent;

        internal BodyHandle AHandle { get; set; } = new(-1);
        internal BodyHandle BHandle { get; set; } = new(-1);

        public ConstraintData(ConstraintComponent constraintComponent)
        {
            ConstraintComponent = constraintComponent;
        }

        internal void BuildConstraint()
        {
            switch (ConstraintComponent)
            {
                case AngularAxisGearMotorConstraintComponent aagmcc:
                    if (aagmcc.BodyA == null || aagmcc.BodyB == null)
                        return;

                    var aHandle = aagmcc.BodyA.ContainerData.BHandle;
                    var BHandle = aagmcc.BodyB.ContainerData.BHandle;

                    var constrain = new AngularAxisGearMotor()
                    {
                        LocalAxisA = aagmcc.LocalAxisA.ToNumericVector(),
                        VelocityScale = aagmcc.VelocityScale,
                        Settings = aagmcc.Settings,
                    };
                    ConstraintComponent.BepuSimulation.Simulation.Solver.Add(aHandle, BHandle, constrain);
                    break;
                case BallSocketConstraintComponent bscc:
                    if (bscc.BodyA == null || bscc.BodyB == null)
                        return;

                    var aHandle1 = bscc.BodyA.ContainerData.BHandle;
                    var BHandle1 = bscc.BodyB.ContainerData.BHandle;

                    var constrain1 = new BallSocket()
                    {
                        LocalOffsetA = bscc.LocalOffsetA.ToNumericVector(),
                        LocalOffsetB = bscc.LocalOffsetB.ToNumericVector(),
                        SpringSettings = bscc.SpringSettings,
                    };
                    ConstraintComponent.BepuSimulation.Simulation.Solver.Add(aHandle1, BHandle1, constrain1);
                    break;

                default:
                    break;
            }
        }
        internal void DestroyConstraint()
        {

        }
    }

}
