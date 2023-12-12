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
    public sealed class BallSocketConstraintComponent : ConstraintComponent<BallSocket>
    {
        public BallSocketConstraintComponent() => BepuConstraint = new() { SpringSettings = new SpringSettings(30, 5) };

        public Vector3 LocalOffsetA
        {
            get
            {
                return BepuConstraint.LocalOffsetA.ToStrideVector();
            }
            set
            {
                BepuConstraint.LocalOffsetA = value.ToNumericVector();
                ConstraintData?.TryUpdateDescription();
            }
        }

        public Vector3 LocalOffsetB
        {
            get
            {
                return BepuConstraint.LocalOffsetB.ToStrideVector();
            }
            set
            {
                BepuConstraint.LocalOffsetB = value.ToNumericVector();
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
