// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core;

namespace Stride.Shaders
{
    [DataContract]
    public sealed class ShaderSourceCollection : List<ShaderSource>
    {
        public ShaderSourceCollection()
        {
        }

        public ShaderSourceCollection(IEnumerable<ShaderSource> collection) : base(collection)
        {
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 0;
                foreach (var current in this)
                    hashCode = (hashCode * 397) ^ (current?.GetHashCode() ?? 0);
                return hashCode;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ShaderSourceCollection)obj);
        }

        public bool Equals(ShaderSourceCollection other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            if (Count != other.Count)
                return false;

            for (int i = 0; i < Count; ++i)
            {
                if (!this[i].Equals(other[i]))
                    return false;
            }

            return true;
        }
    }
}
