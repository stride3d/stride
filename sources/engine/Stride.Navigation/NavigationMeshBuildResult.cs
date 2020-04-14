// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single type

using System;
using System.Collections.Generic;
using Xenko.Core.Mathematics;

namespace Xenko.Navigation
{
    /// <summary>
    /// The result of building a navigation mesh
    /// </summary>
    public class NavigationMeshBuildResult
    {
        /// <summary>
        /// <c>true</c> if the build was successful
        /// </summary>
        public bool Success = false;

        /// <summary>
        /// The generated navigation mesh
        /// </summary>
        public NavigationMesh NavigationMesh;

        /// <summary>
        /// List of updated layers + tiles
        /// </summary>
        public List<NavigationMeshLayerUpdateInfo> UpdatedLayers = new List<NavigationMeshLayerUpdateInfo>();
    }

    /// <summary>
    /// Information about what tiles changes after building a navigation mesh
    /// </summary>
    public class NavigationMeshLayerUpdateInfo
    {
        /// <summary>
        /// The id of the group
        /// </summary>
        public Guid GroupId;

        /// <summary>
        /// Coordinates of the tiles that were updated
        /// </summary>
        public List<Point> UpdatedTiles = new List<Point>();
    }
}
