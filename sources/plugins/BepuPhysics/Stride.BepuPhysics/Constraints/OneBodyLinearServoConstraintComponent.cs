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
public sealed class OneBodyLinearServoConstraintComponent : OneBodyConstraintComponent<OneBodyLinearServo>, IServo, ISpring, IOneBody
{
    public OneBodyLinearServoConstraintComponent() => BepuConstraint = new()
    {
        ServoSettings = new ServoSettings(10, 1, 1000),
        SpringSettings = new SpringSettings(30, 5)
    };

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

    public Vector3 Target
    {
        get
        {
            return BepuConstraint.Target.ToStride();
        }
        set
        {
            BepuConstraint.Target = value.ToNumeric();
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
