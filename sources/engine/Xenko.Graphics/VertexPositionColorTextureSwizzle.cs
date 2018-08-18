// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.InteropServices;

using Xenko.Core.Mathematics;

namespace Xenko.Graphics
{
    /// <summary>
    /// Describes a custom vertex format structure that contains position, color, texture and swizzle information. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VertexPositionColorTextureSwizzle : IEquatable<VertexPositionColorTextureSwizzle>, IVertex
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VertexPositionColorTextureSwizzle"/> struct.
        /// </summary>
        /// <param name="position">The position of this vertex.</param>
        /// <param name="color">The color of this vertex.</param>
        /// <param name="textureCoordinate">UV texture coordinates.</param>
        /// <param name="swizzle">The swizzle mode</param>
        public VertexPositionColorTextureSwizzle(Vector4 position, Color color, Color colorAdd, Vector2 textureCoordinate, SwizzleMode swizzle)
            : this()
        {
            Position = position;
            ColorScale = color;
            ColorAdd = colorAdd;
            TextureCoordinate = textureCoordinate;
            Swizzle = (int)swizzle;
        }

        /// <summary>
        /// XYZ position.
        /// </summary>
        public Vector4 Position;

        /// <summary>
        /// The vertex color.
        /// </summary>
        public Color4 ColorScale;

        /// <summary>
        /// The vertex color.
        /// </summary>
        public Color4 ColorAdd;

        /// <summary>
        /// UV texture coordinates.
        /// </summary>
        public Vector2 TextureCoordinate;

        /// <summary>
        /// The Swizzle mode
        /// </summary>
        public float Swizzle;

        /// <summary>
        /// Defines structure byte size.
        /// </summary>
        public static readonly int Size = 44;

        /// <summary>
        /// The vertex layout of this struct.
        /// </summary>
        public static readonly VertexDeclaration Layout = new VertexDeclaration(
            VertexElement.Position<Vector4>(),
            VertexElement.Color<Color4>(0),
            VertexElement.Color<Color4>(1),
            VertexElement.TextureCoordinate<Vector2>(),
            new VertexElement("BATCH_SWIZZLE", PixelFormat.R32_Float));

        public bool Equals(VertexPositionColorTextureSwizzle other)
        {
            return Position.Equals(other.Position) && ColorScale.Equals(other.ColorScale) && ColorAdd.Equals(other.ColorAdd) && TextureCoordinate.Equals(other.TextureCoordinate);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is VertexPositionColorTextureSwizzle && Equals((VertexPositionColorTextureSwizzle)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Position.GetHashCode();
                hashCode = (hashCode * 397) ^ ColorScale.GetHashCode();
                hashCode = (hashCode * 397) ^ ColorAdd.GetHashCode();
                hashCode = (hashCode * 397) ^ TextureCoordinate.GetHashCode();
                hashCode = (hashCode * 397) ^ Swizzle.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(VertexPositionColorTextureSwizzle left, VertexPositionColorTextureSwizzle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VertexPositionColorTextureSwizzle left, VertexPositionColorTextureSwizzle right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return string.Format("Position: {0}, ColorScale: {1}, ColorAdd: {2}, Texcoord: {3}, Swizzle: {4}", Position, ColorScale, ColorAdd, TextureCoordinate, Swizzle);
        }

        public VertexDeclaration GetLayout()
        {
            return Layout;
        }

        public void FlipWinding()
        {
            TextureCoordinate.X = (1.0f - TextureCoordinate.X);
        }
    }
}
