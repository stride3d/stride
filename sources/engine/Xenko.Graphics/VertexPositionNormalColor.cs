// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;

using Xenko.Core.Mathematics;

namespace Xenko.Graphics
{
    /// <summary>
    /// Describes a custom vertex format structure that contains position, normal and color information. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VertexPositionNormalColor : IEquatable<VertexPositionNormalColor>, IVertex
    {
        /// <summary>
        /// Initializes a new <see cref="VertexPositionNormalColor"/> instance.
        /// </summary>
        /// <param name="position">The position of this vertex.</param>
        /// <param name="normal">The vertex normal.</param>
        /// <param name="color">the color</param>
        public VertexPositionNormalColor(Vector3 position, Vector3 normal, Color color)
            : this()
        {
            Position = position;
            Normal = normal;
            Color = color;
        }

        /// <summary>
        /// XYZ position.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// The vertex normal.
        /// </summary>
        public Vector3 Normal;

        /// <summary>
        /// The color.
        /// </summary>
        public Color Color;

        /// <summary>
        /// Defines structure byte size.
        /// </summary>
        public static readonly int Size = 28;

        /// <summary>
        /// The vertex layout of this structure.
        /// </summary>
        public static readonly VertexDeclaration Layout = new VertexDeclaration(
            VertexElement.Position<Vector3>(),
            VertexElement.Normal<Vector3>(),
            VertexElement.Color<Color>());

        public bool Equals(VertexPositionNormalColor other)
        {
            return Position.Equals(other.Position) && Normal.Equals(other.Normal) && Color.Equals(other.Color);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is VertexPositionNormalColor && Equals((VertexPositionNormalColor)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Position.GetHashCode();
                hashCode = (hashCode * 397) ^ Normal.GetHashCode();
                hashCode = (hashCode * 397) ^ Color.GetHashCode();
                return hashCode;
            }
        }

        public VertexDeclaration GetLayout()
        {
            return Layout;
        }

        public void FlipWinding()
        {
        }

        public static bool operator ==(VertexPositionNormalColor left, VertexPositionNormalColor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VertexPositionNormalColor left, VertexPositionNormalColor right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return string.Format("Position: {0}, Normal: {1}, Color: {2}", Position, Normal, Color);
        }
    }
}
