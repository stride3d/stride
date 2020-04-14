// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Core.Serialization.Serializers;

namespace Stride.Navigation
{
    /// <summary>
    /// Layer containing built tiles for a single <see cref="NavigationAgentSettings"/>
    /// </summary>
    [DataSerializer(typeof(NavigationMeshLayerSerializer))]
    [ContentSerializer(typeof(DataContentSerializer<NavigationMesh>))]
    public class NavigationMeshLayer
    {
        // Backing field of Tiles
        internal Dictionary<Point, NavigationMeshTile> TilesInternal = new Dictionary<Point, NavigationMeshTile>();

        /// <summary>
        /// Contains all the built tiles mapped to their tile coordinates
        /// </summary>
        public IReadOnlyDictionary<Point, NavigationMeshTile> Tiles => TilesInternal;

        /// <summary>
        /// Tries to find a built tile inside this layer
        /// </summary>
        /// <param name="tileCoordinate">The coordinate of the tile</param>
        /// <returns>The found tile or null</returns>
        public NavigationMeshTile FindTile(Point tileCoordinate)
        {
            NavigationMeshTile foundTile;
            if (TilesInternal.TryGetValue(tileCoordinate, out foundTile))
                return foundTile;
            return null;
        }

        internal class NavigationMeshLayerSerializer : DataSerializer<NavigationMeshLayer>
        {
            private DictionarySerializer<Point, NavigationMeshTile> tilesSerializer;

            public override void Initialize(SerializerSelector serializerSelector)
            {
                tilesSerializer = new DictionarySerializer<Point, NavigationMeshTile>();
                tilesSerializer.Initialize(serializerSelector);
            }

            public override void PreSerialize(ref NavigationMeshLayer obj, ArchiveMode mode, SerializationStream stream)
            {
                base.PreSerialize(ref obj, mode, stream);
                if (mode == ArchiveMode.Deserialize)
                {
                    if (obj == null)
                        obj = new NavigationMeshLayer();
                }
            }

            public override void Serialize(ref NavigationMeshLayer obj, ArchiveMode mode, SerializationStream stream)
            {
                tilesSerializer.Serialize(ref obj.TilesInternal, mode, stream);
            }
        }
    }
}
