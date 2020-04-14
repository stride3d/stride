// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_GRAPHICS_API_OPENGL
using System;
#if XENKO_GRAPHICS_API_OPENGLES
using OpenTK.Graphics.ES30;
#else
using OpenTK.Graphics.OpenGL;
#endif

namespace Xenko.Graphics
{
    internal struct VertexAttrib : IEquatable<VertexAttrib>
    {
        public readonly int VertexBufferSlot;
        public readonly int AttributeIndex;
        public readonly int Size;
        public readonly bool IsInteger;
        public readonly VertexAttribPointerType Type;
        public readonly bool Normalized;
        public readonly int Offset;

        public VertexAttrib(int vertexBufferSlot, int attributeIndex, int size, VertexAttribPointerType type, bool normalized, int offset)
        {
            VertexBufferSlot = vertexBufferSlot;
            AttributeIndex = attributeIndex;
            Size = size;
            IsInteger = IsIntegerHelper(type);
            Type = type;
            Normalized = normalized;
            Offset = offset;
        }

        public bool Equals(VertexAttrib other)
        {
            return VertexBufferSlot == other.VertexBufferSlot && AttributeIndex == other.AttributeIndex && Size == other.Size && IsInteger.Equals(other.IsInteger) && Type == other.Type && Normalized.Equals(other.Normalized) && Offset.Equals(other.Offset);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is VertexAttrib && Equals((VertexAttrib) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = VertexBufferSlot;
                hashCode = (hashCode * 397) ^ AttributeIndex;
                hashCode = (hashCode * 397) ^ Size;
                hashCode = (hashCode * 397) ^ IsInteger.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) Type;
                hashCode = (hashCode * 397) ^ Normalized.GetHashCode();
                hashCode = (hashCode * 397) ^ Offset;
                return hashCode;
            }
        }

        public static bool operator ==(VertexAttrib left, VertexAttrib right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VertexAttrib left, VertexAttrib right)
        {
            return !left.Equals(right);
        }

        private static bool IsIntegerHelper(VertexAttribPointerType type)
        {
            switch (type)
            {
                case VertexAttribPointerType.Byte:
                case VertexAttribPointerType.UnsignedByte:
                case VertexAttribPointerType.Short:
                case VertexAttribPointerType.UnsignedShort:
                case VertexAttribPointerType.Int:
                case VertexAttribPointerType.UnsignedInt:
                    return true;
                default:
                    return false;
            }
        }

        internal struct ElementFormat
        {
            public readonly VertexAttribPointerType Type;
            public readonly byte Size;
            public readonly bool Normalized;

            public ElementFormat(VertexAttribPointerType type, byte size, bool normalized = false)
            {
                Type = type;
                Size = size;
                Normalized = normalized;
            }
        }

        internal static ElementFormat ConvertVertexElementFormat(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.R8_SInt:
                    return new ElementFormat(VertexAttribPointerType.Byte, 1);
                case PixelFormat.R8_UInt:
                    return new ElementFormat(VertexAttribPointerType.UnsignedByte, 1);
                case PixelFormat.R16_SInt:
                    return new ElementFormat(VertexAttribPointerType.Short, 1);
                case PixelFormat.R16_UInt:
                    return new ElementFormat(VertexAttribPointerType.UnsignedShort, 1);
                case PixelFormat.R32_SInt:
                    return new ElementFormat(VertexAttribPointerType.Int, 4);
                case PixelFormat.R32_UInt:
                    return new ElementFormat(VertexAttribPointerType.UnsignedInt, 4);
                case PixelFormat.R8G8_SInt:
                    return new ElementFormat(VertexAttribPointerType.Byte, 2);
                case PixelFormat.R8G8_UInt:
                    return new ElementFormat(VertexAttribPointerType.UnsignedByte, 2);
                case PixelFormat.R16G16_SInt:
                    return new ElementFormat(VertexAttribPointerType.Short, 2);
                case PixelFormat.R16G16_UInt:
                    return new ElementFormat(VertexAttribPointerType.UnsignedShort, 2);
                case PixelFormat.R8G8B8A8_SInt:
                    return new ElementFormat(VertexAttribPointerType.Byte, 4);
                case PixelFormat.R8G8B8A8_UInt:
                    return new ElementFormat(VertexAttribPointerType.UnsignedByte, 4);
                case PixelFormat.R16G16B16A16_SInt:
                    return new ElementFormat(VertexAttribPointerType.Short, 4);
                case PixelFormat.R16G16B16A16_UInt:
                    return new ElementFormat(VertexAttribPointerType.UnsignedShort, 4);
                case PixelFormat.R32_Float:
                    return new ElementFormat(VertexAttribPointerType.Float, 1);
                case PixelFormat.R32G32_Float:
                    return new ElementFormat(VertexAttribPointerType.Float, 2);
                case PixelFormat.R32G32B32_Float:
                    return new ElementFormat(VertexAttribPointerType.Float, 3);
                case PixelFormat.R32G32B32A32_Float:
                    return new ElementFormat(VertexAttribPointerType.Float, 4);
                case PixelFormat.R8_UNorm:
                    return new ElementFormat(VertexAttribPointerType.UnsignedByte, 1, true);
                case PixelFormat.R8G8_UNorm:
                    return new ElementFormat(VertexAttribPointerType.UnsignedByte, 2, true);
                case PixelFormat.R8G8B8A8_UNorm:
                    return new ElementFormat(VertexAttribPointerType.UnsignedByte, 4, true);
                case PixelFormat.R8_SNorm:
                    return new ElementFormat(VertexAttribPointerType.Byte, 1, true);
                case PixelFormat.R8G8_SNorm:
                    return new ElementFormat(VertexAttribPointerType.Byte, 2, true);
                case PixelFormat.R8G8B8A8_SNorm:
                    return new ElementFormat(VertexAttribPointerType.Byte, 4, true);
                case PixelFormat.R16_UNorm:
                    return new ElementFormat(VertexAttribPointerType.UnsignedShort, 1, true);
                case PixelFormat.R16G16_UNorm:
                    return new ElementFormat(VertexAttribPointerType.UnsignedShort, 2, true);
                case PixelFormat.R16G16B16A16_UNorm:
                    return new ElementFormat(VertexAttribPointerType.UnsignedShort, 4, true);
                case PixelFormat.R16_SNorm:
                    return new ElementFormat(VertexAttribPointerType.Short, 1, true);
                case PixelFormat.R16G16_SNorm:
                    return new ElementFormat(VertexAttribPointerType.Short, 2, true);
                case PixelFormat.R16G16B16A16_SNorm:
                    return new ElementFormat(VertexAttribPointerType.Short, 4, true);
#if XENKO_GRAPHICS_API_OPENGLES
                // HALF_FLOAT for OpenGL ES 2.x (OES extension)
                case PixelFormat.R16G16B16A16_Float:
                    return new ElementFormat((VertexAttribPointerType)0x8D61, 4); // HALF_FLOAT_OES
                case PixelFormat.R16G16_Float:
                    return new ElementFormat((VertexAttribPointerType)0x8D61, 2); // HALF_FLOAT_OES
#else
                // HALF_FLOAT for OpenGL and OpenGL ES 3.x (also used for OpenGL ES 2.0 under 3.0 emulator)
                case PixelFormat.R16G16B16A16_Float:
                    return new ElementFormat((VertexAttribPointerType)0x8D61, 4); // HALF_FLOAT
                case PixelFormat.R16G16_Float:
                    return new ElementFormat((VertexAttribPointerType)0x8D61, 2); // HALF_FLOAT
#endif
                default:
                    throw new NotSupportedException();
            }
        }
    }
}

#endif
