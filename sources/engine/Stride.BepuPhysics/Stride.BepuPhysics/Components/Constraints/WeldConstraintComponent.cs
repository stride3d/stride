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
    public sealed class WeldConstraintComponent : TwoBodyConstraintComponent<Weld>
    {
        public WeldConstraintComponent() => BepuConstraint = new() { SpringSettings = new SpringSettings(30, 5) };

        public Vector3 LocalOffset
        {
            get
            {
                return BepuConstraint.LocalOffset.ToStrideVector();
            }
            set
            {
                BepuConstraint.LocalOffset = value.ToNumericVector();
                ConstraintData?.TryUpdateDescription();
            }
        }

        public Quaternion LocalOrientation
        {
            get
            {
                return BepuConstraint.LocalOrientation.ToStrideQuaternion();
            }
            set
            {
                BepuConstraint.LocalOrientation = value.ToNumericQuaternion();
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
