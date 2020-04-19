// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Rendering.Materials.ComputeColors
{
    /// <summary>
    /// A compute node that retrieve values from the stream.
    /// </summary>
    public interface IComputeVertexStream : IComputeNode
    {
        /// <summary>
        /// Gets or sets the stream.
        /// </summary>
        /// <value>The stream.</value>
        IVertexStreamDefinition Stream { get; set; }
    }
}
