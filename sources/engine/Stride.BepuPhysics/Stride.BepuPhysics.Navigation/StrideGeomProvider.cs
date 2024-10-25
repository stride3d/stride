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

    private readonly RcVec3f _bMin;
    private readonly RcVec3f _bMax;

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
        _bMin = new RcVec3f(vertices);
        _bMax = new RcVec3f(vertices);
        for (int i = 1; i < vertices.Length / 3; i++)
        {
            _bMin = RcVec3f.Min(_bMin, RcVec.Create(vertices, i * 3));
            _bMax = RcVec3f.Max(_bMax, RcVec.Create(vertices, i * 3));
        }

        _mesh = new RcTriMesh(Vertices, Faces);
    }

    public RcTriMesh GetMesh()
    {
        return _mesh;
    }

    public RcVec3f GetMeshBoundsMin()
    {
        return _bMin;
    }

    public RcVec3f GetMeshBoundsMax()
    {
        return _bMax;
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
