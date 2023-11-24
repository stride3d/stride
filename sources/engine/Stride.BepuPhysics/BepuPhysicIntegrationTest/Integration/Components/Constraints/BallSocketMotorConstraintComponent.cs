using BepuPhysicIntegrationTest.Integration.Extensions;
using BepuPhysicIntegrationTest.Integration.Processors;
using BepuPhysics.Constraints;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace BepuPhysicIntegrationTest.Integration.Components.Constraints
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ConstraintProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Constraint")]
    public class BallSocketMotorConstraintComponent : ConstraintComponent
    {
        internal BallSocketMotor _bepuConstraint = new()
        {
            Settings = new MotorSettings(1000, 10)
        };

        public Vector3 LocalOffsetB
        {
            get
            {
                return _bepuConstraint.LocalOffsetB.ToStrideVector();
            }
            set
            {
                _bepuConstraint.LocalOffsetB = value.ToNumericVector();
                if (ConstraintData?.Exist == true)
                    ConstraintData.BepuSimulation.Simulation.Solver.ApplyDescription(ConstraintData.CHandle, _bepuConstraint);
            }
        }

        public Vector3 TargetVelocityLocalA
        {
            get
            {
                return _bepuConstraint.TargetVelocityLocalA.ToStrideVector();
            }
            set
            {
                _bepuConstraint.TargetVelocityLocalA = value.ToNumericVector();
                if (ConstraintData?.Exist == true)
                    ConstraintData.BepuSimulation.Simulation.Solver.ApplyDescription(ConstraintData.CHandle, _bepuConstraint);
            }
        }

        public float MotorDamping
        {
            get
            {
                return _bepuConstraint.Settings.Damping;
            }
            set
            {
                _bepuConstraint.Settings.Damping = value;
                if (ConstraintData?.Exist == true)
                    ConstraintData.BepuSimulation.Simulation.Solver.ApplyDescription(ConstraintData.CHandle, _bepuConstraint);
            }
        }

        public float MotorMaximumForce
        {
            get
            {
                return _bepuConstraint.Settings.MaximumForce;
            }
            set
            {
                _bepuConstraint.Settings.MaximumForce = value;
                if (ConstraintData?.Exist == true)
                    ConstraintData.BepuSimulation.Simulation.Solver.ApplyDescription(ConstraintData.CHandle, _bepuConstraint);
            }
        }
    }

}
