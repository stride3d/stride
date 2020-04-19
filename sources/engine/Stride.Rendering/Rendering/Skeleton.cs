// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

namespace Stride.Rendering
{
    /// <summary>
    /// Describes hiderarchical nodes in a flattened array.
    /// </summary>
    /// <remarks>
    /// Nodes are ordered so that parents always come first, allowing for hierarchical updates in a simple loop.
    /// </remarks>
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<Skeleton>), Profile = "Content")]
    [ContentSerializer(typeof(DataContentSerializer<Skeleton>))]
    [DataContract]
    public class Skeleton
    {
        /// <summary>
        /// The nodes in this hierarchy.
        /// </summary>
        public ModelNodeDefinition[] Nodes;
    }
}
