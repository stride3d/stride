// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Definitions;

[StructLayout(LayoutKind.Sequential, Size = 32, Pack = 1)]
public struct RigidPose
{
    public Quaternion Orientation { get; set; }
    public Vector3 Position { get; set; }

    public RigidPose()
    {
        Orientation = Quaternion.Identity;
        Position = Vector3.Zero;
    }
    public RigidPose(Vector3 position, Quaternion orientation)
    {
        Position = position;
        Orientation = orientation;
    }

}
