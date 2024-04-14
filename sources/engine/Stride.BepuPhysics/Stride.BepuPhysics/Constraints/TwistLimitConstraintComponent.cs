// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

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
public sealed class TwistLimitConstraintComponent : TwoBodyConstraintComponent<TwistLimit>
{
    public TwistLimitConstraintComponent() => BepuConstraint = new() { SpringSettings = new SpringSettings(30, 5) };

    public Quaternion LocalBasisA
    {
        get
        {
            return BepuConstraint.LocalBasisA.ToStride();
        }
        set
        {
            BepuConstraint.LocalBasisA = value.ToNumeric();
            ConstraintData?.TryUpdateDescription();
        }
    }

    public Quaternion LocalBasisB
    {
        get
        {
            return BepuConstraint.LocalBasisB.ToStride();
        }
        set
        {
            BepuConstraint.LocalBasisB = value.ToNumeric();
            ConstraintData?.TryUpdateDescription();
        }
    }

    public float MinimumAngle
    {
        get { return BepuConstraint.MinimumAngle; }
        set
        {
            BepuConstraint.MinimumAngle = value;
            ConstraintData?.TryUpdateDescription();
        }
    }

    public float MaximumAngle
    {
        get { return BepuConstraint.MaximumAngle; }
        set
        {
            BepuConstraint.MaximumAngle = value;
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
