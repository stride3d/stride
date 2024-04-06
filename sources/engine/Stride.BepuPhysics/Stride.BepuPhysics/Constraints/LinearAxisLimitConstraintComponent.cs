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
public sealed class LinearAxisLimitConstraintComponent : TwoBodyConstraintComponent<LinearAxisLimit>
{
    public LinearAxisLimitConstraintComponent() => BepuConstraint = new()
    {
        SpringSettings = new SpringSettings(30, 5)
    };

    public Vector3 LocalOffsetA
    {
        get
        {
            return BepuConstraint.LocalOffsetA.ToStride();
        }
        set
        {
            BepuConstraint.LocalOffsetA = value.ToNumeric();
            ConstraintData?.TryUpdateDescription();
        }
    }

    public Vector3 LocalOffsetB
    {
        get
        {
            return BepuConstraint.LocalOffsetB.ToStride();
        }
        set
        {
            BepuConstraint.LocalOffsetB = value.ToNumeric();
            ConstraintData?.TryUpdateDescription();
        }
    }

    public Vector3 LocalAxis
    {
        get
        {
            return BepuConstraint.LocalAxis.ToStride();
        }
        set
        {
            BepuConstraint.LocalAxis = value.ToNumeric();
            ConstraintData?.TryUpdateDescription();
        }
    }

    public float MinimumOffset
    {
        get
        {
            return BepuConstraint.MinimumOffset;
        }
        set
        {
            BepuConstraint.MinimumOffset = value;
            ConstraintData?.TryUpdateDescription();
        }
    }

    public float MaximumOffset
    {
        get
        {
            return BepuConstraint.MaximumOffset;
        }
        set
        {
            BepuConstraint.MaximumOffset = value;
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
