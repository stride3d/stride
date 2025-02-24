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
public sealed class LinearAxisServoConstraintComponent : TwoBodyConstraintComponent<LinearAxisServo>
{
    public LinearAxisServoConstraintComponent() => BepuConstraint = new()
    {
        SpringSettings = new SpringSettings(30, 5),
        ServoSettings = new ServoSettings(10, 1, 1000)
    };

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

    public Vector3 LocalPlaneNormal
    {
        get
        {
            return BepuConstraint.LocalPlaneNormal.ToStride();
        }
        set
        {
            BepuConstraint.LocalPlaneNormal = value.ToNumeric();
            TryUpdateDescription();
        }
    }

    public float TargetOffset
    {
        get
        {
            return BepuConstraint.TargetOffset;
        }
        set
        {
            BepuConstraint.TargetOffset = value;
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
