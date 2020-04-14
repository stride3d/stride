// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Globalization;

namespace Stride.Rendering.Materials.ComputeColors
{
    /// <summary>
    /// An indexed stream. 
    /// </summary>
    public abstract class IndexedVertexStreamDefinition : VertexStreamDefinitionBase
    {
        protected IndexedVertexStreamDefinition()
        {
        }

        protected IndexedVertexStreamDefinition(int index)
        {
            Index = index;
        }

        /// <summary>
        /// Gets or sets the index.
        /// </summary>
        /// <value>The index.</value>
        /// <userdoc>The index in the named stream</userdoc>
        [DefaultValue(0)]
        public int Index { get; set; }

        protected abstract string GetSemanticPrefixName();

        public override string GetSemanticName()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}{1}", GetSemanticPrefixName(), Index);
        }
    }
}
