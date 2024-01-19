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
public sealed class OneBodyLinearServoConstraintComponent : OneBodyConstraintComponent<OneBodyLinearServo>
{
    public OneBodyLinearServoConstraintComponent() => BepuConstraint = new()
    {
        ServoSettings = new ServoSettings(100, 1, 1000),
        SpringSettings = new SpringSettings(30, 5)
    };

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

    public Vector3 Target
    {
        get
        {
            return BepuConstraint.Target.ToStrideVector();
        }
        set
        {
            BepuConstraint.Target = value.ToNumericVector();
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

    public float ServoMaximumSpeed
    {
        get
        {
            return BepuConstraint.ServoSettings.MaximumSpeed;
        }
        set
        {
            BepuConstraint.ServoSettings.MaximumSpeed = value;
            ConstraintData?.TryUpdateDescription();
        }
    }

    public float ServoBaseSpeed
    {
        get
        {
            return BepuConstraint.ServoSettings.BaseSpeed;
        }
        set
        {
            BepuConstraint.ServoSettings.BaseSpeed = value;
            ConstraintData?.TryUpdateDescription();
        }
    }

    public float ServoMaximumForce
    {
        get
        {
            return BepuConstraint.ServoSettings.MaximumForce;
        }
        set
        {
            BepuConstraint.ServoSettings.MaximumForce = value;
            ConstraintData?.TryUpdateDescription();
        }
    }
}