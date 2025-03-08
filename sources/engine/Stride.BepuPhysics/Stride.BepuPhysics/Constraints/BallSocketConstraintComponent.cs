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
/// Creates a spherical joint (also known as a ball and socket joint) that constrains two bodies to share a connection point.
/// <para>
/// This constraint keeps a specific point on body A (defined by LocalOffsetA) coincident with a specific point
/// on body B (defined by LocalOffsetB), while still allowing full rotational freedom around the connection point.
/// </para>
/// <para>
/// Common uses include:
/// <list type="bullet">
/// <item>Character joint connections (shoulders, hips, etc.)</item>
/// <item>Chain links</item>
/// <item>Pendulums</item>
/// <item>Rag doll physics</item>
/// <item>Cloth and soft body simulation</item>
/// </list>
/// </para>
/// <para>
/// The `SpringFrequency` and `SpringDampingRatio` control how rigid or soft the connection is. Higher frequency values create stiffer
/// connections, while lower values allow more elasticity in the joint.
/// </para>
/// </summary>
[DataContract]
[DefaultEntityComponentProcessor(typeof(ConstraintProcessor), ExecutionMode = ExecutionMode.Runtime)]
[ComponentCategory("Physics - Bepu Constraint")]
public sealed class BallSocketConstraintComponent : TwoBodyConstraintComponent<BallSocket>
{
    public BallSocketConstraintComponent() => BepuConstraint = new() { SpringSettings = new SpringSettings(30, 5) };

    /// <summary>
    /// Offset from the center of body A to its attachment in A's local space.
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
    /// Offset from the center of body B to its attachment in B's local space.
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
    /// Gets or sets the target number of undamped oscillations per unit of time.
    /// </summary>
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
