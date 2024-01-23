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
public sealed class TwistMotorConstraintComponent : TwoBodyConstraintComponent<TwistMotor>
{
    public TwistMotorConstraintComponent() => BepuConstraint = new() { Settings = new MotorSettings(1000, 10) };

    public Vector3 LocalAxisA
    {
        get
        {
            return BepuConstraint.LocalAxisA.ToStrideVector();
        }
        set
        {
            BepuConstraint.LocalAxisA = value.ToNumericVector();
            ConstraintData?.TryUpdateDescription();
        }
    }

    public Vector3 LocalAxisB
    {
        get
        {
            return BepuConstraint.LocalAxisB.ToStrideVector();
        }
        set
        {
            BepuConstraint.LocalAxisB = value.ToNumericVector();
            ConstraintData?.TryUpdateDescription();
        }
    }

    public float TargetVelocity
    {
        get { return BepuConstraint.TargetVelocity; }
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