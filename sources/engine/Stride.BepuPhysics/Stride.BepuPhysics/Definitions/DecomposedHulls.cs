// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Engine.Design;
using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Definitions
{
    /// <summary>
    /// A collection of convex hulls decomposed from an input mesh
    /// </summary>
    [DataContract]
    [ContentSerializer(typeof(DataContentSerializer<DecomposedHulls>))]
    [DataSerializerGlobal(typeof(CloneSerializer<DecomposedHulls>), Profile = "Clone")]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<DecomposedHulls>), Profile = "Content")]
    public class DecomposedHulls
    {
        /// <summary>
        /// The individual convex hulls grouped by meshes.
        /// </summary>
        /// <remarks>
        /// Hulls[0][1] would be the second hull generated from the first mesh.
        /// </remarks>
        [DataMember]
        public Hull[][] Hulls { get; init; } = [];

        [DataContract]
        public class Hull
        {
            /// <summary>
            /// The points marking the bounds of the hull
            /// </summary>
            [DataMember]
            public Vector3[] Points { get; init; } = [];

            /// <summary>
            /// Indices used to recreate a 3d mesh from those points
            /// </summary>
            [DataMember]
            public uint[] Indices { get; init; } = [];
        }
    }
}
