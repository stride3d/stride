// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Particles.VertexLayouts
{
    /// <summary>
    /// Attribute description code used for defining vertex attributes in the <see cref="ParticleVertexBuilder"/>
    /// </summary>
    public sealed class AttributeDescription : IEquatable<AttributeDescription>
    {
        private readonly int hashCode;

        public AttributeDescription(string name)
        {
            Name = name;
            hashCode = name?.GetHashCode() ?? 0;
        }

        public string Name { get; }

        /// <inheritdoc />
        public bool Equals(AttributeDescription other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as AttributeDescription);
        }

        /// <inheritdoc />
        public override int GetHashCode() => hashCode;

        public static bool operator ==(AttributeDescription left, AttributeDescription right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(AttributeDescription left, AttributeDescription right)
        {
            return !Equals(left, right);
        }
    }
}
