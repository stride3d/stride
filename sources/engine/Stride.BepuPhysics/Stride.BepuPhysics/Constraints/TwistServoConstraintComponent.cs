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
public sealed class TwistServoConstraintComponent : TwoBodyConstraintComponent<TwistServo>
{
    public TwistServoConstraintComponent() => BepuConstraint = new() { SpringSettings = new SpringSettings(30, 5), ServoSettings = new ServoSettings(10, 1, 1000) };

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

    public float TargetAngle
    {
        get { return BepuConstraint.TargetAngle; }
        set
        {
            BepuConstraint.TargetAngle = value;
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
