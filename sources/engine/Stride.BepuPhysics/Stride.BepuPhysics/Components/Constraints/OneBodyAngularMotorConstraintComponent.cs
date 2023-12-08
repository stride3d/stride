using BepuPhysics.Constraints;
using Stride.BepuPhysics.Extensions;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Components.Constraints
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ConstraintProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Constraint")]
    public class OneBodyAngularMotorConstraintComponent : ConstraintComponent
    {
        internal OneBodyAngularMotor _bepuConstraint = new()
        {
            Settings = new MotorSettings(10000000, 0.02f)
        };

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
