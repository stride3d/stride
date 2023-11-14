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
    public class CenterDistanceConstraintComponent : ConstraintComponent
    {
        internal CenterDistanceConstraint _bepuConstraint = new() { SpringSettings = new(30, 5) };

        public float TargetDistance
        {
            get
            {
                return _bepuConstraint.TargetDistance;
            }
            set
            {
                _bepuConstraint.TargetDistance = value;
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

        public CenterDistanceConstraintComponent(float targetDistance, SpringSettings springSettings)
        {
            _bepuConstraint = new CenterDistanceConstraint(targetDistance, springSettings);
        }
    }
}
