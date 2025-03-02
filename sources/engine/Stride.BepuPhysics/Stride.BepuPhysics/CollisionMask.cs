// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.BepuPhysics;

/// <summary>
/// Represents a collection of layers, any layer set in this value would be colliding.
/// </summary>
[Flags]
public enum CollisionMask : uint
{
    None = 0,
    Layer0 = 1u << 0,
    Layer1 = 1u << 1,
    Layer2 = 1u << 2,
    Layer3 = 1u << 3,
    Layer4 = 1u << 4,
    Layer5 = 1u << 5,
    Layer6 = 1u << 6,
    Layer7 = 1u << 7,
    Layer8 = 1u << 8,
    Layer9 = 1u << 9,
    Layer10 = 1u << 10,
    Layer11 = 1u << 11,
    Layer12 = 1u << 12,
    Layer13 = 1u << 13,
    Layer14 = 1u << 14,
    Layer15 = 1u << 15,
    Layer16 = 1u << 16,
    Layer17 = 1u << 17,
    Layer18 = 1u << 18,
    Layer19 = 1u << 19,
    Layer20 = 1u << 20,
    Layer21 = 1u << 21,
    Layer22 = 1u << 22,
    Layer23 = 1u << 23,
    Layer24 = 1u << 24,
    Layer25 = 1u << 25,
    Layer26 = 1u << 26,
    Layer27 = 1u << 27,
    Layer28 = 1u << 28,
    Layer29 = 1u << 29,
    Layer30 = 1u << 30,
    Layer31 = 1u << 31,
    Everything = 0b1111_1111_1111_1111_1111_1111_1111_1111,
}

public static class CollisionLayersExtension
{
    /// <summary>
    /// Returns whether <paramref name="layer"/> can be found in <paramref name="mask"/>
    /// </summary>
    public static bool IsSet(this CollisionMask mask, CollisionLayer layer)
    {
        return ((int)mask & (1 << (int)layer)) != 0;
    }

    /// <summary>
    /// Returns whether <paramref name="layer"/> can be found in <paramref name="mask"/>
    /// </summary>
    public static bool IsSetIn(this CollisionLayer layer, CollisionMask mask)
    {
        return mask.IsSet(layer);
    }

    /// <summary>
    /// Returns a new <see cref="CollisionMask"/> where <paramref name="layer"/> is set
    /// </summary>
    public static CollisionMask ToMask(this CollisionLayer layer)
    {
        return (CollisionMask)(1 << (int)layer);
    }
}
