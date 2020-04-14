// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#include "../Stride.Native/StrideNative.h"

#include "../../deps/NativePath/NativePath.h"
#include "../../deps/NativePath/TINYSTL/vector.h"
#include "../../../deps/Recast/include/DetourCommon.h"
#include "Navigation.hpp"
#include "NavigationMesh.hpp"

NavigationMesh::NavigationMesh()
{
}

NavigationMesh::~NavigationMesh()
{
	// Cleanup allocated tiles
	for(auto tile : m_tileRefs)
	{
		uint8_t* deletedData;
		int deletedDataLength = 0;
		dtStatus status = m_navMesh->removeTile(tile, &deletedData, &deletedDataLength);
		if(dtStatusSucceed(status))
		{
			if (deletedData)
				delete[] deletedData;
		}
	}

	if(m_navQuery)
		dtFreeNavMeshQuery(m_navQuery);
	if(m_navMesh)
		dtFreeNavMesh(m_navMesh);
}

bool NavigationMesh::Init(float cellTileSize)
{
	// Allocate objects
	m_navMesh = dtAllocNavMesh();
	m_navQuery = dtAllocNavMeshQuery();

	if (!m_navMesh || !m_navQuery)
		return false;

	dtNavMeshParams params = { 0 };
	params.orig[0] = 0.0f;
	params.orig[1] = 0.0f;
	params.orig[2] = 0.0f;
	params.tileWidth = cellTileSize;
	params.tileHeight = cellTileSize;

	// TODO: Link these parameters to the builder
	int tileBits = 14;
	if (tileBits > 14) tileBits = 14;
	int polyBits = 22 - tileBits;
	params.maxTiles = 1 << tileBits;
	params.maxPolys = 1 << polyBits;

	dtStatus status = m_navMesh->init(&params);
	if (dtStatusFailed(status))
		return false;

	// Initialize the query object
	status = m_navQuery->init(m_navMesh, 2048);
	if (dtStatusFailed(status))
		return false;
	return true;
}

bool NavigationMesh::LoadTile(uint8_t* navData, int navDataLength)
{
	if (!m_navMesh || !m_navQuery)
		return false;
	if (!navData)
		return false;

	// Copy data
	uint8_t* dataCopy = new uint8_t[navDataLength];
	memcpy(dataCopy, navData, navDataLength);

	dtTileRef tileRef = 0;
	if(dtStatusSucceed(m_navMesh->addTile(dataCopy, navDataLength, 0, 0, &tileRef)))
	{
		m_tileRefs.insert(tileRef);
		return true;
	}

	delete[] dataCopy;
	return false;
}

bool NavigationMesh::RemoveTile(Point tileCoordinate)
{
	dtTileRef tileRef = m_navMesh->getTileRefAt(tileCoordinate.X, tileCoordinate.Y, 0);

	uint8_t* deletedData;
	int deletedDataLength = 0;
	dtStatus status = m_navMesh->removeTile(tileRef, &deletedData, &deletedDataLength);
	if(dtStatusSucceed(status))
	{
		if (deletedData)
			delete[] deletedData;
		m_tileRefs.erase(tileRef);
		return true;
	}

	return false;
}

void NavigationMesh::FindPath(NavMeshPathfindQuery query, NavMeshPathfindResult* result)
{
	// Reset result
	result->pathFound = false;
	dtPolyRef startPoly, endPoly;
	Vector3 startPoint, endPoint;

	// Find the starting polygons and point on it to start from
	dtQueryFilter filter;
	dtStatus status;
	status = m_navQuery->findNearestPoly(&query.source.X, &query.findNearestPolyExtent.X, &filter, &startPoly, &startPoint.X);
	if (dtStatusFailed(status))
		return;
	status = m_navQuery->findNearestPoly(&query.target.X, &query.findNearestPolyExtent.X, &filter, &endPoly, &endPoint.X);
	if (dtStatusFailed(status))
		return;

	tinystl::vector<dtPolyRef> polys;
	polys.resize(query.maxPathPoints);
	int pathPointCount = 0;
	status = m_navQuery->findPath(startPoly, endPoly, &startPoint.X, &endPoint.X, 
		&filter, polys.data(), &pathPointCount, polys.size());
	if (dtStatusFailed(status) || (status & DT_PARTIAL_RESULT) != 0)
		return;

	tinystl::vector<Vector3> straightPath;
	tinystl::vector<uint8_t> straightPathFlags;
	tinystl::vector<dtPolyRef> straightpathPolys;
	straightPath.resize(query.maxPathPoints);
	straightPathFlags.resize(query.maxPathPoints);
	straightpathPolys.resize(query.maxPathPoints);
	status = m_navQuery->findStraightPath(&startPoint.X, &endPoint.X, 
		polys.data(), pathPointCount, 
		(float*)result->pathPoints, straightPathFlags.data(), straightpathPolys.data(), 
		&result->numPathPoints, query.maxPathPoints);
	if (dtStatusFailed(status))
		return;
	result->pathFound = true;
}

void NavigationMesh::Raycast(NavMeshRaycastQuery query, NavMeshRaycastResult* result)
{
	// Reset result
	result->hit = false;
	dtQueryFilter filter;

	dtPolyRef startPoly;
	dtStatus status = m_navQuery->findNearestPoly(&query.start.X, &query.findNearestPolyExtent.X, &filter, &startPoly, 0);
	if (dtStatusFailed(status))
		return;

	float t;
	tinystl::vector<dtPolyRef> polys;
	polys.resize(query.maxPathPoints);
	int raycastPolyCount = 0;
	status = m_navQuery->raycast(startPoly, &query.start.X, &query.end.X, &filter, &t, &result->normal.X, polys.data(), &raycastPolyCount, polys.size());
	if (dtStatusFailed(status))
		return;

	result->hit = true;
	dtVlerp(&result->position.X, &query.start.X, &query.end.X, t);
}
