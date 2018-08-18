// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;

namespace Xenko.Rendering.Materials.ComputeColors
{
    /// <summary>
    /// A color coming from a vertex stream
    /// </summary>
    /// <userdoc>A color coming from a vertex stream.</userdoc>
    [DataContract("ColorVertexStreamDefinition")]
    [Display("Color Vertex Stream")]
    public class ColorVertexStreamDefinition : IndexedVertexStreamDefinition
    {
        private const string SemanticName = "COLOR";

        private static readonly int HashCode = SemanticName.GetHashCode();

        public override int GetHashCode() => HashCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorVertexStreamDefinition"/> class.
        /// </summary>
        public ColorVertexStreamDefinition()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorVertexStreamDefinition"/> class.
        /// </summary>
        /// <param name="index">The index.</param>
        public ColorVertexStreamDefinition(int index)
            : base(index)
        {
        }

        protected override string GetSemanticPrefixName()
        {
            return SemanticName;
        }

        public override int GetSemanticNameHash()
        {
            return HashCode;
        }
    }
}
