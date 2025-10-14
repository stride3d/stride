// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics.Constraints;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Constraints;

[DataContract]
[DefaultEntityComponentProcessor(typeof(ConstraintProcessor), ExecutionMode = ExecutionMode.Runtime)]
[ComponentCategory("Physics - Bepu Constraint")]
public sealed class WeldConstraintComponent : TwoBodyConstraintComponent<Weld>, ISpring
{
    public WeldConstraintComponent() => BepuConstraint = new()
    {
        LocalOrientation = Quaternion.Identity,
        SpringSettings = new SpringSettings(30, 5)
    };

    /// <summary>
    /// Offset from body A to body B in the local space of A
    /// </summary>
    /// <usedoc>
    /// Offset from body A to body B in the local space of A
    /// </usedoc>
    public Vector3 LocalOffset
    {
        get
        {
            return BepuConstraint.LocalOffset.ToStride();
        }
        set
        {
            BepuConstraint.LocalOffset = value.ToNumeric();
            TryUpdateDescription();
        }
    }

    /// <summary>
    /// Target orientation of body B in body A's local space
    /// </summary>
    /// <usedoc>
    /// Target orientation of body B in body A's local space
    /// </usedoc>
    public Quaternion LocalOrientation
    {
        get
        {
            return BepuConstraint.LocalOrientation.ToStride();
        }
        set
        {
            BepuConstraint.LocalOrientation = value.ToNumeric();
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
