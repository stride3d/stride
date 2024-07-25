// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using BepuPhysics.Collidables;

namespace Stride.BepuPhysics;
//Table of truth. If the number in the table is present on X/Y (inside '()') collision occur except if result is "0".
//! indicate no collision
//                  1111 1111 (255)     0000 0001 (1  )      0000 0011 (3  )        0000 0101 (5  )     0000 0000 (0)
//1111 1111 (255)      255                  1                    3                      5                   0!
//0000 0001 (1  )       1                   1                    1                      1                   0!
//0000 0011 (3  )       3                   1                    3                      1!                  0!
//0000 0101 (5  )       5                   1                    1!                     5                   0!
//0000 1001 (9  )       9                   1                    1!                     1!                  0!
//0000 1010 (10 )       10                  0!                   2!                     0!                  0!

/// <summary>
/// Collision occurs when mask 'a' contains all the layers set in 'b' or 'b' contains all the layers of 'a'.
/// </summary>
[Flags]
public enum CollisionMask : ushort
{
    /// <summary> No contact generated nor collisions reported between any object and this one </summary>
    NoCollision = 0b0000_0000__0000_0000,
    Layer0      = 0b0000_0000__0000_0001,
    Layer1      = 0b0000_0000__0000_0010,
    Layer2      = 0b0000_0000__0000_0100,
    Layer3      = 0b0000_0000__0000_1000,
    Layer4      = 0b0000_0000__0001_0000,
    Layer5      = 0b0000_0000__0010_0000,
    Layer6      = 0b0000_0000__0100_0000,
    Layer7      = 0b0000_0000__1000_0000,
    Layer8      = 0b0000_0001__0000_0000,
    Layer9      = 0b0000_0010__0000_0000,
    Layer10     = 0b0000_0100__0000_0000,
    Layer11     = 0b0000_1000__0000_0000,
    Layer12     = 0b0001_0000__0000_0000,
    Layer13     = 0b0010_0000__0000_0000,
    Layer14     = 0b0100_0000__0000_0000,
    Layer15     = 0b1000_0000__0000_0000,
    /// <summary> Collisions will occur between this one and any other colliders unless they have their mask set to <see cref="NoCollision"/> </summary>
    Everything  = 0b1111_1111__1111_1111,
}

public static class CollisionMaskExtension
{
    /// <summary>
    /// Collision occurs when 'a' contains all the layers set in 'b' or 'b' contains all the layers of 'a'.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Collide(this CollisionMask a, CollisionMask b)
    {
        CollisionMask and = a & b;
        return and != 0 && (and == a || and == b);
    }

    /// <summary>
    /// Whether a test with this mask against this collidable should be performed or ignored
    /// </summary>
    /// <returns>True when it should be performed, false when it should be ignored</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AllowTest(this CollisionMask collisionMask, CollidableReference collidable, BepuSimulation sim)
    {
        var component = sim.GetComponent(collidable);
        return collisionMask.Collide(component.CollisionMask);
    }
}
