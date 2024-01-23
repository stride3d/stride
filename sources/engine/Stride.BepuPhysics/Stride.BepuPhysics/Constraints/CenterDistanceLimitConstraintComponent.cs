using BepuPhysics.Constraints;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Constraints;

[DataContract]
[DefaultEntityComponentProcessor(typeof(ConstraintProcessor), ExecutionMode = ExecutionMode.Runtime)]
[ComponentCategory("Bepu - Constraint")]
public sealed class CenterDistanceLimitConstraintComponent : TwoBodyConstraintComponent<CenterDistanceLimit>
{
    public CenterDistanceLimitConstraintComponent() => BepuConstraint = new() { SpringSettings = new SpringSettings(30, 5) };

    public float MinimumDistance
    {
        get { return BepuConstraint.MinimumDistance; }
        set
        {
            BepuConstraint.MinimumDistance = value;
            ConstraintData?.TryUpdateDescription();
        }
    }

    public float MaximumDistance
    {
        get { return BepuConstraint.MaximumDistance; }
        set
        {
            BepuConstraint.MaximumDistance = value;
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