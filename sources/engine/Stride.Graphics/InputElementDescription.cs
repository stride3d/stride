// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

/// <summary>
///   Describes a single input element for the definition of a vertex format layout.
/// </summary>
/// <remarks>
///   Each input element defines how a portion of a vertex or instance is read from a Vertex Buffer
///   and mapped to a shader input.
///   <br/>
///   This structure is typically used to define the layout of vertex data when creating an input layout object.
/// </remarks>
public struct InputElementDescription : IEquatable<InputElementDescription>
{
    /// <summary>
    ///   The semantic name associated with this element, such as <c>"POSITION"</c>, <c>"TEXCOORD"</c>, or <c>"NORMAL"</c>.
    /// </summary>
    /// <remarks>
    ///   This name must match the semantic used in the Vertex Shader input signature.
    /// </remarks>
    public string SemanticName;

    /// <summary>
    ///   The semantic index for the element, used when multiple elements share the same semantic name.
    /// </summary>
    /// <remarks>
    ///   For example, a 4x4 matrix might be passed as four <c>"TEXCOORD"</c> elements with indices 0 through 3.
    /// </remarks>
    public int SemanticIndex;

    /// <summary>
    ///   The data format of the element, such as <see cref="PixelFormat.R32G32B32_Float"/> or <see cref="PixelFormat.R8G8B8A8_UNorm"/>.
    /// </summary>
    /// <remarks>
    ///   This must match the format expected by the Shader and the layout of the Vertex Buffer.
    /// </remarks>
    public PixelFormat Format;

    /// <summary>
    ///   The input slot index from which this element is read.
    /// </summary>
    /// <remarks>
    ///   Multiple Vertex Buffers can be bound to different input slots. This value selects which one to use for this element.
    /// </remarks>
    public int InputSlot;

    /// <summary>
    ///   The byte offset from the start of the vertex to this element.
    /// </summary>
    /// <remarks>
    ///   Use <c>-1</c> (or a constant like <see cref="VertexElement.AppendAligned"/> to automatically align the element after the previous one.
    /// </remarks>
    public int AlignedByteOffset;

    /// <summary>
    ///   Specifies whether the data is <strong>per-vertex</strong> or <strong>per-instance</strong>.
    /// </summary>
    /// <remarks>
    ///   Use <see cref="InputClassification.Vertex"/> for standard vertex attributes,
    ///   or <see cref="InputClassification.Instance"/> for instancing.
    /// </remarks>
    public InputClassification InputSlotClass;

    /// <summary>
    ///   The number of instances to draw using the same per-instance data before advancing to the next element.
    /// </summary>
    /// <remarks>
    ///   This must be 0 for per-vertex data. For instanced data, a value of 1 means the data advances every instance.
    /// </remarks>
    public int InstanceDataStepRate;


    /// <inheritdoc/>
    public readonly bool Equals(InputElementDescription other)
    {
        return string.Equals(SemanticName, other.SemanticName)
               && SemanticIndex == other.SemanticIndex
               && Format == other.Format
               && InputSlot == other.InputSlot
               && AlignedByteOffset == other.AlignedByteOffset
               && InputSlotClass == other.InputSlotClass
               && InstanceDataStepRate == other.InstanceDataStepRate;
    }

    /// <inheritdoc/>
    public override readonly bool Equals(object obj)
    {
        return obj is InputElementDescription iedesc && Equals(iedesc);
    }

    public static bool operator ==(InputElementDescription left, InputElementDescription right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(InputElementDescription left, InputElementDescription right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(SemanticName, SemanticIndex, Format, InputSlot, AlignedByteOffset, InputSlotClass, InstanceDataStepRate);
    }
}
