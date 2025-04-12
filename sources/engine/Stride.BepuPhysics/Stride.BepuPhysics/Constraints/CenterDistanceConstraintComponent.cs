// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics.Constraints;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Constraints;

/// <summary>
/// Constrains the center of two bodies to be separated by a target distance.
/// This constraint ensures that the distance between the center points of two bodies
/// attempts to match a specific target value. Unlike <see cref="DistanceServoConstraintComponent"/>,
/// this constraint operates directly on the body centers rather than on specific points on the bodies.
/// </summary>
/// <remarks>
/// This is a specialized variant of <see cref="DistanceServoConstraintComponent"/> that works with body centers.
/// Use this when you need to constrain the distance between bodies without specifying exact attachment points.
/// For a version that allows a range of distances rather than a single target value, see <see cref="CenterDistanceLimitConstraintComponent"/>.
/// </remarks>
[DataContract]
[DefaultEntityComponentProcessor(typeof(ConstraintProcessor), ExecutionMode = ExecutionMode.Runtime)]
[ComponentCategory("Physics - Bepu Constraint")]
public sealed class CenterDistanceConstraintComponent : TwoBodyConstraintComponent<CenterDistanceConstraint>
{
    public CenterDistanceConstraintComponent() => BepuConstraint = new() { SpringSettings = new SpringSettings(30, 5) };

    /// <summary>
    /// Target distance between the body centers.
    /// </summary>
    public float TargetDistance
    {
        get
        {
            return BepuConstraint.TargetDistance;
        }
        set
        {
            BepuConstraint.TargetDistance = value;
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
