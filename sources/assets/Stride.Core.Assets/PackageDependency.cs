// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;

namespace Stride.Core.Assets
{
    /// <summary>
    /// A collection of <see cref="PackageDependency"/>.
    /// </summary>
    [DataContract("PackageDependencyCollection")]
    public sealed class PackageDependencyCollection : KeyedCollection<string, PackageDependency>
    {
        protected override string GetKeyForItem(PackageDependency item)
        {
            return item.Name;
        }
    }

    /// <summary>
    /// A reference to a package either internal (directly to a <see cref="Package"/> inside the same solution) or external
    /// (to a package distributed on the store).
    /// </summary>
    [DataContract("PackageDependency")]
    public sealed class PackageDependency : PackageReferenceBase, IEquatable<PackageDependency>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PackageDependency"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is used only for serialization.
        /// </remarks>
        public PackageDependency()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageDependency"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="version">The version.</param>
        public PackageDependency(string name, PackageVersionRange version)
        {
            Name = name;
            Version = version;
        }

        /// <summary>
        /// Gets or sets the package name Id.
        /// </summary>
        /// <value>The name.</value>
        /// <remarks>
        /// The setter should only be used during serialization.
        /// </remarks>
        [DefaultValue(null)]
        [DataMember(10)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        [DefaultValue(null)]
        [DataMember(20)]
        public PackageVersionRange Version { get; set; }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>PackageDependency.</returns>
        [NotNull]
        public PackageDependency Clone()
        {
            return new PackageDependency(Name, Version);
        }

        public bool Equals(PackageDependency other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name) && Equals(Version, other.Version);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as PackageDependency);
        }

        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode - this property is not supposed to be set except by serialization
            return Name?.GetHashCode() ?? 0;
        }

        public static bool operator ==(PackageDependency left, PackageDependency right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PackageDependency left, PackageDependency right)
        {
            return !Equals(left, right);
        }

        /// <inherit/>
        public override string ToString()
        {
            return Name != null ? $"{Name} {Version}" : "Empty";
        }
    }
}
