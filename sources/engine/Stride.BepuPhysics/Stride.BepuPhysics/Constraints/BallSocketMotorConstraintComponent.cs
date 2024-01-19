using BepuPhysics.Constraints;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Constraints;

[DataContract]
[DefaultEntityComponentProcessor(typeof(ConstraintProcessor), ExecutionMode = ExecutionMode.Runtime)]
[ComponentCategory("Bepu - Constraint")]
public sealed class BallSocketMotorConstraintComponent : TwoBodyConstraintComponent<BallSocketMotor>
{
    public BallSocketMotorConstraintComponent() => BepuConstraint = new()
    {
        Settings = new MotorSettings(1000, 10)
    };

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