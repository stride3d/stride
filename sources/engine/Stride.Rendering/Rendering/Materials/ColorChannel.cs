// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// A RGBA channel selected when performing texture sampling.
    /// </summary>
    [DataContract("ColorChannel")]
    public enum ColorChannel
    {
        /// <summary>
        /// The sampled color is returned as a float4(R, R, R, R)
        /// </summary>
        R,

        /// <summary>
        /// The sampled color is returned as a float4(G, G, G, G)
        /// </summary>
        G,

        /// <summary>
        /// The sampled color is returned as a float4(B, B, B, B)
        /// </summary>
        B,

        /// <summary>
        /// The sampled color is returned as a float4(A, A, A, A)
        /// </summary>
        A,
    }
}
