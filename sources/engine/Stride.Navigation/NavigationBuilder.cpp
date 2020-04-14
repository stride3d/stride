// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// Contains modified code from Sample_TileMesh.cpp from the recast/detour sample
// With the following copyright notification:

// Copyright (c) 2009-2010 Mikko Mononen memon@inside.org
//
// This software is provided 'as-is', without any express or implied
// warranty.  In no event will the authors be held liable for any damages
// arising from the use of this software.
// Permission is granted to anyone to use this software for any purpose,
// including commercial applications, and to alter it and redistribute it
// freely, subject to the following restrictions:
// 1. The origin of this software must not be misrepresented; you must not
//    claim that you wrote the original software. If you use this software
//    in a product, an acknowledgment in the product documentation would be
//    appreciated but is not required.
// 2. Altered source versions must be plainly marked as such, and must not be
//    misrepresented as being the original software.
// 3. This notice may not be removed or altered from any source distribution.
//

#include "../../../deps/Recast/include/DetourNavMeshBuilder.h"
#include "../../deps/NativePath/NativePath.h"
#include "../../deps/NativePath/NativeTime.h"

#include "../Stride.Native/StrideNative.h"
#include "Navigation.hpp"
#include "NavigationBuilder.hpp"

NavigationBuilder::NavigationBuilder()
{
	m_context = new rcContext(false);
}
NavigationBuilder::~NavigationBuilder()
{
	delete m_context;
	Cleanup();
}
void NavigationBuilder::Cleanup()
{
	if(m_navmeshData)
	{
		dtFree(m_navmeshData);
		m_navmeshData = nullptr;
		m_navmeshDataLength = 0;
	}
	if (m_solid)
	{
		rcFreeHeightField(m_solid);
		m_solid = nullptr;
	}
	if (m_triareas)
	{
		delete[] m_triareas;
		m_triareas = nullptr;
	}
	if (m_chf)
	{
		rcFreeCompactHeightfield(m_chf);
		m_chf = nullptr;
	}
	if (m_pmesh)
	{
		rcFreePolyMesh(m_pmesh);
		m_pmesh = nullptr;
	}
	if (m_dmesh)
	{
		rcFreePolyMeshDetail(m_dmesh);
		m_dmesh = nullptr;
	}
}
GeneratedData* NavigationBuilder::BuildNavmesh(Vector3* vertices, int numVertices, int* indices, int numIndices)
{
	GeneratedData* ret = &m_result;
	ret->success = false;

	float bmin[3];
	memcpy(bmin, &m_buildSettings.boundingBox.minimum.X, sizeof(float) * 3);
	float bmax[3];
	memcpy(bmax, &m_buildSettings.boundingBox.maximum.X, sizeof(float) * 3);

	float bbSize[3];
	rcVsub(bbSize, bmax, bmin);
	if (bbSize[0] <= 0.0f || bbSize[1] <= 0.0f || bbSize[2] <= 0.0f)
		return ret; // Negative or empty bounding box

	// Check input parameters
	if (m_buildSettings.detailSampleDistInput < 1.0f)
		return ret;
	if (m_buildSettings.detailSampleMaxErrorInput <= 0.0f)
		return ret;
	if (m_buildSettings.edgeMaxError < 0.1f)
		return ret;
	if (m_buildSettings.edgeMaxLen < 0.0f)
		return ret;
	if (m_buildSettings.regionMinArea < 0.0f)
		return ret;
	if (m_buildSettings.regionMergeArea < 0.0f)
		return ret;
	if (m_buildSettings.tileSize <= 0)
		return ret;

	// Limit cell size to not freeze the process with calculating a huge amount of cells
	if (m_buildSettings.cellSize < 0.01f)
		m_buildSettings.cellSize = 0.01f;
	if (m_buildSettings.cellHeight < 0.01f)
		m_buildSettings.cellHeight = 0.01f;

	int maxEdgeLen = (int)(m_buildSettings.edgeMaxLen / m_buildSettings.cellSize);
	float maxSimplificationError = m_buildSettings.edgeMaxError;
	int maxVertsPerPoly = 6;
	float detailSampleDist = m_buildSettings.cellSize * m_buildSettings.detailSampleDistInput;
	float detailSampleMaxError = m_buildSettings.cellHeight * m_buildSettings.detailSampleMaxErrorInput;

	int walkableHeight = (int)ceilf(m_buildSettings.agentHeight / m_buildSettings.cellHeight);
	int walkableClimb = (int)floorf(m_buildSettings.agentMaxClimb / m_buildSettings.cellHeight);
	int walkableRadius = (int)ceilf(m_buildSettings.agentRadius / m_buildSettings.cellSize);

	// Size of the tile border
	int borderSize = walkableRadius + 3;
	int tileSize = m_buildSettings.tileSize;

	// Expand bounding box by border size so that all required geometry is included
	bmin[0] -= borderSize * m_buildSettings.cellSize;
	bmin[2] -= borderSize * m_buildSettings.cellSize;
	bmax[0] += borderSize * m_buildSettings.cellSize;
	bmax[2] += borderSize * m_buildSettings.cellSize;

	int width = tileSize + borderSize * 2;
	int height = tileSize + borderSize * 2;

	double totalTime = npSeconds();

	// Make sure state is clean
	Cleanup();

	if (numIndices == 0 || numVertices == 0)
		return ret;

	if (walkableClimb < 0)
		return ret;

	m_solid = rcAllocHeightfield();
	if (!rcCreateHeightfield(m_context, *m_solid, width, height, bmin, bmax, m_buildSettings.cellSize, m_buildSettings.cellHeight))
	{
		return ret;
	}

	int numTriangles = numIndices / 3;
	m_triareas = new uint8_t[numTriangles];
	if (!m_triareas)
	{
		return ret;
	}

	// Find walkable triangles and rasterize into heightfield
	memset(m_triareas, 0, numTriangles * sizeof(unsigned char));
	rcMarkWalkableTriangles(m_context, m_buildSettings.agentMaxSlope, (float*)vertices, numVertices, indices, numTriangles, m_triareas);
	if (!rcRasterizeTriangles(m_context, (float*)vertices, numVertices, indices, m_triareas, numTriangles, *m_solid, walkableClimb))
	{
		return ret;
	}

	// Filter walkables surfaces.
	rcFilterLowHangingWalkableObstacles(m_context, walkableClimb, *m_solid);
	rcFilterLedgeSpans(m_context, walkableHeight, walkableClimb, *m_solid);
	rcFilterWalkableLowHeightSpans(m_context, walkableHeight, *m_solid);

	// Compact the heightfield so that it is faster to handle from now on.
	// This will result more cache coherent data as well as the neighbours
	// between walkable cells will be calculated.
	m_chf = rcAllocCompactHeightfield();
	if (!m_chf)
	{
		return ret;
	}
	if (!rcBuildCompactHeightfield(m_context, walkableHeight, walkableClimb, *m_solid, *m_chf))
	{
		return ret;
	}

	// No longer need solid heightfield after compacting it
	rcFreeHeightField(m_solid);
	m_solid = 0;

	// Erode the walkable area by agent radius.
	if (!rcErodeWalkableArea(m_context, walkableRadius, *m_chf))
	{
		return ret;
	}

	// Prepare for region partitioning, by calculating distance field along the walkable surface.
	if (!rcBuildDistanceField(m_context, *m_chf))
	{
		return ret;
	}
	// Partition the walkable surface into simple regions without holes.
	if (!rcBuildRegions(m_context, *m_chf, borderSize, m_buildSettings.regionMinArea, m_buildSettings.regionMergeArea))
	{
		return ret;
	}

	// Create contours.
	m_cset = rcAllocContourSet();
	if (!m_cset)
	{
		return ret;
	}
	if (!rcBuildContours(m_context, *m_chf, maxSimplificationError, maxEdgeLen, *m_cset))
	{
		return ret;
	}

	// Build polygon navmesh from the contours.
	m_pmesh = rcAllocPolyMesh();
	if (!m_pmesh)
	{
		return ret;
	}
	if (!rcBuildPolyMesh(m_context, *m_cset, maxVertsPerPoly, *m_pmesh))
	{
		return ret;
	}

	// Free intermediate results
	rcFreeContourSet(m_cset);
	m_cset = nullptr;

	m_dmesh = rcAllocPolyMeshDetail();
	if (!m_dmesh)
	{
		return ret;
	}

	if (!rcBuildPolyMeshDetail(m_context, *m_pmesh, *m_chf, detailSampleDist, detailSampleMaxError, *m_dmesh))
	{
		return ret;
	}

	// Free intermediate results
	rcFreeCompactHeightfield(m_chf);
	m_chf = nullptr;

	// Update poly flags from areas.
	for (int i = 0; i < m_pmesh->npolys; ++i)
	{
		if (m_pmesh->areas[i] == RC_WALKABLE_AREA)
			m_pmesh->areas[i] = 0;

		if (m_pmesh->areas[i] == 0)
		{
			m_pmesh->flags[i] = 1;
		}
	}

	// Generate native navmesh format and store the data pointers in the return structure
	if (!CreateDetourMesh())
		return ret;
	ret->navmeshData = m_navmeshData;
	ret->navmeshDataLength = m_navmeshDataLength;
	ret->success = true;

	totalTime = npSeconds() - totalTime;
	return ret;
}
void NavigationBuilder::SetSettings(BuildSettings buildSettings)
{
	// Copy this to have access to original settings
	m_buildSettings = buildSettings;
}
bool NavigationBuilder::CreateDetourMesh()
{
	dtNavMeshCreateParams params = { 0 };
	params.verts = m_pmesh->verts;
	params.vertCount = m_pmesh->nverts;
	params.polys = m_pmesh->polys;
	params.polyAreas = m_pmesh->areas;
	params.polyFlags = m_pmesh->flags;
	params.polyCount = m_pmesh->npolys;
	params.nvp = m_pmesh->nvp;
	params.detailMeshes = m_dmesh->meshes;
	params.detailVerts = m_dmesh->verts;
	params.detailVertsCount = m_dmesh->nverts;
	params.detailTris = m_dmesh->tris;
	params.detailTriCount = m_dmesh->ntris;
	// TODO: Support off-mesh connections
	params.offMeshConVerts = nullptr;
	params.offMeshConRad = nullptr;
	params.offMeshConDir = nullptr;
	params.offMeshConAreas = nullptr;
	params.offMeshConFlags = nullptr;
	params.offMeshConUserID = nullptr;
	params.offMeshConCount = 0;
	params.walkableHeight = m_buildSettings.agentHeight;
	params.walkableClimb = m_buildSettings.agentMaxClimb;
	params.walkableRadius = m_buildSettings.agentRadius;
	rcVcopy(params.bmin, m_pmesh->bmin);
	rcVcopy(params.bmax, m_pmesh->bmax);
	params.cs = m_buildSettings.cellSize;
	params.ch = m_buildSettings.cellHeight;
	params.buildBvTree = true;
	params.tileX = m_buildSettings.tilePosition.X;
	params.tileY = m_buildSettings.tilePosition.Y;

	if (!dtCreateNavMeshData(&params, &m_navmeshData, &m_navmeshDataLength))
	{
		dtFree(m_navmeshData);
		return false;
	}
	if (m_navmeshDataLength == 0 || !m_navmeshData)
		return false;
	return true;
}
