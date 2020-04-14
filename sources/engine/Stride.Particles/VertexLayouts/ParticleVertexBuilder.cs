// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Graphics;
using Buffer = Stride.Graphics.Buffer;

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
            Utilities.CopyMemory(VertexBuffer + accessor.Offset, ptrRef, accessor.Size);
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
                Utilities.CopyMemory(VertexBuffer + accessor.Offset + i * VertexStride, ptrRef, accessor.Size);
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
                Utilities.CopyMemory(VertexBuffer + accessor.Offset + i * VertexStride, ptrRef, accessor.Size);
            }
        }

        /// <summary>
        /// Transforms attribute data using already written data from another attribute
        /// </summary>
        /// <typeparam name="T">Type data</typeparam>
        /// <param name="accessorTo">Vertex attribute accessor to the destination attribute</param>
        /// <param name="accessorFrom">Vertex attribute accessor to the source attribute</param>
        /// <param name="transformMethod">Transform method for the type data</param>
        public void TransformAttributePerSegment<T, U>(AttributeAccessor accessorFrom, AttributeAccessor accessorTo, IAttributeTransformer<T, U> transformMethod, ref U transformer) 
            where T : struct
            where U : struct
        {
            for (var i = 0; i < VerticesPerSegCurrent; i++)
            {
                var temp = Utilities.Read<T>(VertexBuffer + accessorFrom.Offset + i * VertexStride);

                transformMethod.Transform(ref temp, ref transformer);

                Utilities.Write(VertexBuffer + accessorTo.Offset + i * VertexStride, ref temp);
            }
        }

        public void TransformAttributePerParticle<T, U>(AttributeAccessor accessorFrom, AttributeAccessor accessorTo, IAttributeTransformer<T, U> transformMethod, ref U transformer) 
            where T : struct
            where U : struct
        {
            for (var i = 0; i < vertexBuilder.VerticesPerParticle; i++)
            {
                var temp = Utilities.Read<T>(VertexBuffer + accessorFrom.Offset + i * VertexStride);

                transformMethod.Transform(ref temp, ref transformer);

                Utilities.Write(VertexBuffer + accessorTo.Offset + i * VertexStride, ref temp);
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

    /// <summary>
    /// Manager class for the vertex buffer stream which can dynamically change the required vertex layout and rebuild the buffer based on the particle fields
    /// </summary>
    public class ParticleVertexBuilder
    {
        public int VerticesPerParticle { get; private set; } = 4;
        private const int verticesPerQuad = 4;

        public readonly int IndicesPerQuad = 6;

        public delegate void TransformAttributeDelegate<T>(ref T value);

        private readonly int indexStructSize;

        private readonly Dictionary<AttributeDescription, AttributeAccessor> availableAttributes;

        private readonly List<VertexElement> vertexElementList;

        private int requiredQuads;

        public int MaxParticles { get; private set; }

        public int LivingQuads { get; private set; }

        public bool IsBufferDirty { get; private set; } = true;

        /// <summary>
        /// Default constructor
        /// </summary>
        public ParticleVertexBuilder()
        {
            vertexElementList = new List<VertexElement>();

            ResetVertexElementList();

            indexStructSize = sizeof(short);

            availableAttributes = new Dictionary<AttributeDescription, AttributeAccessor>();

            UpdateVertexLayout();
        }

        /// <summary>
        /// The current <see cref="Graphics.VertexDeclaration"/> of the contained vertex buffer
        /// </summary>
        public VertexDeclaration VertexDeclaration { get; private set; }

        /// <summary>
        /// The current number of vertices which have to be drawn
        /// </summary>
        public int VertexCount => LivingQuads * verticesPerQuad;

        /// <summary>
        /// The default texture coordinates will default to the first texture coordinates element added to the list in case there are two or more sets
        /// </summary>
        public AttributeDescription DefaultTexCoords { get; private set; } = new AttributeDescription(null);

        /// <summary>
        /// Resets the list of required vertex elements, setting it to the minimum mandatory length
        /// </summary>
        public void ResetVertexElementList()
        {
            vertexElementList.Clear();

            // Mandatory
            AddVertexElement(ParticleVertexElements.Position);
        }

        /// <summary>
        /// Adds a new required element to the list of vertex elements, if it's not in the list already
        /// </summary>
        /// <param name="element">New element to add</param>
        public void AddVertexElement(VertexElement element)
        {
            if (vertexElementList.Contains(element))
                return;

            vertexElementList.Add(element);
        }

        /// <summary>
        /// Updates the vertex layout with the new list. Should be called only when there have been changes to the list.
        /// </summary>
        public void UpdateVertexLayout()
        {
            VertexDeclaration = new VertexDeclaration(vertexElementList.ToArray());

            availableAttributes.Clear();
            DefaultTexCoords = new AttributeDescription(null);

            var totalOffset = 0;
            foreach (var vertexElement in VertexDeclaration.VertexElements)
            {
                var attrDesc = new AttributeDescription(vertexElement.SemanticAsText);
                if (DefaultTexCoords.GetHashCode() == 0 && vertexElement.SemanticAsText.Contains("TEXCOORD"))
                {
                    DefaultTexCoords = attrDesc;
                }

                var stride = vertexElement.Format.SizeInBytes();
                var attrAccs = new AttributeAccessor { Offset = totalOffset, Size = stride };
                totalOffset += stride;

                availableAttributes.Add(attrDesc, attrAccs);
            }

            IsBufferDirty = true;
        }

        /// <summary>
        /// Sets the required quads per particle and number of particles so that a sufficiently big buffer can be allocated
        /// </summary>
        /// <param name="quadsPerParticle">Required quads per particle, assuming 1 quad = 4 vertices = 6 indices</param>
        /// <param name="livingParticles">Number of living particles this frame</param>
        /// <param name="totalParticles">Number of total number of possible particles for the parent emitter</param>
        public void SetRequiredQuads(int quadsPerParticle, int livingParticles, int totalParticles)
        {
            VerticesPerParticle = quadsPerParticle * verticesPerQuad;
            var minQuads = quadsPerParticle * livingParticles;
            var maxQuads = quadsPerParticle * totalParticles;

            LivingQuads = minQuads;
            MaxParticles = livingParticles;

            if (minQuads > requiredQuads || maxQuads < requiredQuads / 2)
            {
                requiredQuads = maxQuads;
                IsBufferDirty = true;
            }
        }

        /// <summary>
        /// Resets the <see cref="ParticleVertexBuilder"/> to its initial state, freeing any graphics memory used
        /// </summary>
        public void Reset()
        {
            SetRequiredQuads(4, 0, 0);
        }

        public void SetDirty(bool dirty)
        {
            IsBufferDirty = dirty;
        }

        internal AttributeAccessor GetAccessor(AttributeDescription desc)
        {
            AttributeAccessor accessor;
            if (!availableAttributes.TryGetValue(desc, out accessor))
            {
                return new AttributeAccessor { Offset = 0, Size = 0 };
            }

            return accessor;
        }
    }
}
