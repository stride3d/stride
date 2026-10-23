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
public sealed class TwistServoConstraintComponent : TwoBodyConstraintComponent<TwistServo>, IServo, ISpring
{
    public TwistServoConstraintComponent() => BepuConstraint = new()
    {
        LocalBasisA = Quaternion.Identity,
        LocalBasisB = Quaternion.Identity,
        SpringSettings = new SpringSettings(30, 5),
        ServoSettings = new ServoSettings(10, 1, 1000)
    };

    /// <summary>
    /// Local space basis attached to body A against which to measure body B's transformed axis.
    /// Expressed as a 3x3 rotation matrix, the X axis corresponds with 0 degrees, the Y axis corresponds to 90 degrees, and the -Z axis is the twist axis.
    /// When viewed along the twist axis, positive change in angle causes counterclockwise rotation in right handed coordinates.
    /// </summary>
    /// <userdoc>
    /// Local space basis attached to body A against which to measure body B's transformed axis.
    /// Expressed as a 3x3 rotation matrix, the X axis corresponds with 0 degrees, the Y axis corresponds to 90 degrees, and the -Z axis is the twist axis.
    /// When viewed along the twist axis, positive change in angle causes counterclockwise rotation in right handed coordinates.
    /// </userdoc>
    public Quaternion LocalBasisA
    {
        get
        {
            return BepuConstraint.LocalBasisA.ToStride();
        }
        set
        {
            BepuConstraint.LocalBasisA = value.ToNumeric();
            TryUpdateDescription();
        }
    }

    /// <summary>
    /// Local space basis attached to body B that will be measured against body A's basis.
    /// Expressed as a 3x3 rotation matrix, the transformed X axis will be measured against A's X and Y axes.
    /// The Z axis is the twist axis.
    /// </summary>
    /// <userdoc>
    /// Local space basis attached to body B that will be measured against body A's basis.
    /// Expressed as a 3x3 rotation matrix, the transformed X axis will be measured against A's X and Y axes.
    /// The Z axis is the twist axis.
    /// </userdoc>
    public Quaternion LocalBasisB
    {
        get
        {
            return BepuConstraint.LocalBasisB.ToStride();
        }
        set
        {
            BepuConstraint.LocalBasisB = value.ToNumeric();
            TryUpdateDescription();
        }
    }

    /// <summary>
    /// Target angle between B's axis to measure and A's measurement axis.
    /// </summary>
    public float TargetAngle
    {
        get { return BepuConstraint.TargetAngle; }
        set
        {
            BepuConstraint.TargetAngle = value;
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

    /// <inheritdoc/>
    public float ServoMaximumSpeed
    {
        get
        {
            return BepuConstraint.ServoSettings.MaximumSpeed;
        }
        set
        {
            BepuConstraint.ServoSettings.MaximumSpeed = value;
            TryUpdateDescription();
        }
    }

    /// <inheritdoc/>
    public float ServoBaseSpeed
    {
        get
        {
            return BepuConstraint.ServoSettings.BaseSpeed;
        }
        set
        {
            BepuConstraint.ServoSettings.BaseSpeed = value;
            TryUpdateDescription();
        }
    }

    /// <inheritdoc/>
    public float ServoMaximumForce
    {
        get
        {
            return BepuConstraint.ServoSettings.MaximumForce;
        }
        set
        {
            BepuConstraint.ServoSettings.MaximumForce = value;
            TryUpdateDescription();
        }
    }
}
