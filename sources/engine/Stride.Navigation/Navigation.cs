// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using DotRecast.Detour;
using Stride.Core.Mathematics;

namespace Stride.Navigation
{
    internal struct RaycastQuery
    {
        public Vector3 Source;
        public Vector3 Target;
        public Vector3 FindNearestPolyExtent;
        public int MaxPathPoints;
    }
    
    internal struct PathFindQuery
    {
        public Vector3 Source;
        public Vector3 Target;
        public Vector3 FindNearestPolyExtent;
        public int MaxPathPoints;
    }

    internal struct PathFindResult
    {
        public bool PathFound;
        public List<DtStraightPath> PathPoints;
    }
    
    internal struct BuildSettings
    {
        public BoundingBox BoundingBox;
        public float CellHeight;
        public float CellSize;
        public int TileSize;
        public Point TilePosition;
        public int RegionMinArea;
        public int RegionMergeArea;
        public float EdgeMaxLen;
        public float EdgeMaxError;
        public float DetailSampleDist;
        public float DetailSampleMaxError;
        public float AgentHeight;
        public float AgentRadius;
        public float AgentMaxClimb;
        public float AgentMaxSlope;
    }
    
    internal struct GeneratedData
    {
        public bool Success;
        public DtMeshData NavmeshData;
    }
}
