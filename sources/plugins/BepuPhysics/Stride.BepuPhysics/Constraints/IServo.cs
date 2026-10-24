// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.BepuPhysics.Constraints;

/// <summary>
/// Servos change velocity of the connected bodies to achieve a position or orientation goal
/// </summary>
public interface IServo
{
    /// <summary>
    /// Maximum speed that the constraint can try to use to move towards the target.
    /// </summary>
    /// <userdoc>
    /// Maximum speed that the constraint can try to use to move towards the target.
    /// </userdoc>
    float ServoMaximumSpeed { get; set; }
    /// <summary>
    /// Minimum speed that the constraint will try to use to move towards the target.
    /// If the speed implied by the spring configuration is higher than this, the servo will attempt to use the higher speed.
    /// Will be clamped by the MaximumSpeed.
    /// </summary>
    /// <userdoc>
    /// Minimum speed that the constraint will try to use to move towards the target.
    /// If the speed implied by the spring configuration is higher than this, the servo will attempt to use the higher speed.
    /// Will be clamped by the MaximumSpeed.
    /// </userdoc>
    float ServoBaseSpeed { get; set; }
    /// <summary>
    /// The maximum force that the constraint can apply to move towards the target.
    /// </summary>
    /// <remarks>
    /// This value is specified in terms of force: a change in momentum over time. It is approximated as a maximum impulse (an instantaneous change in momentum) on a per-substep basis. In other words, for a given velocity iteration, the constraint's impulse can be no larger than <see cref="ServoMaximumForce"/> * dt where dt is the substep duration.
    /// </remarks>
    /// <userdoc>
    /// The maximum force that the constraint can apply to move towards the target.
    /// </userdoc>
    float ServoMaximumForce { get; set; }
}
