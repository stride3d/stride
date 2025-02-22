// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Definitions;

public struct ShapeTransform
{
    public Vector3 PositionLocal = Vector3.Zero;
    public Quaternion RotationLocal = Quaternion.Identity;
    public Vector3 Scale = Vector3.One;

    public ShapeTransform()
    {
    }
}