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
    public class TwistMotorConstraintComponent : ConstraintComponent
    {
        internal TwistMotor _bepuConstraint = new() { Settings = new MotorSettings(1000, 10) };

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

        public Vector3 LocalAxisB
        {
            get
            {
                return _bepuConstraint.LocalAxisB.ToStrideVector();
            }
            set
            {
                _bepuConstraint.LocalAxisB = value.ToNumericVector();
                if (ConstraintData?.Exist == true)
                    ConstraintData.BepuSimulation.Simulation.Solver.ApplyDescription(ConstraintData.CHandle, _bepuConstraint);
            }
        }

        public float TargetVelocity
        {
            get { return _bepuConstraint.TargetVelocity; }
            set
            {
                _bepuConstraint.TargetVelocity = value;
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
