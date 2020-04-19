// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.InteropServices;
using Stride.Core.Mathematics;

namespace Stride.Graphics
{
    /// <summary>
    /// Describes a custom vertex format structure that contains position and color information. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VertexPositionColorTexture : IEquatable<VertexPositionColorTexture>, IVertex
    {
        /// <summary>
        /// Initializes a new <see cref="VertexPositionColorTexture"/> instance.
        /// </summary>
        /// <param name="position">The position of this vertex.</param>
        /// <param name="color">The color of this vertex.</param>
        /// <param name="textureCoordinate">UV texture coordinates.</param>
        public VertexPositionColorTexture(Vector3 position, Color color, Vector2 textureCoordinate)
            : this()
        {
            Position = position;
            Color = color;
            TextureCoordinate = textureCoordinate;
        }

        /// <summary>
        /// XYZ position.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// The vertex color.
        /// </summary>
        public Color Color;

        /// <summary>
        /// UV texture coordinates.
        /// </summary>
        public Vector2 TextureCoordinate;

        /// <summary>
        /// Defines structure byte size.
        /// </summary>
        public static readonly int Size = 24;

        /// <summary>
        /// The vertex layout of this struct.
        /// </summary>
        public static readonly VertexDeclaration Layout = new VertexDeclaration(
            VertexElement.Position<Vector3>(),
            VertexElement.Color<Color>(),
            VertexElement.TextureCoordinate<Vector2>());

        public bool Equals(VertexPositionColorTexture other)
        {
            return Position.Equals(other.Position) && Color.Equals(other.Color) && TextureCoordinate.Equals(other.TextureCoordinate);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is VertexPositionColorTexture && Equals((VertexPositionColorTexture)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Position.GetHashCode();
                hashCode = (hashCode * 397) ^ Color.GetHashCode();
                hashCode = (hashCode * 397) ^ TextureCoordinate.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(VertexPositionColorTexture left, VertexPositionColorTexture right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VertexPositionColorTexture left, VertexPositionColorTexture right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return string.Format("Position: {0}, Color: {1}, Texcoord: {2}", Position, Color, TextureCoordinate);
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
