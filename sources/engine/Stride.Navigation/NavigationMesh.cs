// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Core.Serialization.Serializers;

namespace Stride.Navigation
{
    /// <summary>
    /// A Navigation Mesh, can be used for pathfinding.
    /// </summary>
    [DataContract("NavigationMesh")]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<NavigationMesh>), Profile = "Content")]
    [DataSerializer(typeof(NavigationMeshSerializer))]
    [ContentSerializer(typeof(DataContentSerializer<NavigationMesh>))]
    public class NavigationMesh
    {
        // Stores the cached build information to allow incremental building on this navigation mesh
        internal NavigationMeshCache Cache;

        internal float TileSize;

        internal float CellSize;
        
        internal Dictionary<Guid, NavigationMeshLayer> LayersInternal = new Dictionary<Guid, NavigationMeshLayer>();

        /// <summary>
        /// The layers of this navigation mesh, there will be one layer for each enabled group that a navigation mesh is selected to build for
        /// </summary>
        public IReadOnlyDictionary<Guid, NavigationMeshLayer> Layers => LayersInternal;

        internal class NavigationMeshSerializer : DataSerializer<NavigationMesh>
        {
            private DictionarySerializer<Guid, NavigationMeshLayer> layersSerializer;
            private DataSerializer<NavigationMeshCache> cacheSerializer;

            public override void Initialize(SerializerSelector serializerSelector)
            {
                cacheSerializer = MemberSerializer<NavigationMeshCache>.Create(serializerSelector, false);
                
                layersSerializer = new DictionarySerializer<Guid, NavigationMeshLayer>();
                layersSerializer.Initialize(serializerSelector);
            }

            public override void PreSerialize(ref NavigationMesh obj, ArchiveMode mode, SerializationStream stream)
            {
                base.PreSerialize(ref obj, mode, stream);
                if (mode == ArchiveMode.Deserialize)
                {
                    if (obj == null)
                        obj = new NavigationMesh();
                }
            }

            public override void Serialize(ref NavigationMesh obj, ArchiveMode mode, SerializationStream stream)
            {
                // Serialize tile size because it is needed
                stream.Serialize(ref obj.TileSize);
                stream.Serialize(ref obj.CellSize);

                cacheSerializer.Serialize(ref obj.Cache, mode, stream);
                layersSerializer.Serialize(ref obj.LayersInternal, mode, stream);
            }
        }
    }
}
