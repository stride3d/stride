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
    public record DecomposedHulls(DecomposedHulls.Hull[][] Hulls)
    {
        /// <summary>
        /// The individual convex hulls grouped by meshes.
        /// </summary>
        /// <remarks>
        /// Usage of this type does not expect writing to those arrays after construction.
        /// Hulls[0][1] would be the second hull generated from the first mesh.
        /// </remarks>
        [DataMember]
        internal Hull[][] Hulls { get; init; } = Hulls;

        /// <summary>
        /// The individual convex hulls grouped by meshes.
        /// </summary>
        /// <remarks>
        /// Hulls[0][1] would be the second hull generated from the first mesh.
        /// </remarks>
        public ReadOnlySpan<Hull[]> Meshes => Hulls;

        [DataContract]
        public record Hull(Vector3[] Points, uint[] Indices)
        {
            /// <summary>
            /// The points marking the bounds of the hull
            /// </summary>
            /// <remarks>
            /// Usage of this type does not expect writing to this array after construction.
            /// </remarks>
            [DataMember]
            internal Vector3[] Points { get; init; } = Points;

            /// <summary>
            /// Indices used to recreate a 3d mesh from those points
            /// </summary>
            /// <remarks>
            /// Usage of this type does not expect writing to this array after construction.
            /// </remarks>
            [DataMember]
            internal uint[] Indices { get; init; } = Indices;

            /// <summary>
            /// The points marking the bounds of the hull
            /// </summary>
            public ReadOnlySpan<Vector3> HullPoints => Points;

            /// <summary>
            /// Indices used to recreate a 3d mesh from those points
            /// </summary>
            public ReadOnlySpan<uint> HullIndices => Indices;
        }
    }
}
