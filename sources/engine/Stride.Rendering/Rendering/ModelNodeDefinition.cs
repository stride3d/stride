// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Serialization;

namespace Stride.Rendering
{
    /// <summary>
    /// Describes a single transformation node, usually in a <see cref="Model"/> node hierarchy.
    /// </summary>
    [DataContract]
    public struct ModelNodeDefinition
    {
        /// <summary>
        /// The parent node index.
        /// </summary>
        public int ParentIndex;

        /// <summary>
        /// The local transform.
        /// </summary>
        public TransformTRS Transform;

        /// <summary>
        /// The name of this node.
        /// </summary>
        public string Name;

        /// <summary>
        /// The flags of this node.
        /// </summary>
        public ModelNodeFlags Flags;

        public override string ToString()
        {
            return string.Format("Parent: {0} Name: {1}", ParentIndex, Name);
        }
    }
}
