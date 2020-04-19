// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Reflection;

namespace Stride.Navigation
{
    /// <summary>
    /// Provides settings for the navigation mesh builder to control granularity and error margins 
    /// </summary>
    [DataContract]
    [ObjectFactory(typeof(NavigationBuildSettingsFactory))]
    public struct NavigationMeshBuildSettings
    {
        /// <summary>
        /// The Height of a grid cell in the navigation mesh building steps using heightfields. 
        /// A lower number means higher precision on the vertical axis but longer build times
        /// </summary>
        [DataMemberRange(0.01, 4)]
        public float CellHeight;

        /// <summary>
        /// The Width/Height of a grid cell in the navigation mesh building steps using heightfields. 
        /// A lower number means higher precision on the horizontal axes but longer build times
        /// </summary>
        [DataMemberRange(0.01, 4)]
        public float CellSize;

        /// <summary>
        /// Tile size used for Navigation mesh tiles, the final size of a tile is CellSize*TileSize
        /// </summary>
        [DataMemberRange(8, 4096, 1, 8, 0)]
        public int TileSize;

        /// <summary>
        /// The minimum number of cells allowed to form isolated island areas
        /// </summary>
        [Display("Minimum Region Area")]
        [DataMemberRange(0, 0)]
        public int MinRegionArea;

        /// <summary>
        /// Any regions with a span count smaller than this value will, if possible, 
        /// be merged with larger regions.
        /// </summary>
        [DataMemberRange(0, 0)]
        public int RegionMergeArea;

        /// <summary>
        /// The maximum allowed length for contour edges along the border of the mesh.
        /// </summary>
        [Display("Maximum Edge Length")]
        [DataMemberRange(0, 0)]
        public float MaxEdgeLen;

        /// <summary>
        /// The maximum distance a simplfied contour's border edges should deviate from the original raw contour.
        /// </summary>
        [Display("Maximum Edge Error")]
        [DataMemberRange(0.1, 4)]
        public float MaxEdgeError;

        /// <summary>
        /// Sets the sampling distance to use when generating the detail mesh. (For height detail only.)
        /// </summary>
        [DataMemberRange(1.0, 3)]
        public float DetailSamplingDistance;

        /// <summary>
        /// The maximum distance the detail mesh surface should deviate from heightfield data. (For height detail only.)
        /// </summary>
        [Display("Maximum Detail Sampling Error")]
        [DataMemberRange(0.0, 3)]
        public float MaxDetailSamplingError;

        public bool Equals(NavigationMeshBuildSettings other)
        {
            return CellHeight.Equals(other.CellHeight) && CellSize.Equals(other.CellSize) && TileSize == other.TileSize && MinRegionArea.Equals(other.MinRegionArea) &&
                   RegionMergeArea.Equals(other.RegionMergeArea) && MaxEdgeLen.Equals(other.MaxEdgeLen) && MaxEdgeError.Equals(other.MaxEdgeError) &&
                   DetailSamplingDistance.Equals(other.DetailSamplingDistance) && MaxDetailSamplingError.Equals(other.MaxDetailSamplingError);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is NavigationMeshBuildSettings && Equals((NavigationMeshBuildSettings)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = CellHeight.GetHashCode();
                hashCode = (hashCode * 397) ^ CellSize.GetHashCode();
                hashCode = (hashCode * 397) ^ TileSize;
                hashCode = (hashCode * 397) ^ MinRegionArea.GetHashCode();
                hashCode = (hashCode * 397) ^ RegionMergeArea.GetHashCode();
                hashCode = (hashCode * 397) ^ MaxEdgeLen.GetHashCode();
                hashCode = (hashCode * 397) ^ MaxEdgeError.GetHashCode();
                hashCode = (hashCode * 397) ^ DetailSamplingDistance.GetHashCode();
                hashCode = (hashCode * 397) ^ MaxDetailSamplingError.GetHashCode();
                return hashCode;
            }
        }
    }

    public class NavigationBuildSettingsFactory : IObjectFactory
    {
        public object New(Type type)
        {
            return new NavigationMeshBuildSettings
            {
                CellHeight = 0.2f,
                CellSize = 0.3f,
                TileSize = 32,
                MinRegionArea = 2,
                RegionMergeArea = 20,
                MaxEdgeLen = 12.0f,
                MaxEdgeError = 1.3f,
                DetailSamplingDistance = 6.0f,
                MaxDetailSamplingError = 1.0f,
            };
        }
    }
}
