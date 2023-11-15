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
    public class OneBodyLinearServoConstraintComponent : ConstraintComponent
    {
        internal OneBodyLinearServo _bepuConstraint = new() { ServoSettings = new(100, 1, 1000), SpringSettings = new(30, 5) };

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

        public Vector3 Target
        {
            get
            {
                return _bepuConstraint.Target.ToStrideVector();
            }
            set
            {
                _bepuConstraint.Target = value.ToNumericVector();
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
