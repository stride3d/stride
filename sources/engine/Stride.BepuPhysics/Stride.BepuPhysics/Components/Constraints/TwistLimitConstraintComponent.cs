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
    public class TwistLimitConstraintComponent : ConstraintComponent
    {
        internal TwistLimit _bepuConstraint = new() { SpringSettings = new SpringSettings(30, 5) };

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

        public float MinimumAngle
        {
            get { return _bepuConstraint.MinimumAngle; }
            set
            {
                _bepuConstraint.MinimumAngle = value;
                if (ConstraintData?.Exist == true)
                    ConstraintData.BepuSimulation.Simulation.Solver.ApplyDescription(ConstraintData.CHandle, _bepuConstraint);
            }
        }

        public float MaximumAngle
        {
            get { return _bepuConstraint.MaximumAngle; }
            set
            {
                _bepuConstraint.MaximumAngle = value;
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
    }
}
