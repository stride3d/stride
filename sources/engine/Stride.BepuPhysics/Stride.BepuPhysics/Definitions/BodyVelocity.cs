// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Definitions;

[StructLayout(LayoutKind.Explicit, Size = 32)]
public struct BodyVelocity
{
    /// <summary>
    /// Linear velocity associated with the body.
    /// </summary>
    [FieldOffset(0)]
    public Vector3 Linear;

    /// <summary>
    /// Angular velocity associated with the body.
    /// </summary>
    [FieldOffset(16)]
    public Vector3 Angular;

    public BodyVelocity()
    {
        Linear = Vector3.Zero;
        Angular = Vector3.Zero;
    }

    /// <summary>
    /// Creates a new set of body velocities.
    /// </summary>
    /// <param name="linear">Linear velocity to use for the body.</param>
    /// <param name="angular">Angular velocity to use for the body.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BodyVelocity(Vector3 linear, Vector3 angular)
    {
        Linear = linear;
        Angular = angular;
    }
   
}
