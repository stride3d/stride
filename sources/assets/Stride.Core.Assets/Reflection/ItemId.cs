// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Storage;

namespace Stride.Core.Reflection
{
    /// <summary>
    /// Represents the identifier of an item in a collection or an entry in a dictionary.
    /// </summary>
    [DataContract]
    public struct ItemId : IComparable<ItemId>, IEquatable<ItemId>
    {
        private readonly ObjectId value;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemId"/> structure from an array of bytes.
        /// </summary>
        /// <param name="bytes">The array of bytes from which to create this <see cref="ItemId"/>.</param>
        public ItemId(byte[] bytes)
        {
            value = new ObjectId(bytes);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemId"/> structure from an <see cref="ObjectId"/>.
        /// </summary>
        /// <param name="id">The <see cref="ObjectId"/> from which to create this <see cref="ItemId"/>.</param>
        public ItemId(ObjectId id)
        {
            value = id;
        }

        /// <summary>
        /// Gets an <see cref="ItemId"/> representing an empty or non-existing item.
        /// </summary>
        public static ItemId Empty { get; } = new ItemId(ObjectId.Empty);

        /// <summary>
        /// Generates a new random <see cref="ItemId"/>.
        /// </summary>
        /// <returns></returns>
        public static ItemId New()
        {
            return new ItemId(ObjectId.New());
        }

        /// <summary>
        /// Parses an <see cref="ItemId"/> from a string.
        /// </summary>
        /// <param name="input">The input string to parse.</param>
        /// <returns>An <see cref="ItemId"/> corresponding to the parsed string.</returns>
        /// <exception cref="FormatException">The given string cannot be parsed as an <see cref="ItemId"/>.</exception>
        public static ItemId Parse(string input)
        {
            ItemId itemId;
            if (!TryParse(input, out itemId))
                throw new FormatException("Unable to parse the input string.");

            return itemId;
        }

        /// <summary>
        /// Attempts to parse an <see cref="ItemId"/> from a string.
        /// </summary>
        /// <param name="input">The input string to parse.</param>
        /// <param name="itemId">The resulting <see cref="ItemId"/>.</param>
        /// <returns>True if the string could be successfully parsed, False otherwise.</returns>
        public static bool TryParse(string input, out ItemId itemId)
        {
            ObjectId objectId;
            if (ObjectId.TryParse(input, out objectId))
            {
                itemId = new ItemId(objectId);
                return true;
            }
            itemId = Empty;
            return false;
        }

        /// <inheritdoc/>
        public bool Equals(ItemId other)
        {
            return value.Equals(other.value);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is ItemId && Equals((ItemId)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        /// <inheritdoc/>
        public static bool operator ==(ItemId left, ItemId right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(ItemId left, ItemId right)
        {
            return !left.Equals(right);
        }

        /// <inheritdoc/>
        public int CompareTo(ItemId other)
        {
            return value.CompareTo(other.value);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return value.ToString();
        }
    }
}
