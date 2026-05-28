// C ABI shim around kmammou/v-hacd v4 (single-header).
// Consumed via P/Invoke by Stride.Assets.Physics.ConvexHullMesh.

#define ENABLE_VHACD_IMPLEMENTATION 1
#include "VHACD.h"

#include <cstdint>
#include <map>
#include <mutex>
#include <vector>

#if defined(_WIN32)
    #define VHACD_EXPORT extern "C" __declspec(dllexport)
#else
    #define VHACD_EXPORT extern "C" __attribute__((visibility("default")))
#endif

namespace
{
    // Tracks live decomposers indexed by a caller-supplied token so vhacdCancel
    // can locate the IVHACD instance from another thread.
    std::mutex g_tokenMutex;
    std::map<int32_t, VHACD::IVHACD*> g_tokenMap;

    struct Compound
    {
        std::vector<std::vector<VHACD::Vertex>> hullPoints;
        std::vector<std::vector<VHACD::Triangle>> hullTriangles;
    };
}

VHACD_EXPORT void* vhacdGenerate(
    const float* points, uint32_t pointCount,
    const uint32_t* indices, uint32_t triangleCount,
    bool simpleHull,
    uint32_t maxConvexHulls,
    uint32_t resolution,
    uint32_t maxRecursionDepth,
    double minimumVolumePercentErrorAllowed,
    bool shrinkWrap,
    int32_t fillMode,
    uint32_t maxNumVerticesPerCH,
    int32_t cancelToken)
{
    auto* compound = new Compound();

    // SimpleHull bypasses decomposition: return a single hull made of all input
    // points + triangles directly. Matches the v1 wrapper behaviour so the
    // "Decomposition.Enabled = false" toggle keeps working.
    if (simpleHull)
    {
        compound->hullPoints.emplace_back();
        compound->hullTriangles.emplace_back();
        auto& pts = compound->hullPoints[0];
        auto& tris = compound->hullTriangles[0];
        pts.reserve(pointCount);
        for (uint32_t i = 0; i < pointCount; ++i)
            pts.emplace_back(points[i * 3 + 0], points[i * 3 + 1], points[i * 3 + 2]);
        tris.reserve(triangleCount);
        for (uint32_t i = 0; i < triangleCount; ++i)
            tris.emplace_back(indices[i * 3 + 0], indices[i * 3 + 1], indices[i * 3 + 2]);
        return compound;
    }

    VHACD::IVHACD::Parameters params;
    params.m_maxConvexHulls = maxConvexHulls;
    params.m_resolution = resolution;
    params.m_maxRecursionDepth = maxRecursionDepth;
    params.m_minimumVolumePercentErrorAllowed = minimumVolumePercentErrorAllowed;
    params.m_shrinkWrap = shrinkWrap;
    params.m_fillMode = static_cast<VHACD::FillMode>(fillMode);
    params.m_maxNumVerticesPerCH = maxNumVerticesPerCH;

    VHACD::IVHACD* vhacd = VHACD::CreateVHACD();

    {
        std::lock_guard<std::mutex> lock(g_tokenMutex);
        g_tokenMap[cancelToken] = vhacd;
    }

    bool ok = vhacd->Compute(points, pointCount, indices, triangleCount, params);

    {
        std::lock_guard<std::mutex> lock(g_tokenMutex);
        g_tokenMap.erase(cancelToken);
    }

    if (ok)
    {
        uint32_t hullCount = vhacd->GetNConvexHulls();
        compound->hullPoints.resize(hullCount);
        compound->hullTriangles.resize(hullCount);
        VHACD::IVHACD::ConvexHull hull;
        for (uint32_t i = 0; i < hullCount; ++i)
        {
            if (vhacd->GetConvexHull(i, hull))
            {
                compound->hullPoints[i] = std::move(hull.m_points);
                compound->hullTriangles[i] = std::move(hull.m_triangles);
            }
        }
    }

    vhacd->Release();
    return compound;
}

VHACD_EXPORT void vhacdRelease(void* handle)
{
    delete static_cast<Compound*>(handle);
}

VHACD_EXPORT void vhacdCancel(int32_t cancelToken)
{
    std::lock_guard<std::mutex> lock(g_tokenMutex);
    auto it = g_tokenMap.find(cancelToken);
    if (it != g_tokenMap.end() && it->second != nullptr)
        it->second->Cancel();
}

VHACD_EXPORT uint32_t vhacdGetHullCount(void* handle)
{
    return static_cast<uint32_t>(static_cast<Compound*>(handle)->hullPoints.size());
}

VHACD_EXPORT uint32_t vhacdGetHullPointCount(void* handle, uint32_t hullIndex)
{
    return static_cast<uint32_t>(static_cast<Compound*>(handle)->hullPoints[hullIndex].size());
}

VHACD_EXPORT void vhacdCopyHullPoints(void* handle, uint32_t hullIndex, float* outXyz)
{
    const auto& pts = static_cast<Compound*>(handle)->hullPoints[hullIndex];
    for (size_t i = 0; i < pts.size(); ++i)
    {
        outXyz[i * 3 + 0] = static_cast<float>(pts[i].mX);
        outXyz[i * 3 + 1] = static_cast<float>(pts[i].mY);
        outXyz[i * 3 + 2] = static_cast<float>(pts[i].mZ);
    }
}

VHACD_EXPORT uint32_t vhacdGetHullIndexCount(void* handle, uint32_t hullIndex)
{
    return static_cast<uint32_t>(static_cast<Compound*>(handle)->hullTriangles[hullIndex].size());
}

VHACD_EXPORT void vhacdCopyHullIndices(void* handle, uint32_t hullIndex, uint32_t* outIndices)
{
    const auto& tris = static_cast<Compound*>(handle)->hullTriangles[hullIndex];
    for (size_t i = 0; i < tris.size(); ++i)
    {
        outIndices[i * 3 + 0] = tris[i].mI0;
        outIndices[i * 3 + 1] = tris[i].mI1;
        outIndices[i * 3 + 2] = tris[i].mI2;
    }
}
