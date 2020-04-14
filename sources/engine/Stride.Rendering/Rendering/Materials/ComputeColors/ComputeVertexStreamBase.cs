// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Shaders;

namespace Xenko.Rendering.Materials.ComputeColors
{
    /// <summary>
    /// Class ComputeVertexStreamBase.
    /// </summary>
    public abstract class ComputeVertexStreamBase : ComputeNode, IComputeVertexStream
    {
        [DataMember(10)]
        [NotNull]
        [InlineProperty]
        public IVertexStreamDefinition Stream { get; set; }

        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            var channel = GetColorChannelAsString();
            return Stream == null || string.IsNullOrWhiteSpace(Stream.GetSemanticName()) ? new ShaderClassSource("ComputeColor") : new ShaderClassSource("ComputeColorFromStream", Stream.GetSemanticName(), channel);
        }

        protected abstract string GetColorChannelAsString();
    }
}
