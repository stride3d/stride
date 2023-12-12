using BepuPhysics.Constraints;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Components.Constraints
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ConstraintProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Constraint")]
    public sealed class CenterDistanceConstraintComponent : ConstraintComponent<CenterDistanceConstraint>
    {
        public CenterDistanceConstraintComponent() => BepuConstraint = new() { SpringSettings = new SpringSettings(30, 5) };

        public float TargetDistance
        {
            get
            {
                return BepuConstraint.TargetDistance;
            }
            set
            {
                BepuConstraint.TargetDistance = value;
                ConstraintData?.TryUpdateDescription();
            }
        }

        public float SpringFrequency
        {
            get
            {
                return BepuConstraint.SpringSettings.Frequency;
            }
            set
            {
                BepuConstraint.SpringSettings.Frequency = value;
                ConstraintData?.TryUpdateDescription();
            }
        }

        public float SpringDampingRatio
        {
            get
            {
                return BepuConstraint.SpringSettings.DampingRatio;
            }
            set
            {
                BepuConstraint.SpringSettings.DampingRatio = value;
                ConstraintData?.TryUpdateDescription();
            }
        }

    }
}
