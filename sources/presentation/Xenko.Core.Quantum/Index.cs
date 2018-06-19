// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Annotations;

namespace Xenko.Core.Quantum
{
    /// <summary>
    /// A container structure to represent indices in Quantum nodes.
    /// </summary>
    public struct Index : IEquatable<Index>, IComparable<Index>, IComparable
    {
        /// <summary>
        /// An index that is null.
        /// </summary>
        public static readonly Index Empty = new Index();

        /// <summary>
        /// The value of the index.
        /// </summary>
        public readonly object Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="Index"/> structure.
        /// </summary>
        /// <param name="value">The value of the index.</param>
        public Index(object value) : this()
        {
            // Sanity check, to avoid boxing index into index
            if (value is Index) throw new ArgumentException($"A {nameof(Index)} instance cannot be passed as the value of another {nameof(Index)} instance.");
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
        public bool Equals(Index other)
        {
            return Equals(Value, other.Value);
        }

        /// <inheritdoc/>
        public int CompareTo(Index other)
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
            return obj is Index && Equals((Index)obj);
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

            var other = (Index)obj;
            return CompareTo(other);
        }

        public static bool operator==(Index a, Index b)
        {
            return Equals(a.Value, b.Value);
        }

        /// <inheritdoc/>
        public static bool operator!=(Index a, Index b)
        {
            return !Equals(a.Value, b.Value);
        }
    }
}
