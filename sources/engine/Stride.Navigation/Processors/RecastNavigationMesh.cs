// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Navigation.Processors
{
    /// <summary>
    /// Recast native navigation mesh wrapper
    /// </summary>
    public unsafe class RecastNavigationMesh : IDisposable
    {
        private readonly Navigation.NavMeshHandle navmesh;
        private readonly HashSet<Point> tileCoordinates = new();

        public RecastNavigationMesh(NavigationMesh navigationMesh)
        {
            navmesh = Navigation.CreateNavmesh(navigationMesh.TileSize * navigationMesh.CellSize);
        }

        public void Dispose()
        {
            Navigation.DestroyNavmesh(navmesh);
        }

        /// <summary>
        /// Adds or replaces a tile in the navigation mesh
        /// </summary>
        /// <remarks>The coordinate of the tile is embedded inside the tile data header</remarks>
        public bool AddOrReplaceTile(byte[] data)
        {
            fixed (byte* dataPtr = data)
            {
                Debug.Assert(Unsafe.SizeOf<Navigation.TileHeader>() <= (data?.Length ?? 0));
                Navigation.TileHeader* header = (Navigation.TileHeader*)dataPtr;
                var coord = new Point(header->X, header->Y);

                // Remove old tile if it exists
                RemoveTile(coord);

                tileCoordinates.Add(coord);
                return Navigation.AddTile(navmesh, dataPtr, data.Length);
            }
        }

        /// <summary>
        /// Removes a tile at given coordinate
        /// </summary>
        /// <param name="coord">The tile coordinate</param>
        public bool RemoveTile(Point coord)
        {
            if (!tileCoordinates.Contains(coord))
                return false;

            tileCoordinates.Remove(coord);
            return Navigation.RemoveTile(navmesh, coord);
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
            Navigation.RaycastQuery query = new()
            {
                Source = start,
                Target = end,
                MaxPathPoints = querySettings.MaxPathPoints,
                FindNearestPolyExtent = querySettings.FindNearestPolyExtent
            };
            Navigation.DoRaycastQuery(navmesh, query, out var queryResult);
            if (!queryResult.Hit)
                return new() { Hit = false };

            return new()
            {
                Hit = true,
                Position = queryResult.Position,
                Normal = queryResult.Normal
            };
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
            if (navmesh == default)
                return false;

            Navigation.PathFindQuery query;
            query.Source = start;
            query.Target = end;
            query.MaxPathPoints = querySettings.MaxPathPoints;
            query.FindNearestPolyExtent = querySettings.FindNearestPolyExtent;
            Navigation.PathFindResult queryResult = default;
            Vector3[] generatedPathPoints = new Vector3[querySettings.MaxPathPoints];
            fixed (Vector3* generatedPathPointsPtr = generatedPathPoints)
            {
                queryResult.PathPoints = (nint)generatedPathPointsPtr;
                Navigation.DoPathFindQuery(navmesh, query, ref queryResult);
                if (!queryResult.PathFound)
                    return false;
            }

            // Read path from unsafe result
            Vector3* points = (Vector3*)queryResult.PathPoints;
            for (int i = 0; i < queryResult.NumPathPoints; i++)
            {
                path.Add(points[i]);
            }
            return true;
        }
    }
}
