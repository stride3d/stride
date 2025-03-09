// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics.Constraints;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Constraints;

/// <summary>
/// Constrains points on two bodies to be separated by a distance within a specified range.
/// This constraint ensures that the distance between two points on two bodies remains within
/// a minimum and maximum range. It is useful for creating elastic or flexible connections
/// between bodies, where the distance can vary within the specified limits.
/// </summary>
/// <remarks>
/// Unlike <see cref="CenterDistanceLimitConstraintComponent"/>, this constraint allows you to specify
/// exact attachment points on each body using <see cref="LocalOffsetA"/> and <see cref="LocalOffsetB"/> properties. If you need to
/// constrain only the centers of bodies, use <see cref="CenterDistanceLimitConstraintComponent"/> instead.
/// </remarks>
[DataContract]
[DefaultEntityComponentProcessor(typeof(ConstraintProcessor), ExecutionMode = ExecutionMode.Runtime)]
[ComponentCategory("Physics - Bepu Constraint")]
public sealed class DistanceLimitConstraintComponent : TwoBodyConstraintComponent<DistanceLimit>
{
    public DistanceLimitConstraintComponent() => BepuConstraint = new() { SpringSettings = new SpringSettings(30, 5) };

    /// <summary>
    /// Local offset from the center of body A to its attachment point.
    /// </summary>
    public Vector3 LocalOffsetA
    {
        get
        {
            return BepuConstraint.LocalOffsetA.ToStride();
        }
        set
        {
            BepuConstraint.LocalOffsetA = value.ToNumeric();
            TryUpdateDescription();
        }
    }

    /// <summary>
    /// Local offset from the center of body B to its attachment point.
    /// </summary>
    public Vector3 LocalOffsetB
    {
        get
        {
            return BepuConstraint.LocalOffsetB.ToStride();
        }
        set
        {
            BepuConstraint.LocalOffsetB = value.ToNumeric();
            TryUpdateDescription();
        }
    }

    /// <summary>
    /// Minimum distance permitted between the point on A and the point on B.
    /// </summary>
    public float MinimumDistance
    {
        get
        {
            return BepuConstraint.MinimumDistance;
        }
        set
        {
            BepuConstraint.MinimumDistance = value;
            TryUpdateDescription();
        }
    }

    /// <summary>
    /// Maximum distance permitted between the point on A and the point on B.
    /// </summary>
    public float MaximumDistance
    {
        get
        {
            return BepuConstraint.MaximumDistance;
        }
        set
        {
            BepuConstraint.MaximumDistance = value;
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
