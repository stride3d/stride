using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Components.Containers.Interfaces;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.BepuPhysics.Extensions;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Physics;
using Stride.Rendering;

namespace Stride.BepuPhysics
{
    internal class BepuShapeCacheSystem
    {
        private IGame? _game = null;

        private readonly BodyShapeData _boxShapeData;
        private readonly BodyShapeData _cylinderShapeData;
        private readonly BodyShapeData _sphereShapeData;
        private readonly ConditionalWeakTable<Model, ModelShapeCache> _modelsShapeData = new();
        private readonly Dictionary<PhysicsColliderShape, BodyShapeData> _hullShapeData = new();

        record ModelShapeCache(BodyShapeData BodyShapeData); // Weak table doesn't support structures as values, wrap around our structure in a class

        public BepuShapeCacheSystem(IServiceRegistry Services)
        {
            var box = GeometricPrimitive.Cube.New(new Vector3(1, 1, 1));
            var cylinder = GeometricPrimitive.Cylinder.New(1, 1, 8);
            var sphere = GeometricPrimitive.Sphere.New(1, 8);

            _boxShapeData = new() { Vertices = box.Vertices.Select(x => new VertexPosition3(x.Position)).ToArray(), Indices = box.Indices };
            _cylinderShapeData = new() { Vertices = cylinder.Vertices.Select(x => new VertexPosition3(x.Position)).ToArray(), Indices = cylinder.Indices };
            _sphereShapeData = new() { Vertices = sphere.Vertices.Select(x => new VertexPosition3(x.Position)).ToArray(), Indices = sphere.Indices };

            _game = Services.GetService<IGame>();
        }

        public void AppendCachedShapesFor(ContainerComponent containerCompo, List<(BodyShapeData data, BodyShapeTransform transform)> shapes)
        {
            if (containerCompo is IContainerWithMesh meshContainer)
            {
                shapes.Add(BorrowMesh(meshContainer));
            }
            else if (containerCompo is IContainerWithColliders withColliders)
            {
                foreach (var collider in withColliders.Colliders)
                {
                    shapes.Add(collider switch
                    {
                        BoxCollider box => new(_boxShapeData, new() { PositionLocal = collider.PositionLocal, RotationLocal = collider.RotationLocal, Scale = box.Size }),
                        CapsuleCollider cap => new(buildCapsule(cap), new() { PositionLocal = collider.PositionLocal, RotationLocal = collider.RotationLocal, Scale = new(1, 1, 1) }),
                        CylinderCollider cyl => new(_cylinderShapeData, new() { PositionLocal = collider.PositionLocal, RotationLocal = collider.RotationLocal, Scale = new(cyl.Radius, cyl.Length, cyl.Radius) }),
                        SphereCollider sph => new(_sphereShapeData, new() { PositionLocal = collider.PositionLocal, RotationLocal = collider.RotationLocal, Scale = new(sph.Radius, sph.Radius, sph.Radius) }),
                        TriangleCollider tri => new(buildTriangle(tri), new() { PositionLocal = collider.PositionLocal, RotationLocal = collider.RotationLocal, Scale = new(1, 1, 1) }),
                        ConvexHullCollider con => BorrowHull(con),
                        _ => throw new NotImplementedException($"collider type {collider.GetType()} is missing in ContainerShapeProcessor, please fill an issue or fix it"),
                    });
                }
            }
            else
            {
                throw new NotImplementedException($"Container type {containerCompo.GetType()} is missing in ContainerShapeProcessor, please fill an issue or fix it");
            }
        }


#warning that's slow, we could consider build a dictionary<float LenRadRatio, BodyShapeData>.
        private BodyShapeData buildCapsule(CapsuleCollider cap)
        {
            var capGeo = GeometricPrimitive.Capsule.New(cap.Length, cap.Radius, 8);
            return new BodyShapeData() { Vertices = capGeo.Vertices.Select(x => new VertexPosition3(x.Position)).ToArray(), Indices = capGeo.Indices };
        }
        private BodyShapeData buildTriangle(TriangleCollider tri)
        {
            return new BodyShapeData() { Vertices = new[] { tri.A, tri.B, tri.C }.Select(x => new VertexPosition3(x)).ToArray(), Indices = Enumerable.Range(0, 3).ToArray() };
        }

#warning returning empty data or null ?
        public (BodyShapeData data, BodyShapeTransform transform) BorrowHull(ConvexHullCollider convex)
        {
            if (convex.Hull == null)
            {
                return new(new(), new());
            }

            if (_hullShapeData.TryGetValue(convex.Hull, out var hull) == false)
            {
                ExtractHull(convex.Hull, out var points, out var indices);
                hull = new() { Vertices = points, Indices = indices };
                _hullShapeData.Add(convex.Hull, hull);
            }

            return new(hull, new() { PositionLocal = convex.PositionLocal, RotationLocal = convex.RotationLocal, Scale = convex.Scale });
        }
        public (BodyShapeData data, BodyShapeTransform transform) BorrowMesh(IContainerWithMesh meshContainer)
        {
            if (meshContainer.Model == null)
            {
                return new(new(), new());
            }

            if (_modelsShapeData.TryGetValue(meshContainer.Model, out ModelShapeCache? shapeCache) == false)
            {
                ExtractMesh(meshContainer.Model, _game.GraphicsContext.CommandList, out VertexPosition3[] vertices, out int[] indices);
                shapeCache = new(new() { Vertices = vertices, Indices = indices });
                _modelsShapeData.Add(meshContainer.Model, shapeCache);
            }

#warning maybe allow mesh transform ? (by adding Scale, Orientation & Offset to IContainerWithMesh)
            return ((shapeCache.BodyShapeData, new() { PositionLocal = Vector3.Zero, RotationLocal = Quaternion.Identity, Scale = (meshContainer).Entity.Transform.Scale }));
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
        private static unsafe void ExtractMesh(Model model, CommandList commandList, out VertexPosition3[] vertices, out int[] indices)
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

        //internal void ClearShape(ContainerComponent component)
        //{
        //    if (component is IContainerWithMesh withMesh)
        //    {
        //        if (withMesh.Model != null)
        //        {
        //            _modelsShapeData.Remove(withMesh.Model);
        //        }
        //    }
        //    else if (component is IContainerWithColliders withColliders)
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

    }
}
