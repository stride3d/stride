// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Graphics.Data;

namespace Stride.Extensions
{
    public static class BoundingExtensions
    {
        public static unsafe BoundingBox ComputeBounds(this VertexBufferBinding vertexBufferBinding, ref Matrix matrix, out BoundingSphere boundingSphere)
        {
            var positionOffset = vertexBufferBinding.Declaration
                .EnumerateWithOffsets()
                .First(x => x.VertexElement.SemanticAsText == "POSITION")
                .Offset;

            var boundingBox = BoundingBox.Empty;
            boundingSphere = new BoundingSphere();

            var vertexStride = vertexBufferBinding.Declaration.VertexStride;
            fixed (byte* bufferStart = &vertexBufferBinding.Buffer.GetSerializationData().Content[vertexBufferBinding.Offset])
            {
                // Calculates bounding box and bounding sphere center
                byte* buffer = bufferStart + positionOffset;
                for (int i = 0; i < vertexBufferBinding.Count; ++i)
                {
                    var position = (Vector3*)buffer;
                    Vector3 transformedPosition;

                    Vector3.TransformCoordinate(ref *position, ref matrix, out transformedPosition);

                    // Prepass calculate the center of the sphere
                    Vector3.Add(ref transformedPosition, ref boundingSphere.Center, out boundingSphere.Center);
                    
                    BoundingBox.Merge(ref boundingBox, ref transformedPosition, out boundingBox);
                    
                    buffer += vertexStride;
                }

                //This is the center of our sphere.
                boundingSphere.Center /= (float)vertexBufferBinding.Count;

                // Calculates bounding sphere center
                buffer = bufferStart + positionOffset;
                for (int i = 0; i < vertexBufferBinding.Count; ++i)
                {
                    var position = (Vector3*)buffer;
                    Vector3 transformedPosition;

                    Vector3.TransformCoordinate(ref *position, ref matrix, out transformedPosition);

                    //We are doing a relative distance comparasin to find the maximum distance
                    //from the center of our sphere.
                    float distance;
                    Vector3.DistanceSquared(ref boundingSphere.Center, ref transformedPosition, out distance);

                    if (distance > boundingSphere.Radius)
                        boundingSphere.Radius = distance;

                    buffer += vertexStride;
                }

                //Find the real distance from the DistanceSquared.
                boundingSphere.Radius = (float)Math.Sqrt(boundingSphere.Radius);
            }

            return boundingBox;
        }
    }
}
