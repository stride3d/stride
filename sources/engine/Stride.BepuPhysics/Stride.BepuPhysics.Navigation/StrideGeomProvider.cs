// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using DotRecast.Core.Numerics;
using DotRecast.Core;
using DotRecast.Recast.Geom;
using DotRecast.Recast;

namespace Stride.BepuPhysics.Navigation;
internal class StrideGeomProvider : IInputGeomProvider
{
    /// <summary> Object does not expect this array to mutate </summary>
    public readonly float[] Vertices;
    /// <summary> Object does not expect this array to mutate </summary>
    public readonly int[] Faces;

    private readonly RcVec3f _bmin;
    private readonly RcVec3f _bmax;

    private readonly List<RcConvexVolume> _convexVolumes = new List<RcConvexVolume>();
    private readonly List<RcOffMeshConnection> _offMeshConnections = new List<RcOffMeshConnection>();
    private readonly RcTriMesh _mesh;

    /// <summary>
    /// Do note that this object expects ownership over the arrays provided, do not write to them
    /// </summary>
    public StrideGeomProvider(float[] vertices, int[] faces)
    {
        Vertices = vertices;
        Faces = faces;
        _bmin = RcVecUtils.Create(Vertices);
        _bmax = RcVecUtils.Create(Vertices);
        for (int i = 1; i < vertices.Length / 3; i++)
        {
            _bmin = RcVecUtils.Min(_bmin, Vertices, i * 3);
            _bmax = RcVecUtils.Max(_bmax, Vertices, i * 3);
        }

        _mesh = new RcTriMesh(Vertices, Faces);
    }

    public RcTriMesh GetMesh()
    {
        return _mesh;
    }

    public RcVec3f GetMeshBoundsMin()
    {
        return _bmin;
    }

    public RcVec3f GetMeshBoundsMax()
    {
        return _bmax;
    }

    public IList<RcConvexVolume> ConvexVolumes()
    {
        return _convexVolumes;
    }

    public IEnumerable<RcTriMesh> Meshes()
    {
        yield return _mesh;
    }

    public List<RcOffMeshConnection> GetOffMeshConnections()
    {
        return _offMeshConnections;
    }

    public void AddOffMeshConnection(RcVec3f start, RcVec3f end, float radius, bool bidir, int area, int flags)
    {
        _offMeshConnections.Add(new RcOffMeshConnection(start, end, radius, bidir, area, flags));
    }

    public void RemoveOffMeshConnections(Predicate<RcOffMeshConnection> filter)
    {
        //offMeshConnections.RetainAll(offMeshConnections.Stream().Filter(c -> !filter.Test(c)).Collect(ToList()));
        _offMeshConnections.RemoveAll(filter); // TODO : 확인 필요 <- "Need to be confirmed"
    }

    /// <summary>
    /// This method is unoptimized, avoid frequent calls or rewrite it
    /// </summary>
    public bool RaycastMesh(RcVec3f src, RcVec3f dst, out float tmin)
    {
        tmin = 1.0f;

        if (!RcIntersections.IsectSegAABB(src, dst, _bmin, _bmax, out var btmin, out var btmax)) // This ray-box intersection could be accelerated through SIMD
        {
            return false; // Exit if this ray doesn't intersect with the bounding box
        }

        var p = new RcVec2f();
        var q = new RcVec2f();
        p.X = src.X + (dst.X - src.X) * btmin;
        p.Y = src.Z + (dst.Z - src.Z) * btmin;
        q.X = src.X + (dst.X - src.X) * btmax;
        q.Y = src.Z + (dst.Z - src.Z) * btmax;

        List<RcChunkyTriMeshNode> chunks = _mesh.chunkyTriMesh.GetChunksOverlappingSegment(p, q); // Inline this method to avoid the list allocation
        if (0 == chunks.Count)
        {
            return false;
        }

        tmin = 1.0f;
        bool hit = false;
        foreach (RcChunkyTriMeshNode chunk in chunks)
        {
            int[] tris = chunk.tris;
            for (int j = 0; j < chunk.tris.Length; j += 3)
            {
                RcVec3f v1 = new RcVec3f(
                    Vertices[tris[j] * 3],
                    Vertices[tris[j] * 3 + 1],
                    Vertices[tris[j] * 3 + 2]
                );
                RcVec3f v2 = new RcVec3f(
                    Vertices[tris[j + 1] * 3],
                    Vertices[tris[j + 1] * 3 + 1],
                    Vertices[tris[j + 1] * 3 + 2]
                );
                RcVec3f v3 = new RcVec3f(
                    Vertices[tris[j + 2] * 3],
                    Vertices[tris[j + 2] * 3 + 1],
                    Vertices[tris[j + 2] * 3 + 2]
                );
                if (RcIntersections.IntersectSegmentTriangle(src, dst, v1, v2, v3, out var t)) // This ray-box intersection could be accelerated through SIMD
                {
                    if (t < tmin)
                    {
                        tmin = t;
                    }

                    hit = true;
                }
            }
        }

        return hit;
    }


    public void AddConvexVolume(float[] verts, float minh, float maxh, RcAreaModification areaMod)
    {
        RcConvexVolume volume = new RcConvexVolume();
        volume.verts = verts;
        volume.hmin = minh;
        volume.hmax = maxh;
        volume.areaMod = areaMod;
        AddConvexVolume(volume);
    }

    public void AddConvexVolume(RcConvexVolume volume)
    {
        _convexVolumes.Add(volume);
    }

    public void ClearConvexVolumes()
    {
        _convexVolumes.Clear();
    }
}
