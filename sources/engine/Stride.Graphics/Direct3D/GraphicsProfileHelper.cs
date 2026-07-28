// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D

using Silk.NET.Core.Native;

using Stride.Core.UnsafeExtensions;

namespace Stride.Graphics;

/// <summary>
///   Provides utility methods for converting between <see cref="GraphicsProfile"/> and <see cref="D3DFeatureLevel"/>.
/// </summary>
internal static class GraphicsProfileHelper
{
    /// <summary>
    ///   Converts an array of <see cref="GraphicsProfile"/>s to an array of corresponding <see cref="D3DFeatureLevel"/>s.
    /// </summary>
    /// <param name="profiles">An array of <see cref="GraphicsProfile"/>s to convert.</param>
    /// <returns>An array of Direct3D <see cref="D3DFeatureLevel"/>s.</returns>
    public static D3DFeatureLevel[] ToFeatureLevel(this GraphicsProfile[] profiles)
    {
        if (profiles is null or [])
            return null;

        var featureLevels = profiles.AsReadOnlySpan<GraphicsProfile, D3DFeatureLevel>().ToArray();
        return featureLevels;
    }

    /// <summary>
    ///   Converts a <see cref="GraphicsProfile"/> to its corresponding <see cref="D3DFeatureLevel"/>.
    /// </summary>
    /// <param name="profile">A <see cref="GraphicsProfile"/> to convert.</param>
    /// <returns>A Direct3D <see cref="D3DFeatureLevel"/>.</returns>
    public static D3DFeatureLevel ToFeatureLevel(this GraphicsProfile profile) => profile switch
    {
        // Direct3D has no feature level 11.2: D3D 11.2 was an API revision that runs on FL 11_1
        // hardware, not a new feature level (real levels jump 11_1 (0xB100) -> 12_0 (0xC000)). Map it
        // to its real capability tier (11_1) so device creation succeeds and the backend runs,
        // matching Vulkan; a raw cast would emit the non-existent 0xB200 and fail CreateDevice on
        // both D3D11 and D3D12. Every other GraphicsProfile value equals a real D3DFeatureLevel, so a
        // direct cast is correct for them.
        GraphicsProfile.Level_11_2 => (D3DFeatureLevel) GraphicsProfile.Level_11_1,
        _ => (D3DFeatureLevel) profile,
    };

    /// <summary>
    ///   Converts a <see cref="D3DFeatureLevel"/> to its corresponding <see cref="GraphicsProfile"/>.
    /// </summary>
    /// <param name="level">A <see cref="D3DFeatureLevel"/> to convert.</param>
    /// <returns>A Stride <see cref="GraphicsProfile"/>.</returns>
    public static GraphicsProfile FromFeatureLevel(D3DFeatureLevel level) => (GraphicsProfile) level;
}

#endif
