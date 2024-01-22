using System.Runtime.InteropServices;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Physics;
using Stride.Rendering;
using BufferPool = BepuUtilities.Memory.BufferPool;
using Mesh = BepuPhysics.Collidables.Mesh;

namespace Stride.BepuPhysics.Systems;

internal class ShapeCacheSystem
{
    private IGame _game;

    internal readonly BasicMeshBuffers _boxShapeData;
    internal readonly BasicMeshBuffers _cylinderShapeData;
    internal readonly BasicMeshBuffers _sphereShapeData;
    private readonly Dictionary<PhysicsColliderShape, BasicMeshBuffers> _hullShapeData = new();

    private readonly BufferPool _sharedPool = new();
    private readonly Dictionary<Model, WeakReference<Cache>> _bepuMeshCache = new();

    public ShapeCacheSystem(IServiceRegistry Services)
    {
        var box = GeometricPrimitive.Cube.New(new Vector3(1, 1, 1));
        var cylinder = GeometricPrimitive.Cylinder.New(1, 1, 8);
        var sphere = GeometricPrimitive.Sphere.New(1, 8);

        _boxShapeData = new() { Vertices = box.Vertices.Select(x => new VertexPosition3(x.Position)).ToArray(), Indices = box.Indices };
        _cylinderShapeData = new() { Vertices = cylinder.Vertices.Select(x => new VertexPosition3(x.Position)).ToArray(), Indices = cylinder.Indices };
        _sphereShapeData = new() { Vertices = sphere.Vertices.Select(x => new VertexPosition3(x.Position)).ToArray(), Indices = sphere.Indices };

        _game = Services.GetService<IGame>();
    }

    /// <summary>
    /// Retrieve the cache for a given model, you MUST store <paramref name="cache"/> to keep the cache alive,
    /// it will be cleaned up by the GC while you're using it otherwise.
    /// </summary>
    /// <param name="model">The model to retrieve data from</param>
    /// <param name="cache">A class that you must store for as long as the data contained is used, that way other calls with the same model hit the cache instead of being rebuilt</param>
    /// <param name="flushModelChanges">Discard the cache for this model, ensuring latest changes made to the model are reflected</param>
    public void GetModelCache(Model model, out Cache cache, bool flushModelChanges = false)
    {
        if (flushModelChanges == false)
        {
            lock (_bepuMeshCache)
            {
                if (_bepuMeshCache.TryGetValue(model, out var weakRefFromCache) && weakRefFromCache.TryGetTarget(out var cached))
                {
                    cache = cached;
                    return;
                }
            }
        }

        cache = new(model, this);
        var weakRef = new WeakReference<Cache>(cache);

        lock (_bepuMeshCache)
        {
            _bepuMeshCache[model] = weakRef; // Setting the key directly, it's fine to overwrite whatever was on that key if it so happens
        }
    }

#warning that's slow, we could consider build a dictionary<float LenRadRatio, BodyShapeData>.
    internal BasicMeshBuffers BuildCapsule(CapsuleCollider cap)
    {
        var capGeo = GeometricPrimitive.Capsule.New(cap.Length, cap.Radius, 8);
        return new() { Vertices = capGeo.Vertices.Select(x => new VertexPosition3(x.Position)).ToArray(), Indices = capGeo.Indices };
    }
    internal BasicMeshBuffers BuildTriangle(TriangleCollider tri)
    {
        return new() { Vertices = new VertexPosition3[] { new(tri.A), new(tri.B), new(tri.C) }, Indices = new[]{0, 1, 2} };
    }

    public BasicMeshBuffers BorrowHull(ConvexHullCollider convex)
    {
        if (convex.Hull == null)
        {
            return new();
        }

        if (_hullShapeData.TryGetValue(convex.Hull, out var hull) == false)
        {
            ExtractHull(convex.Hull, out var points, out var indices);
            hull = new() { Vertices = points, Indices = indices };
            _hullShapeData.Add(convex.Hull, hull);
        }

        return hull;
    }

    private static void ExtractHull(PhysicsColliderShape Hull, out VertexPosition3[] outPoints, out int[] outIndices)
    {
        int vertexCount = 0;
        int indexCount = 0;
        foreach (var colliderShapeDesc in Hull.Descriptions)
        {
            if (colliderShapeDesc is not ConvexHullColliderShapeDesc hullDesc) // This casting nonsense should be replaced once we have a proper asset to host convex shapes
                continue;

            for (int mesh = 0; mesh < hullDesc.ConvexHulls.Count; mesh++)
            {
                for (var hull = 0; hull < hullDesc.ConvexHulls[mesh].Count; hull++)
                {
                    vertexCount += hullDesc.ConvexHulls[mesh][hull].Count;
                    indexCount += hullDesc.ConvexHullsIndices[mesh][hull].Count;
                }
            }
        }

        outPoints = new VertexPosition3[vertexCount];
        var outPointsWithAutoCast = MemoryMarshal.Cast<VertexPosition3, System.Numerics.Vector3>(outPoints.AsSpan());
        outIndices = new int[indexCount];
        int vertexWriteHead = 0;
        int indexWriteHead = 0;

        foreach (var colliderShapeDesc in Hull.Descriptions)
        {
            if (colliderShapeDesc is not ConvexHullColliderShapeDesc hullDesc)
                continue;

            System.Numerics.Vector3 hullScaling = hullDesc.Scaling.ToNumericVector();
            for (int mesh = 0; mesh < hullDesc.ConvexHulls.Count; mesh++)
            {
                for (var hull = 0; hull < hullDesc.ConvexHulls[mesh].Count; hull++)
                {
                    var hullVerts = hullDesc.ConvexHulls[mesh][hull];
                    var hullIndices = hullDesc.ConvexHullsIndices[mesh][hull];

                    int vertMappingStart = vertexWriteHead;
                    for (int i = 0; i < hullVerts.Count; i++)
                        outPointsWithAutoCast[vertexWriteHead++] = hullVerts[i].ToNumericVector() * hullScaling;

                    for (int i = 0; i < hullIndices.Count; i++)
                        outIndices[indexWriteHead++] = vertMappingStart + (int)hullIndices[i];
                }
            }
        }
    }

    //internal void ClearShape(ContainerComponent component)
    //{
    //    if (component is ContainerComponentWithMesh withMesh)
    //    {
    //        if (withMesh.Model != null)
    //        {
    //            _modelsShapeData.Remove(withMesh.Model);
    //        }
    //    }
    //    else if (component is ContainerComponentWithColliders withColliders)
    //    {
    //        foreach (var collider in withColliders.Colliders)
    //        {
    //            if (collider is ConvexHullCollider con && con.Hull != null)
    //            {
    //                _hullShapeData.Remove(con.Hull);
    //            }
    //        }
    //    }
    //}

    internal static unsafe BepuUtilities.Memory.Buffer<Triangle> ExtractBepuMesh(Model model, IGame game, BufferPool pool)
    {
        int totalIndices = 0;
        foreach (var meshData in model.Meshes)
        {
            totalIndices += meshData.Draw.IndexBuffer.Count;
        }

        pool.Take<Triangle>(totalIndices / 3, out var triangles);
        var triangleAsV3 = triangles.As<Vector3>();
        int triangleV3Index = 0;

        foreach (var meshData in model.Meshes)
        {
            // Get mesh data from GPU or shared memory, this can be quite slow
            byte[] verticesBytes = meshData.Draw.VertexBuffers[0].Buffer.GetData<byte>(game.GraphicsContext.CommandList);
            byte[] indicesBytes = meshData.Draw.IndexBuffer.Buffer.GetData<byte>(game.GraphicsContext.CommandList);

            var vBindings = meshData.Draw.VertexBuffers[0];
            int vStride = vBindings.Declaration.VertexStride;
            var position = vBindings.Declaration.EnumerateWithOffsets().First(x => x.VertexElement.SemanticName == VertexElementUsage.Position);

            if (position.VertexElement.Format is PixelFormat.R32G32B32_Float or PixelFormat.R32G32B32A32_Float == false)
                throw new ArgumentException($"{model}'s vertex position must be declared as float3 or float4");

            fixed (byte* vBuffer = &verticesBytes[vBindings.Offset])
            fixed (byte* iBuffer = indicesBytes)
            {
                if (meshData.Draw.IndexBuffer.Is32Bit)
                {
                    foreach (int i in new Span<int>(iBuffer + meshData.Draw.IndexBuffer.Offset, meshData.Draw.IndexBuffer.Count))
                    {
                        triangleAsV3[triangleV3Index++] = *(Vector3*)(vBuffer + vStride * i + position.Offset); // start of the buffer, move to the 'i'th vertex, and read from the position field of that vertex
                    }
                }
                else
                {
                    foreach (ushort i in new Span<ushort>(iBuffer + meshData.Draw.IndexBuffer.Offset, meshData.Draw.IndexBuffer.Count))
                    {
                        triangleAsV3[triangleV3Index++] = *(Vector3*)(vBuffer + vStride * i + position.Offset);
                    }
                }
            }
        }

        return triangles;
    }

    private static unsafe void ExtractMeshBuffers(Model model, Graphics.CommandList commandList, out VertexPosition3[] vertices, out int[] indices)
    {
        int totalVertices = 0, totalIndices = 0;
        foreach (var meshData in model.Meshes)
        {
            totalVertices += meshData.Draw.VertexBuffers[0].Count;
            totalIndices += meshData.Draw.IndexBuffer.Count;
        }

        vertices = new VertexPosition3[totalVertices];
        indices = new int[totalIndices];

        int vertexWriteHead = 0;
        int indexWriteHead = 0;
        foreach (var meshData in model.Meshes)
        {
            var vBuffer = meshData.Draw.VertexBuffers[0].Buffer;
            var iBuffer = meshData.Draw.IndexBuffer.Buffer;
            byte[] verticesBytes = vBuffer.GetData<byte>(commandList);
            byte[] indicesBytes = iBuffer.GetData<byte>(commandList);

            if (verticesBytes == null || indicesBytes == null)
                throw new InvalidOperationException($"Could not extract data from gpu for '{model}', maybe this model isn't uploaded to the gpu yet ?");

            if (verticesBytes.Length == 0 || indicesBytes.Length == 0)
            {
                vertices = Array.Empty<VertexPosition3>();
                indices = Array.Empty<int>();
                return;
            }

            int vertMappingStart = vertexWriteHead;
            fixed (byte* bytePtr = verticesBytes)
            {
                var vBindings = meshData.Draw.VertexBuffers[0];
                int count = vBindings.Count;
                int stride = vBindings.Declaration.VertexStride;

                for (int i = 0, vHead = vBindings.Offset; i < count; i++, vHead += stride)
                {
                    vertices[vertexWriteHead++].Position = *(Vector3*)(bytePtr + vHead);
                }
            }

            fixed (byte* bytePtr = indicesBytes)
            {
                var count = meshData.Draw.IndexBuffer.Count;

                if (meshData.Draw.IndexBuffer.Is32Bit)
                {
                    foreach (int indexBufferValue in new Span<int>(bytePtr + meshData.Draw.IndexBuffer.Offset, count))
                    {
                        indices[indexWriteHead++] = vertMappingStart + indexBufferValue;
                    }
                }
                else
                {
                    foreach (ushort indexBufferValue in new Span<ushort>(bytePtr + meshData.Draw.IndexBuffer.Offset, count))
                    {
                        indices[indexWriteHead++] = vertMappingStart + indexBufferValue;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Matrices may not produce a valid local scale when decomposed into T,R,S; the basis of the matrix may be skewed/sheered depending on transformations
    /// coming from parents, sheering is by definition not on the local basis
    /// </summary>
    internal static Vector3 GetClosestToDecomposableScale(Matrix matrix)
    {
        float d1 = Vector3.Dot((Vector3)matrix.Row1, (Vector3)matrix.Row2);
        float d2 = Vector3.Dot((Vector3)matrix.Row2, (Vector3)matrix.Row3);
        float d3 = Vector3.Dot((Vector3)matrix.Row1, (Vector3)matrix.Row3);
        Vector3 o;
        if (MathF.Abs(d1) > float.Epsilon || MathF.Abs(d2) > float.Epsilon || MathF.Abs(d3) > float.Epsilon) // Matrix is skewed, scale cannot be
        {
            Span<Vector3> basisIn = stackalloc Vector3[]
            {
                (Vector3)matrix.Row1,
                (Vector3)matrix.Row2,
                (Vector3)matrix.Row3,
            };
            Span<Vector3> basisOut = stackalloc Vector3[3];
            Orthogonalize(basisIn, basisOut);
            o.X = basisOut[0].Length();
            o.Y = basisOut[1].Length();
            o.Z = basisOut[2].Length();
        }
        else
        {
            o.X = matrix.Row1.Length();
            o.Y = matrix.Row2.Length();
            o.Z = matrix.Row3.Length();
        }

        return o;

        static void Orthogonalize(ReadOnlySpan<Vector3> source, Span<Vector3> destination)
        {
            // Dump of strides' method to strip the memory alloc, refer to Vector3.Orthogonalize
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (destination.Length < source.Length)
                throw new ArgumentOutOfRangeException(nameof(destination), "The destination array must be of same length or larger length than the source array.");

            for (int i = 0; i < source.Length; ++i)
            {
                Vector3 newVector = source[i];

                for (int r = 0; r < i; ++r)
                {
                    newVector -= (Vector3.Dot(destination[r], newVector) / Vector3.Dot(destination[r], destination[r])) * destination[r];
                }

                destination[i] = newVector;
            }
        }
    }

    /// <summary>
    /// Hold onto this to keep the cache and the bepu shape for the corresponding mesh alive
    /// </summary>
    public record Cache(Model TargetModel, ShapeCacheSystem CacheSystem)
    {
#warning consider splitting buffer and bepu cache into individual caches instead of grouped like here, otherwise buffers will be kept in memory when hit once by the navmesh and held through mesh physics
        (VertexPosition3[] Vertices, int[] Indices)? _buffers;
        Mesh? _bepuMesh;
        public Mesh GetBepuMesh(Vector3 scale)
        {
            Mesh newMesh;

            if (_bepuMesh is { } mesh)
            {
                newMesh = mesh;
            }
            else
            {
                lock (CacheSystem._sharedPool)
                {
                    var triangles = ExtractBepuMesh(TargetModel, CacheSystem._game, CacheSystem._sharedPool);
                    newMesh = new Mesh(triangles, System.Numerics.Vector3.One, CacheSystem._sharedPool);
                }
            }

            _bepuMesh = newMesh;
            newMesh.Scale = scale.ToNumericVector();
            return newMesh;
        }

        public void GetBuffers(out VertexPosition3[] vertices, out int[] indices)
        {
            if (_buffers is { } buffers)
            {
                vertices = buffers.Vertices;
                indices = buffers.Indices;
            }
            else
            {
                ExtractMeshBuffers(TargetModel, CacheSystem._game.GraphicsContext.CommandList, out vertices, out indices);
                _buffers = (vertices, indices);
            }
        }

        ~Cache()
        {
            // /!\ THIS MAY RUN OUTSIDE OF THE MAIN THREAD /!\

            lock (CacheSystem._bepuMeshCache)
            {
                if (CacheSystem._bepuMeshCache.TryGetValue(TargetModel, out var weakRef))
                {
                    // It could be the case that the cache was replaced by another cache, do not remove in those cases
                    if (weakRef.TryGetTarget(out var cache) == false || ReferenceEquals(cache, this))
                        CacheSystem._bepuMeshCache.Remove(TargetModel);
                }
            }

            lock (CacheSystem._sharedPool)
            {
                _bepuMesh?.Dispose(CacheSystem._sharedPool);
            }

            // /!\ THIS MAY RUN OUTSIDE OF THE MAIN THREAD /!\
        }
    }
}