using BepuPhysics.Constraints;
using Stride.BepuPhysics.Extensions;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Components.Constraints
{
    [DataContract("AngularServoConstraint")]
    [DefaultEntityComponentProcessor(typeof(ConstraintProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Constraint")]
    public class AngularServoConstraintComponent : ConstraintComponent
    {
        internal AngularServo _bepuConstraint = new()
        {
            SpringSettings = new SpringSettings(30, 5),
            ServoSettings = new ServoSettings(10, 1, 1000)
        };

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
