// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.BepuPhysics.Definitions;

public interface IVertexStructure : IVertex
{
    static abstract VertexDeclaration Declaration();
}

/// <summary>
/// Describes a custom vertex format structure that only contains a Vector3 position.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct VertexPosition3 : IEquatable<VertexPosition3>, IVertexStructure
{
    /// <summary>
    /// Initializes a new <see cref="VertexPositionTexture"/> instance.
    /// </summary>
    /// <param name="position">The position of this vertex.</param>
    public VertexPosition3(Vector3 position)
        : this()
    {
        Position = position;
    }

    /// <summary>
    /// XYZ position.
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// The vertex layout of this struct.
    /// </summary>
    public static readonly VertexDeclaration Layout = new VertexDeclaration(VertexElement.Position<Vector3>());

    public static VertexDeclaration Declaration() => Layout;

    public bool Equals(VertexPosition3 other)
    {
        return Position.Equals(other.Position);
    }

    public override bool Equals(object? obj)
    {
        return obj is VertexPosition3 position3 && Equals(position3);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return Position.GetHashCode();
        }
    }

    public static bool operator ==(VertexPosition3 left, VertexPosition3 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(VertexPosition3 left, VertexPosition3 right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"Position: {Position}";
    }

    public VertexDeclaration GetLayout()
    {
        return Layout;
    }

    public void FlipWinding()
    {
    }
}
