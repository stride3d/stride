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
public sealed class DistanceServoConstraintComponent : TwoBodyConstraintComponent<DistanceServo>
{
    public DistanceServoConstraintComponent() => BepuConstraint = new()
    {
        SpringSettings = new SpringSettings(30, 5),
        ServoSettings = new ServoSettings(10, 1, 1000)
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

    public float TargetDistance
    {
        get
        {
            return BepuConstraint.TargetDistance;
        }
        set
        {
            BepuConstraint.TargetDistance = value;
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