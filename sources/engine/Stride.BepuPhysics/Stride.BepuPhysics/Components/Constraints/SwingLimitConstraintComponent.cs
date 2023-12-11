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
    public sealed class SwingLimitConstraintComponent : ConstraintComponent<SwingLimit>
    {
        public SwingLimitConstraintComponent() => BepuConstraint = new() { SpringSettings = new SpringSettings(30, 5) };

        public Vector3 AxisLocalA
        {
            get
            {
                return BepuConstraint.AxisLocalA.ToStrideVector();
            }
            set
            {
                BepuConstraint.AxisLocalA = value.ToNumericVector();
                ConstraintData?.TryUpdateDescription();
            }
        }

        public Vector3 AxisLocalB
        {
            get
            {
                return BepuConstraint.AxisLocalB.ToStrideVector();
            }
            set
            {
                BepuConstraint.AxisLocalB = value.ToNumericVector();
                ConstraintData?.TryUpdateDescription();
            }
        }

        public float MinimumDot
        {
            get { return BepuConstraint.MinimumDot; }
            set
            {
                BepuConstraint.MinimumDot = value;
                ConstraintData?.TryUpdateDescription();
            }
        }

        public float MaximumSwingAngle
        {
            get { return (float)Math.Acos(MinimumDot); }
            set
            {
                MinimumDot = (float)Math.Cos(value);
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
