// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D

using System;

using Silk.NET.Core.Native;

namespace Stride.Graphics;

/// <summary>
///   Provides utility methods for converting between <see cref="GraphicsProfile"/> and <see cref="D3DFeatureLevel"/>.
/// </summary>
internal static class GraphicsProfileHelper
{
    /// <summary>
    ///   Converts a <see cref="GraphicsProfile"/> to its corresponding <see cref="D3DFeatureLevel"/>.
    /// </summary>
    /// <param name="profile">A <see cref="GraphicsProfile"/> to convert.</param>
    /// <returns>A Direct3D <see cref="D3DFeatureLevel"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="profile"/> is not a known profile.</exception>
    /// <remarks>
    ///   Direct3D has no feature level 11.2, as 11.2 was an API revision running on 11_1 hardware, so
    ///   <see cref="GraphicsProfile.Level_11_2"/> maps to 11_1, its real capability tier.
    /// </remarks>
    public static D3DFeatureLevel ToFeatureLevel(this GraphicsProfile profile) => profile switch
    {
        GraphicsProfile.Level_9_1 => D3DFeatureLevel.Level91,
        GraphicsProfile.Level_9_2 => D3DFeatureLevel.Level92,
        GraphicsProfile.Level_9_3 => D3DFeatureLevel.Level93,
        GraphicsProfile.Level_10_0 => D3DFeatureLevel.Level100,
        GraphicsProfile.Level_10_1 => D3DFeatureLevel.Level101,
        GraphicsProfile.Level_11_0 => D3DFeatureLevel.Level110,
        GraphicsProfile.Level_11_1 or GraphicsProfile.Level_11_2 => D3DFeatureLevel.Level111,

        _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unknown graphics profile.")
    };

    /// <summary>
    ///   Converts a <see cref="D3DFeatureLevel"/> to its corresponding <see cref="GraphicsProfile"/>.
    /// </summary>
    /// <param name="level">A <see cref="D3DFeatureLevel"/> to convert.</param>
    /// <returns>A Stride <see cref="GraphicsProfile"/>.</returns>
    public static GraphicsProfile FromFeatureLevel(D3DFeatureLevel level) => (GraphicsProfile) level;
}

#endif
