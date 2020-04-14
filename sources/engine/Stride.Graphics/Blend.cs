// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;

namespace Xenko.Graphics
{
    /// <summary>
    /// Blend option. A blend option identifies the data source and an optional pre-blend operation.
    /// </summary>
    /// <remarks>
    /// Blend options are specified in a <see cref="BlendState"/>. 
    /// </remarks>
    [DataContract]
    public enum Blend
    {
        /// <summary>
        /// The data source is the color black (0, 0, 0, 0). No pre-blend operation. 
        /// </summary>
        Zero = 1,

        /// <summary>
        /// The data source is the color white (1, 1, 1, 1). No pre-blend operation. 
        /// </summary>
        One = 2,

        /// <summary>
        /// The data source is color data (RGB) from a pixel shader. No pre-blend operation. 
        /// </summary>
        SourceColor = 3,

        /// <summary>
        /// The data source is color data (RGB) from a pixel shader. The pre-blend operation inverts the data, generating 1 - RGB. 
        /// </summary>
        InverseSourceColor = 4,

        /// <summary>
        /// The data source is alpha data (A) from a pixel shader. No pre-blend operation. 
        /// </summary>
        SourceAlpha = 5,

        /// <summary>
        /// The data source is alpha data (A) from a pixel shader. The pre-blend operation inverts the data, generating 1 - A. 
        /// </summary>
        InverseSourceAlpha = 6,

        /// <summary>
        /// The data source is alpha data from a rendertarget. No pre-blend operation. 
        /// </summary>
        DestinationAlpha = 7,

        /// <summary>
        /// The data source is alpha data from a rendertarget. The pre-blend operation inverts the data, generating 1 - A. 
        /// </summary>
        InverseDestinationAlpha = 8,

        /// <summary>
        /// The data source is color data from a rendertarget. No pre-blend operation. 
        /// </summary>
        DestinationColor = 9,

        /// <summary>
        /// The data source is color data from a rendertarget. The pre-blend operation inverts the data, generating 1 - RGB. 
        /// </summary>
        InverseDestinationColor = 10,

        /// <summary>
        /// The data source is alpha data from a pixel shader. The pre-blend operation clamps the data to 1 or less. 
        /// </summary>
        SourceAlphaSaturate = 11,

        /// <summary>
        /// The data source is the blend factor set with <see cref="GraphicsDevice.BlendStates"/>. No pre-blend operation. 
        /// </summary>
        BlendFactor = 14,

        /// <summary>
        /// The data source is the blend factor set with <see cref="GraphicsDevice.SetBlendState"/>. The pre-blend operation inverts the blend factor, generating 1 - blend_factor. 
        /// </summary>
        InverseBlendFactor = 15,

        /// <summary>
        /// The data sources are both color data output by a pixel shader. There is no pre-blend operation. This options supports dual-source color blending. 
        /// </summary>
        SecondarySourceColor = 16,

        /// <summary>
        /// The data sources are both color data output by a pixel shader. The pre-blend operation inverts the data, generating 1 - RGB. This options supports dual-source color blending. 
        /// </summary>
        InverseSecondarySourceColor = 17,

        /// <summary>
        /// The data sources are alpha data output by a pixel shader. There is no pre-blend operation. This options supports dual-source color blending. 
        /// </summary>
        SecondarySourceAlpha = 18,

        /// <summary>
        /// The data sources are alpha data output by a pixel shader. The pre-blend operation inverts the data, generating 1 - A. This options supports dual-source color blending. 
        /// </summary>
        InverseSecondarySourceAlpha = 19,
    }
}
