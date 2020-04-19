// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Graphics
{
    /// <summary>
    /// Identify a technique for resolving texture coordinates that are outside of the boundaries of a texture.
    /// </summary>
    [DataContract("TextureAddressMode")]
    public enum TextureAddressMode 
    {
        /// <summary>
        /// Tile the texture at every (u,v) integer junction. For example, for u values between 0 and 3, the texture is repeated three times. 
        /// </summary>
        Wrap = 1,

        /// <summary>
        /// Flip the texture at every (u,v) integer junction. For u values between 0 and 1, for example, the texture is addressed normally; between 1 and 2, the texture is flipped (mirrored); between 2 and 3, the texture is normal again; and so on.  
        /// </summary>
        Mirror = 2,

        /// <summary>
        /// Texture coordinates outside the range [0.0, 1.0] are set to the texture color at 0.0 or 1.0, respectively. 
        /// </summary>
        Clamp = 3,

        /// <summary>
        /// Texture coordinates outside the range [0.0, 1.0] are set to the border color specified in <see cref="SamplerState"/> or HLSL code. 
        /// </summary>
        Border = 4,

        /// <summary>
        /// Similar to D3D11_TEXTURE_ADDRESS_MIRROR and D3D11_TEXTURE_ADDRESS_CLAMP. Takes the absolute value of the texture coordinate (thus, mirroring around 0), and then clamps to the maximum value. 
        /// </summary>
        MirrorOnce = 5,
    }
}
