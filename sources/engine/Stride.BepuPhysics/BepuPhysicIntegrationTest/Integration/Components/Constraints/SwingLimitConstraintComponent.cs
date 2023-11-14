using System;
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
    public class SwingLimitConstraintComponent : ConstraintComponent
    {
        internal SwingLimit _bepuConstraint = new();

        public Vector3 AxisLocalA
        {
            get
            {
                return _bepuConstraint.AxisLocalA.ToStrideVector();
            }
            set
            {
                _bepuConstraint.AxisLocalA = value.ToNumericVector();
                if (ConstraintData?.Exist == true)
                    ConstraintData.BepuSimulation.Simulation.Solver.ApplyDescription(ConstraintData.CHandle, _bepuConstraint);
            }
        }

        public Vector3 AxisLocalB
        {
            get
            {
                return _bepuConstraint.AxisLocalB.ToStrideVector();
            }
            set
            {
                _bepuConstraint.AxisLocalB = value.ToNumericVector();
                if (ConstraintData?.Exist == true)
                    ConstraintData.BepuSimulation.Simulation.Solver.ApplyDescription(ConstraintData.CHandle, _bepuConstraint);
            }
        }

        public float MinimumDot
        {
            get { return _bepuConstraint.MinimumDot; }
            set
            {
                _bepuConstraint.MinimumDot = value;
                if (ConstraintData?.Exist == true)
                    ConstraintData.BepuSimulation.Simulation.Solver.ApplyDescription(ConstraintData.CHandle, _bepuConstraint);
            }
        }

        public float MaximumSwingAngle
        {
            get { return (float)Math.Acos(MinimumDot); }
            set
            {
                MinimumDot = (float)Math.Cos(value);
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
    }
}
