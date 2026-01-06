// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2012 SharpDX - Alexandre Mutel
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Text.RegularExpressions;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;

using Half = Stride.Core.Mathematics.Half;

namespace Stride.Graphics;

/// <summary>
/// A description of a single element for the input-assembler stage. This structure is related to <see cref="SharpDX.Direct3D11.InputElement"/>.
/// </summary>
/// <remarks>
/// Because <see cref="SharpDX.Direct3D11.InputElement"/> requires to have the same <see cref="VertexBufferLayout.SlotIndex"/>, <see cref="VertexBufferLayout.VertexClassification"/> and <see cref="VertexBufferLayout.instanceDataStepRate"/>,
/// the <see cref="VertexBufferLayout"/> structure encapsulates a set of <see cref="VertexElement"/> for a particular slot, classification and instance data step rate.
/// Unlike the default <see cref="SharpDX.Direct3D11.InputElement"/>, this structure accepts a semantic name with a postfix number that will be automatically extracted to the semantic index.
/// </remarks>
/// <seealso cref="VertexBufferLayout"/>
[DataContract]
[DataSerializer(typeof(Serializer))]
public partial struct VertexElement : IEquatable<VertexElement>
{
    private string semanticName;
    private int semanticIndex;
    private PixelFormat format;
    private int alignedByteOffset;
    private readonly int hashCode;


    // Match the last digit of a semantic name.
    private static readonly Regex MatchSemanticIndex = MatchSemanticIndexRegex();
    [GeneratedRegex(@"(.*)(\d+)$")]
    private static partial Regex MatchSemanticIndexRegex();

    /// <summary>
    ///   Returns a value that can be used for the offset parameter of an Vertex Element to indicate that the element
    ///   should be aligned directly after the previous one, including any packing if neccessary.
    /// </summary>
    public const int AppendAligned = -1;


    /// <summary>
    ///   Initializes a new instance of the <see cref="VertexElement" /> structure.
    /// </summary>
    /// <param name="semanticName">
    ///   The semantic name associated with this element, such as <c>"POSITION"</c>, <c>"TEXCOORD"</c>, or <c>"NORMAL"</c>.
    ///   <br/>
    ///   If the semantic name contains a postfix number, this number will be used as a semantic index,
    ///   such as <c>"TEXCOORD1"</c> or <c>"COLOR0"</c>.
    /// </param>
    /// <param name="format">
    ///   The data format of the element, such as <see cref="PixelFormat.R32G32B32_Float"/> (equivalent to <see cref="Vector3"/>)
    ///   or <see cref="PixelFormat.R8G8B8A8_UNorm"/> (equivalent to a 32-bit, 8bpc RGBA color).
    /// </param>
    /// <exception cref="ArgumentException">
    ///   <paramref name="semanticName"/> is <see langword="null"/> or an empty <see langword="string"/>.
    /// </exception>
    public VertexElement(string semanticName, PixelFormat format)
        : this()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(semanticName);

        // All semantics will be upper case
        semanticName = semanticName.ToUpperInvariant();

        var match = MatchSemanticIndex.Match(semanticName);
        if (match.Success)
        {
            // Convert to singleton string in order to speed up things
            // TODO: Stale comment? Use string.Intern?
            this.semanticName = match.Groups[1].Value;

            if (!uint.TryParse(match.Groups[2].Value, out var semanticIndex))
                throw new ArgumentException("Could not parse semantic index from the semantic name", nameof(semanticName));

            this.semanticIndex = (int) semanticIndex;
        }
        else this.semanticName = semanticName;

        this.format = format;
        alignedByteOffset = AppendAligned;

        // Precalculate hashcode
        hashCode = ComputeHashCode();
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref="VertexElement" /> structure.
    /// </summary>
    /// <param name="semanticName">
    ///   The semantic name associated with this element, such as <c>"POSITION"</c>, <c>"TEXCOORD"</c>, or <c>"NORMAL"</c>.
    /// </param>
    /// <param name="semanticIndex">
    ///   The semantic index for the element, used when multiple elements share the same semantic name,
    ///   such as the <c>1</c> in <c>"TEXCOORD1"</c> or the <c>0</c> in <c>"COLOR0"</c>.
    /// </param>
    /// <param name="format">
    ///   The data format of the element, such as <see cref="PixelFormat.R32G32B32_Float"/> (equivalent to <see cref="Vector3"/>)
    ///   or <see cref="PixelFormat.R8G8B8A8_UNorm"/> (equivalent to a 32-bit, 8bpc RGBA color).
    /// </param>
    /// <param name="alignedByteOffset">
    ///   The byte offset from the start of the vertex to this element.
    ///   Use <c>-1</c> (or a constant like <see cref="AppendAligned"/> to automatically align the element after the previous one.
    /// </param>
    /// <exception cref="ArgumentException">
    ///   <paramref name="semanticName"/> is <see langword="null"/> or an empty <see langword="string"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///   Cannot specify a semantic index in the <paramref name="semanticName"/> when using this constructor.
    ///   Use the constructor with explicit semantic index instead (<see cref="VertexElement(string, PixelFormat)"/>).
    /// </exception>
    public VertexElement(string semanticName, int semanticIndex, PixelFormat format, int alignedByteOffset = AppendAligned)
        : this()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(semanticName);

        // All semantics will be upper case
        semanticName = semanticName.ToUpperInvariant();

        var match = MatchSemanticIndex.Match(semanticName);
        if (match.Success)
            throw new ArgumentException("Cannot specify a semantic index when using the constructor with explicit semantic index. Use the implicit semantic index constructor.");

        // Convert to singleton string in order to speed up things
        // TODO: Stale comment? Use string.Intern?
        this.semanticName = semanticName;
        this.semanticIndex = semanticIndex;
        this.format = format;
        this.alignedByteOffset = alignedByteOffset;

        // Precalculate hashcode
        hashCode = ComputeHashCode();
    }


    /// <summary>
    ///   Gets the HLSL semantic name associated with this element, such as <c>"POSITION"</c>, <c>"TEXCOORD"</c>, or <c>"NORMAL"</c>.
    /// </summary>
    /// <remarks>
    ///   This name must match the semantic used in the Vertex Shader input signature.
    /// </remarks>
    public readonly string SemanticName => semanticName;

    /// <summary>
    ///   Gets the HLSL semantic name associated with this element, such as <c>"POSITION"</c>, <c>"TEXCOORD"</c>, or <c>"NORMAL"</c>.
    ///   <br/>
    ///   It can contain a postfix number, the <see cref="SemanticIndex"/>, such as <c>"TEXCOORD1"</c> or <c>"COLOR0"</c>.
    /// </summary>
    /// <remarks>
    ///   This name must match the semantic used in the Vertex Shader input signature.
    /// </remarks>
    /// <seealso cref="SemanticName"/>
    /// <seealso cref="SemanticIndex"/>
    public readonly string SemanticAsText
        => semanticIndex == 0
            ? semanticName
            : FormattableString.Invariant($"{semanticName}{semanticIndex}");

    /// <summary>
    ///   Gets the semantic index for the element, used when multiple elements share the same semantic name.
    /// </summary>
    /// <remarks>
    ///   For example, a 4x4 matrix might be passed as four <c>"TEXCOORD"</c> elements with indices 0 through 3.
    /// </remarks>
    public readonly int SemanticIndex => semanticIndex;

    /// <summary>
    ///   The data format of the element, such as <see cref="PixelFormat.R32G32B32_Float"/> or <see cref="PixelFormat.R8G8B8A8_UNorm"/>.
    /// </summary>
    /// <remarks>
    ///   This must match the format expected by the Shader and the layout of the Vertex Buffer.
    /// </remarks>
    public readonly PixelFormat Format => format;

    /// <summary>
    ///   The byte offset from the start of the vertex to this element.
    /// </summary>
    /// <remarks>
    ///   Use <c>-1</c> (or a constant like <see cref="AppendAligned"/> to automatically align the element after the previous one,
    ///   including any packing if necessary.
    /// </remarks>
    public readonly int AlignedByteOffset => alignedByteOffset;


    /// <inheritdoc/>
    public readonly bool Equals(VertexElement other)
    {
        // First use hashCode to compute
        return hashCode == other.hashCode
            && semanticName.Equals(other.semanticName)
            && semanticIndex == other.semanticIndex
            && format == other.format
            && alignedByteOffset == other.alignedByteOffset;
    }

    /// <inheritdoc/>
    public override readonly bool Equals(object obj)
    {
        return obj is VertexElement ve && Equals(ve);
    }

    public static bool operator ==(VertexElement left, VertexElement right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(VertexElement left, VertexElement right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode() => hashCode;
    //
    // Computes the hash code for this VertexElement so it can be cached.
    //
    private readonly int ComputeHashCode()
    {
        return HashCode.Combine(semanticName, semanticIndex, format, alignedByteOffset);
    }

    /// <inheritdoc/>
    public override readonly string ToString()
    {
        return FormattableString.Invariant($"{SemanticAsText},{Format},{AlignedByteOffset}");
    }

    #region Common VertexElements

    /// <summary>
    ///   Declares a <see cref="VertexElement"/> with the semantic <c>"COLOR"</c>.
    /// </summary>
    /// <typeparam name="T">The type of the color element.</typeparam>
    /// <param name="semanticIndex">The semantic index for the element, used when multiple elements share the same semantic name.</param>
    /// <param name="offsetInBytes">
    ///   The byte offset from the start of the vertex to this element.
    ///   Use <c>-1</c> (or a constant like <see cref="AppendAligned"/> to automatically align the element after the previous one.
    /// </param>
    /// <returns>A new Vertex Element that represents the <c>"COLOR"</c> semantic.</returns>
    /// <exception cref="NotSupportedException">
    ///   The specified <typeparamref name="T"/> is not supported. It cannot be converted to a <see cref="PixelFormat"/>.
    /// </exception>
    public static VertexElement Color<T>(int semanticIndex = 0, int offsetInBytes = AppendAligned) where T : struct
    {
        return Color(semanticIndex, ConvertTypeToFormat<T>(), offsetInBytes);
    }

    /// <summary>
    ///   Declares a <see cref="VertexElement"/> with the semantic <c>"COLOR"</c>.
    /// </summary>
    /// <param name="format">The data format of the element.</param>
    /// <param name="offsetInBytes">
    ///   The byte offset from the start of the vertex to this element.
    ///   Use <c>-1</c> (or a constant like <see cref="AppendAligned"/> to automatically align the element after the previous one.
    /// </param>
    /// <returns>A new Vertex Element that represents the <c>"COLOR"</c> semantic.</returns>
    public static VertexElement Color(PixelFormat format, int offsetInBytes = AppendAligned)
    {
        return Color(0, format, offsetInBytes);
    }

    /// <summary>
    ///   Declares a <see cref="VertexElement"/> with the semantic <c>"COLOR"</c>.
    /// </summary>
    /// <param name="semanticIndex">The semantic index for the element, used when multiple elements share the same semantic name.</param>
    /// <param name="format">The data format of the element.</param>
    /// <param name="offsetInBytes">
    ///   The byte offset from the start of the vertex to this element.
    ///   Use <c>-1</c> (or a constant like <see cref="AppendAligned"/> to automatically align the element after the previous one.
    /// </param>
    /// <returns>A new Vertex Element that represents the <c>"COLOR"</c> semantic.</returns>
    public static VertexElement Color(int semanticIndex, PixelFormat format, int offsetInBytes = AppendAligned)
    {
        return new VertexElement("COLOR", semanticIndex, format, offsetInBytes);
    }

    /// <summary>
    ///   Declares a <see cref="VertexElement"/> with the semantic <c>"NORMAL"</c>.
    /// </summary>
    /// <typeparam name="T">The type of the normal element.</typeparam>
    /// <param name="semanticIndex">The semantic index for the element, used when multiple elements share the same semantic name.</param>
    /// <param name="offsetInBytes">
    ///   The byte offset from the start of the vertex to this element.
    ///   Use <c>-1</c> (or a constant like <see cref="AppendAligned"/> to automatically align the element after the previous one.
    /// </param>
    /// <returns>A new Vertex Element that represents the <c>"NORMAL"</c> semantic.</returns>
    /// <exception cref="NotSupportedException">
    ///   The specified <typeparamref name="T"/> is not supported. It cannot be converted to a <see cref="PixelFormat"/>.
    /// </exception>
    public static VertexElement Normal<T>(int semanticIndex = 0, int offsetInBytes = AppendAligned) where T : struct
    {
        return Normal(semanticIndex, ConvertTypeToFormat<T>(), offsetInBytes);
    }

    /// <summary>
    ///   Declares a <see cref="VertexElement"/> with the semantic <c>"NORMAL"</c>.
    /// </summary>
    /// <param name="format">The data format of the element.</param>
    /// <param name="offsetInBytes">
    ///   The byte offset from the start of the vertex to this element.
    ///   Use <c>-1</c> (or a constant like <see cref="AppendAligned"/> to automatically align the element after the previous one.
    /// </param>
    /// <returns>A new Vertex Element that represents the <c>"NORMAL"</c> semantic.</returns>
    public static VertexElement Normal(PixelFormat format, int offsetInBytes = AppendAligned)
    {
        return Normal(0, format, offsetInBytes);
    }

    /// <summary>
    ///   Declares a <see cref="VertexElement"/> with the semantic <c>"NORMAL"</c>.
    /// </summary>
    /// <param name="semanticIndex">The semantic index for the element, used when multiple elements share the same semantic name.</param>
    /// <param name="format">The data format of the element.</param>
    /// <param name="offsetInBytes">
    ///   The byte offset from the start of the vertex to this element.
    ///   Use <c>-1</c> (or a constant like <see cref="AppendAligned"/> to automatically align the element after the previous one.
    /// </param>
    /// <returns>A new Vertex Element that represents the <c>"NORMAL"</c> semantic.</returns>
    public static VertexElement Normal(int semanticIndex, PixelFormat format, int offsetInBytes = AppendAligned)
    {
        return new VertexElement("NORMAL", semanticIndex, format, offsetInBytes);
    }

    /// <summary>
    ///   Declares a <see cref="VertexElement"/> with the semantic <c>"POSITION"</c>.
    /// </summary>
    /// <typeparam name="T">The type of the position element.</typeparam>
    /// <param name="semanticIndex">The semantic index for the element, used when multiple elements share the same semantic name.</param>
    /// <param name="offsetInBytes">
    ///   The byte offset from the start of the vertex to this element.
    ///   Use <c>-1</c> (or a constant like <see cref="AppendAligned"/> to automatically align the element after the previous one.
    /// </param>
    /// <returns>A new Vertex Element that represents the <c>"POSITION"</c> semantic.</returns>
    /// <exception cref="NotSupportedException">
    ///   The specified <typeparamref name="T"/> is not supported. It cannot be converted to a <see cref="PixelFormat"/>.
    /// </exception>
    public static VertexElement Position<T>(int semanticIndex = 0, int offsetInBytes = AppendAligned) where T : struct
    {
        return Position(semanticIndex, ConvertTypeToFormat<T>(), offsetInBytes);
    }

    /// <summary>
    ///   Declares a <see cref="VertexElement"/> with the semantic <c>"POSITION"</c>.
    /// </summary>
    /// <param name="format">The data format of the element.</param>
    /// <param name="offsetInBytes">
    ///   The byte offset from the start of the vertex to this element.
    ///   Use <c>-1</c> (or a constant like <see cref="AppendAligned"/> to automatically align the element after the previous one.
    /// </param>
    /// <returns>A new Vertex Element that represents the <c>"POSITION"</c> semantic.</returns>
    public static VertexElement Position(PixelFormat format, int offsetInBytes = AppendAligned)
    {
        return Position(0, format, offsetInBytes);
    }

    /// <summary>
    ///   Declares a <see cref="VertexElement"/> with the semantic <c>"POSITION"</c>.
    /// </summary>
    /// <param name="semanticIndex">The semantic index for the element, used when multiple elements share the same semantic name.</param>
    /// <param name="format">The data format of the element.</param>
    /// <param name="offsetInBytes">
    ///   The byte offset from the start of the vertex to this element.
    ///   Use <c>-1</c> (or a constant like <see cref="AppendAligned"/> to automatically align the element after the previous one.
    /// </param>
    /// <returns>A new Vertex Element that represents the <c>"POSITION"</c> semantic.</returns>
    public static VertexElement Position(int semanticIndex, PixelFormat format, int offsetInBytes = AppendAligned)
    {
        return new VertexElement("POSITION", semanticIndex, format, offsetInBytes);
    }

    /// <summary>
    ///   Declares a <see cref="VertexElement"/> with the semantic <c>"SV_POSITION"</c> (transformed position).
    /// </summary>
    /// <typeparam name="T">The type of the tranformed position element.</typeparam>
    /// <param name="semanticIndex">The semantic index for the element, used when multiple elements share the same semantic name.</param>
    /// <param name="offsetInBytes">
    ///   The byte offset from the start of the vertex to this element.
    ///   Use <c>-1</c> (or a constant like <see cref="AppendAligned"/> to automatically align the element after the previous one.
    /// </param>
    /// <returns>A new Vertex Element that represents the <c>"SV_POSITION"</c> semantic.</returns>
    /// <exception cref="NotSupportedException">
    ///   The specified <typeparamref name="T"/> is not supported. It cannot be converted to a <see cref="PixelFormat"/>.
    /// </exception>
    public static VertexElement PositionTransformed<T>(int semanticIndex = 0, int offsetInBytes = AppendAligned) where T : struct
    {
        return PositionTransformed(semanticIndex, ConvertTypeToFormat<T>(), offsetInBytes);
    }

    /// <summary>
    ///   Declares a <see cref="VertexElement"/> with the semantic <c>"SV_POSITION"</c> (transformed position).
    /// </summary>
    /// <param name="format">The data format of the element.</param>
    /// <param name="offsetInBytes">
    ///   The byte offset from the start of the vertex to this element.
    ///   Use <c>-1</c> (or a constant like <see cref="AppendAligned"/> to automatically align the element after the previous one.
    /// </param>
    /// <returns>A new Vertex Element that represents the <c>"SV_POSITION"</c> semantic.</returns>
    public static VertexElement PositionTransformed(PixelFormat format, int offsetInBytes = AppendAligned)
    {
        return PositionTransformed(0, format, offsetInBytes);
    }

    /// <summary>
    ///   Declares a <see cref="VertexElement"/> with the semantic <c>"SV_POSITION"</c> (transformed position).
    /// </summary>
    /// <param name="semanticIndex">The semantic index for the element, used when multiple elements share the same semantic name.</param>
    /// <param name="format">The data format of the element.</param>
    /// <param name="offsetInBytes">
    ///   The byte offset from the start of the vertex to this element.
    ///   Use <c>-1</c> (or a constant like <see cref="AppendAligned"/> to automatically align the element after the previous one.
    /// </param>
    /// <returns>A new Vertex Element that represents the <c>"SV_POSITION"</c> semantic.</returns>
    public static VertexElement PositionTransformed(int semanticIndex, PixelFormat format, int offsetInBytes = AppendAligned)
    {
        return new VertexElement("SV_POSITION", semanticIndex, format, offsetInBytes);
    }

    /// <summary>
    ///   Declares a <see cref="VertexElement"/> with the semantic <c>"TEXCOORD"</c> (texture coordinates).
    /// </summary>
    /// <typeparam name="T">The type of the texture coordinates element.</typeparam>
    /// <param name="semanticIndex">The semantic index for the element, used when multiple elements share the same semantic name.</param>
    /// <param name="offsetInBytes">
    ///   The byte offset from the start of the vertex to this element.
    ///   Use <c>-1</c> (or a constant like <see cref="AppendAligned"/> to automatically align the element after the previous one.
    /// </param>
    /// <returns>A new Vertex Element that represents the <c>"TEXCOORD"</c> semantic.</returns>
    /// <exception cref="NotSupportedException">
    ///   The specified <typeparamref name="T"/> is not supported. It cannot be converted to a <see cref="PixelFormat"/>.
    /// </exception>
    public static VertexElement TextureCoordinate<T>(int semanticIndex = 0, int offsetInBytes = AppendAligned) where T : struct
    {
        return TextureCoordinate(semanticIndex, ConvertTypeToFormat<T>(), offsetInBytes);
    }

    /// <summary>
    ///   Declares a <see cref="VertexElement"/> with the semantic <c>"TEXCOORD"</c> (texture coordinates).
    /// </summary>
    /// <param name="format">The data format of the element.</param>
    /// <param name="offsetInBytes">
    ///   The byte offset from the start of the vertex to this element.
    ///   Use <c>-1</c> (or a constant like <see cref="AppendAligned"/> to automatically align the element after the previous one.
    /// </param>
    /// <returns>A new Vertex Element that represents the <c>"TEXCOORD"</c> semantic.</returns>
    public static VertexElement TextureCoordinate(PixelFormat format, int offsetInBytes = AppendAligned)
    {
        return TextureCoordinate(0, format, offsetInBytes);
    }

    /// <summary>
    ///   Declares a <see cref="VertexElement"/> with the semantic <c>"TEXCOORD"</c> (texture coordinates).
    /// </summary>
    /// <param name="semanticIndex">The semantic index for the element, used when multiple elements share the same semantic name.</param>
    /// <param name="format">The data format of the element.</param>
    /// <param name="offsetInBytes">
    ///   The byte offset from the start of the vertex to this element.
    ///   Use <c>-1</c> (or a constant like <see cref="AppendAligned"/> to automatically align the element after the previous one.
    /// </param>
    /// <returns>A new Vertex Element that represents the <c>"TEXCOORD"</c> semantic.</returns>
    public static VertexElement TextureCoordinate(int semanticIndex, PixelFormat format, int offsetInBytes = AppendAligned)
    {
        return new VertexElement("TEXCOORD", semanticIndex, format, offsetInBytes);
    }

    /// <summary>
    ///   Declares a <see cref="VertexElement"/> with the semantic <c>"TANGENT"</c>.
    /// </summary>
    /// <typeparam name="T">The type of the tangent element.</typeparam>
    /// <param name="semanticIndex">The semantic index for the element, used when multiple elements share the same semantic name.</param>
    /// <param name="offsetInBytes">
    ///   The byte offset from the start of the vertex to this element.
    ///   Use <c>-1</c> (or a constant like <see cref="AppendAligned"/> to automatically align the element after the previous one.
    /// </param>
    /// <returns>A new Vertex Element that represents the <c>"TANGENT"</c> semantic.</returns>
    /// <exception cref="NotSupportedException">
    ///   The specified <typeparamref name="T"/> is not supported. It cannot be converted to a <see cref="PixelFormat"/>.
    /// </exception>
    public static VertexElement Tangent<T>(int semanticIndex = 0, int offsetInBytes = AppendAligned) where T : struct
    {
        return Tangent(semanticIndex, ConvertTypeToFormat<T>(), offsetInBytes);
    }

    /// <summary>
    ///   Declares a <see cref="VertexElement"/> with the semantic <c>"TANGENT"</c>.
    /// </summary>
    /// <param name="format">The data format of the element.</param>
    /// <param name="offsetInBytes">
    ///   The byte offset from the start of the vertex to this element.
    ///   Use <c>-1</c> (or a constant like <see cref="AppendAligned"/> to automatically align the element after the previous one.
    /// </param>
    /// <returns>A new Vertex Element that represents the <c>"TANGENT"</c> semantic.</returns>
    public static VertexElement Tangent(PixelFormat format, int offsetInBytes = AppendAligned)
    {
        return Tangent(0, format, offsetInBytes);
    }

    /// <summary>
    ///   Declares a <see cref="VertexElement"/> with the semantic <c>"TANGENT"</c>.
    /// </summary>
    /// <param name="semanticIndex">The semantic index for the element, used when multiple elements share the same semantic name.</param>
    /// <param name="format">The data format of the element.</param>
    /// <param name="offsetInBytes">
    ///   The byte offset from the start of the vertex to this element.
    ///   Use <c>-1</c> (or a constant like <see cref="AppendAligned"/> to automatically align the element after the previous one.
    /// </param>
    /// <returns>A new Vertex Element that represents the <c>"TANGENT"</c> semantic.</returns>
    public static VertexElement Tangent(int semanticIndex, PixelFormat format, int offsetInBytes = AppendAligned)
    {
        return new VertexElement("TANGENT", semanticIndex, format, offsetInBytes);
    }

    /// <summary>
    ///   Declares a <see cref="VertexElement"/> with the semantic <c>"BITANGENT"</c>.
    /// </summary>
    /// <typeparam name="T">The type of the bitangent element.</typeparam>
    /// <param name="semanticIndex">The semantic index for the element, used when multiple elements share the same semantic name.</param>
    /// <param name="offsetInBytes">
    ///   The byte offset from the start of the vertex to this element.
    ///   Use <c>-1</c> (or a constant like <see cref="AppendAligned"/> to automatically align the element after the previous one.
    /// </param>
    /// <returns>A new Vertex Element that represents the <c>"BITANGENT"</c> semantic.</returns>
    /// <exception cref="NotSupportedException">
    ///   The specified <typeparamref name="T"/> is not supported. It cannot be converted to a <see cref="PixelFormat"/>.
    /// </exception>
    public static VertexElement BiTangent<T>(int semanticIndex = 0, int offsetInBytes = AppendAligned) where T : struct
    {
        return BiTangent(semanticIndex, ConvertTypeToFormat<T>(), offsetInBytes);
    }

    /// <summary>
    ///   Declares a <see cref="VertexElement"/> with the semantic <c>"BITANGENT"</c>.
    /// </summary>
    /// <param name="format">The data format of the element.</param>
    /// <param name="offsetInBytes">
    ///   The byte offset from the start of the vertex to this element.
    ///   Use <c>-1</c> (or a constant like <see cref="AppendAligned"/> to automatically align the element after the previous one.
    /// </param>
    /// <returns>A new Vertex Element that represents the <c>"BITANGENT"</c> semantic.</returns>
    public static VertexElement BiTangent(PixelFormat format, int offsetInBytes = AppendAligned)
    {
        return BiTangent(0, format, offsetInBytes);
    }

    /// <summary>
    ///   Declares a <see cref="VertexElement"/> with the semantic <c>"BITANGENT"</c>.
    /// </summary>
    /// <param name="semanticIndex">The semantic index for the element, used when multiple elements share the same semantic name.</param>
    /// <param name="format">The data format of the element.</param>
    /// <param name="offsetInBytes">
    ///   The byte offset from the start of the vertex to this element.
    ///   Use <c>-1</c> (or a constant like <see cref="AppendAligned"/> to automatically align the element after the previous one.
    /// </param>
    /// <returns>A new Vertex Element that represents the <c>"BITANGENT"</c> semantic.</returns>
    public static VertexElement BiTangent(int semanticIndex, PixelFormat format, int offsetInBytes = AppendAligned)
    {
        return new VertexElement("BITANGENT", semanticIndex, format, offsetInBytes);
    }

    #endregion

    /// <summary>
    ///   Converts a type <typeparamref name="T"/> to its equivalent <see cref="PixelFormat"/>.
    /// </summary>
    /// <typeparam name="T">The type of the Vertex Element to convert.</typeparam>
    /// <returns>The equivalent <see cref="PixelFormat"/> to <typeparamref name="T"/>.</returns>
    /// <exception cref="NotSupportedException">
    ///   The specified <typeparamref name="T"/> is not supported. It cannot be converted to a <see cref="PixelFormat"/>.
    /// </exception>
    public static PixelFormat ConvertTypeToFormat<T>() where T : struct
    {
        return ConvertTypeToFormat(typeof(T));

        static PixelFormat ConvertTypeToFormat(Type type)
        {
            if (typeof(Vector4) == type || typeof(Color4) == type)
                return PixelFormat.R32G32B32A32_Float;
            if (typeof(Vector3) == type || typeof(Color3) == type)
                return PixelFormat.R32G32B32_Float;
            if (typeof(Vector2) == type)
                return PixelFormat.R32G32_Float;
            if (typeof(float) == type)
                return PixelFormat.R32_Float;

            if (typeof(Color) == type)
                return PixelFormat.R8G8B8A8_UNorm;
            if (typeof(ColorBGRA) == type)
                return PixelFormat.B8G8R8A8_UNorm;

            if (typeof(Half4) == type)
                return PixelFormat.R16G16B16A16_Float;
            if (typeof(Half2) == type)
                return PixelFormat.R16G16_Float;
            if (typeof(Half) == type)
                return PixelFormat.R16_Float;

            if (typeof(Int4) == type)
                return PixelFormat.R32G32B32A32_UInt;
            if (typeof(Int3) == type)
                return PixelFormat.R32G32B32_UInt;
            if (typeof(int) == type)
                return PixelFormat.R32_UInt;
            if (typeof(uint) == type)
                return PixelFormat.R32_UInt;

            //if (typeof(Bool4) == typeT)
            //    return PixelFormat.R32G32B32A32_UInt;

            //if (typeof(Bool) == typeT)
            //    return PixelFormat.R32_UInt;

            throw new NotSupportedException($"Type [{type.Name}] is not supported. You must specify an explicit PixelFormat");
        }
    }

    #region Serializer

    /// <summary>
    ///   Provides functionality to serialize and deserialize <see cref="VertexElement"/> objects.
    /// </summary>
    internal class Serializer : DataSerializer<VertexElement>
    {
        /// <summary>
        ///   Serializes or deserializes a <see cref="VertexElement"/> object.
        /// </summary>
        /// <param name="vertexElement">The object to serialize or deserialize.</param>
        /// <inheritdoc/>
        public override void Serialize(ref VertexElement vertexElement, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                vertexElement.semanticName = stream.ReadString();
                vertexElement.semanticIndex = stream.ReadInt32();
                vertexElement.format = stream.Read<PixelFormat>();
                vertexElement.alignedByteOffset = stream.ReadInt32();
                vertexElement.ComputeHashCode();
            }
            else
            {
                stream.Write(vertexElement.semanticName);
                stream.Write(vertexElement.semanticIndex);
                stream.Write(vertexElement.format);
                stream.Write(vertexElement.alignedByteOffset);
            }
        }
    }

    #endregion
}
