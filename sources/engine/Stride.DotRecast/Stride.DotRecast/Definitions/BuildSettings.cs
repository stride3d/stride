// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using DotRecast.Recast;
using Stride.Core;

namespace Stride.DotRecast.Definitions;
[DataContract("DotRecastBuildSettings")]
public class BuildSettings
{
    public float CellSize = 0.3f;

    public float CellHeight = 0.2f;

    public float AgentHeight = 2f;

    public float AgentRadius = 0.6f;

    public float AgentMaxClimb = 0.9f;

    public float AgentMaxSlope = 45f;

    public float AgentMaxAcceleration = 8f;

    //public float AgentMaxSpeed = 3.5f;

    public int MinRegionSize = 8;

    public int MergedRegionSize = 20;

    public RcPartition PartitionType
    {
        get
        {
            return RcPartitionType.OfValue(Partitioning);
        }
        set
        {
            Partitioning = (int)value;
        }
    }

    [DataMemberIgnore]
    public int Partitioning = RcPartitionType.WATERSHED.Value;

    public bool FilterLowHangingObstacles = true;

    public bool FilterLedgeSpans = true;

    public bool FilterWalkableLowHeightSpans = true;

    public float EdgeMaxLen = 12f;

    public float EdgeMaxError = 1.3f;

    public int VertsPerPoly = 6;

    public float DetailSampleDist = 6f;

    public float DetailSampleMaxError = 1f;

    public bool Tiled;

    public int TileSize = 32;
}
