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
    public class OneBodyLinearMotorConstraintComponent : ConstraintComponent
    {
        internal OneBodyLinearMotor _bepuConstraint = new();

        public Vector3 LocalOffset
        {
            get
            {
                return _bepuConstraint.LocalOffset.ToStrideVector();
            }
            set
            {
                _bepuConstraint.LocalOffset = value.ToNumericVector();
                if (ConstraintData?.Exist == true)
                    ConstraintData.BepuSimulation.Simulation.Solver.ApplyDescription(ConstraintData.CHandle, _bepuConstraint);
            }
        }

        public Vector3 TargetVelocity
        {
            get
            {
                return _bepuConstraint.TargetVelocity.ToStrideVector();
            }
            set
            {
                _bepuConstraint.TargetVelocity = value.ToNumericVector();
                if (ConstraintData?.Exist == true)
                    ConstraintData.BepuSimulation.Simulation.Solver.ApplyDescription(ConstraintData.CHandle, _bepuConstraint);
            }
        }

        public MotorSettings Settings
        {
            get
            {
                return _bepuConstraint.Settings;
            }
            set
            {
                _bepuConstraint.Settings = value;
                if (ConstraintData?.Exist == true)
                    ConstraintData.BepuSimulation.Simulation.Solver.ApplyDescription(ConstraintData.CHandle, _bepuConstraint);
            }
        }
    }
}
