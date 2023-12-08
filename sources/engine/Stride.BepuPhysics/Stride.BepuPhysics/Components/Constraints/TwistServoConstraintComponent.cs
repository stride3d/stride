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
    public class TwistServoConstraintComponent : ConstraintComponent
    {
        internal TwistServo _bepuConstraint = new() { SpringSettings = new SpringSettings(30, 5), ServoSettings = new ServoSettings(10, 1, 1000) };

        public Quaternion LocalBasisA
        {
            get
            {
                return _bepuConstraint.LocalBasisA.ToStrideQuaternion();
            }
            set
            {
                _bepuConstraint.LocalBasisA = value.ToNumericQuaternion();
                if (ConstraintData?.Exist == true)
                    ConstraintData.BepuSimulation.Simulation.Solver.ApplyDescription(ConstraintData.CHandle, _bepuConstraint);
            }
        }

        public Quaternion LocalBasisB
        {
            get
            {
                return _bepuConstraint.LocalBasisB.ToStrideQuaternion();
            }
            set
            {
                _bepuConstraint.LocalBasisB = value.ToNumericQuaternion();
                if (ConstraintData?.Exist == true)
                    ConstraintData.BepuSimulation.Simulation.Solver.ApplyDescription(ConstraintData.CHandle, _bepuConstraint);
            }
        }

        public float TargetAngle
        {
            get { return _bepuConstraint.TargetAngle; }
            set
            {
                _bepuConstraint.TargetAngle = value;
                if (ConstraintData?.Exist == true)
                    ConstraintData.BepuSimulation.Simulation.Solver.ApplyDescription(ConstraintData.CHandle, _bepuConstraint);
            }
        }

        public float SpringFrequency
        {
            get
            {
                return _bepuConstraint.SpringSettings.Frequency;
            }
            set
            {
                _bepuConstraint.SpringSettings.Frequency = value;
                if (ConstraintData?.Exist == true)
                    ConstraintData.BepuSimulation.Simulation.Solver.ApplyDescription(ConstraintData.CHandle, _bepuConstraint);
            }
        }

        public float SpringDampingRatio
        {
            get
            {
                return _bepuConstraint.SpringSettings.DampingRatio;
            }
            set
            {
                _bepuConstraint.SpringSettings.DampingRatio = value;
                if (ConstraintData?.Exist == true)
                    ConstraintData.BepuSimulation.Simulation.Solver.ApplyDescription(ConstraintData.CHandle, _bepuConstraint);
            }
        }

        public float ServoMaximumSpeed
        {
            get
            {
                return _bepuConstraint.ServoSettings.MaximumSpeed;
            }
            set
            {
                _bepuConstraint.ServoSettings.MaximumSpeed = value;
                if (ConstraintData?.Exist == true)
                    ConstraintData.BepuSimulation.Simulation.Solver.ApplyDescription(ConstraintData.CHandle, _bepuConstraint);
            }
        }

        public float ServoBaseSpeed
        {
            get
            {
                return _bepuConstraint.ServoSettings.BaseSpeed;
            }
            set
            {
                _bepuConstraint.ServoSettings.BaseSpeed = value;
                if (ConstraintData?.Exist == true)
                    ConstraintData.BepuSimulation.Simulation.Solver.ApplyDescription(ConstraintData.CHandle, _bepuConstraint);
            }
        }

        public float ServoMaximumForce
        {
            get
            {
                return _bepuConstraint.ServoSettings.MaximumForce;
            }
            set
            {
                _bepuConstraint.ServoSettings.MaximumForce = value;
                if (ConstraintData?.Exist == true)
                    ConstraintData.BepuSimulation.Simulation.Solver.ApplyDescription(ConstraintData.CHandle, _bepuConstraint);
            }
        }
    }

}
