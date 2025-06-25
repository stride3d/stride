// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

using Stride.Core;
using Stride.Core.Serialization;

namespace Stride.Graphics;

[DataContract]
[DataSerializer(typeof(Serializer))]
public class VertexDeclaration : IEquatable<VertexDeclaration>
{
    private readonly VertexElement[] elements;
    private readonly int instanceCount;  // TODO: InstanceCount is not used in any place. Consider removing it or using it in a meaningful way
    private readonly int vertexStride;

    private readonly int hashCode;


    /// <summary>
    /// The layout of a vertex buffer with a set of <see cref="VertexElement" />.
    /// </summary>
    internal VertexDeclaration() { }

    public VertexDeclaration(params VertexElement[] elements)
        : this(elements, instanceCount: 0, vertexStride: 0)
    { }

    public VertexDeclaration(VertexElement[] elements, int instanceCount, int vertexStride)
    {
        ArgumentNullException.ThrowIfNull(elements);

        /// <summary>
        /// Initializes a new instance of the <see cref="VertexDeclaration"/> class.
        /// </summary>
        this.elements = elements;
        this.vertexStride = vertexStride == 0 ? VertexElementValidator.GetVertexStride(elements) : vertexStride;
        this.instanceCount = instanceCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="VertexDeclaration"/> class.
        /// </summary>
        /// <param name="elements">The elements.</param>
        VertexElementValidator.Validate(this.vertexStride, elements);

        /// <summary>
        /// Initializes a new instance of the <see cref="VertexDeclaration"/> class.
        /// </summary>
        /// <param name="elements">The elements.</param>
        /// <param name="instanceCount">The instance count.</param>
        /// <param name="vertexStride">The vertex stride.</param>
        /// <exception cref="System.ArgumentNullException">elements</exception>
        hashCode = HashCode.Combine(instanceCount, this.vertexStride, elements);
    }


            // Validate Vertices
    [DataMember]
    public VertexElement[] VertexElements => elements;

    // TODO: InstanceCount is not used in any place. Consider removing it or using it in a meaningful way
    public int InstanceCount => instanceCount;

        /// <summary>
        /// Gets the vertex elements.
        /// </summary>
        /// <value>The vertex elements.</value>
    public int VertexStride => vertexStride;

        /// <summary>
        /// Gets the instance count.
        /// </summary>
        /// <value>The instance count.</value>

        /// <summary>
        /// Gets the vertex stride.
        /// </summary>
        /// <value>The vertex stride.</value>
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
        /// Enumerates <see cref="VertexElement"/> with declared offsets.
        /// </summary>
        /// <returns>A set of <see cref="VertexElement"/> with offsets.</returns>
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

            // Elements are not necessarily ordered by increasing offsets
            size = Math.Max(size, offset);
        }

        /// <summary>
        /// Calculate the size of the vertex declaration.
        /// </summary>
        /// <returns>The size in bytes of the vertex declaration</returns>
        return size;
    }


    public bool Equals(VertexDeclaration other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return hashCode == other.hashCode
            && vertexStride == other.vertexStride
            && instanceCount == other.instanceCount
            && elements.SequenceEqualAllowNull(other.elements);
    }

    public override bool Equals(object obj)
    {
        return obj is VertexDeclaration vertexDeclaration && Equals(vertexDeclaration);
    }

    public override int GetHashCode() => hashCode;


    public static implicit operator VertexDeclaration(VertexElement element) => new(element);

        /// <summary>
        /// Performs an implicit conversion from <see cref="VertexElement"/> to <see cref="VertexDeclaration"/>.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The result of the conversion.</returns>
    public static implicit operator VertexDeclaration(VertexElement[] elements) => new(elements);

    #region Serializer

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

        /// <summary>
        /// Performs an implicit conversion from <see cref="VertexElement[][]"/> to <see cref="VertexDeclaration"/>.
        /// </summary>
        /// <param name="elements">The elements.</param>
        /// <returns>The result of the conversion.</returns>
        public override void Serialize(ref VertexDeclaration vertexDeclaration, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                var elements = stream.Read<VertexElement[]>();
                var instanceCount = stream.ReadInt32();
                var vertexStride = stream.ReadInt32();
                vertexDeclaration = new VertexDeclaration(elements, instanceCount, vertexStride);
            }
            else
            {
                stream.Write(vertexDeclaration.elements);
                stream.Write(vertexDeclaration.instanceCount);
                stream.Write(vertexDeclaration.vertexStride);
            }
        }

        public void EnumerateGenericInstantiations(SerializerSelector serializerSelector, IList<Type> genericInstantiations)
        {
            genericInstantiations.Add(typeof(VertexElement[]));
        }
    }

    #endregion
}
