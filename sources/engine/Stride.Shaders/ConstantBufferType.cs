// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;

namespace Xenko.Shaders
{
    /// <summary>
    /// Describes the type of constant buffer.
    /// </summary>
    [DataContract]
    public enum ConstantBufferType
    {
        /// <summary>
        /// An unknown buffer.
        /// </summary>
        Unknown,

        /// <summary>
        /// A standard constant buffer
        /// </summary>
        ConstantBuffer,

        /// <summary>
        /// A texture buffer
        /// </summary>
        TextureBuffer,
    }
}
