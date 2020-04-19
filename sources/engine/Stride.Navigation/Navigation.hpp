// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#pragma once
#pragma pack(push, 4)
struct Point
{
	int X;
	int Y;
};
struct BuildSettings
{
	// Bounding box for the generated navigation mesh
	BoundingBox boundingBox;
	float cellHeight;
	float cellSize;
	int tileSize;
	Point tilePosition;
	int regionMinArea;
	int regionMergeArea;
	float edgeMaxLen;
	float edgeMaxError;
	float detailSampleDistInput;
	float detailSampleMaxErrorInput;
	float agentHeight;
	float agentRadius;
	float agentMaxClimb;
	float agentMaxSlope;
};
struct GeneratedData
{
	bool success;
	Vector3* navmeshVertices = nullptr;
	int numNavmeshVertices = 0;
	uint8_t* navmeshData = nullptr;
	int navmeshDataLength = 0;
};
#pragma pack(pop)
