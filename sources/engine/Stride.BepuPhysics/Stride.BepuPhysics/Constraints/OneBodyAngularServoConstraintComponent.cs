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
/// Constrains a single body to a target orientation in space, controlling both rotation and tilt.
/// <para>This constraint applies torque to make a body gradually align with a specific target orientation.
/// It works like a spring system that pulls the body's orientation (rotation around any axis, including tilt)
/// toward the specified target orientation.</para>
/// <para>Common uses include:</para>
/// <list type="bullet">
/// <item>Stabilizing objects to maintain a specific orientation</item>
/// <item>Creating motorized joints that rotate/tilt objects to desired angles</item>
/// <item>Simulating gyroscopic or magnetic orientation control</item>
/// </list>
/// <para>The spring and servo settings control how quickly and forcefully the body moves toward the target orientation.</para>
/// </summary>
[DataContract]
[DefaultEntityComponentProcessor(typeof(ConstraintProcessor), ExecutionMode = ExecutionMode.Runtime)]
[ComponentCategory("Physics - Bepu Constraint")]
public sealed class OneBodyAngularServoConstraintComponent : OneBodyConstraintComponent<OneBodyAngularServo>, IServo, ISpring, IOneBody
{
    public OneBodyAngularServoConstraintComponent() => BepuConstraint = new()
    {
        TargetOrientation = Quaternion.Identity,
        ServoSettings = new ServoSettings(),
        SpringSettings = new SpringSettings(30, 5)
    };

    public Quaternion TargetOrientation
    {
        get
        {
            return BepuConstraint.TargetOrientation.ToStride();
        }
        set
        {
            BepuConstraint.TargetOrientation = value.ToNumeric();
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
