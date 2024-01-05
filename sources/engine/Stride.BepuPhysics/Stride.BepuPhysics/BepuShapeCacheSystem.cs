using BepuPhysics;
using BepuPhysics.Collidables;
using Microsoft.Win32;
using Silk.NET.OpenGL;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Components.Containers.Interfaces;
using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.BepuPhysics.Extensions;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Games;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;
using GeoMeshData = Stride.Graphics.GeometricMeshData<Stride.Graphics.VertexPositionNormalTexture>;

namespace Stride.BepuPhysics
{
    public class BepuShapeCacheSystem
    {
        private IGame? _game = null;

        private BodyShapeData _boxShapeData;
        private BodyShapeData _capsuleShapeData;
        private BodyShapeData _cylinderShapeData;
        private BodyShapeData _sphereShapeData;
        private Dictionary<Model, BodyShapeData> _modelsShapeData = new();
        private Dictionary<Physics.PhysicsColliderShape, BodyShapeData> _hullShapeData = new();

        public BepuShapeCacheSystem(IServiceRegistry Services)
        {
            _boxShapeData = new() { Vertex = GeometricPrimitive.Cube.New(new Vector3(1, 1, 1)).Vertices };
            _capsuleShapeData = new() { Vertex = GeometricPrimitive.Capsule.New(1, 1, 4).Vertices };
            _cylinderShapeData = new() { Vertex = GeometricPrimitive.Cylinder.New(1, 1, 16).Vertices };
            _sphereShapeData = new() { Vertex = GeometricPrimitive.Sphere.New(1, 8).Vertices };

            _game = Services.GetService<IGame>();
        }

        public (BodyShapeData data, BodyShapeTransform transform)[] GetShapeAndOffsets(ContainerComponent component)
        {
            var result = new List<(BodyShapeData data, BodyShapeTransform transform)>();

            if (_game == null)
                return result.ToArray();

            if (component is IContainerWithMesh withMesh)
            {
                if (withMesh.Model == null)
                    return result.ToArray();

                if (!_modelsShapeData.ContainsKey(withMesh.Model))
                {
                    _modelsShapeData.Add(withMesh.Model, GetStrideMeshData(withMesh.Model, _game, new Vector3(1)));
                }

#warning maybe allow mesh transform ? (by adding Scale, Orientation & Offset to IContainerWithMesh)
                result.Add(new(_modelsShapeData[withMesh.Model], new() { }));
            }
            else if (component is IContainerWithColliders withColliders)
            {
                foreach (var collider in withColliders.Colliders)
                {
#warning rotationOffset = collider.RotationOffset '.ToQuaternion"; TODO
                    var rotationOffset = Quaternion.Identity;
                    if (collider is BoxCollider box)
                    {
                        result.Add(new(_boxShapeData, new() { LinearOffset = collider.LinearOffset, RotationOffset = rotationOffset, Scale = box.Size }));
                    }
                    else if (collider is CapsuleCollider cap)
                    {
                        result.Add(new(_capsuleShapeData, new() { LinearOffset = collider.LinearOffset, RotationOffset = rotationOffset, Scale = new Vector3(cap.Radius, cap.Length, cap.Radius) }));
                    }
                    else if (collider is CylinderCollider cyl)
                    {
                        result.Add(new(_cylinderShapeData, new() { LinearOffset = collider.LinearOffset, RotationOffset = rotationOffset, Scale = new Vector3(cyl.Radius, cyl.Length, cyl.Radius) }));
                    }
                    else if (collider is SphereCollider sph)
                    {
                        result.Add(new(_sphereShapeData, new() { LinearOffset = collider.LinearOffset, RotationOffset = rotationOffset, Scale = new Vector3(sph.Radius, sph.Radius, sph.Radius) }));
                    }
                    else if (collider is TriangleCollider tri)
                    {
#warning La flemme n'est pas une raison valable pour balancer une exception
                        throw new Exception("Flemme");
                    }
                    else if (collider is ConvexHullCollider con)
                    {
                        if (con.Hull != null)
                        {
                            if (!_hullShapeData.ContainsKey(con.Hull))
                            {
                                var points = con.GetMeshPoints();
                                var pointTransformed = new VertexPositionNormalTexture[points.Length];

                                for (int i = 0; i < points.Length; i++)
                                {
                                    pointTransformed[i] = new(points[i].ToStrideVector(), Vector3.Zero, Vector2.One);
#warning normals
                                }

                                _hullShapeData.Add(con.Hull, new() { Vertex = pointTransformed, Indices = new int[0] });
                            }
                        }
                    }
                    else
                    {
                        throw new Exception($"collider type {collider.GetType()} is missing in ContainerShapeProcessor, please fill an issue or fix it");
                    }
                }
            }
            else
            {
                throw new Exception($"Container type {component.GetType()} is missing in ContainerShapeProcessor, please fill an issue or fix it");
            }
            return result.ToArray();
        }

#warning need testing (:
        private static unsafe BodyShapeData GetStrideMeshData(Model model, IGame game, Vector3 scale)
        {
            BodyShapeData bodyData = new BodyShapeData();
            int totalVertices = 0, totalIndices = 0;
            foreach (var meshData in model.Meshes)
            {
                totalVertices += meshData.Draw.VertexBuffers[0].Count;
                totalIndices += meshData.Draw.IndexBuffer.Count;
            }

            foreach (var meshData in model.Meshes)
            {
                var vBuffer = meshData.Draw.VertexBuffers[0].Buffer;
                var iBuffer = meshData.Draw.IndexBuffer.Buffer;
                byte[] verticesBytes = vBuffer.GetData<byte>(game.GraphicsContext.CommandList);
                byte[] indicesBytes = iBuffer.GetData<byte>(game.GraphicsContext.CommandList);

                if ((verticesBytes?.Length ?? 0) == 0 || (indicesBytes?.Length ?? 0) == 0)
                {
                    // returns empty lists if there is an issue
                    return bodyData;
                }


                int vertMappingStart = bodyData.Vertex.Length;

                fixed (byte* bytePtr = verticesBytes)
                {
                    var vBindings = meshData.Draw.VertexBuffers[0];
                    int count = vBindings.Count;
                    int stride = vBindings.Declaration.VertexStride;

                    Array.Resize(ref bodyData.Vertex, vertMappingStart + count);

                    for (int i = 0, vHead = vBindings.Offset; i < count; i++, vHead += stride)
                    {
                        var point = *(Vector3*)(bytePtr + vHead);
                        bodyData.Vertex[vertMappingStart + i] = new VertexPositionNormalTexture(point * scale, Vector3.Zero, new Vector2());
#warning normals
                    }
                }

                fixed (byte* bytePtr = indicesBytes)
                {
                    var index = 0;

                    var indiceMappingStart = bodyData.Indices.Length;
                    var count = meshData.Draw.IndexBuffer.Count;

                    Array.Resize(ref bodyData.Vertex, indiceMappingStart + count);


                    if (meshData.Draw.IndexBuffer.Is32Bit)
                    {
                        foreach (int i in new Span<int>(bytePtr + meshData.Draw.IndexBuffer.Offset, count))
                        {
                            bodyData.Indices[indiceMappingStart + index++] = vertMappingStart + i;
                        }
                    }
                    else
                    {
                        foreach (ushort i in new Span<ushort>(bytePtr + meshData.Draw.IndexBuffer.Offset, count))
                        {
                            bodyData.Indices[indiceMappingStart + index++] = vertMappingStart + i;
                        }
                    }
                }
            }

            return bodyData;
        }

    }
}
