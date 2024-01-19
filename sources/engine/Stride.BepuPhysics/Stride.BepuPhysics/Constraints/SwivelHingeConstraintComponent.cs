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
public sealed class SwivelHingeConstraintComponent : TwoBodyConstraintComponent<SwivelHinge>
{
    public SwivelHingeConstraintComponent() => BepuConstraint = new() { SpringSettings = new SpringSettings(30, 5) };

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

    public Vector3 LocalSwivelAxisA
    {
        get
        {
            return BepuConstraint.LocalSwivelAxisA.ToStrideVector();
        }
        set
        {
            BepuConstraint.LocalSwivelAxisA = value.ToNumericVector();
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

    public Vector3 LocalHingeAxisB
    {
        get
        {
            return BepuConstraint.LocalHingeAxisB.ToStrideVector();
        }
        set
        {
            BepuConstraint.LocalHingeAxisB = value.ToNumericVector();
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