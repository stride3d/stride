// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#pragma once
#include "../../../deps/Recast/include/Recast.h"

class NavigationBuilder
{
	rcHeightfield* m_solid = nullptr;
	uint8_t* m_triareas = nullptr;
	rcCompactHeightfield* m_chf = nullptr;
	rcContourSet* m_cset = nullptr;
	rcPolyMesh* m_pmesh = nullptr;
	rcPolyMeshDetail* m_dmesh = nullptr;
	BuildSettings m_buildSettings;
	rcContext* m_context;

	// Detour returned navigation mesh data
	// free with dtFree()
	uint8_t* m_navmeshData = nullptr;
	int m_navmeshDataLength = 0;

	GeneratedData m_result;
public:
	NavigationBuilder();
	~NavigationBuilder();
	void Cleanup();
	GeneratedData* BuildNavmesh(Vector3* vertices, int numVertices, int* indices, int numIndices);
	void SetSettings(BuildSettings buildSettings);

private:
	bool CreateDetourMesh();
};
