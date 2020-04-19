// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using NuGet;
using Stride.Core;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Stride.Core.Annotations;
using Constants = NuGet.ProjectManagement.Constants;
using IPackageMetadata = NuGet.Packaging.IPackageMetadata;

namespace Stride.Core.Packages
{
    /// <summary>
    /// Nuget abstraction of a package.
    /// </summary>
    public abstract class NugetPackage : IEquatable<NugetPackage>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="NugetPackage"/> using some NuGet data.
        /// </summary>
        /// <param name="package">The NuGet metadata we will use to construct the current instance.</param>
        internal NugetPackage([NotNull] IPackageSearchMetadata package)
        {
            packageMetadata = package ?? throw new ArgumentNullException(nameof(package));
        }

        /// <summary>
        /// Storage for the NuGet metatadata.
        /// </summary>
        private readonly IPackageSearchMetadata packageMetadata;

        /// <inheritdoc />
        public bool Equals(NugetPackage other)
        {
            return packageMetadata.Identity.Equals(other.packageMetadata.Identity);
        }

        /// <inheritdoc />
        public override bool Equals(object other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.GetType() != GetType()) return false;
            return Equals((NugetPackage)other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return packageMetadata.GetHashCode();
        }

        /// <summary>
        /// Determines whether two specified <see cref="NugetPackage"/> objects are equal.
        /// </summary>
        /// <param name="left">The first <see cref="NugetPackage"/>object.</param>
        /// <param name="right">The second <see cref="NugetPackage"/>object.</param>
        /// <returns><c>true</c> if <paramref name="left"/> is equal to <paramref name="right"/>, <c>false</c> otherwise.</returns>
        public static bool operator ==(NugetPackage left, NugetPackage right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two specified <see cref="NugetPackage"/> objects are not equal.
        /// </summary>
        /// <param name="left">The first <see cref="NugetPackage"/>object.</param>
        /// <param name="right">The second <see cref="NugetPackage"/>object.</param>
        /// <returns><c>true</c> if <paramref name="left"/> is not equal to <paramref name="right"/>, <c>false</c> otherwise.</returns>
        public static bool operator !=(NugetPackage left, NugetPackage right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Version of current package.
        /// </summary>
        public PackageVersion Version => packageMetadata.Identity.Version.ToPackageVersion();

        /// <summary>
        /// The <see cref="NuGetVersion"/> of this package's version.
        /// </summary>
        /// <remarks>Internal since it exposes a NuGet type.</remarks>
        internal NuGetVersion NuGetVersion => packageMetadata.Identity.Version;

        /// <summary>
        /// The <see cref="PackageIdentity"/> of this package.
        /// </summary>
        /// <remarks>Internal since it exposes a NuGet type.</remarks>
        internal PackageIdentity Identity => packageMetadata.Identity;

        /// <summary>
        /// The Id of this package.
        /// </summary>
        public string Id => packageMetadata.Identity.Id;

        /// <summary>
        /// The listed status of this package.
        /// </summary>
        public bool Listed => !Published.HasValue || Published > Constants.Unpublished;

        /// <summary>
        /// The date of publication if present.
        /// </summary>
        public DateTimeOffset? Published => packageMetadata.Published;

        /// <summary>
        /// The title of this package.
        /// </summary>
        public string Title => packageMetadata.Title;

        /// <summary>
        /// The list of authors of this package.
        /// </summary>
        public IEnumerable<string> Authors => new List<string>(1) { packageMetadata.Authors };

        /// <summary>
        /// The list of owners of this package.
        /// </summary>
        public IEnumerable<string> Owners => new List<string>(1) { packageMetadata.Owners };

        /// <summary>
        /// The URL of this package's icon.
        /// </summary>
        public Uri IconUrl => packageMetadata.IconUrl;

        /// <summary>
        /// The URL of this package's license.
        /// </summary>
        public Uri LicenseUrl => packageMetadata.LicenseUrl;

        /// <summary>
        /// The URL of this package's project.
        /// </summary>
        public Uri ProjectUrl => packageMetadata.ProjectUrl;

        /// <summary>
        /// Determines if this package requires a license acceptance.
        /// </summary>
        public bool RequireLicenseAcceptance => packageMetadata.RequireLicenseAcceptance;

        /// <summary>
        /// The description of this package.
        /// </summary>
        public string Description => packageMetadata.Description;

        /// <summary>
        /// The summary description of this package.
        /// </summary>
        public string Summary => packageMetadata.Summary;

        /// <summary>
        /// The list of tags of this package separated by spaces.
        /// </summary>
        public string Tags => packageMetadata.Tags;

        /// <summary>
        /// The list of dependencies of this package.
        /// </summary>
        /// <remarks>Internal since it exposes a NuGet type.</remarks>
        internal IEnumerable<NuGet.Packaging.PackageDependencyGroup> DependencySets => packageMetadata.DependencySets;

        /// <summary>
        /// The number of downloads for this package. It is specific to the version of this package.
        /// </summary>
        public long DownloadCount => VersionInfo.DownloadCount ?? 0;

        /// <summary>
        /// The URL to report abused on this package.
        /// </summary>
        public Uri ReportAbuseUrl => packageMetadata.ReportAbuseUrl;

        /// <summary>
        /// The number of dependency sets.
        /// </summary>
        public int DependencySetsCount => DependencySets?.Count() ?? 0;

        /// <summary>
        /// Computed the list of dependencies of this package.
        /// </summary>
        public IEnumerable<Tuple<string, PackageVersionRange>>  Dependencies
        {
            get
            {
                var res = new List<Tuple<string, PackageVersionRange>>();
                var set = DependencySets.FirstOrDefault();
                if (set != null)
                {
                    foreach (var dependency in set.Packages)
                    {
                        res.Add(new Tuple<string, PackageVersionRange>(dependency.Id, dependency.VersionRange.ToPackageVersionRange()));
                    }
                }
                return res;
            }
        }

        /// <summary>
        /// The <see cref="VersionInfo"/> associated with this package.
        /// </summary>
        private VersionInfo VersionInfo
        {
            get
            {
                if (versionInfo == null)
                {
                    // Get all versions of the current package and filter on the current package's version.
                    versionInfo = packageMetadata.GetVersionsAsync().Result.First(v=>v.Version.Equals(Version.ToNuGetVersion()));
                }
                return versionInfo;
            }
        }

        private VersionInfo versionInfo;

    }
}
