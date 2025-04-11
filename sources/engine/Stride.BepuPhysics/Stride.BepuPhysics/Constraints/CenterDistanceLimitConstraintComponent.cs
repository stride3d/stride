// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics.Constraints;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Constraints;

/// <summary>
/// Constrains the center of two bodies to be separated by a distance within a range.
/// This constraint ensures that the distance between the center points of two bodies remains within
/// a minimum and maximum range. Unlike <see cref="DistanceLimitConstraintComponent"/>, this constraint
/// operates directly on the body centers rather than on specific points on the bodies.
/// </summary>
/// <remarks>
/// This is a specialized variant of <see cref="DistanceLimitConstraintComponent"/> that works with body centers.
/// Use this when you need to constrain the overall distance between bodies without specifying exact attachment points.
/// </remarks>
[DataContract]
[DefaultEntityComponentProcessor(typeof(ConstraintProcessor), ExecutionMode = ExecutionMode.Runtime)]
[ComponentCategory("Physics - Bepu Constraint")]
public sealed class CenterDistanceLimitConstraintComponent : TwoBodyConstraintComponent<CenterDistanceLimit>
{
    public CenterDistanceLimitConstraintComponent() => BepuConstraint = new() { SpringSettings = new SpringSettings(30, 5) };

    /// <summary>
    /// Minimum distance between the body centers.
    /// </summary>
    public float MinimumDistance
    {
        get { return BepuConstraint.MinimumDistance; }
        set
        {
            BepuConstraint.MinimumDistance = value;
            TryUpdateDescription();
        }
    }

    /// <summary>
    /// Maximum distance between the body centers.
    /// </summary>
    public float MaximumDistance
    {
        get { return BepuConstraint.MaximumDistance; }
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
