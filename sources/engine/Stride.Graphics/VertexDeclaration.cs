// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

using Stride.Core;
using Stride.Core.Serialization;

namespace Stride.Graphics;

/// <summary>
///   Defines the layout of the vertices in a Vertex Buffer by specifying its component <see cref="VertexElement"/>s.
/// </summary>
[DataContract]
[DataSerializer(typeof(Serializer))]
public sealed class VertexDeclaration : IEquatable<VertexDeclaration>
{
    private readonly VertexElement[] elements;
    private readonly int instanceCount;  // TODO: InstanceCount is not used in any place. Consider removing it or using it in a meaningful way
    private readonly int vertexStride;

    // The precomputed hash code for this VertexDeclaration instance
    private readonly int hashCode;


    /// <summary>
    ///   Initializes a new instance of the <see cref="VertexDeclaration"/> class.
    /// </summary>
    internal VertexDeclaration() { }

    /// <summary>
    ///   Initializes a new instance of the <see cref="VertexDeclaration"/> class.
    /// </summary>
    /// <param name="elements">The elements that compose a vertex.</param>
    public VertexDeclaration(params VertexElement[] elements)
        : this(elements, instanceCount: 0, vertexStride: 0)
    { }

    /// <summary>
    ///   Initializes a new instance of the <see cref="VertexDeclaration"/> class.
    /// </summary>
    /// <param name="elements">The elements that compose a vertex.</param>
    /// <param name="instanceCount">The instance count.</param>
    /// <param name="vertexStride">
    ///   The size of a single vertex in bytes. This is the distance between two consecutive vertices in the Vertex Buffer.
    ///   Specify <c>0</c> to auto-discover the stride from the <paramref name="elements"/>.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="elements"/> is <see langword="null"/>.</exception>
    public VertexDeclaration(VertexElement[] elements, int instanceCount, int vertexStride)
    {
        ArgumentNullException.ThrowIfNull(elements);

        this.elements = elements;
        this.vertexStride = vertexStride == 0 ? VertexElementValidator.GetVertexStride(elements) : vertexStride;
        this.instanceCount = instanceCount;

        // Validate Vertices
        VertexElementValidator.Validate(this.vertexStride, elements);

        // Precompute hash code
        HashCode hash = new();
        hash.Add(instanceCount);
        hash.Add(this.vertexStride);
        foreach (var element in elements)
            hash.Add(element);
        hashCode = hash.ToHashCode();
    }


    /// <summary>
    ///   Gets the Vertex Elements that define the layout of the vertices in this declaration.
    /// </summary>
    [DataMember]
    public VertexElement[] VertexElements => elements;

    // TODO: InstanceCount is not used in any place. Consider removing it or using it in a meaningful way
    /// <summary>
    ///   Gets the instance count.
    /// </summary>
    public int InstanceCount => instanceCount;

    /// <summary>
    ///   Gets the size, in bytes, of a single vertex in this declaration.
    /// </summary>
    public int VertexStride => vertexStride;


    /// <summary>
    ///   Enumerates the Vertex Elements along with their declared offsets.
    /// </summary>
    /// <returns>A sequence of <see cref="VertexElementWithOffset"/>s structures.</returns>
    public IEnumerable<VertexElementWithOffset> EnumerateWithOffsets()
    {
        int offset = 0;
        foreach (var element in VertexElements)
        {
            // Get new offset (if specified)
            var currentElementOffset = element.AlignedByteOffset;
            if (currentElementOffset != VertexElement.AppendAligned)
                offset = currentElementOffset;

            var elementSize = element.Format.SizeInBytes;
            yield return new VertexElementWithOffset(element, offset, elementSize);

            // Compute next offset (if automatic)
            offset += elementSize;
        }
    }

    /// <summary>
    ///   Calculates the size in bytes of a vertex described by this Vertex Declaration.
    /// </summary>
    /// <returns>The size in bytes of the vertices using the layout described by this instance.</returns>
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

            var elementSize = element.Format.SizeInBytes;

            // Compute next offset (if automatic)
            offset += elementSize;

            // Elements are not necessarily ordered by increasing offsets
            size = Math.Max(size, offset);
        }

        return size;
    }


    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
        return obj is VertexDeclaration vertexDeclaration && Equals(vertexDeclaration);
    }

    /// <inheritdoc/>
    public override int GetHashCode() => hashCode;


    /// <summary>
    ///   Performs an implicit conversion from <see cref="VertexElement"/> to <see cref="VertexDeclaration"/>,
    ///   creating a Vertex Declaration with a single Vertex Element.
    /// </summary>
    /// <param name="element">The single Vertex Element.</param>
    /// <returns>The resulting Vertex Declaration.</returns>
    public static implicit operator VertexDeclaration(VertexElement element) => new(element);

    /// <summary>
    ///   Performs an implicit conversion from an array of <see cref="VertexElement"/> to <see cref="VertexDeclaration"/>,
    ///   creating a Vertex Declaration with the provided Vertex Elements.
    /// </summary>
    /// <param name="elements">The Vertex Elements.</param>
    /// <returns>The resulting Vertex Declaration.</returns>
    public static implicit operator VertexDeclaration(VertexElement[] elements) => new(elements);

    #region Serializer

    /// <summary>
    ///   Provides functionality to serialize and deserialize <see cref="VertexDeclaration"/> objects.
    /// </summary>
    internal class Serializer : DataSerializer<VertexDeclaration>, IDataSerializerGenericInstantiation
    {
        /// <inheritdoc/>
        public override void PreSerialize(ref object obj, ArchiveMode mode, SerializationStream stream)
        {
            // We are creating object at deserialization time
            if (mode == ArchiveMode.Serialize)
            {
                base.PreSerialize(ref obj, mode, stream);
            }
        }

        /// <summary>
        ///   Serializes or deserializes a <see cref="VertexDeclaration"/> object.
        /// </summary>
        /// <param name="vertexDeclaration">The object to serialize or deserialize.</param>
        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void EnumerateGenericInstantiations(SerializerSelector serializerSelector, IList<Type> genericInstantiations)
        {
            genericInstantiations.Add(typeof(VertexElement[]));
        }
    }

    #endregion
}
