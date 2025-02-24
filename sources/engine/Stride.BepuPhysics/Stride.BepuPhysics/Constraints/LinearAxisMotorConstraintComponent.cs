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
public sealed class LinearAxisMotorConstraintComponent : TwoBodyConstraintComponent<LinearAxisMotor>
{
    public LinearAxisMotorConstraintComponent() => BepuConstraint = new()
    {
        Settings = new MotorSettings(1000, 10)
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

    public Vector3 LocalAxis
    {
        get
        {
            return BepuConstraint.LocalAxis.ToStride();
        }
        set
        {
            BepuConstraint.LocalAxis = value.ToNumeric();
            TryUpdateDescription();
        }
    }

    public float TargetVelocity
    {
        get
        {
            return BepuConstraint.TargetVelocity;
        }
        set
        {
            BepuConstraint.TargetVelocity = value;
            TryUpdateDescription();
        }
    }

    public float MotorDamping
    {
        get
        {
            return BepuConstraint.Settings.Damping;
        }
        set
        {
            BepuConstraint.Settings.Damping = value;
            TryUpdateDescription();
        }
    }

    public float MotorMaximumForce
    {
        get
        {
            return BepuConstraint.Settings.MaximumForce;
        }
        set
        {
            BepuConstraint.Settings.MaximumForce = value;
            TryUpdateDescription();
        }
    }
}
