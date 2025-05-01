// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.BepuPhysics.Constraints;

/// <summary>
/// Servos change velocity of the connected bodies to achieve a position or orientation goal
/// </summary>
public interface IServo
{
    float ServoMaximumSpeed { get; set; }
    float ServoBaseSpeed { get; set; }
    float ServoMaximumForce { get; set; }
}
