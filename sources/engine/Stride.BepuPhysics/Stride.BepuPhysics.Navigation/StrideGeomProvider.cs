using DotRecast.Core.Collections;
using DotRecast.Core.Numerics;
using DotRecast.Core;
using DotRecast.Recast.Geom;
using DotRecast.Recast.Toolset.Geom;
using DotRecast.Recast;

namespace Stride.BepuPhysics.Navigation;
internal class StrideGeomProvider : IInputGeomProvider
{
	public readonly float[] vertices;
	public readonly int[] faces;
	public readonly float[] normals;
	private readonly RcVec3f bmin;
	private readonly RcVec3f bmax;

	private readonly List<RcConvexVolume> _convexVolumes = new List<RcConvexVolume>();
	private readonly List<RcOffMeshConnection> _offMeshConnections = new List<RcOffMeshConnection>();
	private readonly RcTriMesh _mesh;

	public static DemoInputGeomProvider LoadFile(string objFilePath)
	{
		byte[] chunk = RcResources.Load(objFilePath);
		var context = RcObjImporter.LoadContext(chunk);
		return new DemoInputGeomProvider(context.vertexPositions, context.meshFaces);
	}

	public StrideGeomProvider(List<float> vertexPositions, List<int> meshFaces) :
		this(MapVertices(vertexPositions), MapFaces(meshFaces))
	{
	}

	public StrideGeomProvider(float[] vertices, int[] faces)
	{
		this.vertices = vertices;
		this.faces = faces;
		normals = new float[faces.Length];
		CalculateNormals();
		bmin = RcVecUtils.Create(vertices);
		bmax = RcVecUtils.Create(vertices);
		for (int i = 1; i < vertices.Length / 3; i++)
		{
			bmin = RcVecUtils.Min(bmin, vertices, i * 3);
			bmax = RcVecUtils.Max(bmax, vertices, i * 3);
		}

		_mesh = new RcTriMesh(vertices, faces);
	}

	public RcTriMesh GetMesh()
	{
		return _mesh;
	}

	public RcVec3f GetMeshBoundsMin()
	{
		return bmin;
	}

	public RcVec3f GetMeshBoundsMax()
	{
		return bmax;
	}

	public void CalculateNormals()
	{
		for (int i = 0; i < faces.Length; i += 3)
		{
			int v0 = faces[i] * 3;
			int v1 = faces[i + 1] * 3;
			int v2 = faces[i + 2] * 3;
			var e0 = RcVecUtils.Subtract(vertices, v1, v0);
			var e1 = RcVecUtils.Subtract(vertices, v2, v0);

			normals[i] = e0.Y * e1.Z - e0.Z * e1.Y;
			normals[i + 1] = e0.Z * e1.X - e0.X * e1.Z;
			normals[i + 2] = e0.X * e1.Y - e0.Y * e1.X;
			float d = MathF.Sqrt(normals[i] * normals[i] + normals[i + 1] * normals[i + 1] + normals[i + 2] * normals[i + 2]);
			if (d > 0)
			{
				d = 1.0f / d;
				normals[i] *= d;
				normals[i + 1] *= d;
				normals[i + 2] *= d;
			}
		}
	}

	public IList<RcConvexVolume> ConvexVolumes()
	{
		return _convexVolumes;
	}

	public IEnumerable<RcTriMesh> Meshes()
	{
		return RcImmutableArray.Create(_mesh);
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
		_offMeshConnections.RemoveAll(filter); // TODO : 확인 필요
	}

	public bool RaycastMesh(RcVec3f src, RcVec3f dst, out float tmin)
	{
		tmin = 1.0f;

		// Prune hit ray.
		if (!RcIntersections.IsectSegAABB(src, dst, bmin, bmax, out var btmin, out var btmax))
		{
			return false;
		}

		var p = new RcVec2f();
		var q = new RcVec2f();
		p.X = src.X + (dst.X - src.X) * btmin;
		p.Y = src.Z + (dst.Z - src.Z) * btmin;
		q.X = src.X + (dst.X - src.X) * btmax;
		q.Y = src.Z + (dst.Z - src.Z) * btmax;

		List<RcChunkyTriMeshNode> chunks = _mesh.chunkyTriMesh.GetChunksOverlappingSegment(p, q);
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
					vertices[tris[j] * 3],
					vertices[tris[j] * 3 + 1],
					vertices[tris[j] * 3 + 2]
				);
				RcVec3f v2 = new RcVec3f(
					vertices[tris[j + 1] * 3],
					vertices[tris[j + 1] * 3 + 1],
					vertices[tris[j + 1] * 3 + 2]
				);
				RcVec3f v3 = new RcVec3f(
					vertices[tris[j + 2] * 3],
					vertices[tris[j + 2] * 3 + 1],
					vertices[tris[j + 2] * 3 + 2]
				);
				if (RcIntersections.IntersectSegmentTriangle(src, dst, v1, v2, v3, out var t))
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

	private static int[] MapFaces(List<int> meshFaces)
	{
		int[] faces = new int[meshFaces.Count];
		for (int i = 0; i < faces.Length; i++)
		{
			faces[i] = meshFaces[i];
		}

		return faces;
	}

	private static float[] MapVertices(List<float> vertexPositions)
	{
		float[] vertices = new float[vertexPositions.Count];
		for (int i = 0; i < vertices.Length; i++)
		{
			vertices[i] = vertexPositions[i];
		}

		return vertices;
	}
}
