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
    public sealed class LinearAxisMotorConstraintComponent : TwoBodyConstraintComponent<LinearAxisMotor>
    {
        public LinearAxisMotorConstraintComponent() => BepuConstraint = new()
        {
            Settings = new MotorSettings(1000, 10)
        };

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

        public Vector3 LocalAxis
        {
            get
            {
                return BepuConstraint.LocalAxis.ToStrideVector();
            }
            set
            {
                BepuConstraint.LocalAxis = value.ToNumericVector();
                ConstraintData?.TryUpdateDescription();
            }
        }

        public float TargetVelocity
        {
            get
            {
                return BepuConstraint.TargetVelocity;
            }
            set
            {
                BepuConstraint.TargetVelocity = value;
                ConstraintData?.TryUpdateDescription();
            }
        }

        public float MotorDamping
        {
            get
            {
                return BepuConstraint.Settings.Damping;
            }
            set
            {
                BepuConstraint.Settings.Damping = value;
                ConstraintData?.TryUpdateDescription();
            }
        }

        public float MotorMaximumForce
        {
            get
            {
                return BepuConstraint.Settings.MaximumForce;
            }
            set
            {
                BepuConstraint.Settings.MaximumForce = value;
                ConstraintData?.TryUpdateDescription();
            }
        }
    }

}
