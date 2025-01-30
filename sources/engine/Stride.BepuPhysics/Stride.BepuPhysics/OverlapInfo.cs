// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.BepuPhysics;

/// <summary>
/// Information about an overlap test
/// </summary>
/// <param name="Collidable">The object the test shape overlaps with</param>
/// <param name="PenetrationDirection">Direction the test shape as to move towards for it to exit out of this particular manifold</param>
/// <param name="PenetrationLength">Distance the test shape as to move towards for it to exit out of this particular manifold</param>
public readonly record struct OverlapInfo(CollidableComponent Collidable, Vector3 PenetrationDirection, float PenetrationLength);

/// <summary>
/// Unmanaged low level information about an overlap test
/// </summary>
/// <param name="PenetrationDirection">Direction the test shape as to move towards for it to exit out of this particular manifold</param>
/// <param name="PenetrationLength">Distance the test shape as to move towards for it to exit out of this particular manifold</param>
public readonly record struct OverlapInfoStack(CollidableStack CollidableStack, Vector3 PenetrationDirection, float PenetrationLength);
