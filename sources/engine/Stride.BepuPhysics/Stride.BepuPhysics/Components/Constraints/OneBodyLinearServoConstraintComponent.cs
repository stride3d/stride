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
    public class OneBodyLinearServoConstraintComponent : ConstraintComponent
    {
        internal OneBodyLinearServo _bepuConstraint = new()
        {
            ServoSettings = new ServoSettings(100, 1, 1000),
            SpringSettings = new SpringSettings(30, 5)
        };

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
