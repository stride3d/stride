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
public sealed class OneBodyAngularMotorConstraintComponent : OneBodyConstraintComponent<OneBodyAngularMotor>
{
    public OneBodyAngularMotorConstraintComponent() => BepuConstraint = new()
    {
        Settings = new MotorSettings(10000000, 0.02f)
    };

    public Vector3 TargetVelocity
    {
        get
        {
            return BepuConstraint.TargetVelocity.ToStride();
        }
        set
        {
            BepuConstraint.TargetVelocity = value.ToNumeric();
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
