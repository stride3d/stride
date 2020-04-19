// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.InteropServices;

using Stride.Core.Mathematics;

namespace Stride.Graphics
{
    /// <summary>
    /// Describes a custom vertex format structure that contains position as a Vector2. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VertexPosition2 : IEquatable<VertexPosition2>, IVertex
    {
        /// <summary>
        /// Initializes a new <see cref="VertexPositionTexture"/> instance.
        /// </summary>
        /// <param name="position">The position of this vertex.</param>
        public VertexPosition2(Vector2 position)
            : this()
        {
            Position = position;
        }

        /// <summary>
        /// XY position.
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// Defines structure byte size.
        /// </summary>
        public static readonly int Size = 8;

        /// <summary>
        /// The vertex layout of this struct.
        /// </summary>
        public static readonly VertexDeclaration Layout = new VertexDeclaration(VertexElement.Position<Vector2>());

        public bool Equals(VertexPosition2 other)
        {
            return Position.Equals(other.Position);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is VertexPosition2 && Equals((VertexPosition2)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Position.GetHashCode();
            }
        }

        public static bool operator ==(VertexPosition2 left, VertexPosition2 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VertexPosition2 left, VertexPosition2 right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return string.Format("Position: {0}", Position);
        }

        public VertexDeclaration GetLayout()
        {
            return Layout;
        }

        public void FlipWinding()
        {
        }
    }
}
