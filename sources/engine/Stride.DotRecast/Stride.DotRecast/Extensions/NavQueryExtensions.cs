using DotRecast.Core.Numerics;
using DotRecast.Detour;
using Stride.Core.Mathematics;
using Stride.DotRecast.Definitions;

namespace Stride.DotRecast.Extensions;
public static class NavQueryExtensions
{

    /// <summary>
    /// Find a path from the start polygon to the end polygon.
    /// </summary>
    /// <param name="navQuery"></param>
    /// <param name="startRef"></param>
    /// <param name="endRef"></param>
    /// <param name="startPt"></param>
    /// <param name="endPt"></param>
    /// <param name="filter"></param>
    /// <param name="enableRaycast"></param>
    /// <param name="polys"></param>
    /// <param name="pathIterPolyCount"></param>
    /// <param name="smoothPath"></param>
    /// <param name="navSettings"></param>
    /// <returns></returns>
    public static DtStatus FindFollowPath(this DtNavMeshQuery navQuery, long startRef, long endRef, RcVec3f startPt, RcVec3f endPt, IDtQueryFilter filter, bool enableRaycast, ref List<long> polys, int pathIterPolyCount, ref List<Vector3> smoothPath, PathfindingSettings navSettings)
    {
        if (startRef == 0 || endRef == 0)
        {
            polys.Clear();
            smoothPath.Clear();

            return DtStatus.DT_FAILURE;
        }

        polys.Clear();
        smoothPath.Clear();
        pathIterPolyCount = 0;

        var opt = new DtFindPathOption(enableRaycast ? DtFindPathOptions.DT_FINDPATH_ANY_ANGLE : 0, float.MaxValue);
        navQuery.FindPath(startRef, endRef, startPt, endPt, filter, ref polys, opt);
        if (0 >= polys.Count)
            return DtStatus.DT_FAILURE;

        pathIterPolyCount = polys.Count;

        // Iterate over the path to find smooth path on the detail mesh surface.
        navQuery.ClosestPointOnPoly(startRef, startPt, out var iterPos, out _);
        navQuery.ClosestPointOnPoly(polys[polys.Count - 1], endPt, out var targetPos, out _);

        const float STEP_SIZE = 0.5f;
        const float SLOP = 0.01f;

        smoothPath.Clear();
        smoothPath.Add(iterPos.ToStrideVector());

        Span<long> visited = stackalloc long[navSettings.MaxAllowedVisitedTiles];
        int nvisited = 0;

        // Move towards target a small advancement at a time until target reached or
        // when ran out of memory to store the path.
        while (0 < polys.Count && smoothPath.Count < navSettings.MaxSmoothing)
        {
            // Find location to steer towards.
            if (!DtPathUtils.GetSteerTarget(navQuery, iterPos, targetPos, SLOP,
                    polys, polys.Count, out var steerPos, out var steerPosFlag, out var steerPosRef))
            {
                break;
            }

            bool endOfPath = (steerPosFlag & DtStraightPathFlags.DT_STRAIGHTPATH_END) != 0
                ? true
                : false;
            bool offMeshConnection = (steerPosFlag & DtStraightPathFlags.DT_STRAIGHTPATH_OFFMESH_CONNECTION) != 0
                ? true
                : false;
            // Find movement delta.
            RcVec3f delta = RcVec3f.Subtract(steerPos, iterPos);
            float len = MathF.Sqrt(RcVec3f.Dot(delta, delta));
            // If the steer target is end of path or off-mesh link, do not move past the location.
            if ((endOfPath || offMeshConnection) && len < STEP_SIZE)
            {
                len = 1;
            }
            else
            {
                len = STEP_SIZE / len;
            }

            RcVec3f moveTgt = RcVec.Mad(iterPos, delta, len);

            // Move
            navQuery.MoveAlongSurface(polys[0], iterPos, moveTgt, filter, out var result, visited, out nvisited, visited.Length);

            iterPos = result;

            pathIterPolyCount = DtPathUtils.MergeCorridorStartMoved(ref polys, pathIterPolyCount, navSettings.MaxPolys, visited, nvisited);
            pathIterPolyCount = DtPathUtils.FixupShortcuts(ref polys, pathIterPolyCount, navQuery);

            var status = navQuery.GetPolyHeight(polys[0], result, out var h);
            if (status.Succeeded())
            {
                iterPos.Y = h;
            }

            // Store results.
            if (smoothPath.Count < navSettings.MaxSmoothing)
            {
                smoothPath.Add(iterPos.ToStrideVector());
            }
        }

        return DtStatus.DT_SUCCESS;
    }

}
