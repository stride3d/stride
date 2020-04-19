// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Annotations;

namespace Stride.Core.Reflection
{
    /// <summary>
    /// A key used to attach/retrieve property values from a <see cref="ShadowObject"/>
    /// </summary>
    /// <remarks>
    /// This key allow to associate two pseudo-keys together.
    /// </remarks>
    public struct ShadowObjectPropertyKey : IEquatable<ShadowObjectPropertyKey>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ShadowObjectPropertyKey"/>
        /// </summary>
        /// <param name="item1">The first part of this key. Cannot be null</param>
        /// <param name="copyValueOnClone">Indicate whether this shadow object property should be copied when the host object is cloned.</param>
        public ShadowObjectPropertyKey([NotNull] object item1, bool copyValueOnClone) : this()
        {
            if (item1 == null) throw new ArgumentNullException(nameof(item1));
            Item1 = item1;
            CopyValueOnClone = copyValueOnClone;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ShadowObjectPropertyKey"/>
        /// </summary>
        /// <param name="item1">The first part of this key. Cannot be null</param>
        /// <param name="item2">The second part of this key. Can be null</param>
        /// <param name="copyValueOnClone">Indicate whether this shadow object property should be copied when the host object is cloned.</param>
        public ShadowObjectPropertyKey([NotNull] object item1, object item2, bool copyValueOnClone)
        {
            if (item1 == null) throw new ArgumentNullException(nameof(item1));
            Item1 = item1;
            Item2 = item2;
            CopyValueOnClone = copyValueOnClone;
        }

        /// <summary>
        /// First part of this key.
        /// </summary>
        public readonly object Item1;

        /// <summary>
        /// Second part of this key.
        /// </summary>
        public readonly object Item2;

        /// <summary>
        /// Indicate whether this shadow object property should be copied when the host object is cloned.
        /// </summary>
        public readonly bool CopyValueOnClone;

        public bool Equals(ShadowObjectPropertyKey other)
        {
            return Equals(Item1, other.Item1) && Equals(Item2, other.Item2);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ShadowObjectPropertyKey && Equals((ShadowObjectPropertyKey)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Item1?.GetHashCode() ?? 0) * 397) ^ (Item2?.GetHashCode() ?? 0);
            }
        }

        public static bool operator ==(ShadowObjectPropertyKey left, ShadowObjectPropertyKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ShadowObjectPropertyKey left, ShadowObjectPropertyKey right)
        {
            return !left.Equals(right);
        }
    }
}
