// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.BepuPhysics.Constraints;

/// <summary>
/// Motors try to reach a given relative velocity between the connected bodies
/// </summary>
public interface IMotor
{
    /// <summary>
    /// Maximum amount of force the motor can apply in one unit of time.
    /// </summary>
    /// <userdoc>
    /// Maximum amount of force the motor can apply in one unit of time.
    /// </userdoc>
    float MotorMaximumForce { get; set; }

    /// <summary>
    /// Mass-scaled damping constant, How rigid the constraint is.
    /// </summary>
    /// <remarks>
    /// If you want to simulate a viscous damping coefficient of D with an object of mass M, set this damping value to D / M.
    /// </remarks>
    /// <userdoc>
    /// Mass-scaled damping constant, How rigid the constraint is.
    /// </userdoc>
    float MotorDamping { get; set; }

    /// <summary>
    /// Gets or sets how soft the constraint is. Values range from 0 to infinity. Softness is inverse damping; 0 is perfectly rigid, 1 is very soft, float.MaxValue is effectively nonexistent.
    /// </summary>
    float MotorSoftness { get { return 1f / MotorDamping; } set { MotorDamping = value <= 0 ? float.MaxValue : 1f / value; } }
}
