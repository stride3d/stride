// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics.Collidables;
using NVector3 = System.Numerics.Vector3;

namespace Stride.BepuPhysics;

/// <summary>
/// Unmanaged low level information about an overlap test
/// </summary>
/// <param name="Reference">Collidable the test shape overlaps with</param>
/// <param name="Versioning">Stores the version the collidable was sampled on to conditionally skip collidables that were removed or otherwise changed while the iteration ran</param>
public readonly record struct CollidableStack(CollidableReference Reference, uint Versioning);
