// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Shaders.Parser
{
    /// <summary>
    /// Macro to be used with <see cref="PreProcessor"/>.
    /// </summary>
    public struct ShaderMacro : IEquatable<ShaderMacro>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderMacro"/> struct.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="definition">The definition.</param>
        public ShaderMacro(string name, object definition)
        {
            if (name == null) throw new ArgumentNullException("name");

            Name = name;
            Definition = definition == null ? string.Empty : (definition is bool ? definition.ToString().ToLowerInvariant() : definition.ToString());
        }

        /// <summary>
        /// Name of the macro to set.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Value of the macro to set.
        /// </summary>
        public readonly string Definition;

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        public bool Equals(ShaderMacro other)
        {
            return Equals(other.Name, Name) && Equals(other.Definition, Definition);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != typeof (ShaderMacro)) return false;
            return Equals((ShaderMacro)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (Name.GetHashCode()* 397) ^ (Definition != null ? Definition.GetHashCode() : 0);
            }
        }

        public override string ToString()
        {
            return string.Format("{0}={1}", Name, Definition);
        }
    }
}
