// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.ComponentModel;

using Stride.Core;

namespace Stride.Rendering.Materials.ComputeColors
{
    /// <summary>
    /// A compute scalar producing a scalar from a stream.
    /// </summary>
    [DataContract("ComputeVertexStreamScalar")]
    [Display("Vertex Stream")]
    public class ComputeVertexStreamScalar : ComputeVertexStreamBase, IComputeScalar
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ComputeVertexStreamScalar"/> class.
        /// </summary>
        public ComputeVertexStreamScalar()
        {
            Stream = new ColorVertexStreamDefinition();
            Channel = ColorChannel.R;
        }

        /// <summary>
        /// Gets or sets the channel.
        /// </summary>
        /// <value>The channel.</value>
        /// <userdoc>Selects the RGBA channel to sample from the texture.</userdoc>
        [DataMember(20)]
        [DefaultValue(ColorChannel.R)]
        public ColorChannel Channel { get; set; }

        protected override string GetColorChannelAsString()
        {
            return MaterialUtility.GetAsShaderString(Channel);
        }
    }
}
