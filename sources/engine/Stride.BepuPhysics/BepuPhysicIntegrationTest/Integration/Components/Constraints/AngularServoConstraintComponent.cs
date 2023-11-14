using BepuPhysicIntegrationTest.Integration.Processors;
using BepuPhysics.Constraints;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace BepuPhysicIntegrationTest.Integration.Components.Constraints
{
    [DataContract("AngularServoConstraint")]
    [DefaultEntityComponentProcessor(typeof(ConstraintProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Constraint")]
    public class AngularServoConstraintComponent : ConstraintComponent
    {
        internal AngularServo _bepuConstraint = new() { SpringSettings = new SpringSettings(30, 5) };

        public Quaternion TargetRelativeRotationLocalA
        {
            get
            {
                return _bepuConstraint.TargetRelativeRotationLocalA.ToStrideQuaternion();
            }
            set
            {
                _bepuConstraint.TargetRelativeRotationLocalA = value.ToNumericQuaternion();
                if (ConstraintData?.Exist == true)
                    ConstraintData.BepuSimulation.Simulation.Solver.ApplyDescription(ConstraintData.CHandle, _bepuConstraint);
            }
        }

        public SpringSettings SpringSettings
        {
            get
            {
                return _bepuConstraint.SpringSettings;
            }
            set
            {
                _bepuConstraint.SpringSettings = value;
                if (ConstraintData?.Exist == true)
                    ConstraintData.BepuSimulation.Simulation.Solver.ApplyDescription(ConstraintData.CHandle, _bepuConstraint);
            }
        }

        public ServoSettings ServoSettings
        {
            get
            {
                return _bepuConstraint.ServoSettings;
            }
            set
            {
                _bepuConstraint.ServoSettings = value;
                if (ConstraintData?.Exist == true)
                    ConstraintData.BepuSimulation.Simulation.Solver.ApplyDescription(ConstraintData.CHandle, _bepuConstraint);
            }
        }
    }
}
