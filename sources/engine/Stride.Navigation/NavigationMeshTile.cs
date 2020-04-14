// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization;

namespace Xenko.Navigation
{
    /// <summary>
    /// Tiles contained within <see cref="NavigationMeshLayer"/>
    /// </summary>
    [DataContract("NavigationMeshTile")]
    [DataSerializer(typeof(NavigationMeshTileSerializer))]
    public class NavigationMeshTile
    {
        /// <summary>
        /// Binary data of the built navigation mesh tile
        /// </summary>
        [DataMemberCustomSerializer]
        public byte[] Data;
        
        /// <summary>
        /// Extracts the navigation mesh geometry from the data for this tile
        /// </summary>
        /// <param name="vertices">Vertex output list</param>
        /// <param name="indices">Index ouput list</param>
        /// <returns><c>true</c> on success, <c>false</c> on failure</returns>
        internal unsafe bool GetTileVertices(IList<Vector3> vertices, IList<int> indices)
        {
            if (Data == null || Data.Length == 0)
                return false;

            fixed (byte* dataPtr = Data)
            {
                Navigation.TileHeader* header = (Navigation.TileHeader*)dataPtr;
                if (header->VertCount == 0)
                    return false;

                int headerSize = Navigation.DtAlign4(sizeof(Navigation.TileHeader));
                int vertsSize = Navigation.DtAlign4(sizeof(float) * 3 * header->VertCount);

                byte* ptr = dataPtr;
                ptr += headerSize;

                Vector3* vertexPtr = (Vector3*)ptr;
                ptr += vertsSize;
                Navigation.Poly* polyPtr = (Navigation.Poly*)ptr;

                for (int i = 0; i < header->VertCount; i++)
                {
                    vertices.Add(vertexPtr[i]);
                }

                for (int i = 0; i < header->PolyCount; i++)
                {
                    // Expand polygons into triangles
                    var poly = polyPtr[i];
                    for (int j = 0; j <= poly.VertexCount - 3; j++)
                    {
                        indices.Add(poly.Vertices[0]);
                        indices.Add(poly.Vertices[j + 1]);
                        indices.Add(poly.Vertices[j + 2]);
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Serializes individually build tiles inside navigation meshes
        /// </summary>
        internal class NavigationMeshTileSerializer : DataSerializer<NavigationMeshTile>
        {
            private DataSerializer<Vector3> pointSerializer;

            public override void Initialize(SerializerSelector serializerSelector)
            {
                pointSerializer = MemberSerializer<Vector3>.Create(serializerSelector);
            }

            public override void Serialize(ref NavigationMeshTile tile, ArchiveMode mode, SerializationStream stream)
            {
                if (mode == ArchiveMode.Deserialize)
                    tile = new NavigationMeshTile();

                int dataLength = tile.Data?.Length ?? 0;
                stream.Serialize(ref dataLength);
                if (mode == ArchiveMode.Deserialize)
                    tile.Data = new byte[dataLength];

                if (dataLength > 0)
                    stream.Serialize(tile.Data, 0, tile.Data.Length);
            }
        }
    }
}
