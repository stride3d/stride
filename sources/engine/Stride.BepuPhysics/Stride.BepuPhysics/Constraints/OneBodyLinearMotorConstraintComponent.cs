using BepuPhysics.Constraints;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Constraints;

[DataContract]
[DefaultEntityComponentProcessor(typeof(ConstraintProcessor), ExecutionMode = ExecutionMode.Runtime)]
[ComponentCategory("Bepu - Constraint")]
public sealed class OneBodyLinearMotorConstraintComponent : OneBodyConstraintComponent<OneBodyLinearMotor>
{
    public OneBodyLinearMotorConstraintComponent() => BepuConstraint = new() { Settings = new MotorSettings(1000, 10) };

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

    public Vector3 TargetVelocity
    {
        get
        {
            return BepuConstraint.TargetVelocity.ToStrideVector();
        }
        set
        {
            BepuConstraint.TargetVelocity = value.ToNumericVector();
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