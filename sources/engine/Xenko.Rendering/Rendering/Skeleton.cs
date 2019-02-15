// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;

namespace Xenko.Rendering
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
