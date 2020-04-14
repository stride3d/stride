// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
using System.Globalization;
using System.Text.RegularExpressions;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Serializers;

namespace Xenko.Graphics
{
    /// <summary>
    /// A description of a single element for the input-assembler stage. This structure is related to <see cref="Direct3D11.InputElement"/>.
    /// </summary>
    /// <remarks>
    /// Because <see cref="Direct3D11.InputElement"/> requires to have the same <see cref="VertexBufferLayout.SlotIndex"/>, <see cref="VertexBufferLayout.VertexClassification"/> and <see cref="VertexBufferLayout.instanceDataStepRate"/>,
    /// the <see cref="VertexBufferLayout"/> structure encapsulates a set of <see cref="VertexElement"/> for a particular slot, classification and instance data step rate.
    /// Unlike the default <see cref="Direct3D11.InputElement"/>, this structure accepts a semantic name with a postfix number that will be automatically extracted to the semantic index.
    /// </remarks>
    /// <seealso cref="VertexBufferLayout"/>
    [DataContract]
    [DataSerializer(typeof(Serializer))]
    public struct VertexElement : IEquatable<VertexElement>
    {
        private string semanticName;

        private int semanticIndex;

        private PixelFormat format;

        private int alignedByteOffset;

        private int hashCode;

        // Match the last digit of a semantic name.
        internal static readonly Regex MatchSemanticIndex = new Regex(@"(.*)(\d+)$");

        /// <summary>
        ///   Returns a value that can be used for the offset parameter of an InputElement to indicate that the element
        ///   should be aligned directly after the previous element, including any packing if neccessary.
        /// </summary>
        /// <returns>A value used to align input elements.</returns>
        public const int AppendAligned = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="VertexElement" /> struct.
        /// </summary>
        /// <param name="semanticName">Name of the semantic.</param>
        /// <param name="format">The format.</param>
        /// <remarks>
        /// If the semantic name contains a postfix number, this number will be used as a semantic index.
        /// </remarks>
        public VertexElement(string semanticName, PixelFormat format)
            : this()
        {
            if (semanticName == null)
                throw new ArgumentNullException("semanticName");

            // All semantics will be upper case.
            semanticName = semanticName.ToUpperInvariant();

            var match = MatchSemanticIndex.Match(semanticName);
            if (match.Success)
            {
                // Convert to singleton string in order to speed up things.
                this.semanticName = match.Groups[1].Value;
                semanticIndex = int.Parse(match.Groups[2].Value);
            }
            else
            {
                this.semanticName = semanticName;
            }

            this.format = format;
            alignedByteOffset = AppendAligned;

            // Precalculate hashcode
            hashCode = ComputeHashCode();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VertexElement" /> struct.
        /// </summary>
        /// <param name="semanticName">Name of the semantic.</param>
        /// <param name="semanticIndex">Index of the semantic.</param>
        /// <param name="format">The format.</param>
        /// <param name="alignedByteOffset">The aligned byte offset.</param>
        public VertexElement(string semanticName, int semanticIndex, PixelFormat format, int alignedByteOffset = AppendAligned)
            : this()
        {
            if (semanticName == null)
                throw new ArgumentNullException("semanticName");

            // All semantics will be upper case.
            semanticName = semanticName.ToUpperInvariant();

            var match = MatchSemanticIndex.Match(semanticName);
            if (match.Success)
                throw new ArgumentException("Semantic name cannot a semantic index when using constructor with explicit semantic index. Use implicit semantic index constructor.");

            // Convert to singleton string in order to speed up things.
            this.semanticName = semanticName;
            this.semanticIndex = semanticIndex;
            this.format = format;
            this.alignedByteOffset = alignedByteOffset;

            // Precalculate hashcode
            hashCode = ComputeHashCode();
        }

        /// <summary>
        /// <dd> <p>The HLSL semantic associated with this element in a shader input-signature.</p> </dd>
        /// </summary>
        public string SemanticName
        {
            get
            {
                return semanticName;
            }
        }

        /// <summary>
        /// <dd> <p>The HLSL semantic associated with this element in a shader input-signature.</p> </dd>
        /// </summary>
        public string SemanticAsText
        {
            get
            {
                if (semanticIndex == 0)
                    return semanticName;
                return string.Format("{0}{1}", semanticName, semanticIndex.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// <dd> <p>The semantic index for the element. A semantic index modifies a semantic, with an integer index number. A semantic index is only needed in a  case where there is more than one element with the same semantic. For example, a 4x4 matrix would have four components each with the semantic  name </p>  <pre><code>matrix</code></pre>  <p>, however each of the four component would have different semantic indices (0, 1, 2, and 3).</p> </dd>
        /// </summary>
        public int SemanticIndex
        {
            get
            {
                return semanticIndex;
            }
        }

        /// <summary>
        /// <dd> <p>The data type of the element data. See <strong><see cref="SharpDX.DXGI.Format"/></strong>.</p> </dd>
        /// </summary>
        public PixelFormat Format
        {
            get
            {
                return format;
            }
        }

        /// <summary>
        /// <dd> <p>Optional. Offset (in bytes) between each element. Use D3D11_APPEND_ALIGNED_ELEMENT for convenience to define the current element directly  after the previous one, including any packing if necessary.</p> </dd>
        /// </summary>
        public int AlignedByteOffset
        {
            get
            {
                return alignedByteOffset;
            }
        }

        public bool Equals(VertexElement other)
        {
            // First use hashCode to compute
            return hashCode == other.hashCode && semanticName.Equals(other.semanticName) && semanticIndex == other.semanticIndex && format == other.format && alignedByteOffset == other.alignedByteOffset;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is VertexElement && Equals((VertexElement)obj);
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        internal int ComputeHashCode()
        {
            unchecked
            {
                int localHashCode = semanticName.GetHashCode();
                localHashCode = (localHashCode * 397) ^ semanticIndex;
                localHashCode = (localHashCode * 397) ^ format.GetHashCode();
                localHashCode = (localHashCode * 397) ^ alignedByteOffset;
                return localHashCode;
            }
        }

        public static bool operator ==(VertexElement left, VertexElement right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VertexElement left, VertexElement right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return string.Format("{0}{1},{2},{3}", semanticName, semanticIndex == 0 ? string.Empty : string.Empty + semanticIndex, format, alignedByteOffset);
        }

        /// <summary>
        /// Declares a VertexElement with the semantic "COLOR".
        /// </summary>
        /// <typeparam name="T">Type of the Color semantic.</typeparam>
        /// <param name="semanticIndex">The semantic index.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
        public static VertexElement Color<T>(int semanticIndex = 0, int offsetInBytes = AppendAligned) where T : struct
        {
            return Color(semanticIndex, ConvertTypeToFormat<T>(), offsetInBytes);
        }

        /// <summary>
        /// Declares a VertexElement with the semantic "COLOR".
        /// </summary>
        /// <param name="format">Format of this element.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
        public static VertexElement Color(PixelFormat format, int offsetInBytes = AppendAligned)
        {
            return Color(0, format, offsetInBytes);
        }

        /// <summary>
        /// Declares a VertexElement with the semantic "COLOR".
        /// </summary>
        /// <param name="semanticIndex">The semantic index.</param>
        /// <param name="format">Format of this element.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
        public static VertexElement Color(int semanticIndex, PixelFormat format, int offsetInBytes = AppendAligned)
        {
            return new VertexElement("COLOR", semanticIndex, format, offsetInBytes);
        }

        /// <summary>
        /// Declares a VertexElement with the semantic "NORMAL".
        /// </summary>
        /// <typeparam name="T">Type of the Normal semantic.</typeparam>
        /// <param name="semanticIndex">The semantic index.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
        public static VertexElement Normal<T>(int semanticIndex = 0, int offsetInBytes = AppendAligned) where T : struct
        {
            return Normal(semanticIndex, ConvertTypeToFormat<T>(), offsetInBytes);
        }

        /// <summary>
        /// Declares a VertexElement with the semantic "NORMAL".
        /// </summary>
        /// <param name="format">Format of this element.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
        public static VertexElement Normal(PixelFormat format, int offsetInBytes = AppendAligned)
        {
            return Normal(0, format, offsetInBytes);
        }

        /// <summary>
        /// Declares a VertexElement with the semantic "NORMAL".
        /// </summary>
        /// <param name="semanticIndex">The semantic index.</param>
        /// <param name="format">Format of this element.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
        public static VertexElement Normal(int semanticIndex, PixelFormat format, int offsetInBytes = AppendAligned)
        {
            return new VertexElement("NORMAL", semanticIndex, format, offsetInBytes);
        }

        /// <summary>
        /// Declares a VertexElement with the semantic "POSITION".
        /// </summary>
        /// <typeparam name="T">Type of the Position semantic.</typeparam>
        /// <param name="semanticIndex">The semantic index.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
        public static VertexElement Position<T>(int semanticIndex = 0, int offsetInBytes = AppendAligned) where T : struct
        {
            return Position(semanticIndex, ConvertTypeToFormat<T>(), offsetInBytes);
        }

        /// <summary>
        /// Declares a VertexElement with the semantic "POSITION".
        /// </summary>
        /// <param name="format">Format of this element.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
        public static VertexElement Position(PixelFormat format, int offsetInBytes = AppendAligned)
        {
            return Position(0, format, offsetInBytes);
        }

        /// <summary>
        /// Declares a VertexElement with the semantic "POSITION".
        /// </summary>
        /// <param name="semanticIndex">The semantic index.</param>
        /// <param name="format">Format of this element.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
        public static VertexElement Position(int semanticIndex, PixelFormat format, int offsetInBytes = AppendAligned)
        {
            return new VertexElement("POSITION", semanticIndex, format, offsetInBytes);
        }

        /// <summary>
        /// Declares a VertexElement with the semantic "SV_POSITION".
        /// </summary>
        /// <typeparam name="T">Type of the PositionTransformed semantic.</typeparam>
        /// <param name="semanticIndex">The semantic index.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
        public static VertexElement PositionTransformed<T>(int semanticIndex = 0, int offsetInBytes = AppendAligned) where T : struct
        {
            return PositionTransformed(semanticIndex, ConvertTypeToFormat<T>(), offsetInBytes);
        }

        /// <summary>
        /// Declares a VertexElement with the semantic "SV_POSITION".
        /// </summary>
        /// <param name="format">Format of this element.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
        public static VertexElement PositionTransformed(PixelFormat format, int offsetInBytes = AppendAligned)
        {
            return PositionTransformed(0, format, offsetInBytes);
        }

        /// <summary>
        /// Declares a VertexElement with the semantic "SV_POSITION".
        /// </summary>
        /// <param name="semanticIndex">The semantic index.</param>
        /// <param name="format">Format of this element.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
        public static VertexElement PositionTransformed(int semanticIndex, PixelFormat format, int offsetInBytes = AppendAligned)
        {
            return new VertexElement("SV_POSITION", semanticIndex, format, offsetInBytes);
        }

        /// <summary>
        /// Declares a VertexElement with the semantic "TEXCOORD".
        /// </summary>
        /// <typeparam name="T">Type of the TextureCoordinate semantic.</typeparam>
        /// <param name="semanticIndex">The semantic index.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
        public static VertexElement TextureCoordinate<T>(int semanticIndex = 0, int offsetInBytes = AppendAligned) where T : struct
        {
            return TextureCoordinate(semanticIndex, ConvertTypeToFormat<T>(), offsetInBytes);
        }

        /// <summary>
        /// Declares a VertexElement with the semantic "TEXCOORD".
        /// </summary>
        /// <param name="format">Format of this element.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
        public static VertexElement TextureCoordinate(PixelFormat format, int offsetInBytes = AppendAligned)
        {
            return TextureCoordinate(0, format, offsetInBytes);
        }

        /// <summary>
        /// Declares a VertexElement with the semantic "TEXCOORD".
        /// </summary>
        /// <param name="semanticIndex">The semantic index.</param>
        /// <param name="format">Format of this element.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
        public static VertexElement TextureCoordinate(int semanticIndex, PixelFormat format, int offsetInBytes = AppendAligned)
        {
            return new VertexElement("TEXCOORD", semanticIndex, format, offsetInBytes);
        }

        /// <summary>
        /// Declares a VertexElement with the semantic "TANGENT".
        /// </summary>
        /// <typeparam name="T">Type of the Tangent semantic.</typeparam>
        /// <param name="semanticIndex">The semantic index.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
        public static VertexElement Tangent<T>(int semanticIndex = 0, int offsetInBytes = AppendAligned) where T : struct
        {
            return Tangent(semanticIndex, ConvertTypeToFormat<T>(), offsetInBytes);
        }

        /// <summary>
        /// Declares a VertexElement with the semantic "TANGENT".
        /// </summary>
        /// <param name="format">Format of this element.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
        public static VertexElement Tangent(PixelFormat format, int offsetInBytes = AppendAligned)
        {
            return Tangent(0, format, offsetInBytes);
        }

        /// <summary>
        /// Declares a VertexElement with the semantic "TANGENT".
        /// </summary>
        /// <param name="semanticIndex">The semantic index.</param>
        /// <param name="format">Format of this element.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
        public static VertexElement Tangent(int semanticIndex, PixelFormat format, int offsetInBytes = AppendAligned)
        {
            return new VertexElement("TANGENT", semanticIndex, format, offsetInBytes);
        }

        /// <summary>
        /// Declares a VertexElement with the semantic "BITANGENT".
        /// </summary>
        /// <typeparam name="T">Type of the BiTangent semantic.</typeparam>
        /// <param name="semanticIndex">The semantic index.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
        public static VertexElement BiTangent<T>(int semanticIndex = 0, int offsetInBytes = AppendAligned) where T : struct
        {
            return BiTangent(semanticIndex, ConvertTypeToFormat<T>(), offsetInBytes);
        }

        /// <summary>
        /// Declares a VertexElement with the semantic "BITANGENT".
        /// </summary>
        /// <param name="format">Format of this element.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
        public static VertexElement BiTangent(PixelFormat format, int offsetInBytes = AppendAligned)
        {
            return BiTangent(0, format, offsetInBytes);
        }

        /// <summary>
        /// Declares a VertexElement with the semantic "BITANGENT".
        /// </summary>
        /// <param name="semanticIndex">The semantic index.</param>
        /// <param name="format">Format of this element.</param>
        /// <param name="offsetInBytes">The offset in bytes of this element. Use <see cref="AppendAligned"/> to compute automatically the offset from previous elements.</param>
        /// <returns>A new instance of <see cref="VertexElement" /> that represents this semantic.</returns>
        public static VertexElement BiTangent(int semanticIndex, PixelFormat format, int offsetInBytes = AppendAligned)
        {
            return new VertexElement("BITANGENT", semanticIndex, format, offsetInBytes);
        }

        public static PixelFormat ConvertTypeToFormat<T>() where T : struct
        {
            return ConvertTypeToFormat(typeof(T));
        }

        /// <summary>
        /// Converts a type to a <see cref="SharpDX.DXGI.Format"/>.
        /// </summary>
        /// <param name="typeT">The type T.</param>
        /// <returns>The equivalent Format.</returns>
        /// <exception cref="System.NotSupportedException">If the convertion for this type is not supported.</exception>
        private static PixelFormat ConvertTypeToFormat(Type typeT)
        {
            if (typeof(Vector4) == typeT || typeof(Color4) == typeT)
                return PixelFormat.R32G32B32A32_Float;
            if (typeof(Vector3) == typeT || typeof(Color3) == typeT)
                return PixelFormat.R32G32B32_Float;
            if (typeof(Vector2) == typeT)
                return PixelFormat.R32G32_Float;
            if (typeof(float) == typeT)
                return PixelFormat.R32_Float;

            if (typeof(Color) == typeT)
                return PixelFormat.R8G8B8A8_UNorm;
            if (typeof(ColorBGRA) == typeT)
                return PixelFormat.B8G8R8A8_UNorm;

            if (typeof(Half4) == typeT)
                return PixelFormat.R16G16B16A16_Float;
            if (typeof(Half2) == typeT)
                return PixelFormat.R16G16_Float;
            if (typeof(Half) == typeT)
                return PixelFormat.R16_Float;

            if (typeof(Int4) == typeT)
                return PixelFormat.R32G32B32A32_UInt;
            if (typeof(Int3) == typeT)
                return PixelFormat.R32G32B32_UInt;
            if (typeof(int) == typeT)
                return PixelFormat.R32_UInt;
            if (typeof(uint) == typeT)
                return PixelFormat.R32_UInt;

            //if (typeof(Bool4) == typeT)
            //    return PixelFormat.R32G32B32A32_UInt;

            //if (typeof(Bool) == typeT)
            //    return PixelFormat.R32_UInt;

            throw new NotSupportedException(string.Format("Type [{0}] is not supported. You must specify an explicit DXGI.Format", typeT.Name));
        }

        internal class Serializer : DataSerializer<VertexElement>
        {
            public override void Serialize(ref VertexElement obj, ArchiveMode mode, SerializationStream stream)
            {
                if (mode == ArchiveMode.Deserialize)
                {
                    obj.semanticName = stream.ReadString();
                    obj.semanticIndex = stream.ReadInt32();
                    obj.format = stream.Read<PixelFormat>();
                    obj.alignedByteOffset = stream.ReadInt32();
                    obj.ComputeHashCode();
                }
                else
                {
                    stream.Write(obj.semanticName);
                    stream.Write(obj.semanticIndex);
                    stream.Write(obj.format);
                    stream.Write(obj.alignedByteOffset);
                }
            }
        }
    }
}
