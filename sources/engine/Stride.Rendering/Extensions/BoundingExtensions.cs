// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Graphics.Data;
using Stride.Graphics.Semantics;

namespace Stride.Extensions
{
    public static class BoundingExtensions
    {
        public static BoundingBox ComputeBounds(this VertexBufferBinding vertexBufferBinding, ref Matrix matrix, out BoundingSphere boundingSphere)
        {
            var helper = new VertexBufferHelper(vertexBufferBinding, vertexBufferBinding.Buffer.GetSerializationData().Content, out _);

            var computeBoundsStruct = new ComputeBoundsStruct
            {
                Box = BoundingBox.Empty, 
                Sphere = new BoundingSphere(),
                Matrix = matrix
            };
            helper.Read<PositionSemantic, Vector3, ComputeBoundsStruct>(default, computeBoundsStruct);

            boundingSphere = computeBoundsStruct.Sphere;
            return computeBoundsStruct.Box;
        }

        struct ComputeBoundsStruct : VertexBufferHelper.IReader<Vector3>
        {
            public required BoundingBox Box;
            public required BoundingSphere Sphere;
            public required Matrix Matrix;

            public unsafe void Read<TConversion, TSource>(byte* startPointer, int elementCount, int stride, Span<Vector3> destination) where TConversion : IConversion<TSource, Vector3> where TSource : unmanaged
            {
                // Calculates bounding box and bounding sphere center
                for (byte* sourcePtr = startPointer, end = startPointer + elementCount * stride; sourcePtr < end; sourcePtr += stride)
                {
                    TConversion.Convert(*(TSource*)sourcePtr, out var position);
                    Vector3 transformedPosition;

                    Vector3.TransformCoordinate(ref position, ref Matrix, out transformedPosition);

                    // Prepass calculate the center of the sphere
                    Vector3.Add(ref transformedPosition, ref Sphere.Center, out Sphere.Center);
                    
                    BoundingBox.Merge(ref Box, ref transformedPosition, out Box);
                }

                //This is the center of our sphere.
                Sphere.Center /= elementCount;

                // Calculates bounding sphere center
                for (byte* sourcePtr = startPointer, end = startPointer + elementCount * stride; sourcePtr < end; sourcePtr += stride)
                {
                    TConversion.Convert(*(TSource*)sourcePtr, out var position);
                    Vector3 transformedPosition;

                    Vector3.TransformCoordinate(ref position, ref Matrix, out transformedPosition);

                    //We are doing a relative distance comparison to find the maximum distance
                    //from the center of our sphere.
                    float distance;
                    Vector3.DistanceSquared(ref Sphere.Center, ref transformedPosition, out distance);

                    if (distance > Sphere.Radius)
                        Sphere.Radius = distance;
                }

                //Find the real distance from the DistanceSquared.
                Sphere.Radius = MathF.Sqrt(Sphere.Radius);
            }
        }
    }
}
