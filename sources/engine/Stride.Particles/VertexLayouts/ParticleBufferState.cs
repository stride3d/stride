// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using Stride.Core;

namespace Stride.Particles.VertexLayouts
{
    public struct ParticleBufferState
    {
        public readonly IntPtr VertexBufferOrigin;
        private readonly ParticleVertexBuilder vertexBuilder;

        public IntPtr VertexBuffer;
        public int CurrentParticleIndex;
        public int CurrentVertex;
        public int VertexStride;

        public int VerticesPerSegCurrent;

        public int VerticesPerSegFirst;
        public int VerticesPerSegMiddle;
        public int VerticesPerSegLast;


        public ParticleBufferState(IntPtr vertexBufferPtr, ParticleVertexBuilder builder)
        {
            VertexBuffer        = vertexBufferPtr;
            VertexBufferOrigin  = vertexBufferPtr;

            vertexBuilder       = builder;

            VertexStride        = builder.VertexDeclaration.VertexStride;
            CurrentParticleIndex = 0;
            CurrentVertex       = 0;

            VerticesPerSegCurrent = vertexBuilder.VerticesPerParticle;
            VerticesPerSegFirst = vertexBuilder.VerticesPerParticle;
            VerticesPerSegMiddle = vertexBuilder.VerticesPerParticle;
            VerticesPerSegLast = vertexBuilder.VerticesPerParticle;
        }

        /// <summary>
        /// Sets how many vertices are associated with the first, middle and last quad segments of the buffer. In case of billboards 1 segment = 1 quad but other shapes might be laid out differently
        /// </summary>
        /// <param name="verticesForFirstSegment">Number of vertices for the first segment</param>
        /// <param name="verticesForMiddleSegment">Number of vertices for the middle segments</param>
        /// <param name="verticesForLastSegment">Number of vertices for the last segment</param>
        public void SetVerticesPerSegment(int verticesForFirstSegment, int verticesForMiddleSegment, int verticesForLastSegment)
        {
            VerticesPerSegFirst = verticesForFirstSegment;
            VerticesPerSegMiddle = verticesForMiddleSegment;
            VerticesPerSegLast = verticesForLastSegment;

            VerticesPerSegCurrent = VerticesPerSegFirst;
        }

        /// <summary>
        /// Sets the data for the current vertex using the provided <see cref="AttributeAccessor"/>
        /// </summary>
        /// <param name="accessor">Accessor to the vertex data</param>
        /// <param name="ptrRef">Pointer to the source data</param>
        public void SetAttribute(AttributeAccessor accessor, IntPtr ptrRef)
        {
            CoreUtilities.CopyBlockUnaligned(VertexBuffer + accessor.Offset, ptrRef, accessor.Size);
        }

        /// <summary>
        /// Sets the same data for the all vertices in the current particle using the provided <see cref="AttributeAccessor"/>
        /// </summary>
        /// <param name="accessor">Accessor to the vertex data</param>
        /// <param name="ptrRef">Pointer to the source data</param>
        public void SetAttributePerParticle(AttributeAccessor accessor, IntPtr ptrRef)
        {
            for (var i = 0; i < vertexBuilder.VerticesPerParticle; i++)
            {
                CoreUtilities.CopyBlockUnaligned(VertexBuffer + accessor.Offset + i * VertexStride, ptrRef, accessor.Size);
            }
        }

        /// <summary>
        /// Sets the same data for the all vertices in the current particle using the provided <see cref="AttributeAccessor"/>
        /// </summary>
        /// <param name="accessor">Accessor to the vertex data</param>
        /// <param name="ptrRef">Pointer to the source data</param>
        public void SetAttributePerSegment(AttributeAccessor accessor, IntPtr ptrRef)
        {
            for (var i = 0; i < VerticesPerSegCurrent; i++)
            {
                CoreUtilities.CopyBlockUnaligned(VertexBuffer + accessor.Offset + i * VertexStride, ptrRef, accessor.Size);
            }
        }

        /// <summary>
        /// Transforms attribute data using already written data from another attribute
        /// </summary>
        /// <typeparam name="T">Type data</typeparam>
        /// <param name="accessorTo">Vertex attribute accessor to the destination attribute</param>
        /// <param name="accessorFrom">Vertex attribute accessor to the source attribute</param>
        /// <param name="transformMethod">Transform method for the type data</param>
        public unsafe void TransformAttributePerSegment<T, U>(AttributeAccessor accessorFrom, AttributeAccessor accessorTo, IAttributeTransformer<T, U> transformMethod, ref U transformer) 
            where T : struct
            where U : struct
        {
            for (var i = 0; i < VerticesPerSegCurrent; i++)
            {
                var temp = Unsafe.ReadUnaligned<T>((byte*)VertexBuffer + accessorFrom.Offset + i * VertexStride);

                transformMethod.Transform(ref temp, ref transformer);

                Unsafe.WriteUnaligned((byte*)VertexBuffer + accessorTo.Offset + i * VertexStride, temp);
            }
        }

        public unsafe void TransformAttributePerParticle<T, U>(AttributeAccessor accessorFrom, AttributeAccessor accessorTo, IAttributeTransformer<T, U> transformMethod, ref U transformer) 
            where T : struct
            where U : struct
        {
            for (var i = 0; i < vertexBuilder.VerticesPerParticle; i++)
            {
                var temp = Unsafe.ReadUnaligned<T>((byte*)VertexBuffer + accessorFrom.Offset + i * VertexStride);

                transformMethod.Transform(ref temp, ref transformer);

                Unsafe.WriteUnaligned((byte*)VertexBuffer + accessorTo.Offset + i * VertexStride, temp);
            }
        }


        public AttributeAccessor GetAccessor(AttributeDescription desc) => vertexBuilder.GetAccessor(desc);

        public AttributeDescription DefaultTexCoords => vertexBuilder.DefaultTexCoords;

        /// <summary>
        /// Advances the pointer to the next vertex in the buffer, so that it can be written
        /// </summary>
        public void NextVertex()
        {
            if (++CurrentVertex >= (vertexBuilder.MaxParticles * vertexBuilder.VerticesPerParticle))
                CurrentVertex = (vertexBuilder.MaxParticles * vertexBuilder.VerticesPerParticle) - 1;

            VertexBuffer = VertexBufferOrigin + VertexStride * CurrentVertex;
        }

        /// <summary>
        /// Advances the pointer to the next particle in the buffer, so that its first vertex can be written
        /// </summary>
        public void NextParticle()
        {
            if (++CurrentParticleIndex >= vertexBuilder.MaxParticles)
                CurrentParticleIndex = vertexBuilder.MaxParticles - 1;

            VertexBuffer = VertexBufferOrigin + (VertexStride * CurrentParticleIndex * vertexBuilder.VerticesPerParticle);
        }

        /// <summary>
        /// Advances the pointer to the next segment in the buffer, so that its first vertex can be written
        /// </summary>
        public void NextSegment()
        {
            // The number of segments is tied to the number of particles
            if (++CurrentParticleIndex >= vertexBuilder.MaxParticles)
            {
                // Already at the last particle
                CurrentParticleIndex = vertexBuilder.MaxParticles - 1;
                return;
            }

            VertexBuffer += VertexStride * VerticesPerSegCurrent;
            VerticesPerSegCurrent = (CurrentParticleIndex < vertexBuilder.MaxParticles - 1) ? VerticesPerSegMiddle : VerticesPerSegLast;
        }

        /// <summary>
        /// Moves the index to the beginning of the buffer so that the data can be filled from the first particle again
        /// </summary>
        public void StartOver()
        {
            VertexBuffer = VertexBufferOrigin;
            CurrentParticleIndex = 0;
            CurrentVertex = 0;
            VerticesPerSegCurrent = VerticesPerSegFirst;
        }
    }
}
