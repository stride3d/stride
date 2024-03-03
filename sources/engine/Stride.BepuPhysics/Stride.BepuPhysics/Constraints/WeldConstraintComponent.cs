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
public sealed class WeldConstraintComponent : TwoBodyConstraintComponent<Weld>
{
    public WeldConstraintComponent() => BepuConstraint = new() { SpringSettings = new SpringSettings(30, 5) };

    public Vector3 LocalOffset
    {
        get
        {
            return BepuConstraint.LocalOffset.ToStride();
        }
        set
        {
            BepuConstraint.LocalOffset = value.ToNumeric();
            ConstraintData?.TryUpdateDescription();
        }
    }

    public Quaternion LocalOrientation
    {
        get
        {
            return BepuConstraint.LocalOrientation.ToStride();
        }
        set
        {
            BepuConstraint.LocalOrientation = value.ToNumeric();
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
