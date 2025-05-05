// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Constraints;

public interface IWithTwoLocalOffset : ITwoBody
{
    /// <summary>
    /// Offset from the center of body A to its attachment in A's local space.
    /// </summary>
    /// <userdoc>
    /// Offset from the center of body A to its attachment in A's local space.
    /// </userdoc>
    public Vector3 LocalOffsetA { get; set; }

    /// <summary>
    /// Offset from the center of body B to its attachment in B's local space.
    /// </summary>
    /// <userdoc>
    /// Offset from the center of body B to its attachment in B's local space.
    /// </userdoc>
    public Vector3 LocalOffsetB { get; set; }
}
