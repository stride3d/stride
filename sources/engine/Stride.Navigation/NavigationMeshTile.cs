// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using DotRecast.Detour;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;

namespace Stride.Navigation
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
        public DtMeshData Data;

        /// <summary>
        /// Extracts the navigation mesh geometry from the data for this tile
        /// </summary>
        /// <param name="vertices">Vertex output list</param>
        /// <param name="indices">Index output list</param>
        /// <returns><c>true</c> on success, <c>false</c> on failure</returns>
        internal bool GetTileVertices(IList<Vector3> vertices, IList<int> indices)
        {
            if (Data == null)
                return false;

            DtMeshHeader header = Data.header;
            if (header.vertCount == 0)
                return false;
            
            int count = Data.verts.Length / 3;
            Vector3[] vertsVec = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                vertsVec[i] = new Vector3(
                    Data.verts[i * 3 + 0],
                    Data.verts[i * 3 + 1],
                    Data.verts[i * 3 + 2]
                );
            }

            for (int i = 0; i < header.vertCount; i++)
            {
                vertices.Add(vertsVec[i]);
            }

            for (int i = 0; i < header.polyCount; i++)
            {
                // Expand polygons into triangles
                var poly = Data.polys[i];
                for (int j = 0; j <= poly.vertCount - 3; j++)
                {
                    indices.Add(poly.verts[0]);
                    indices.Add(poly.verts[j + 1]);
                    indices.Add(poly.verts[j + 2]);
                }
            }

            return true;
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
                var serializer = new DtMeshDataSerializer(stream);
                if (mode == ArchiveMode.Deserialize)
                {
                    tile = new NavigationMeshTile()
                    {
                        Data = new DtMeshData()
                    };
                    tile.Data.header = serializer.ReadDtMeshHeader();
                    tile.Data.verts = serializer.ReadDtMeshVerts();
                    tile.Data.polys = serializer.ReadDtMeshPolys();
                    tile.Data.detailMeshes = serializer.ReadDtMeshDetailMeshes();
                    tile.Data.detailVerts = serializer.ReadDtMeshDetailVerts();
                    tile.Data.detailTris = serializer.ReadDtMeshDetailTris();
                    tile.Data.bvTree = serializer.ReadDtMeshBvTree();
                    tile.Data.offMeshCons = serializer.ReadDtMeshOffMeshCons();
                }

                else
                {
                    serializer.WriteDtMeshHeader(tile.Data.header);
                    serializer.WriteDtMeshVerts(tile.Data.verts);
                    serializer.WriteDtMeshPolys(tile.Data.polys);
                    serializer.WriteDtMeshDetailMeshes(tile.Data.detailMeshes);
                    serializer.WriteDtMeshDetailVerts(tile.Data.detailVerts);
                    serializer.WriteDtMeshDetailTris(tile.Data.detailTris);
                    serializer.WriteDtMeshBvTree(tile.Data.bvTree);
                    serializer.WriteDtMeshOffMeshCons(tile.Data.offMeshCons);
                }
                
            }

        }
    }
}
