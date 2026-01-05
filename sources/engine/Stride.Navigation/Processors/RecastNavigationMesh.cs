// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using DotRecast.Detour;
using Stride.Core.Mathematics;

namespace Stride.Navigation.Processors
{
    /// <summary>
    /// Recast native navigation mesh wrapper
    /// </summary>
    public class RecastNavigationMesh(NavigationMesh navigationMesh)
    {
        private readonly InternalNavigationMesh navmesh = new(navigationMesh.TileSize * navigationMesh.CellSize);
        private readonly HashSet<Point> tileCoordinates = [];

        /// <summary>
        /// Adds or replaces a tile in the navigation mesh
        /// </summary>
        /// <remarks>The coordinate of the tile is embedded inside the tile data header</remarks>
        public bool AddOrReplaceTile(DtMeshData data)
        {
            var coord = new Point(data.header.x, data.header.y);

            // Remove old tile if it exists
            _ = RemoveTile(coord);

            tileCoordinates.Add(coord);
            return navmesh.LoadTile(data);
        }

        /// <summary>
        /// Removes a tile at given coordinate
        /// </summary>
        /// <param name="coord">The tile coordinate</param>
        public bool RemoveTile(Point coord)
        {
            return tileCoordinates.Remove(coord) && navmesh.RemoveTile(coord);
        }

        /// <summary>
        /// Performs a raycast on the navigation mesh to perform line of sight or similar checks
        /// </summary>
        /// <param name="start">Starting point</param>
        /// <param name="end">Ending point</param>
        /// <param name="querySettings">Advanced settings to be provided to the navigation mesh query</param>
        /// <returns>The found raycast hit if <see cref="NavigationRaycastResult.Hit"/> is true</returns>
        public NavigationRaycastResult Raycast(Vector3 start, Vector3 end, NavigationQuerySettings querySettings)
        {
            RaycastQuery query = new()
            {
                Source = start,
                Target = end,
                MaxPathPoints = querySettings.MaxPathPoints,
                FindNearestPolyExtent = querySettings.FindNearestPolyExtent
            };

            navmesh.DoRaycastQuery(query, out var queryResult);
            if (!queryResult.Hit)
                return new() { Hit = false };

            return queryResult with { Hit = true };
        }

        /// <summary>
        /// Finds a path from point <paramref cref="start"/> to <paramref cref="end"/>
        /// </summary>
        /// <param name="start">The starting location of the pathfinding query</param>
        /// <param name="end">The ending location of the pathfinding query</param>
        /// <param name="querySettings">Advanced settings to be provided to the navigation mesh query</param>
        /// <param name="path">The waypoints for the found path, if any (at least 2 if a path was found)</param>
        /// <returns>The found path points or null</returns>
        public bool TryFindPath(Vector3 start, Vector3 end, ICollection<Vector3> path, NavigationQuerySettings querySettings)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (navmesh == null)
                return false;

            PathFindQuery query;
            query.Source = start;
            query.Target = end;
            query.MaxPathPoints = querySettings.MaxPathPoints;
            query.FindNearestPolyExtent = querySettings.FindNearestPolyExtent;
            PathFindResult queryResult = default;
            
            queryResult.PathPoints = new List<DtStraightPath>(querySettings.MaxPathPoints);
            navmesh.DoPathFindQuery(query, ref queryResult);
            if (!queryResult.PathFound)
                return false;

            for (int i = 0; i < queryResult.PathPoints.Count; i++)
            {
                path.Add(queryResult.PathPoints[i].pos.ToStrideVector());
            }
            return true;
        }
    }
}
