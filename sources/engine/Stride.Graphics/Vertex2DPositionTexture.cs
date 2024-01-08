// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.InteropServices;
using Stride.Core.Mathematics;

namespace Stride.Graphics
{
    /// <summary>
    /// Describes a custom vertex format structure that contains position and UV information.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Vertex2DPositionTexture : IEquatable<Vertex2DPositionTexture>, IVertex
    {
        /// <summary>
        /// Initializes a new <see cref="Vertex2DPositionTexture"/> instance.
        /// </summary>
        /// <param name="position">The position of this vertex.</param>
        /// <param name="textureCoordinate">UV texture coordinates.</param>
        public Vertex2DPositionTexture(Vector2 position, Vector2 textureCoordinate)
            : this()
        {
            Position = position;
            TextureCoordinate = textureCoordinate;
        }

        /// <summary>
        /// XY position.
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// UV texture coordinates.
        /// </summary>
        public Vector2 TextureCoordinate;

        /// <summary>
        /// Defines structure byte size.
        /// </summary>
        public static readonly int Size = 16;

        /// <summary>
        /// The vertex layout of this struct.
        /// </summary>
        public static readonly VertexDeclaration Layout = new VertexDeclaration(VertexElement.Position<Vector2>(), VertexElement.TextureCoordinate<Vector2>());

        public bool Equals(Vertex2DPositionTexture other)
        {
            return Position.Equals(other.Position) && TextureCoordinate.Equals(other.TextureCoordinate);
        }

        public override bool Equals(object obj)
        {
            return obj is Vertex2DPositionTexture texture && Equals(texture);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Position.GetHashCode();
                hashCode = (hashCode * 397) ^ TextureCoordinate.GetHashCode();
                return hashCode;
            }
        }

        public VertexDeclaration GetLayout()
        {
            return Layout;
        }

        public void FlipWinding()
        {
            TextureCoordinate.X = (1.0f - TextureCoordinate.X);
        }

        public static bool operator ==(Vertex2DPositionTexture left, Vertex2DPositionTexture right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vertex2DPositionTexture left, Vertex2DPositionTexture right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"Position: {Position}, Texcoord: {TextureCoordinate}";
        }
    }
}
