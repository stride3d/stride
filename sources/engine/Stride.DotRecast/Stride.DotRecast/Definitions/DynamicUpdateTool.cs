using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotRecast.Core.Numerics;
using DotRecast.Core;
using DotRecast.Detour.Dynamic.Colliders;
using DotRecast.Detour.Dynamic.Io;
using DotRecast.Detour.Dynamic;
using DotRecast.Detour;
using DotRecast.Recast.Toolset.Builder;
using DotRecast.Recast.Toolset.Geom;
using DotRecast.Recast.Toolset.Gizmos;
using DotRecast.Recast.Toolset.Tools;
using DotRecast.Recast.Toolset;
using DotRecast.Recast;

namespace Stride.DotRecast.Definitions;

public class DynamicUpdateTool : IRcToolable
{
    private DtDynamicNavMesh dynaMesh;
    private readonly Dictionary<long, RcGizmo> colliderGizmos;

    private readonly IRcRand random;

    public DynamicUpdateTool(IRcRand rand)
    {
        this.colliderGizmos = new Dictionary<long, RcGizmo>();
        this.random = rand;
    }

    public IEnumerable<RcGizmo> GetGizmos()
    {
        return colliderGizmos.Values;
    }

    public string GetName()
    {
        return "Dynamic Updates";
    }

    public DtDynamicNavMesh GetDynamicNavMesh()
    {
        return dynaMesh;
    }

    public void RemoveShape(RcVec3f start, RcVec3f dir)
    {
        foreach (var e in colliderGizmos)
        {
            if (Hit(start, dir, e.Value.Collider.Bounds()))
            {
                dynaMesh.RemoveCollider(e.Key);
                colliderGizmos.Remove(e.Key);
                break;
            }
        }
    }

    private bool Hit(RcVec3f point, RcVec3f dir, float[] bounds)
    {
        float cx = 0.5f * (bounds[0] + bounds[3]);
        float cy = 0.5f * (bounds[1] + bounds[4]);
        float cz = 0.5f * (bounds[2] + bounds[5]);
        float dx = 0.5f * (bounds[3] - bounds[0]);
        float dy = 0.5f * (bounds[4] - bounds[1]);
        float dz = 0.5f * (bounds[5] - bounds[2]);
        float rSqr = dx * dx + dy * dy + dz * dz;
        float mx = point.X - cx;
        float my = point.Y - cy;
        float mz = point.Z - cz;
        float c = mx * mx + my * my + mz * mz - rSqr;
        if (c <= 0.0f)
        {
            return true;
        }

        float b = mx * dir.X + my * dir.Y + mz * dir.Z;
        if (b > 0.0f)
        {
            return false;
        }

        float disc = b * b - c;
        return disc >= 0.0f;
    }


    public RcGizmo AddShape(RcDynamicColliderShape colliderShape, RcVec3f p)
    {
        if (dynaMesh == null)
        {
            return null;
        }

        RcGizmo colliderWithGizmo = null;
        if (colliderShape == RcDynamicColliderShape.SPHERE)
        {
            colliderWithGizmo = SphereCollider(p, dynaMesh.config.walkableClimb);
        }
        else if (colliderShape == RcDynamicColliderShape.CAPSULE)
        {
            colliderWithGizmo = CapsuleCollider(p, dynaMesh.config.walkableClimb);
        }
        else if (colliderShape == RcDynamicColliderShape.BOX)
        {
            colliderWithGizmo = BoxCollider(p, dynaMesh.config.walkableClimb);
        }
        else if (colliderShape == RcDynamicColliderShape.CYLINDER)
        {
            colliderWithGizmo = CylinderCollider(p, dynaMesh.config.walkableClimb);
        }
        else if (colliderShape == RcDynamicColliderShape.COMPOSITE)
        {
            colliderWithGizmo = CompositeCollider(p, dynaMesh.config.walkableClimb);
        }

        if (colliderWithGizmo != null)
        {
            long id = dynaMesh.AddCollider(colliderWithGizmo.Collider);
            colliderGizmos.Add(id, colliderWithGizmo);
        }

        return colliderWithGizmo;
    }

    public DtDynamicNavMesh Copy(RcConfig cfg, IList<RcBuilderResult> results)
    {
        var voxelFile = DtVoxelFile.From(cfg, results);
        dynaMesh = new DtDynamicNavMesh(voxelFile);
        dynaMesh.config.keepIntermediateResults = true;
        colliderGizmos.Clear();

        return dynaMesh;
    }

    public DtDynamicNavMesh Load(string filename, IRcCompressor compressor)
    {
        using var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var br = new BinaryReader(fs);
        DtVoxelFileReader reader = new DtVoxelFileReader(compressor);
        DtVoxelFile voxelFile = reader.Read(br);

        dynaMesh = new DtDynamicNavMesh(voxelFile);
        dynaMesh.config.keepIntermediateResults = true;

        colliderGizmos.Clear();

        return dynaMesh;
    }

    public void Save(string filename, bool compression, IRcCompressor compressor)
    {
        DtVoxelFile voxelFile = DtVoxelFile.From(dynaMesh);
        using var fs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
        using var bw = new BinaryWriter(fs);
        DtVoxelFileWriter writer = new DtVoxelFileWriter(compressor);
        writer.Write(bw, voxelFile, compression);
    }


    public RcGizmo SphereCollider(RcVec3f p, float walkableClimb)
    {
        float radius = 1 + (float)random.NextDouble() * 10;
        var collider = new DtSphereCollider(p, radius, SampleAreaModifications.SAMPLE_POLYAREA_TYPE_WATER, walkableClimb);
        var gizmo = RcGizmoFactory.Sphere(p, radius);

        return new RcGizmo(collider, gizmo);
    }

    public RcGizmo CapsuleCollider(RcVec3f p, float walkableClimb)
    {
        float radius = 0.4f + (float)random.NextDouble() * 4f;
        RcVec3f a = new RcVec3f(
            (1f - 2 * (float)random.NextDouble()),
            0.01f + (float)random.NextDouble(),
            (1f - 2 * (float)random.NextDouble())
        );
        a = RcVec3f.Normalize(a);

        float len = 1f + (float)random.NextDouble() * 20f;
        a.X *= len;
        a.Y *= len;
        a.Z *= len;
        RcVec3f start = new RcVec3f(p.X, p.Y, p.Z);
        RcVec3f end = new RcVec3f(p.X + a.X, p.Y + a.Y, p.Z + a.Z);
        var collider = new DtCapsuleCollider(start, end, radius, SampleAreaModifications.SAMPLE_POLYAREA_TYPE_WATER, walkableClimb);
        var gizmo = RcGizmoFactory.Capsule(start, end, radius);
        return new RcGizmo(collider, gizmo);
    }

    public RcGizmo BoxCollider(RcVec3f p, float walkableClimb)
    {
        RcVec3f extent = new RcVec3f(
            0.5f + (float)random.NextDouble() * 6f,
        0.5f + (float)random.NextDouble() * 6f,
            0.5f + (float)random.NextDouble() * 6f
        );
        RcVec3f forward = new RcVec3f((1f - 2 * (float)random.NextDouble()), 0, (1f - 2 * (float)random.NextDouble()));
        RcVec3f up = new RcVec3f((1f - 2 * (float)random.NextDouble()), 0.01f + (float)random.NextDouble(), (1f - 2 * (float)random.NextDouble()));
        RcVec3f[] halfEdges = DtBoxCollider.GetHalfEdges(up, forward, extent);
        var collider = new DtBoxCollider(p, halfEdges, SampleAreaModifications.SAMPLE_POLYAREA_TYPE_WATER, walkableClimb);
        var gizmo = RcGizmoFactory.Box(p, halfEdges);
        return new RcGizmo(collider, gizmo);
    }

    public RcGizmo CylinderCollider(RcVec3f p, float walkableClimb)
    {
        float radius = 0.7f + (float)random.NextDouble() * 4f;
        RcVec3f a = new RcVec3f(1f - 2 * (float)random.NextDouble(), 0.01f + (float)random.NextDouble(), 1f - 2 * (float)random.NextDouble());
        a = RcVec3f.Normalize(a);
        float len = 2f + (float)random.NextDouble() * 20f;
        a.X *= len;
        a.Y *= len;
        a.Z *= len;
        RcVec3f start = new RcVec3f(p.X, p.Y, p.Z);
        RcVec3f end = new RcVec3f(p.X + a.X, p.Y + a.Y, p.Z + a.Z);
        var collider = new DtCylinderCollider(start, end, radius, SampleAreaModifications.SAMPLE_POLYAREA_TYPE_WATER, walkableClimb);
        var gizmo = RcGizmoFactory.Cylinder(start, end, radius);

        return new RcGizmo(collider, gizmo);
    }

    public RcGizmo CompositeCollider(RcVec3f p, float walkableClimb)
    {
        RcVec3f baseExtent = new RcVec3f(5, 3, 8);
        RcVec3f baseCenter = new RcVec3f(p.X, p.Y + 3, p.Z);
        RcVec3f baseUp = new RcVec3f(0, 1, 0);
        RcVec3f forward = new RcVec3f((1f - 2 * (float)random.NextDouble()), 0, (1f - 2 * (float)random.NextDouble()));
        forward = RcVec3f.Normalize(forward);

        RcVec3f side = RcVec3f.Cross(forward, baseUp);
        DtBoxCollider @base = new DtBoxCollider(baseCenter, DtBoxCollider.GetHalfEdges(baseUp, forward, baseExtent),
            SampleAreaModifications.SAMPLE_POLYAREA_TYPE_ROAD, walkableClimb);
        var roofUp = RcVec3f.Zero;
        RcVec3f roofExtent = new RcVec3f(4.5f, 4.5f, 8f);
        var rx = RcMatrix4x4f.CreateFromRotate(45, forward.X, forward.Y, forward.Z);
        roofUp = MulMatrixVector(ref roofUp, rx, baseUp);
        RcVec3f roofCenter = new RcVec3f(p.X, p.Y + 6, p.Z);
        DtBoxCollider roof = new DtBoxCollider(roofCenter, DtBoxCollider.GetHalfEdges(roofUp, forward, roofExtent),
            SampleAreaModifications.SAMPLE_POLYAREA_TYPE_ROAD, walkableClimb);
        RcVec3f trunkStart = new RcVec3f(
            baseCenter.X - forward.X * 15 + side.X * 6,
            p.Y,
            baseCenter.Z - forward.Z * 15 + side.Z * 6
        );
        RcVec3f trunkEnd = new RcVec3f(trunkStart.X, trunkStart.Y + 10, trunkStart.Z);
        DtCapsuleCollider trunk = new DtCapsuleCollider(trunkStart, trunkEnd, 0.5f, SampleAreaModifications.SAMPLE_POLYAREA_TYPE_ROAD,
            walkableClimb);
        RcVec3f crownCenter = new RcVec3f(
        baseCenter.X - forward.X * 15 + side.X * 6, p.Y + 10,
            baseCenter.Z - forward.Z * 15 + side.Z * 6
        );
        DtSphereCollider crown = new DtSphereCollider(crownCenter, 4f, SampleAreaModifications.SAMPLE_POLYAREA_TYPE_GRASS,
            walkableClimb);
        DtCompositeCollider collider = new DtCompositeCollider(@base, roof, trunk, crown);
        IRcGizmoMeshFilter baseGizmo = RcGizmoFactory.Box(baseCenter, DtBoxCollider.GetHalfEdges(baseUp, forward, baseExtent));
        IRcGizmoMeshFilter roofGizmo = RcGizmoFactory.Box(roofCenter, DtBoxCollider.GetHalfEdges(roofUp, forward, roofExtent));
        IRcGizmoMeshFilter trunkGizmo = RcGizmoFactory.Capsule(trunkStart, trunkEnd, 0.5f);
        IRcGizmoMeshFilter crownGizmo = RcGizmoFactory.Sphere(crownCenter, 4f);
        IRcGizmoMeshFilter gizmo = RcGizmoFactory.Composite(baseGizmo, roofGizmo, trunkGizmo, crownGizmo);
        return new RcGizmo(collider, gizmo);
    }

    private RcGizmo TrimeshCollider(RcVec3f p, DemoInputGeomProvider geom, float walkableClimb)
    {
        float[] verts = TransformVertices(p, geom, 0);
        var collider = new DtTrimeshCollider(verts, geom.faces, SampleAreaModifications.SAMPLE_POLYAREA_TYPE_ROAD,
            walkableClimb * 10);
        var gizmo = RcGizmoFactory.Trimesh(verts, geom.faces);

        return new RcGizmo(collider, gizmo);
    }

    private float[] TransformVertices(RcVec3f p, DemoInputGeomProvider geom, float ax)
    {
        var rx = RcMatrix4x4f.CreateFromRotate((float)random.NextDouble() * ax, 1, 0, 0);
        var ry = RcMatrix4x4f.CreateFromRotate((float)random.NextDouble() * 360, 0, 1, 0);
        var m = RcMatrix4x4f.Mul(ref rx, ref ry);
        float[] verts = new float[geom.vertices.Length];
        RcVec3f v = new RcVec3f();
        RcVec3f vr = new RcVec3f();
        for (int i = 0; i < geom.vertices.Length; i += 3)
        {
            v.X = geom.vertices[i];
            v.Y = geom.vertices[i + 1];
            v.Z = geom.vertices[i + 2];
            MulMatrixVector(ref vr, m, v);
            vr.X += p.X;
            vr.Y += p.Y - 0.1f;
            vr.Z += p.Z;
            verts[i] = vr.X;
            verts[i + 1] = vr.Y;
            verts[i + 2] = vr.Z;
        }

        return verts;
    }

    private static float[] MulMatrixVector(float[] resultvector, float[] matrix, float[] pvector)
    {
        resultvector[0] = matrix[0] * pvector[0] + matrix[4] * pvector[1] + matrix[8] * pvector[2];
        resultvector[1] = matrix[1] * pvector[0] + matrix[5] * pvector[1] + matrix[9] * pvector[2];
        resultvector[2] = matrix[2] * pvector[0] + matrix[6] * pvector[1] + matrix[10] * pvector[2];
        return resultvector;
    }

    private static RcVec3f MulMatrixVector(ref RcVec3f resultvector, RcMatrix4x4f matrix, RcVec3f pvector)
    {
        resultvector.X = matrix.M11 * pvector.X + matrix.M21 * pvector.Y + matrix.M31 * pvector.Z;
        resultvector.Y = matrix.M12 * pvector.X + matrix.M22 * pvector.Y + matrix.M32 * pvector.Z;
        resultvector.Z = matrix.M13 * pvector.X + matrix.M23 * pvector.Y + matrix.M33 * pvector.Z;
        return resultvector;
    }

    public bool Update(TaskFactory executor)
    {
        if (dynaMesh == null)
        {
            return false;
        }

        return dynaMesh.Update(executor);
    }

    public bool Raycast(RcVec3f spos, RcVec3f epos, out float hitPos, out RcVec3f raycastHitPos)
    {
        RcVec3f sp = new RcVec3f(spos.X, spos.Y + 1.3f, spos.Z);
        RcVec3f ep = new RcVec3f(epos.X, epos.Y + 1.3f, epos.Z);

        bool hasHit = dynaMesh.VoxelQuery().Raycast(sp, ep, out hitPos);
        raycastHitPos = hasHit
            ? new RcVec3f(sp.X + hitPos * (ep.X - sp.X), sp.Y + hitPos * (ep.Y - sp.Y), sp.Z + hitPos * (ep.Z - sp.Z))
            : ep;

        return hasHit;
    }
}
