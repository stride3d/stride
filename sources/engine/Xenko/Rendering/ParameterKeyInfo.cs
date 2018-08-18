// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;

namespace Xenko.Rendering
{
    [DataContract]
    public struct ParameterKeyInfo
    {
        // Common
        public ParameterKey Key;

        // Values
        public int Offset;
        public int Count;

        // Resources
        public int BindingSlot;

        /// <summary>
        /// Describes a value parameter.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public ParameterKeyInfo(ParameterKey key, int offset, int count)
        {
            Key = key;
            Offset = offset;
            Count = count;
            BindingSlot = -1;
        }

        /// <summary>
        /// Describes a resource parameter.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="bindingSlot"></param>
        public ParameterKeyInfo(ParameterKey key, int bindingSlot)
        {
            Key = key;
            BindingSlot = bindingSlot;
            Offset = -1;
            Count = 1;
        }

        public override string ToString()
        {
            return $"{Key} ({(BindingSlot != -1 ? "BindingSlot " + BindingSlot : "Offset " + Offset)}, Size {Count})";
        }

        public bool Equals(ParameterKeyInfo other)
        {
            return Key.Equals(other.Key) && Offset == other.Offset && Count == other.Count && BindingSlot == other.BindingSlot;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ParameterKeyInfo && Equals((ParameterKeyInfo)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Key.GetHashCode();
                hashCode = (hashCode * 397) ^ Offset;
                hashCode = (hashCode * 397) ^ Count;
                hashCode = (hashCode * 397) ^ BindingSlot;
                return hashCode;
            }
        }

        public static bool operator ==(ParameterKeyInfo left, ParameterKeyInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ParameterKeyInfo left, ParameterKeyInfo right)
        {
            return !left.Equals(right);
        }

        internal ParameterCollection.Accessor GetObjectAccessor()
        {
            return new ParameterCollection.Accessor(BindingSlot, Count);
        }

        internal ParameterCollection.Accessor GetValueAccessor()
        {
            return new ParameterCollection.Accessor(Offset, Count);
        }
    }
}
