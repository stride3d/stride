// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands
{
    /// <summary>
    /// Define a value that can be set by <see cref="CreateNewInstanceCommand"/>.
    /// </summary>
    public abstract class AbstractNodeEntry : IEquatable<AbstractNodeEntry>
    {
        /// <summary>
        /// An arbitrary integer value representing the order of this entry.
        /// Entries are sorted by <see cref="Order"/> first, then by <see cref="DisplayValue"/>.
        /// </summary>
        public abstract int Order { get; }

        /// <summary>
        /// The display value, as a string.
        /// </summary>
        public abstract string DisplayValue { get; }

        /// <summary>
        /// Gets or creates a new value, used by <see cref="CreateNewInstanceCommand"/>.
        /// </summary>
        /// <param name="currentValue">The current value (might be kept if type didn't change).</param>
        /// <returns></returns>
        public abstract object GenerateValue(object currentValue);

        /// <summary>
        /// Returns true if value is matching the current entry.
        /// </summary>
        /// <param name="value">The value to check against.</param>
        /// <returns>True if it matches, otherwise false.</returns>
        public abstract bool IsMatchingValue(object value);

        public abstract bool Equals(AbstractNodeEntry other);

        protected abstract int ComputeHashCode();

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((AbstractNodeEntry)obj);
        }

        public override int GetHashCode()
        {
            return ComputeHashCode();
        }

        public static bool operator ==(AbstractNodeEntry left, AbstractNodeEntry right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(AbstractNodeEntry left, AbstractNodeEntry right)
        {
            return !Equals(left, right);
        }
    }
}
