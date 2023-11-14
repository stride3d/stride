using BepuPhysicIntegrationTest.Integration.Components.Constraints;
using BepuPhysicIntegrationTest.Integration.Processors;
using BepuPhysicIntegrationTest.Integration;
using BepuPhysics.Constraints;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace BepuPhysicIntegrationTest.Integration.Components.Constraints
{
    [DataContract("AngularMotorConstraint")]
    [DefaultEntityComponentProcessor(typeof(ConstraintProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Constraint")]
    public class AngularMotorConstraintComponent : ConstraintComponent
    {
        internal AngularMotor _bepuConstraint = new();

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
