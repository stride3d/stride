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
    /// A description of a single element for the input-assembler stage. This structure is related to <see cref="SharpDX.Direct3D11.InputElement"/>.
    /// </summary>
    /// <remarks>
    /// Because <see cref="SharpDX.Direct3D11.InputElement"/> requires to have the same <see cref="VertexBufferLayout.SlotIndex"/>, <see cref="VertexBufferLayout.VertexClassification"/> and <see cref="VertexBufferLayout.instanceDataStepRate"/>,
    /// the <see cref="VertexBufferLayout"/> structure encapsulates a set of <see cref="VertexElement"/> for a particular slot, classification and instance data step rate.
    /// Unlike the default <see cref="SharpDX.Direct3D11.InputElement"/>, this structure accepts a semantic name with a postfix number that will be automatically extracted to the semantic index.
    /// </remarks>
    /// <seealso cref="VertexBufferLayout"/>
    public const int AppendAligned = -1;


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
        ///   Returns a value that can be used for the offset parameter of an InputElement to indicate that the element
        ///   should be aligned directly after the previous element, including any packing if neccessary.
        /// </summary>
        /// <returns>A value used to align input elements.</returns>
        /// <summary>
        /// Initializes a new instance of the <see cref="VertexElement" /> struct.
        /// </summary>
        /// <param name="semanticName">Name of the semantic.</param>
        /// <param name="format">The format.</param>
        /// <remarks>
        /// If the semantic name contains a postfix number, this number will be used as a semantic index.
        /// </remarks>
    public VertexElement(string semanticName, int semanticIndex, PixelFormat format, int alignedByteOffset = AppendAligned)
        : this()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(semanticName);

        // All semantics will be upper case
        semanticName = semanticName.ToUpperInvariant();

        var match = MatchSemanticIndex.Match(semanticName);
        if (match.Success)
            throw new ArgumentException("Cannot specify a semantic index when using the constructor with explicit semantic index. Use the implicit semantic index constructor.");

        /// <summary>
        /// Initializes a new instance of the <see cref="VertexElement" /> struct.
        /// </summary>
        /// <param name="semanticName">Name of the semantic.</param>
        /// <param name="semanticIndex">Index of the semantic.</param>
        /// <param name="format">The format.</param>
        /// <param name="alignedByteOffset">The aligned byte offset.</param>
        // Convert to singleton string in order to speed up things
        // TODO: Stale comment? Use string.Intern?
        this.semanticName = semanticName;
        this.semanticIndex = semanticIndex;
        this.format = format;
        this.alignedByteOffset = alignedByteOffset;

        // Precalculate hashcode
        hashCode = ComputeHashCode();
    }


    public readonly string SemanticName => semanticName;

    public readonly string SemanticAsText
        => semanticIndex == 0
            ? semanticName
            : FormattableString.Invariant($"{semanticName}{semanticIndex}");

        /// <summary>
        /// <dd> <p>The HLSL semantic associated with this element in a shader input-signature.</p> </dd>
        /// </summary>
    public readonly int SemanticIndex => semanticIndex;

        /// <summary>
        /// <dd> <p>The HLSL semantic associated with this element in a shader input-signature.</p> </dd>
        /// </summary>
    public readonly PixelFormat Format => format;

        /// <summary>
        /// <dd> <p>The semantic index for the element. A semantic index modifies a semantic, with an integer index number. A semantic index is only needed in a  case where there is more than one element with the same semantic. For example, a 4x4 matrix would have four components each with the semantic  name </p>  <pre><code>matrix</code></pre>  <p>, however each of the four component would have different semantic indices (0, 1, 2, and 3).</p> </dd>
        /// </summary>
    public readonly int AlignedByteOffset => alignedByteOffset;

        /// <summary>
        /// <dd> <p>The data type of the element data. See <strong><see cref="SharpDX.DXGI.Format"/></strong>.</p> </dd>
        /// </summary>

        /// <summary>
        /// <dd> <p>Optional. Offset (in bytes) between each element. Use D3D11_APPEND_ALIGNED_ELEMENT for convenience to define the current element directly  after the previous one, including any packing if necessary.</p> </dd>
        /// </summary>
    public readonly bool Equals(VertexElement other)
    {
        // First use hashCode to compute
        return hashCode == other.hashCode
            && semanticName.Equals(other.semanticName)
            && semanticIndex == other.semanticIndex
            && format == other.format
            && alignedByteOffset == other.alignedByteOffset;
    }

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

    public override readonly int GetHashCode() => hashCode;
    // Computes the hash code for this VertexElement so it can be cached.
    private readonly int ComputeHashCode()
    {
        return HashCode.Combine(semanticName, semanticIndex, format, alignedByteOffset);
    }

    public override readonly string ToString()
    {
        return FormattableString.Invariant($"{SemanticAsText},{Format},{AlignedByteOffset}");
    }

    #region Common VertexElements

    public static VertexElement Color<T>(int semanticIndex = 0, int offsetInBytes = AppendAligned) where T : struct
    {
        return Color(semanticIndex, ConvertTypeToFormat<T>(), offsetInBytes);
    }

        /// <summary>
        /// Declares a VertexElement with the semantic "COLOR".
        /// </summary>
        /// <typeparam name="T">Type of the Color semantic.</typeparam>
        /// <param name="semanticIndex">The semantic index.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
    public static VertexElement Color(PixelFormat format, int offsetInBytes = AppendAligned)
    {
        return Color(0, format, offsetInBytes);
    }

        /// <summary>
        /// Declares a VertexElement with the semantic "COLOR".
        /// </summary>
        /// <param name="format">Format of this element.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
    public static VertexElement Color(int semanticIndex, PixelFormat format, int offsetInBytes = AppendAligned)
    {
        return new VertexElement("COLOR", semanticIndex, format, offsetInBytes);
    }

        /// <summary>
        /// Declares a VertexElement with the semantic "COLOR".
        /// </summary>
        /// <param name="semanticIndex">The semantic index.</param>
        /// <param name="format">Format of this element.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
    public static VertexElement Normal<T>(int semanticIndex = 0, int offsetInBytes = AppendAligned) where T : struct
    {
        return Normal(semanticIndex, ConvertTypeToFormat<T>(), offsetInBytes);
    }

        /// <summary>
        /// Declares a VertexElement with the semantic "NORMAL".
        /// </summary>
        /// <typeparam name="T">Type of the Normal semantic.</typeparam>
        /// <param name="semanticIndex">The semantic index.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
    public static VertexElement Normal(PixelFormat format, int offsetInBytes = AppendAligned)
    {
        return Normal(0, format, offsetInBytes);
    }

        /// <summary>
        /// Declares a VertexElement with the semantic "NORMAL".
        /// </summary>
        /// <param name="format">Format of this element.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
    public static VertexElement Normal(int semanticIndex, PixelFormat format, int offsetInBytes = AppendAligned)
    {
        return new VertexElement("NORMAL", semanticIndex, format, offsetInBytes);
    }

        /// <summary>
        /// Declares a VertexElement with the semantic "NORMAL".
        /// </summary>
        /// <param name="semanticIndex">The semantic index.</param>
        /// <param name="format">Format of this element.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
    public static VertexElement Position<T>(int semanticIndex = 0, int offsetInBytes = AppendAligned) where T : struct
    {
        return Position(semanticIndex, ConvertTypeToFormat<T>(), offsetInBytes);
    }

        /// <summary>
        /// Declares a VertexElement with the semantic "POSITION".
        /// </summary>
        /// <typeparam name="T">Type of the Position semantic.</typeparam>
        /// <param name="semanticIndex">The semantic index.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
    public static VertexElement Position(PixelFormat format, int offsetInBytes = AppendAligned)
    {
        return Position(0, format, offsetInBytes);
    }

        /// <summary>
        /// Declares a VertexElement with the semantic "POSITION".
        /// </summary>
        /// <param name="format">Format of this element.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
    public static VertexElement Position(int semanticIndex, PixelFormat format, int offsetInBytes = AppendAligned)
    {
        return new VertexElement("POSITION", semanticIndex, format, offsetInBytes);
    }

        /// <summary>
        /// Declares a VertexElement with the semantic "POSITION".
        /// </summary>
        /// <param name="semanticIndex">The semantic index.</param>
        /// <param name="format">Format of this element.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
    public static VertexElement PositionTransformed<T>(int semanticIndex = 0, int offsetInBytes = AppendAligned) where T : struct
    {
        return PositionTransformed(semanticIndex, ConvertTypeToFormat<T>(), offsetInBytes);
    }

        /// <summary>
        /// Declares a VertexElement with the semantic "SV_POSITION".
        /// </summary>
        /// <typeparam name="T">Type of the PositionTransformed semantic.</typeparam>
        /// <param name="semanticIndex">The semantic index.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
    public static VertexElement PositionTransformed(PixelFormat format, int offsetInBytes = AppendAligned)
    {
        return PositionTransformed(0, format, offsetInBytes);
    }

        /// <summary>
        /// Declares a VertexElement with the semantic "SV_POSITION".
        /// </summary>
        /// <param name="format">Format of this element.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
    public static VertexElement PositionTransformed(int semanticIndex, PixelFormat format, int offsetInBytes = AppendAligned)
    {
        return new VertexElement("SV_POSITION", semanticIndex, format, offsetInBytes);
    }

        /// <summary>
        /// Declares a VertexElement with the semantic "SV_POSITION".
        /// </summary>
        /// <param name="semanticIndex">The semantic index.</param>
        /// <param name="format">Format of this element.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
    public static VertexElement TextureCoordinate<T>(int semanticIndex = 0, int offsetInBytes = AppendAligned) where T : struct
    {
        return TextureCoordinate(semanticIndex, ConvertTypeToFormat<T>(), offsetInBytes);
    }

        /// <summary>
        /// Declares a VertexElement with the semantic "TEXCOORD".
        /// </summary>
        /// <typeparam name="T">Type of the TextureCoordinate semantic.</typeparam>
        /// <param name="semanticIndex">The semantic index.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
    public static VertexElement TextureCoordinate(PixelFormat format, int offsetInBytes = AppendAligned)
    {
        return TextureCoordinate(0, format, offsetInBytes);
    }

        /// <summary>
        /// Declares a VertexElement with the semantic "TEXCOORD".
        /// </summary>
        /// <param name="format">Format of this element.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
    public static VertexElement TextureCoordinate(int semanticIndex, PixelFormat format, int offsetInBytes = AppendAligned)
    {
        return new VertexElement("TEXCOORD", semanticIndex, format, offsetInBytes);
    }

        /// <summary>
        /// Declares a VertexElement with the semantic "TEXCOORD".
        /// </summary>
        /// <param name="semanticIndex">The semantic index.</param>
        /// <param name="format">Format of this element.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
    public static VertexElement Tangent<T>(int semanticIndex = 0, int offsetInBytes = AppendAligned) where T : struct
    {
        return Tangent(semanticIndex, ConvertTypeToFormat<T>(), offsetInBytes);
    }

        /// <summary>
        /// Declares a VertexElement with the semantic "TANGENT".
        /// </summary>
        /// <typeparam name="T">Type of the Tangent semantic.</typeparam>
        /// <param name="semanticIndex">The semantic index.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
    public static VertexElement Tangent(PixelFormat format, int offsetInBytes = AppendAligned)
    {
        return Tangent(0, format, offsetInBytes);
    }

        /// <summary>
        /// Declares a VertexElement with the semantic "TANGENT".
        /// </summary>
        /// <param name="format">Format of this element.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
    public static VertexElement Tangent(int semanticIndex, PixelFormat format, int offsetInBytes = AppendAligned)
    {
        return new VertexElement("TANGENT", semanticIndex, format, offsetInBytes);
    }

        /// <summary>
        /// Declares a VertexElement with the semantic "TANGENT".
        /// </summary>
        /// <param name="semanticIndex">The semantic index.</param>
        /// <param name="format">Format of this element.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
    public static VertexElement BiTangent<T>(int semanticIndex = 0, int offsetInBytes = AppendAligned) where T : struct
    {
        return BiTangent(semanticIndex, ConvertTypeToFormat<T>(), offsetInBytes);
    }

        /// <summary>
        /// Declares a VertexElement with the semantic "BITANGENT".
        /// </summary>
        /// <typeparam name="T">Type of the BiTangent semantic.</typeparam>
        /// <param name="semanticIndex">The semantic index.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
    public static VertexElement BiTangent(PixelFormat format, int offsetInBytes = AppendAligned)
    {
        return BiTangent(0, format, offsetInBytes);
    }

        /// <summary>
        /// Declares a VertexElement with the semantic "BITANGENT".
        /// </summary>
        /// <param name="format">Format of this element.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
    public static VertexElement BiTangent(int semanticIndex, PixelFormat format, int offsetInBytes = AppendAligned)
    {
        return new VertexElement("BITANGENT", semanticIndex, format, offsetInBytes);
    }

        /// <summary>
        /// Declares a VertexElement with the semantic "BITANGENT".
        /// </summary>
        /// <param name="semanticIndex">The semantic index.</param>
        /// <param name="format">Format of this element.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
    #endregion

    public static PixelFormat ConvertTypeToFormat<T>() where T : struct
    {
        return ConvertTypeToFormat(typeof(T));

        /// <summary>
        /// Converts a type to a <see cref="SharpDX.DXGI.Format"/>.
        /// </summary>
        /// <param name="typeT">The type T.</param>
        /// <returns>The equivalent Format.</returns>
        /// <exception cref="System.NotSupportedException">If the convertion for this type is not supported.</exception>
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

    internal class Serializer : DataSerializer<VertexElement>
    {
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
