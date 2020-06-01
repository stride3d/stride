// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Serializers;

namespace Stride.Graphics
{
    /// <summary>
    /// The layout of a vertex buffer with a set of <see cref="VertexElement" />.
    /// </summary>
    [DataContract]
    [DataSerializer(typeof(Serializer))]
    public class VertexDeclaration : IEquatable<VertexDeclaration>
    {
        private readonly VertexElement[] elements;
        private readonly int instanceCount;
        private readonly int vertexStride;
        private readonly int hashCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="VertexDeclaration"/> class.
        /// </summary>
        internal VertexDeclaration() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="VertexDeclaration"/> class.
        /// </summary>
        /// <param name="elements">The elements.</param>
        public VertexDeclaration(params VertexElement[] elements)
            : this(elements, 0, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VertexDeclaration"/> class.
        /// </summary>
        /// <param name="elements">The elements.</param>
        /// <param name="instanceCount">The instance count.</param>
        /// <param name="vertexStride">The vertex stride.</param>
        /// <exception cref="System.ArgumentNullException">elements</exception>
        public VertexDeclaration(VertexElement[] elements, int instanceCount, int vertexStride) 
        {
            if (elements == null) throw new ArgumentNullException("elements");

            this.elements = elements;
            this.vertexStride = vertexStride == 0 ? VertexElementValidator.GetVertexStride(elements) : vertexStride;
            this.instanceCount = instanceCount;

            // Validate Vertices
            VertexElementValidator.Validate(VertexStride, elements);

            hashCode = instanceCount;
            hashCode = (hashCode * 397) ^ vertexStride;
            foreach (var vertexElement in elements)
            {
                hashCode = (hashCode * 397) ^ vertexElement.GetHashCode();
            }
        }

        /// <summary>
        /// Gets the vertex elements.
        /// </summary>
        /// <value>The vertex elements.</value>
        public VertexElement[] VertexElements
        {
            get { return elements; }
        }

        /// <summary>
        /// Gets the instance count.
        /// </summary>
        /// <value>The instance count.</value>
        public int InstanceCount
        {
            get
            {
                return instanceCount;
            }
        }

        /// <summary>
        /// Gets the vertex stride.
        /// </summary>
        /// <value>The vertex stride.</value>
        public int VertexStride
        {
            get
            {
                return vertexStride;
            }
        }

        /// <summary>
        /// Enumerates <see cref="VertexElement"/> with declared offsets.
        /// </summary>
        /// <returns>A set of <see cref="VertexElement"/> with offsets.</returns>
        public IEnumerable<VertexElementWithOffset> EnumerateWithOffsets()
        {
            int offset = 0;
            foreach (var element in VertexElements)
            {
                // Get new offset (if specified)
                var currentElementOffset = element.AlignedByteOffset;
                if (currentElementOffset != VertexElement.AppendAligned)
                    offset = currentElementOffset;

                var elementSize = element.Format.SizeInBytes();
                yield return new VertexElementWithOffset(element, offset, elementSize);

                // Compute next offset (if automatic)
                offset += elementSize;
            }
        }

        /// <summary>
        /// Calculate the size of the vertex declaration.
        /// </summary>
        /// <returns>The size in bytes of the vertex declaration</returns>
        public int CalculateSize()
        {
            var size = 0;
            var offset = 0;
            foreach (var element in VertexElements)
            {
                // Get new offset (if specified)
                var currentElementOffset = element.AlignedByteOffset;
                if (currentElementOffset != VertexElement.AppendAligned)
                    offset = currentElementOffset;

                var elementSize = element.Format.SizeInBytes();

                // Compute next offset (if automatic)
                offset += elementSize;

                size = Math.Max(size, offset); // element are not necessary ordered by increasing offsets
            }

            return size;
        }

        public bool Equals(VertexDeclaration other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return hashCode == other.hashCode && vertexStride == other.vertexStride && instanceCount == other.instanceCount && Utilities.Compare(elements, other.elements);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((VertexDeclaration)obj);
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="VertexElement"/> to <see cref="VertexDeclaration"/>.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator VertexDeclaration(VertexElement element)
        {
            return new VertexDeclaration(element);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="VertexElement[][]"/> to <see cref="VertexDeclaration"/>.
        /// </summary>
        /// <param name="elements">The elements.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator VertexDeclaration(VertexElement[] elements)
        {
            return new VertexDeclaration(elements);
        }

        internal class Serializer : DataSerializer<VertexDeclaration>, IDataSerializerGenericInstantiation
        {
            public override void PreSerialize(ref object obj, ArchiveMode mode, SerializationStream stream)
            {
                // We are creating object at deserialization time
                if (mode == ArchiveMode.Serialize)
                {
                    base.PreSerialize(ref obj, mode, stream);
                }
            }

            public override void Serialize(ref VertexDeclaration obj, ArchiveMode mode, SerializationStream stream)
            {
                if (mode == ArchiveMode.Deserialize)
                {
                    var elements = stream.Read<VertexElement[]>();
                    var instanceCount = stream.ReadInt32();
                    var vertexStride = stream.ReadInt32();
                    obj = new VertexDeclaration(elements, instanceCount, vertexStride);
                }
                else
                {
                    stream.Write(obj.elements);
                    stream.Write(obj.instanceCount);
                    stream.Write(obj.vertexStride);
                }
            }

            public void EnumerateGenericInstantiations(SerializerSelector serializerSelector, IList<Type> genericInstantiations)
            {
                genericInstantiations.Add(typeof(VertexElement[]));
            }
        }
    }
}
