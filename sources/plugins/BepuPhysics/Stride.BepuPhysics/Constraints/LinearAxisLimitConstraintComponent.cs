// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics.Constraints;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;
using System.Numerics;

namespace Stride.BepuPhysics.Constraints;

[DataContract]
[DefaultEntityComponentProcessor(typeof(ConstraintProcessor), ExecutionMode = ExecutionMode.Runtime)]
[ComponentCategory("Physics - Bepu Constraint")]
public sealed class LinearAxisLimitConstraintComponent : TwoBodyConstraintComponent<LinearAxisLimit>, ISpring, IWithTwoLocalOffset
{
    public LinearAxisLimitConstraintComponent() => BepuConstraint = new()
    {
        SpringSettings = new SpringSettings(30, 5)
    };

    /// <inheritdoc/>
    public Vector3 LocalOffsetA
    {
        get
        {
            return BepuConstraint.LocalOffsetA;
        }
        set
        {
            BepuConstraint.LocalOffsetA = value;
            TryUpdateDescription();
        }
    }

    /// <inheritdoc/>
    public Vector3 LocalOffsetB
    {
        get
        {
            return BepuConstraint.LocalOffsetB;
        }
        set
        {
            BepuConstraint.LocalOffsetB = value;
            TryUpdateDescription();
        }
    }

    public Vector3 LocalAxis
    {
        get
        {
            return BepuConstraint.LocalAxis;
        }
        set
        {
            BepuConstraint.LocalAxis = value;
            TryUpdateDescription();
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
            TryUpdateDescription();
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
            TryUpdateDescription();
        }
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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
