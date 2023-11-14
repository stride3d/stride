using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BepuPhysicIntegrationTest.Integration.Components.Colliders;
using BepuPhysicIntegrationTest.Integration.Components.Constraints;
using BepuPhysicIntegrationTest.Integration.Components.Containers;
using BepuPhysicIntegrationTest.Integration.Configurations;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using Stride.Core.Annotations;
using Stride.Engine;

namespace BepuPhysicIntegrationTest.Integration.Processors
{
    public class ConstraintProcessor : EntityProcessor<ConstraintComponent>
    {
		private BepuConfiguration _bepuConfig;

		public ConstraintProcessor()
        {
            Order = 10030;
        }

		protected override void OnSystemAdd()
		{
			_bepuConfig = Services.GetService<BepuConfiguration>();
		}

		protected override void OnEntityComponentAdding(Entity entity, [NotNull] ConstraintComponent component, [NotNull] ConstraintComponent data)
        {
			component.BepuSimulation = _bepuConfig.BepuSimulations[0];
			component.ConstraintData = new(component);
            component.ConstraintData.BuildConstraint();
        }
        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] ConstraintComponent component, [NotNull] ConstraintComponent data)
        {
            component.ConstraintData.DestroyConstraint();
            component.ConstraintData = null;
        }
    }

    //Will need rewrite and expose directly the constraint (AgularAxisGearMotor for example) from the component to allow edit.
    internal class ConstraintData
    {
        internal ConstraintComponent ConstraintComponent;

        internal ConstraintHandle CHandle { get; set; } = new(-1);
        internal List<ContainerData> ContainerDataList { get; set; } = new();

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

                    ContainerDataList.Add(aagmcc.BodyA.ContainerData);
                    ContainerDataList.Add(aagmcc.BodyB.ContainerData);

                    var constrain = new AngularAxisGearMotor()
                    {
                        LocalAxisA = aagmcc.LocalAxisA.ToNumericVector(),
                        VelocityScale = aagmcc.VelocityScale,
                        Settings = aagmcc.Settings,
                    };
                    CHandle = ConstraintComponent.BepuSimulation.Simulation.Solver.Add(aHandle, BHandle, constrain);
                    break;
                case AngularAxisMotorConstraintComponent aamcc:
                    if (aamcc.BodyA == null || aamcc.BodyB == null)
                        return;

                    var aHandle1 = aamcc.BodyA.ContainerData.BHandle;
                    var BHandle1 = aamcc.BodyB.ContainerData.BHandle;
                    
                    ContainerDataList.Add(aamcc.BodyA.ContainerData);
                    ContainerDataList.Add(aamcc.BodyB.ContainerData);

                    var constrain1 = new AngularAxisMotor()
                    {
                        LocalAxisA = aamcc.LocalAxisA.ToNumericVector(),
                        TargetVelocity = aamcc.TargetVelocity,
                        Settings = aamcc.Settings,
                    };
                    CHandle = ConstraintComponent.BepuSimulation.Simulation.Solver.Add(aHandle1, BHandle1, constrain1);
                    break;
                case AngularHingeConstraintComponent ahcc:
                    if (ahcc.BodyA == null || ahcc.BodyB == null)
                        return;

                    var aHandle2 = ahcc.BodyA.ContainerData.BHandle;
                    var BHandle2 = ahcc.BodyB.ContainerData.BHandle;

                    ContainerDataList.Add(ahcc.BodyA.ContainerData);
                    ContainerDataList.Add(ahcc.BodyB.ContainerData);

                    var constrain2 = new AngularHinge()
                    {
                        LocalHingeAxisA = ahcc.LocalAxisA.ToNumericVector(),
                        LocalHingeAxisB = ahcc.LocalAxisB.ToNumericVector(),
                        SpringSettings = ahcc.Settings,
                    };
                    CHandle = ConstraintComponent.BepuSimulation.Simulation.Solver.Add(aHandle2, BHandle2, constrain2);
                    break;
                case BallSocketConstraintComponent bscc:
                    if (bscc.BodyA == null || bscc.BodyB == null)
                        return;

                    var aHandle3 = bscc.BodyA.ContainerData.BHandle;
                    var BHandle3 = bscc.BodyB.ContainerData.BHandle;

                    ContainerDataList.Add(bscc.BodyA.ContainerData);
                    ContainerDataList.Add(bscc.BodyB.ContainerData);


                    var constrain3 = new BallSocket()
                    {
                        LocalOffsetA = bscc.LocalOffsetA.ToNumericVector(),
                        LocalOffsetB = bscc.LocalOffsetB.ToNumericVector(),
                        SpringSettings = bscc.SpringSettings,
                    };
                    CHandle = ConstraintComponent.BepuSimulation.Simulation.Solver.Add(aHandle3, BHandle3, constrain3);
                    break;

                default:
                    break;
            }
        }

        internal void DestroyConstraint()
        {
            if (ConstraintComponent.BepuSimulation.Destroyed) return;

            ContainerDataList.Clear();
            if (CHandle.Value != -1 && ConstraintComponent.BepuSimulation.Simulation.Solver.ConstraintExists(CHandle))
            {
                ConstraintComponent.BepuSimulation.Simulation.Solver.Remove(CHandle);
            }
        }
    }

}
