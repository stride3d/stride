// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;

namespace Stride.Shaders
{
    /// <summary>
    /// A shader source that is linked to a sdfx effect.
    /// </summary>
    [DataContract("ShaderMixinGeneratorSource")]
    public sealed class ShaderMixinGeneratorSource : ShaderSource, IEquatable<ShaderMixinGeneratorSource>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderMixinGeneratorSource"/> class.
        /// </summary>
        public ShaderMixinGeneratorSource()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderMixinGeneratorSource"/> class.
        /// </summary>
        /// <param name="name">The name of the sdfx effect.</param>
        public ShaderMixinGeneratorSource(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets or sets the name of the sdfx effect.
        /// </summary>
        /// <value>The name of the sdfx effect.</value>
        public string Name { get; set; }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
        public bool Equals(ShaderMixinGeneratorSource other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name);
        }

        public override object Clone()
        {
            return new ShaderMixinGeneratorSource(Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is ShaderMixinGeneratorSource && Equals((ShaderMixinGeneratorSource)obj);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }

        public static bool operator ==(ShaderMixinGeneratorSource left, ShaderMixinGeneratorSource right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ShaderMixinGeneratorSource left, ShaderMixinGeneratorSource right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            return string.Format("sdfx {0}", Name);
        }
    }
}
