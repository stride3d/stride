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
public sealed class TwistMotorConstraintComponent : TwoBodyConstraintComponent<TwistMotor>, IMotor
{
    public TwistMotorConstraintComponent() => BepuConstraint = new() { Settings = new MotorSettings(1000, 10) };

    /// <summary>
    /// Local twist axis attached to body A
    /// </summary>
    /// <userdoc>
    /// Local twist axis attached to body A
    /// </userdoc>
    public Vector3 LocalAxisA
    {
        get
        {
            return BepuConstraint.LocalAxisA.ToStride();
        }
        set
        {
            BepuConstraint.LocalAxisA = value.ToNumeric();
            TryUpdateDescription();
        }
    }

    /// <summary>
    /// Local twist axis attached to body B
    /// </summary>
    /// <userdoc>
    /// Local twist axis attached to body B
    /// </userdoc>
    public Vector3 LocalAxisB
    {
        get
        {
            return BepuConstraint.LocalAxisB.ToStride();
        }
        set
        {
            BepuConstraint.LocalAxisB = value.ToNumeric();
            TryUpdateDescription();
        }
    }

    /// <summary>
    /// Goal relative twist velocity around the body axes
    /// </summary>
    /// <userdoc>
    /// Goal relative twist velocity around the body axes
    /// </userdoc>
    public float TargetVelocity
    {
        get { return BepuConstraint.TargetVelocity; }
        set
        {
            BepuConstraint.TargetVelocity = value;
            TryUpdateDescription();
        }
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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
