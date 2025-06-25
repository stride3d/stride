// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Shaders;

[DataContract]
public enum ShaderStage
{
    /// <summary>
    /// Enum to specify shader stage.
    /// </summary>
        /// <summary>
        /// No shader stage defined.
        /// </summary>
        /// <summary>
        /// The vertex shader stage.
        /// </summary>
        /// <summary>
        /// The Hull shader stage.
        /// </summary>
        /// <summary>
        /// The domain shader stage.
        /// </summary>
        /// <summary>
        /// The geometry shader stage.
        /// </summary>
        /// <summary>
        /// The pixel shader stage.
        /// </summary>
        /// <summary>
        /// The compute shader stage.
        /// </summary>
    None = 0,

    Vertex = 1,

    Hull = 2,

    Domain = 3,

    Geometry = 4,

    Pixel = 5,

    Compute = 6
}
