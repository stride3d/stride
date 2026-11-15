// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Importer.Common
{
    /// <summary>
    /// How an imported material uses a texture, which decides the type of the texture asset created for it.
    /// </summary>
    public enum TextureRole
    {
        /// <summary>
        /// A color texture, such as a base color or emissive map, sampled in sRGB.
        /// </summary>
        Color,

        /// <summary>
        /// A color texture whose alpha blends the material, sampled in sRGB with its color premultiplied by alpha.
        /// </summary>
        BlendedColor,

        /// <summary>
        /// A tangent-space normal map.
        /// </summary>
        Normal,

        /// <summary>
        /// A data texture, such as a packed occlusion/roughness/metallic map, sampled as linear values.
        /// </summary>
        Data,
    }
}
