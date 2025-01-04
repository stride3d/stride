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
[ComponentCategory("Physics - Bepu Constraint")]
public sealed class SwingLimitConstraintComponent : TwoBodyConstraintComponent<SwingLimit>
{
    public SwingLimitConstraintComponent() => BepuConstraint = new() { SpringSettings = new SpringSettings(30, 5) };

    public Vector3 AxisLocalA
    {
        get
        {
            return BepuConstraint.AxisLocalA.ToStride();
        }
        set
        {
            BepuConstraint.AxisLocalA = value.ToNumeric();
            TryUpdateDescription();
        }
    }

    public Vector3 AxisLocalB
    {
        get
        {
            return BepuConstraint.AxisLocalB.ToStride();
        }
        set
        {
            BepuConstraint.AxisLocalB = value.ToNumeric();
            TryUpdateDescription();
        }
    }

    public float MinimumDot
    {
        get { return BepuConstraint.MinimumDot; }
        set
        {
            BepuConstraint.MinimumDot = value;
            TryUpdateDescription();
        }
    }

    public float MaximumSwingAngle
    {
        get { return (float)Math.Acos(MinimumDot); }
        set
        {
            MinimumDot = (float)Math.Cos(value);
            TryUpdateDescription();
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
            TryUpdateDescription();
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
            TryUpdateDescription();
        }
    }
}
