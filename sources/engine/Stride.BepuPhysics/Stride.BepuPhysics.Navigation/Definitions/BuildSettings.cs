// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using DotRecast.Recast;
using Stride.Core;

namespace Stride.BepuPhysics.Navigation.Definitions;
[DataContract("DotRecastBuildSettings")]
public class BuildSettings
{
	public float cellSize = 0.3f;

	public float cellHeight = 0.2f;

	public float agentHeight = 2f;

	public float agentRadius = 0.6f;

	public float agentMaxClimb = 0.9f;

	public float agentMaxSlope = 45f;

	public float agentMaxAcceleration = 8f;

	public float agentMaxSpeed = 3.5f;

	public int minRegionSize = 8;

	public int mergedRegionSize = 20;

	public int partitioning = RcPartitionType.WATERSHED.Value;

	public bool filterLowHangingObstacles = true;

	public bool filterLedgeSpans = true;

	public bool filterWalkableLowHeightSpans = true;

	public float edgeMaxLen = 12f;

	public float edgeMaxError = 1.3f;

	public int vertsPerPoly = 6;

	public float detailSampleDist = 6f;

	public float detailSampleMaxError = 1f;

	public bool tiled;

	public int tileSize = 32;
}
