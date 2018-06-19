// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core;
using Xenko.Core.IO;

namespace Xenko.Core.Assets
{
    /// <summary>
    /// A reference to a local package loaded into the same <see cref="PackageSession"/>.
    /// </summary>
    [DataContract("PackageReference")]
    public sealed class PackageReference : PackageReferenceBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PackageReference"/> class.
        /// </summary>
        public PackageReference()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageReference"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="location">The location.</param>
        public PackageReference(Guid id, UFile location)
        {
            Id = id;
            Location = location;
        }

        /// <summary>
        /// Gets or sets the identifier of the package.
        /// </summary>
        /// <value>The identifier.</value>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the location of the package description file on the disk, relative to the package that is holding
        /// this reference.
        /// </summary>
        /// <value>The location.</value>
        public UFile Location { get; set; }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>PackageReference.</returns>
        public PackageReference Clone()
        {
            return new PackageReference(Id, Location);
        }

        /// <summary>
        /// Tries to parse a package reference in the format {guid:location}.
        /// </summary>
        /// <param name="packageReferenceAsText">The package reference as text.</param>
        /// <param name="packageReference">The package reference.</param>
        /// <returns><c>true</c> if the package reference is a valid reference, <c>false</c> otherwise.</returns>
        public static bool TryParse(string packageReferenceAsText, out PackageReference packageReference)
        {
            AssetId id;
            UFile location;
            packageReference = null;
            if (!AssetReference.TryParse(packageReferenceAsText, out id, out location)) return false;
            packageReference = new PackageReference((Guid)id, location);
            return true;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Package"/> to <see cref="PackageReference"/>.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator PackageReference(Package package)
        {
            return new PackageReference(package.Id, package.FullPath);
        }

        public override string ToString()
        {
            // WARNING: This should not be modified as it is used for serializing
            return string.Format("{0}:{1}", Id, Location);
        }
    }
}
