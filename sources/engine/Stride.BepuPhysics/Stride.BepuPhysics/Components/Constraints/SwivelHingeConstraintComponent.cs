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
    public class SwivelHingeConstraintComponent : ConstraintComponent
    {
        internal SwivelHinge _bepuConstraint = new() { SpringSettings = new SpringSettings(30, 5) };

        public Vector3 LocalOffsetA
        {
            get
            {
                return _bepuConstraint.LocalOffsetA.ToStrideVector();
            }
            set
            {
                _bepuConstraint.LocalOffsetA = value.ToNumericVector();
                if (ConstraintData?.Exist == true)
                    ConstraintData.BepuSimulation.Simulation.Solver.ApplyDescription(ConstraintData.CHandle, _bepuConstraint);
            }
        }

        public Vector3 LocalSwivelAxisA
        {
            get
            {
                return _bepuConstraint.LocalSwivelAxisA.ToStrideVector();
            }
            set
            {
                _bepuConstraint.LocalSwivelAxisA = value.ToNumericVector();
                if (ConstraintData?.Exist == true)
                    ConstraintData.BepuSimulation.Simulation.Solver.ApplyDescription(ConstraintData.CHandle, _bepuConstraint);
            }
        }

        public Vector3 LocalOffsetB
        {
            get
            {
                return _bepuConstraint.LocalOffsetB.ToStrideVector();
            }
            set
            {
                _bepuConstraint.LocalOffsetB = value.ToNumericVector();
                if (ConstraintData?.Exist == true)
                    ConstraintData.BepuSimulation.Simulation.Solver.ApplyDescription(ConstraintData.CHandle, _bepuConstraint);
            }
        }

        public Vector3 LocalHingeAxisB
        {
            get
            {
                return _bepuConstraint.LocalHingeAxisB.ToStrideVector();
            }
            set
            {
                _bepuConstraint.LocalHingeAxisB = value.ToNumericVector();
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
