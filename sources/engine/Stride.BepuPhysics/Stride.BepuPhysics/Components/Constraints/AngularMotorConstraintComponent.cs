using BepuPhysics.Constraints;
using Stride.BepuPhysics.Extensions;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Components.Constraints
{
    [DataContract("AngularMotorConstraint")]
    [DefaultEntityComponentProcessor(typeof(ConstraintProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Constraint")]
    public sealed class AngularMotorConstraintComponent : TwoBodyConstraintComponent<AngularMotor>
    {
        public AngularMotorConstraintComponent() => BepuConstraint = new() { Settings = new MotorSettings(1000, 10) };

        public Vector3 TargetVelocityLocalA
        {
            get
            {
                return BepuConstraint.TargetVelocityLocalA.ToStrideVector();
            }
            set
            {
                BepuConstraint.TargetVelocityLocalA = value.ToNumericVector();
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
