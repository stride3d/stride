using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Games;
using Stride.Rendering;

namespace Stride.BepuPhysics.Extensions;

public static class MeshExtensions
{
    public static (List<Vector3> verts, List<int> indices) GetMeshVerticesAndIndices(this Model model, IGame game)
    {
        return GetMeshData(model, game.Services, game);
    }

    static unsafe (List<Vector3> verts, List<int> indices) GetMeshData(Model model, IServiceRegistry services, IGame game)
    {
        Matrix[] nodeTransforms = null;

        int totalVerts = 0, totalIndices = 0;
        foreach (var meshData in model.Meshes)
        {
            totalVerts += meshData.Draw.VertexBuffers[0].Count;
            totalIndices += meshData.Draw.IndexBuffer.Count;
        }

        var combinedVerts = new List<Vector3>(totalVerts);
        var combinedIndices = new List<int>(totalIndices);

        foreach (var meshData in model.Meshes)
        {
            var vBuffer = meshData.Draw.VertexBuffers[0].Buffer;
            var iBuffer = meshData.Draw.IndexBuffer.Buffer;
            byte[] verticesBytes = vBuffer.GetData<byte>(game.GraphicsContext.CommandList); ;
            byte[] indicesBytes = iBuffer.GetData<byte>(game.GraphicsContext.CommandList); ;

            if ((verticesBytes?.Length ?? 0) == 0 || (indicesBytes?.Length ?? 0) == 0)
            {
                throw new InvalidOperationException(
                    $"Failed to find mesh buffers while attempting to build. " +
                    $"Make sure that the {nameof(model)} is either an asset on disk, or has its buffer data attached to the buffer through '{nameof(AttachedReference)}'\n" +
                    $"You can also explicitly build using the second constructor instead of this one.");
            }

            int vertMappingStart = combinedVerts.Count;

            fixed (byte* bytePtr = verticesBytes)
            {
                var vBindings = meshData.Draw.VertexBuffers[0];
                int count = vBindings.Count;
                int stride = vBindings.Declaration.VertexStride;
                for (int i = 0, vHead = vBindings.Offset; i < count; i++, vHead += stride)
                {
                    var pos = *(Vector3*)(bytePtr + vHead);
                    if (nodeTransforms != null)
                    {
                        Matrix posMatrix = Matrix.Translation(pos);
                        Matrix.Multiply(ref posMatrix, ref nodeTransforms[meshData.NodeIndex], out var finalMatrix);
                        pos = finalMatrix.TranslationVector;
                    }

                    combinedVerts.Add(pos);
                }
            }

            fixed (byte* bytePtr = indicesBytes)
            {
                if (meshData.Draw.IndexBuffer.Is32Bit)
                {
                    foreach (int i in new Span<int>(bytePtr + meshData.Draw.IndexBuffer.Offset, meshData.Draw.IndexBuffer.Count))
                    {
                        combinedIndices.Add(vertMappingStart + i);
                    }
                }
                else
                {
                    foreach (ushort i in new Span<ushort>(bytePtr + meshData.Draw.IndexBuffer.Offset, meshData.Draw.IndexBuffer.Count))
                    {
                        combinedIndices.Add(vertMappingStart + i);
                    }
                }
            }
        }
        return (combinedVerts, combinedIndices);
    }
}
