// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Annotations;

namespace Stride.Core.Quantum
{
    /// <summary>
    /// A container structure to represent indices in Quantum nodes.
    /// </summary>
    public struct NodeIndex : IEquatable<NodeIndex>, IComparable<NodeIndex>, IComparable
    {
        /// <summary>
        /// An index that is null.
        /// </summary>
        public static readonly NodeIndex Empty = new NodeIndex();

        /// <summary>
        /// The value of the index.
        /// </summary>
        public readonly object Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeIndex"/> structure.
        /// </summary>
        /// <param name="value">The value of the index.</param>
        public NodeIndex(object value) : this()
        {
            // Sanity check, to avoid boxing index into index
            if (value is NodeIndex) throw new ArgumentException($"A {nameof(NodeIndex)} instance cannot be passed as the value of another {nameof(NodeIndex)} instance.");
            Value = value;
        }

        /// <summary>
        /// Gets whether this index is empty.
        /// </summary>
        public bool IsEmpty => Value == null;

        /// <summary>
        /// Gets whether this index is an integer.
        /// </summary>
        public bool IsInt => Value is int;

        /// <summary>
        /// Gets the integer value of this index.
        /// </summary>
        /// <exception cref="InvalidCastException">The value of this index is not an integer.</exception>
        public int Int => (int)Value;

        /// <inheritdoc/>
        public override string ToString()
        {
            return Value?.ToString() ?? "(null)";
        }

        /// <inheritdoc/>
        public bool Equals(NodeIndex other)
        {
            return Equals(Value, other.Value);
        }

        /// <inheritdoc/>
        public int CompareTo(NodeIndex other)
        {
            if (other.IsEmpty)
                return IsEmpty ? 0 : 1;

            var thisValue = Value as IComparable;
            var otherValue = other.Value as IComparable;
            if (thisValue == null)
                return otherValue != null ? -1 : 0;

            return thisValue.CompareTo(other.Value);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is NodeIndex && Equals((NodeIndex)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Value?.GetHashCode() ?? 0;
        }

        int IComparable.CompareTo([NotNull] object obj)
        {
            if (obj == null || obj.GetType() != GetType())
                throw new ArgumentException(@"obj is not the same type as this instance.", nameof(obj));

            var other = (NodeIndex)obj;
            return CompareTo(other);
        }

        public static bool operator==(NodeIndex a, NodeIndex b)
        {
            return Equals(a.Value, b.Value);
        }

        /// <inheritdoc/>
        public static bool operator!=(NodeIndex a, NodeIndex b)
        {
            return !Equals(a.Value, b.Value);
        }
    }
}
