// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using NRigidPose = BepuPhysics.RigidPose;

namespace Stride.BepuPhysics.Definitions.Colliders;

[DataContract]
public sealed class ConvexHullCollider : ColliderBase
{
    private static ConditionalWeakTable<DecomposedHulls, CachedConvexHulls> _convexes = new();
    private static BufferPool _sharedPool = new();

    private DecomposedHulls _hull = null!;
    /// <summary> Holds onto the cached shape as long as it is assigned </summary>
    private CachedConvexHulls? _cache = null;

    /// <remarks>
    /// Changing this value will reset some of the internal physics state of this body
    /// </remarks>
    [MemberRequired(ReportAs = MemberRequiredReportType.Error)]
    public required DecomposedHulls Hull
    {
        get
        {
            return _hull;
        }
        set
        {
            _hull = value;
            TryUpdateFeatures();
        }
    }

    internal override void AddToCompoundBuilder(ShapeCacheSystem shape, BufferPool pool, ref CompoundBuilder builder, NRigidPose localPose)
    {
        if (_convexes.TryGetValue(Hull, out _cache) == false)
        {
            var hulls = new List<(ConvexHull, System.Numerics.Vector3)>(Hull.Meshes.Length);
            foreach (var mesh in Hull.Meshes)
            {
                hulls.EnsureCapacity(hulls.Count + mesh.Hulls.Length);
                // Multiple convex hulls may be set up to create a concave shape, do not merge them
                foreach (var hull in mesh.Hulls)
                {
                    var points = MemoryMarshal.Cast<Vector3, System.Numerics.Vector3>(hull.InternalPoints);
                    var convex = new ConvexHull(points, _sharedPool, out var center);
                    hulls.Add((convex, center));
                }
            }

            _cache = new(hulls);
            _convexes.Add(Hull, _cache);
        }

        foreach (var (hull, center) in _cache.Hulls)
        {
            var poseForThisHull = localPose;
            poseForThisHull.Position += center;
            builder.Add(hull, poseForThisHull, Mass);
        }
    }

    internal override void OnDetach(BufferPool pool)
    {
        _cache = null; // Release reference when detached, it may then be finalized by the GC if no one else holds onto it
    }

    record CachedConvexHulls(List<(ConvexHull, System.Numerics.Vector3)> Hulls)
    {
        ~CachedConvexHulls()
        {
            foreach (var (hull, center) in Hulls)
                hull.Dispose(_sharedPool);
        }
    }
}
