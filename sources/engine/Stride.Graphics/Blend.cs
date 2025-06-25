// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Graphics;

[DataContract]
public enum Blend
{
    /// <summary>
    /// Blend option. A blend option identifies the data source and an optional pre-blend operation.
    /// </summary>
    /// <remarks>
    /// Blend options are specified in a <see cref="BlendStateDescription"/>.
    /// </remarks>
        /// <summary>
        /// The data source is the color black (0, 0, 0, 0). No pre-blend operation.
        /// </summary>
        /// <summary>
        /// The data source is the color white (1, 1, 1, 1). No pre-blend operation.
        /// </summary>
        /// <summary>
        /// The data source is color data (RGB) from a pixel shader. No pre-blend operation.
        /// </summary>
        /// <summary>
        /// The data source is color data (RGB) from a pixel shader. The pre-blend operation inverts the data, generating 1 - RGB.
        /// </summary>
        /// <summary>
        /// The data source is alpha data (A) from a pixel shader. No pre-blend operation.
        /// </summary>
        /// <summary>
        /// The data source is alpha data (A) from a pixel shader. The pre-blend operation inverts the data, generating 1 - A.
        /// </summary>
        /// <summary>
        /// The data source is alpha data from a rendertarget. No pre-blend operation.
        /// </summary>
        /// <summary>
        /// The data source is alpha data from a rendertarget. The pre-blend operation inverts the data, generating 1 - A.
        /// </summary>
        /// <summary>
        /// The data source is color data from a rendertarget. No pre-blend operation.
        /// </summary>
        /// <summary>
        /// The data source is color data from a rendertarget. The pre-blend operation inverts the data, generating 1 - RGB.
        /// </summary>
        /// <summary>
        /// The data source is alpha data from a pixel shader. The pre-blend operation clamps the data to 1 or less.
        /// </summary>
        /// <summary>
        /// The data source is the blend factor set with <see cref="Stride.Graphics.BlendStates"/>. No pre-blend operation.
        /// </summary>
        /// <summary>
        /// The data source is the blend factor set with <see cref="Stride.Graphics.BlendStates"/>. The pre-blend operation inverts the blend factor, generating 1 - blend_factor.
        /// </summary>
        /// <summary>
        /// The data sources are both color data output by a pixel shader. There is no pre-blend operation. This options supports dual-source color blending.
        /// </summary>
        /// <summary>
        /// The data sources are both color data output by a pixel shader. The pre-blend operation inverts the data, generating 1 - RGB. This options supports dual-source color blending.
        /// </summary>
        /// <summary>
        /// The data sources are alpha data output by a pixel shader. There is no pre-blend operation. This options supports dual-source color blending.
        /// </summary>
        /// <summary>
        /// The data sources are alpha data output by a pixel shader. The pre-blend operation inverts the data, generating 1 - A. This options supports dual-source color blending.
        /// </summary>
    Zero = 1,

    One = 2,

    SourceColor = 3,

    InverseSourceColor = 4,

    SourceAlpha = 5,

    InverseSourceAlpha = 6,

    DestinationAlpha = 7,

    InverseDestinationAlpha = 8,

    DestinationColor = 9,

    InverseDestinationColor = 10,

    SourceAlphaSaturate = 11,

    BlendFactor = 14,

    InverseBlendFactor = 15,

    SecondarySourceColor = 16,

    InverseSecondarySourceColor = 17,

    SecondarySourceAlpha = 18,

    InverseSecondarySourceAlpha = 19
}
