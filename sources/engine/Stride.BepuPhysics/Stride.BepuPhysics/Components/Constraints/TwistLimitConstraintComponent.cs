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
    public sealed class TwistLimitConstraintComponent : ConstraintComponent<TwistLimit>
    {
        public TwistLimitConstraintComponent() => BepuConstraint = new() { SpringSettings = new SpringSettings(30, 5) };

        public Quaternion LocalBasisA
        {
            get
            {
                return BepuConstraint.LocalBasisA.ToStrideQuaternion();
            }
            set
            {
                BepuConstraint.LocalBasisA = value.ToNumericQuaternion();
                ConstraintData?.TryUpdateDescription();
            }
        }

        public Quaternion LocalBasisB
        {
            get
            {
                return BepuConstraint.LocalBasisB.ToStrideQuaternion();
            }
            set
            {
                BepuConstraint.LocalBasisB = value.ToNumericQuaternion();
                ConstraintData?.TryUpdateDescription();
            }
        }

        public float MinimumAngle
        {
            get { return BepuConstraint.MinimumAngle; }
            set
            {
                BepuConstraint.MinimumAngle = value;
                ConstraintData?.TryUpdateDescription();
            }
        }

        public float MaximumAngle
        {
            get { return BepuConstraint.MaximumAngle; }
            set
            {
                BepuConstraint.MaximumAngle = value;
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
