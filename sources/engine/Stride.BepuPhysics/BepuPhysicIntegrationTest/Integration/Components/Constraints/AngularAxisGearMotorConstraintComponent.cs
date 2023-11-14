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
    public class AngularAxisGearMotorConstraintComponent : ConstraintComponent
    {
        internal AngularAxisGearMotor _bepuConstraint = new();

        public Vector3 LocalAxisA
        {
            get
            {
                return _bepuConstraint.LocalAxisA.ToStrideVector();
            }
            set
            {
                _bepuConstraint.LocalAxisA = value.ToNumericVector();
                if (ConstraintData?.Exist == true)
                    ConstraintData.BepuSimulation.Simulation.Solver.ApplyDescription(ConstraintData.CHandle, _bepuConstraint);
            }
        }

        public float VelocityScale
        {
            get
            {
                return _bepuConstraint.VelocityScale;
            }
            set
            {
                _bepuConstraint.VelocityScale = value;
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
