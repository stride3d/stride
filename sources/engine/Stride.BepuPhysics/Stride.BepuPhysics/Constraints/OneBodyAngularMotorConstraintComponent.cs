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
public sealed class OneBodyAngularMotorConstraintComponent : OneBodyConstraintComponent<OneBodyAngularMotor>
{
    public OneBodyAngularMotorConstraintComponent() => BepuConstraint = new()
    {
        Settings = new MotorSettings(10000000, 0.02f)
    };

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